using Chatter.Controllers;
using Chatter.Data;
using Chatter.Helpers;
using Chatter.Models;
using Chatter.Models.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Tests.ControllerTests;

[TestClass]
public class AuthControllerTest
{
    private ChatterDbContext CreateDb(string dbName)
    {
        var opts = new DbContextOptionsBuilder<ChatterDbContext>()
            .UseInMemoryDatabase(DbNameMapper.GetDbName(dbName))
            .ConfigureWarnings(w => w.Default(WarningBehavior.Ignore))
            .Options;

        var db = new ChatterDbContext(opts);
        db.Database.EnsureCreated();
        return db;
    }

    private static HttpContext CreateHttpContext(int? userId = null)
    {
        var ctx = new DefaultHttpContext();
        ctx.Session = new TestSession();
        if (userId.HasValue)
            ctx.Session.SetInt32("UserId", userId.Value);
        return ctx;
    }

    // ----------------------------------------------------
    // GET /auth/check
    // ----------------------------------------------------

    [TestMethod]
    public async Task Check_WhenNotAuthenticated_ReturnsFalse()
    {
        var db = CreateDb("auth-check-1");
        var ctrl = new AuthController(db);
        ctrl.ControllerContext = new ControllerContext { HttpContext = CreateHttpContext(null) };

        var res = await ctrl.Check() as OkObjectResult;
        Assert.IsNotNull(res);

        dynamic body = res.Value!;
        Assert.AreEqual(false, body.authenticated);
    }

    [TestMethod]
    public async Task Check_WhenUserMissingInDb_ReturnsFalse_AndClearsSession()
    {
        var db = CreateDb("auth-check-2");
        var ctx = CreateHttpContext(999); // nonexistent
        var ctrl = new AuthController(db)
        {
            ControllerContext = new ControllerContext { HttpContext = ctx }
        };

        var res = await ctrl.Check() as OkObjectResult;
        Assert.IsNotNull(res);

        dynamic body = res.Value!;
        Assert.AreEqual(false, body.authenticated);
        Assert.AreEqual(0, ctx.Session.Keys.Count()); // cleared
    }

    [TestMethod]
    public async Task Check_WhenUserDeactivated_ReturnsFalse()
    {
        var db = CreateDb("auth-check-3");
        var user = new User { Name = "x", Email = "x@x.com", Password = PasswordHasher.HashPassword("testpass"), IsDeactivated = true };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var ctx = CreateHttpContext(user.Id);
        var ctrl = new AuthController(db)
        {
            ControllerContext = new ControllerContext { HttpContext = ctx }
        };

        var res = await ctrl.Check() as OkObjectResult;
        Assert.IsNotNull(res);
        dynamic body = res.Value!;
        Assert.AreEqual(false, body.authenticated);
        Assert.AreEqual(0, ctx.Session.Keys.Count());
    }

    [TestMethod]
    public async Task Check_WhenAuthenticated_ReturnsUser()
    {
        var db = CreateDb("auth-check-4");
        var user = new User
        {
            Name = "Alice",
            Email = "alice@example.com",
            Password = PasswordHasher.HashPassword("pass"),
            IsAdmin = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var ctx = CreateHttpContext(user.Id);
        var ctrl = new AuthController(db)
        {
            ControllerContext = new ControllerContext { HttpContext = ctx }
        };

        var res = await ctrl.Check() as OkObjectResult;
        Assert.IsNotNull(res);
        dynamic body = res.Value!;

        Assert.AreEqual(true, body.authenticated);
        Assert.AreEqual(user.Id, (int)body.user.id);
        Assert.AreEqual("Alice", (string)body.user.name);
        Assert.AreEqual("alice@example.com", (string)body.user.email);
        Assert.AreEqual(true, (bool)body.user.isAdmin);
    }

    // ----------------------------------------------------
    // POST /auth/login
    // ----------------------------------------------------

    [TestMethod]
    public async Task Login_WhenNullRequest_ReturnsBadRequest()
    {
        var db = CreateDb("auth-login-1");
        var ctrl = new AuthController(db);
        ctrl.ControllerContext = new ControllerContext { HttpContext = CreateHttpContext() };

        var res = await ctrl.Login(null!) as BadRequestObjectResult;
        Assert.IsNotNull(res);
    }

    [TestMethod]
    public async Task Login_RequiresUsername()
    {
        var db = CreateDb("auth-login-2");
        var ctrl = new AuthController(db)
        {
            ControllerContext = new ControllerContext { HttpContext = CreateHttpContext() }
        };

        var req = new LoginRequest("", "pw");
        var res = await ctrl.Login(req) as BadRequestObjectResult;
        Assert.IsNotNull(res);
    }

    [TestMethod]
    public async Task Login_RequiresPassword()
    {
        var db = CreateDb("auth-login-3");
        var ctrl = new AuthController(db)
        {
            ControllerContext = new ControllerContext { HttpContext = CreateHttpContext() }
        };

        var req = new LoginRequest("user", "");
        var res = await ctrl.Login(req) as BadRequestObjectResult;
        Assert.IsNotNull(res);
    }

    [TestMethod]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        var db = CreateDb("auth-login-4");
        var ctrl = new AuthController(db)
        {
            ControllerContext = new ControllerContext { HttpContext = CreateHttpContext() }
        };

        var req = new LoginRequest("nope", "bad");
        var res = await ctrl.Login(req) as UnauthorizedObjectResult;

        Assert.IsNotNull(res);
    }

