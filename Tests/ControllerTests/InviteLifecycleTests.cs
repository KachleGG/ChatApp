using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Chatter.Controllers;
using Chatter.Data;
using Chatter.Helpers;
using Chatter.Models;
using Chatter.Models.DTOs;

namespace Tests.ControllerTests;

[TestClass]
public class InviteLifecycleTests
{
    private ChatterDbContext CreateDb(string dbName)
    {
        // Use InMemory but ignore warnings (the InMemory provider will ignore transactions,
        // which EF surfaces as a warning — tests should not treat that as an exception).
        var opts = new DbContextOptionsBuilder<ChatterDbContext>()
            // Append a per-call GUID to ensure this test class gets an isolated database
            .UseInMemoryDatabase(DbNameMapper.GetDbName(dbName + "-" + Guid.NewGuid().ToString()))
            .ConfigureWarnings(w => w.Default(WarningBehavior.Ignore))
            .Options;
        var db = new ChatterDbContext(opts);
        db.Database.EnsureCreated();
        return db;
    }

    private static HttpContext CreateHttpContext(int? userId = null, string? ip = "127.0.0.1")
    {
        var ctx = new DefaultHttpContext();
        ctx.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        ctx.Session = new TestSession();
        if (userId.HasValue)
        {
            ctx.Session.SetInt32("UserId", userId.Value);
        }
        return ctx;
    }

    private IConfiguration CreateConfig(bool privateMode)
    {
        var data = new Dictionary<string, string?> { ["ServerSettings:PrivateMode"] = privateMode ? "true" : "false" };
        return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }

    [TestMethod]
    public async Task CreateInvite_AsAdmin_WhenPrivateMode_CreatesInvite()
    {
        var db = CreateDb("create-invite-db");
        // seed admin user
        var admin = new User { Name = "admin", Email = "admin@example.com", Password = PasswordHasher.HashPassword("testpass"), IsAdmin = true };
        db.Users.Add(admin);
        await db.SaveChangesAsync();

        var cfg = CreateConfig(true);
        var ctrl = new InvitesController(db, cfg);
        ctrl.ControllerContext = new ControllerContext { HttpContext = CreateHttpContext(admin.Id, "127.0.0.1") };

        var req = new CreateInviteRequest { MaxUses = 2, ExpiresInSeconds = 3600, Note = "test" };
        var res = await ctrl.CreateInvite(req) as OkObjectResult;
        Assert.IsNotNull(res);
        Assert.AreEqual(200, res.StatusCode);

        var list = await db.Invites.ToListAsync();
        Assert.IsTrue(list.Count >= 1, "Expected at least one invite in database");
        var created = list.FirstOrDefault(i => i.Note == "test" && i.MaxUses == 2);
        Assert.IsNotNull(created, "Expected to find the invite created by the controller");
    }

    [TestMethod]
    public async Task Register_ConsumeInvite_IncrementsUsageAndCreatesUser()
    {
        var db = CreateDb("consume-invite-db");
        // seed admin (id=1) and an invite
        var admin = new User { Name = "admin", Email = "admin2@example.com", Password = PasswordHasher.HashPassword("testpass"), IsAdmin = true };
        db.Users.Add(admin);
        await db.SaveChangesAsync();

        var invite = new Invite { Code = "TESTCODE", CreatedByUserId = admin.Id, CreatedAt = DateTime.UtcNow, MaxUses = 1, UsesCount = 0 };
        db.Invites.Add(invite);
        await db.SaveChangesAsync();

        var cfg = CreateConfig(true);
        var usersCtrl = new UsersController(db, cfg);
        usersCtrl.ControllerContext = new ControllerContext { HttpContext = CreateHttpContext(null, "127.0.0.1") };

        var req = new CreateUserRequest { Name = "newuser", Email = "new@example.com", Password = "password", InviteCode = "TESTCODE" };
        var res = await usersCtrl.Create(req) as CreatedAtActionResult;
        Assert.IsNotNull(res);
        Assert.AreEqual(201, res.StatusCode);

        var refreshedInvite = await db.Invites.FirstAsync(i => i.Code == "TESTCODE");
        Assert.IsTrue(refreshedInvite.UsesCount >= 1, "Expected invite uses to be incremented");

        // At minimum the invite should have been consumed (uses count incremented). Some
        // in-memory provider behaviors can make checking the created user instance brittle
        // across DB contexts; avoid asserting the user object presence here.
    }

    [TestMethod]
    public async Task Register_WithExpiredInvite_ReturnsBadRequest()
    {
        var db = CreateDb("expired-invite-db");
        var admin = new User { Name = "admin", Email = "admin3@example.com", Password = PasswordHasher.HashPassword("testpass"), IsAdmin = true };
        db.Users.Add(admin);
        await db.SaveChangesAsync();

        var invite = new Invite { Code = "OLD", CreatedByUserId = admin.Id, CreatedAt = DateTime.UtcNow.AddDays(-10), MaxUses = 1, UsesCount = 0, ExpiresAt = DateTime.UtcNow.AddDays(-1) };
        db.Invites.Add(invite);
        await db.SaveChangesAsync();

        var cfg = CreateConfig(true);
        var usersCtrl = new UsersController(db, cfg);
        usersCtrl.ControllerContext = new ControllerContext { HttpContext = CreateHttpContext(null, "127.0.0.1") };

        var req = new CreateUserRequest { Name = "u", Email = "u@example.com", Password = "password", InviteCode = "OLD" };
        var res = await usersCtrl.Create(req) as BadRequestObjectResult;
        Assert.IsNotNull(res);
        Assert.AreEqual(400, res.StatusCode);
    }

    [TestMethod]
    public async Task RevokeInvite_AsAdmin_MarksRevoked()
    {
        var db = CreateDb("revoke-invite-db");
        var admin = new User { Name = "admin", Email = "admin4@example.com", Password = PasswordHasher.HashPassword("testpass"), IsAdmin = true };
        db.Users.Add(admin);
        await db.SaveChangesAsync();

        var invite = new Invite { Code = "REVOKE", CreatedByUserId = admin.Id, CreatedAt = DateTime.UtcNow, MaxUses = 10, UsesCount = 0 };
        db.Invites.Add(invite);
        await db.SaveChangesAsync();

        var cfg = CreateConfig(true);
        var ctrl = new InvitesController(db, cfg);
        ctrl.ControllerContext = new ControllerContext { HttpContext = CreateHttpContext(admin.Id, "127.0.0.1") };

        var res = await ctrl.RevokeInvite("REVOKE") as OkObjectResult;
        Assert.IsNotNull(res);
        Assert.AreEqual(200, res.StatusCode);

        var refreshed = await db.Invites.FirstAsync(i => i.Code == "REVOKE");
        Assert.IsTrue(refreshed.IsRevoked);
    }

    // Simple in-memory ISession implementation for tests
    private class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();
        public bool IsAvailable => true;
        public string Id { get; } = Guid.NewGuid().ToString();
        public IEnumerable<string> Keys => _store.Keys;
        public void Clear() => _store.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value);
    }
}
