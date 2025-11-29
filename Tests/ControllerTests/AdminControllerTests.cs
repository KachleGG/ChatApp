using Chatter.Controllers;
using Chatter.Data;
using Chatter.Helpers;
using Chatter.Models;
using Chatter.Models.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using System.Text.Json.Nodes;

namespace Tests.ControllerTests;

[TestClass]
public class AdminControllerTests
{
    private ChatterDbContext _db;
    private IConfiguration _config;
    private TestEnvironment _env;
    private AdminController _controller;
    private DefaultHttpContext _httpContext;

    [TestInitialize]
    public void Init()
    {
        var options = new DbContextOptionsBuilder<ChatterDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new ChatterDbContext(options);

        // Real configuration builder
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ServerSettings:PrivateMode", "true" },
                { "ServerSettings:ProhibitGroups", "false" },
                { "ServerSettings:ProhibitGeneral", "true" },
                { "Kestrel:Endpoints:Http:Url", "http://localhost:5000" },
                { "Kestrel:Endpoints:Https:Url", "https://localhost:5001" }
            })
            .Build();

        // Stub environment
        _env = new TestEnvironment
        {
            ContentRootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
            IsDev = true
        };
        Directory.CreateDirectory(_env.ContentRootPath);

        // HTTP + Session
        _httpContext = new DefaultHttpContext();
        _httpContext.Session = new TestSession();

        _controller = new AdminController(_db, _config, _env)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            }
        };
    }

    // -------------------------
    // GET /config tests
    // -------------------------

    [TestMethod]
    public void GetConfig_Forbidden_WhenNoSession()
    {
        var result = _controller.GetConfig();
        Assert.IsInstanceOfType(result, typeof(ForbidResult));
    }

    [TestMethod]
    public void GetConfig_Forbidden_WhenUserNotFound()
    {
        _httpContext.Session.SetInt32("UserId", 300);
        var result = _controller.GetConfig();
        Assert.IsInstanceOfType(result, typeof(ForbidResult));
    }

    [TestMethod]
    public void GetConfig_Forbidden_WhenUserDeactivated()
    {
        _db.Users.Add(new User { Id = 1, Name = "admin", Email = "admin@example.com", Password = PasswordHasher.HashPassword("testpass"), IsAdmin = true, IsDeactivated = true });
        _db.SaveChanges();

        _httpContext.Session.SetInt32("UserId", 1);

        var result = _controller.GetConfig();
        Assert.IsInstanceOfType(result, typeof(ForbidResult));
        Assert.AreEqual(0, _httpContext.Session.Keys.Count()); // cleared
    }

    [TestMethod]
    public void GetConfig_Forbidden_WhenUserNotAdmin()
    {
        _db.Users.Add(new User { Id = 1, Name = "user", Email = "user@example.com", Password = PasswordHasher.HashPassword("testpass"), IsAdmin = false });
        _db.SaveChanges();
        _httpContext.Session.SetInt32("UserId", 1);

        var result = _controller.GetConfig();

        Assert.IsInstanceOfType(result, typeof(ForbidResult));
    }

    [TestMethod]
    public void GetConfig_ReturnsConfig_WhenAdmin()
    {
        _db.Users.Add(new User { Id = 1, Name = "admin", Email = "admin@example.com", Password = PasswordHasher.HashPassword("testpass"), IsAdmin = true });
        _db.SaveChanges();
        _httpContext.Session.SetInt32("UserId", 1);

        var result = _controller.GetConfig() as OkObjectResult;

        Assert.IsNotNull(result);
        dynamic obj = result.Value;

        Assert.AreEqual(true, obj.privateMode);
        Assert.AreEqual(false, obj.prohibitGroups);
        Assert.AreEqual(true, obj.prohibitGeneral);
        Assert.AreEqual("http://localhost:5000", obj.httpUrl);
        Assert.AreEqual("https://localhost:5001", obj.httpsUrl);
    }

    // -------------------------
    // PUT /config tests
    // -------------------------

    [TestMethod]
    public void UpdateConfig_Forbidden_WhenNoSession()
    {
        var result = _controller.UpdateConfig(new AdminConfigRequest());
        Assert.IsInstanceOfType(result, typeof(ForbidResult));
    }

    [TestMethod]
    public void UpdateConfig_BadRequest_WhenRequestIsNull()
    {
        _db.Users.Add(new User { Id = 1, Name = "admin", Email = "admin@example.com", Password = PasswordHasher.HashPassword("testpass"), IsAdmin = true });
        _db.SaveChanges();
        _httpContext.Session.SetInt32("UserId", 1);

        var result = _controller.UpdateConfig(null);
        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public void UpdateConfig_NotFound_WhenFileMissing()
    {
        _db.Users.Add(new User { Id = 1, Name = "admin", Email = "admin@example.com", Password = PasswordHasher.HashPassword("testpass"), IsAdmin = true });
        _db.SaveChanges();
        _httpContext.Session.SetInt32("UserId", 1);

        var result = _controller.UpdateConfig(new AdminConfigRequest());
        Assert.IsInstanceOfType(result, typeof(NotFoundObjectResult));
    }

    [TestMethod]
    public void UpdateConfig_UpdatesFileSuccessfully()
    {
        _db.Users.Add(new User { Id = 1, Name = "admin", Email = "admin@example.com", Password = "x", IsAdmin = true });
        _db.SaveChanges();
        _httpContext.Session.SetInt32("UserId", 1);

        // Create config file
        string configFile = Path.Combine(_env.ContentRootPath, "appsettings.Development.json");
        File.WriteAllText(configFile,
@"{
    ""ServerSettings"": {
        ""PrivateMode"": false,
        ""ProhibitGroups"": false,
        ""ProhibitGeneral"": false
    },
    ""Kestrel"": {
        ""Endpoints"": {
            ""Http"": { ""Url"": ""http://old"" }
        }
    }
}");

        var req = new AdminConfigRequest
        {
            PrivateMode = true,
            HttpUrl = "http://new"
        };

        var result = _controller.UpdateConfig(req) as OkObjectResult;
        Assert.IsNotNull(result);

        // Assert file updated
        var updated = JsonNode.Parse(File.ReadAllText(configFile));
        Assert.AreEqual(true, updated["ServerSettings"]["PrivateMode"].GetValue<bool>());
        Assert.AreEqual("http://new", updated["Kestrel"]["Endpoints"]["Http"]["Url"].GetValue<string>());
    }

    [TestMethod]
    public async Task PromoteUser_AsAdmin_PromotesSuccessfully()
    {
        // seed admin and a normal user
        _db.Users.Add(new User { Id = 1, Name = "admin", Email = "admin@ex.com", Password = PasswordHasher.HashPassword("testpass"), IsAdmin = true });
        _db.Users.Add(new User { Id = 2, Name = "bob", Email = "bob@ex.com", Password = PasswordHasher.HashPassword("testpass"), IsAdmin = false });
        _db.SaveChanges();

        _httpContext.Session.SetInt32("UserId", 1);

        var res = await _controller.PromoteUser(2) as OkObjectResult;
        Assert.IsNotNull(res);
        dynamic body = res.Value;
        Assert.AreEqual(2, (int)body.id);
        Assert.AreEqual(true, (bool)body.isAdmin);

        var promoted = _db.Users.Find(2);
        Assert.IsNotNull(promoted);
        Assert.IsTrue(promoted!.IsAdmin);
    }
}


// ============================================================================
//   Supporting Classes (NO MOCKS)
// ============================================================================

class TestEnvironment : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "TestApp";
    public IFileProvider WebRootFileProvider { get; set; }
    public IFileProvider ContentRootFileProvider { get; set; }
    public string WebRootPath { get; set; }
    public string ContentRootPath { get; set; }

    public bool IsDev { get; set; } = true;

    public string EnvironmentName
    {
        get => IsDev ? "Development" : "Production";
        set => IsDev = value == "Development";
    }
}

public class TestSession : ISession
{
    private readonly Dictionary<string, byte[]> _storage = new();

    public bool IsAvailable => true;
    public string Id { get; } = Guid.NewGuid().ToString();
    public IEnumerable<string> Keys => _storage.Keys;

    public void Clear() => _storage.Clear();

    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void Remove(string key) => _storage.Remove(key);

    public void Set(string key, byte[] value) => _storage[key] = value;

    public bool TryGetValue(string key, out byte[] value) =>
        _storage.TryGetValue(key, out value);
}