    [TestMethod]
    public async Task Login_DeactivatedUser_ReturnsUnauthorized()
    {
        var db = CreateDb("auth-login-5");
        var user = new User
        {
            Name = "bob",
            Email = "bob@example.com",
            Password = PasswordHasher.HashPassword("secret"),
            IsDeactivated = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var ctrl = new AuthController(db)
        {
            ControllerContext = new ControllerContext { HttpContext = CreateHttpContext() }
        };

        var req = new LoginRequest("bob@example.com", "secret");
        var res = await ctrl.Login(req) as UnauthorizedObjectResult;

        Assert.IsNotNull(res);
    }

    [TestMethod]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var db = CreateDb("auth-login-6");
        var user = new User
        {
            Name = "sally",
            Email = "sally@example.com",
            Password = PasswordHasher.HashPassword("correctpass")
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var ctrl = new AuthController(db)
        {
            ControllerContext = new ControllerContext { HttpContext = CreateHttpContext() }
        };

        var req = new LoginRequest("sally@example.com", "wrongpass");
        var res = await ctrl.Login(req) as UnauthorizedObjectResult;

        Assert.IsNotNull(res);
    }

    [TestMethod]
    public async Task Login_Success_SetsSession()
    {
        var db = CreateDb("auth-login-7");
        var user = new User
        {
            Name = "jim",
            Email = "jim@example.com",
            Password = PasswordHasher.HashPassword("secret"),
            IsAdmin = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var ctx = CreateHttpContext();
        var ctrl = new AuthController(db)
        {
            ControllerContext = new ControllerContext { HttpContext = ctx }
        };

        var req = new LoginRequest("jim@example.com", "secret");
        var res = await ctrl.Login(req) as OkObjectResult;

        Assert.IsNotNull(res);

        Assert.IsTrue(ctx.Session.GetInt32("UserId") > 0);
        Assert.AreEqual("jim@example.com", ctx.Session.GetString("UserEmail"));
        Assert.AreEqual("jim", ctx.Session.GetString("UserName"));
    }

    // ----------------------------------------------------
    // POST /auth/logout
    // ----------------------------------------------------

    [TestMethod]
    public void Logout_WhenNotLoggedIn_ReturnsBadRequest()
    {
        var db = CreateDb("auth-logout-1");
        var ctrl = new AuthController(db)
        {
            ControllerContext = new ControllerContext { HttpContext = CreateHttpContext() }
        };

        var res = ctrl.Logout() as BadRequestObjectResult;
        Assert.IsNotNull(res);
    }

    [TestMethod]
    public void Logout_ClearsSession()
    {
        var db = CreateDb("auth-logout-2");

        var ctx = CreateHttpContext(10);
        ctx.Session.SetString("UserName", "testuser");

        var ctrl = new AuthController(db)
        {
            ControllerContext = new ControllerContext { HttpContext = ctx }
        };

        var res = ctrl.Logout() as OkObjectResult;
        Assert.IsNotNull(res);

        Assert.AreEqual(0, ctx.Session.Keys.Count());
    }

    // ----------------------------------------------------
    // Test Session
    // ----------------------------------------------------

    private class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();
        public bool IsAvailable => true;
        public string Id { get; } = Guid.NewGuid().ToString();
        public IEnumerable<string> Keys => _store.Keys;

        public void Clear() => _store.Clear();
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value);

        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
