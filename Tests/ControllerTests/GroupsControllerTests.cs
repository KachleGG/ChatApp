using Chatter.Controllers;
using Chatter.Data;
using Chatter.Helpers;
using Chatter.Models;
using Chatter.Models.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Tests.ControllerTests
{
    [TestClass]
    public class GroupsControllerTests
    {
        private ChatterDbContext _dbContext = null!;
        private IConfiguration _configuration = null!;
        private GroupsController _controller = null!;
        private DefaultHttpContext _httpContext = null!;

        [TestInitialize]
        public void Setup()
        {
            // In-memory EF Core
            var options = new DbContextOptionsBuilder<ChatterDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _dbContext = new ChatterDbContext(options);

            // Seed users
            _dbContext.Users.AddRange(
                new User { Id = 1, Name = "Admin", Email = "admin@test.com", IsAdmin = true, Password = PasswordHasher.HashPassword("testpass"), IsDeactivated = false },
                new User { Id = 2, Name = "User", Email = "user@test.com", IsAdmin = false, Password = PasswordHasher.HashPassword("testpass"), IsDeactivated = false },
                new User { Id = 3, Name = "Deactivated", Email = "dead@test.com", IsAdmin = false, Password = PasswordHasher.HashPassword("testpass"), IsDeactivated = true }
            );

            // Seed a general group
            _dbContext.Groups.Add(new Group { Id = 1, Name = "General", OwnerId = 1, CreatedAt = DateTime.UtcNow, IsDeactivated = false });

            _dbContext.SaveChanges();

            // Configuration mock
            var inMemorySettings = new Dictionary<string, string>
            {
                {"ServerSettings:ProhibitGeneral", "false"},
                {"ServerSettings:UserGroupLimit", "2"}
            };
            _configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

            // Controller
            _controller = new GroupsController(_dbContext, _configuration);

            // HttpContext with session
            _httpContext = new DefaultHttpContext();
            _httpContext.Features.Set<ISessionFeature>(new SessionFeature { Session = new TestSession() });
            _controller.ControllerContext = new ControllerContext { HttpContext = _httpContext };
        }

        private void SetSessionUser(int userId)
        {
            _httpContext.Session.SetInt32("UserId", userId);
        }

        [TestMethod]
        public async Task GetGroups_Unauthorized_WithoutUser()
        {
            var result = await _controller.GetGroups();
            Assert.IsInstanceOfType(result, typeof(UnauthorizedObjectResult));
        }

        [TestMethod]
        public async Task GetGroups_ReturnsGeneral_ForNormalUser()
        {
            SetSessionUser(2);
            var result = await _controller.GetGroups() as OkObjectResult;
            Assert.IsNotNull(result);
            var groups = result!.Value as IEnumerable<object>;
            Assert.IsNotNull(groups);
            Assert.AreEqual(1, groups!.Count());
        }

        [TestMethod]
        public async Task CreateGroup_Success()
        {
            SetSessionUser(2);
            var request = new CreateGroupRequest { Name = "NewGroup" };
            var result = await _controller.CreateGroup(request) as CreatedAtActionResult;
            Assert.IsNotNull(result);
            dynamic group = result!.Value;
            Assert.AreEqual("NewGroup", group.Name);
            Assert.AreEqual(2, group.OwnerId);
        }

        [TestMethod]
        public async Task CreateGroup_EnforcesLimit()
        {
            SetSessionUser(2);

            _dbContext.Groups.Add(new Group { Name = "G1", OwnerId = 2, CreatedAt = DateTime.UtcNow });
            _dbContext.Groups.Add(new Group { Name = "G2", OwnerId = 2, CreatedAt = DateTime.UtcNow });
            await _dbContext.SaveChangesAsync();

            var request = new CreateGroupRequest { Name = "ExtraGroup" };
            var result = await _controller.CreateGroup(request);
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        }

        [TestMethod]
        public async Task LeaveGroup_TransfersOwnership()
        {
            SetSessionUser(2);
            var request = new CreateGroupRequest { Name = "LeaveGroup" };
            var created = await _controller.CreateGroup(request) as CreatedAtActionResult;
            var groupId = ((dynamic)created!.Value).Id;

            // Add another member
            _dbContext.GroupMemberships.Add(new GroupMembership { GroupId = groupId, UserId = 1, JoinedAt = DateTime.UtcNow });
            await _dbContext.SaveChangesAsync();

            var leaveResult = await _controller.LeaveGroup(groupId) as OkObjectResult;
            Assert.IsNotNull(leaveResult);
            dynamic value = leaveResult!.Value;
            Assert.AreEqual(1, value.ownerId); // ownership transferred
        }

        // --- session helpers ---
        public class TestSession : ISession
        {
            private readonly Dictionary<string, byte[]> _storage = new();
            public IEnumerable<string> Keys => _storage.Keys;
            public string Id => "test";
            public bool IsAvailable => true;
            public void Clear() => _storage.Clear();
            public Task CommitAsync(System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task LoadAsync(System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
            public void Remove(string key) => _storage.Remove(key);
            public void Set(string key, byte[] value) => _storage[key] = value;
            public bool TryGetValue(string key, out byte[] value) => _storage.TryGetValue(key, out value);
        }

        public class SessionFeature : ISessionFeature
        {
            public ISession Session { get; set; } = null!;
        }
    }
}
