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
    public class UsersControllerTest
    {
        private ChatterDbContext _dbContext = null!;
        private IConfiguration _configuration = null!;
        private UsersController _controller = null!;
        private DefaultHttpContext _httpContext = null!;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ChatterDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _dbContext = new ChatterDbContext(options);

            var inMemorySettings = new Dictionary<string, string>
            {
                {"ServerSettings:PrivateMode", "false"}
            };
            _configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

            _controller = new UsersController(_dbContext, _configuration);

            _httpContext = new DefaultHttpContext();
            _httpContext.Features.Set<ISessionFeature>(new SessionFeature { Session = new TestSession() });
            _controller.ControllerContext = new ControllerContext { HttpContext = _httpContext };
        }

        private void SetSessionUser(int userId)
        {
            _httpContext.Session.SetInt32("UserId", userId);
        }

        #region Create Tests

        [TestMethod]
        public async Task Create_NullRequest_ReturnsBadRequest()
        {
            var result = await _controller.Create(null!);
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        }

        [TestMethod]
        public async Task Create_InvalidName_ReturnsBadRequest()
        {
            var req = new CreateUserRequest { Name = "", Email = "test@test.com", Password = "123456" };
            var result = await _controller.Create(req);
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        }

        [TestMethod]
        public async Task Create_DuplicateEmail_ReturnsConflict()
        {
            _dbContext.Users.Add(new User { Name = "Test", Email = "dup@test.com", Password = PasswordHasher.HashPassword("testpass") });
            await _dbContext.SaveChangesAsync();

            var req = new CreateUserRequest { Name = "New", Email = "dup@test.com", Password = "123456" };
            var result = await _controller.Create(req);
            Assert.IsInstanceOfType(result, typeof(ConflictObjectResult));
        }

        [TestMethod]
        public async Task Create_FirstUserBecomesAdmin()
        {
            var req = new CreateUserRequest { Name = "Admin", Email = "admin@test.com", Password = "123456" };
            var result = await _controller.Create(req) as CreatedAtActionResult;
            dynamic user = result!.Value;
            Assert.IsTrue(user.isAdmin);
        }

        [TestMethod]
        public async Task Create_NormalUser_Success()
        {
            // add first user
            _dbContext.Users.Add(new User { Name = "First", Email = "first@test.com", Password = PasswordHasher.HashPassword("testpass") });
            await _dbContext.SaveChangesAsync();

            var req = new CreateUserRequest { Name = "Second", Email = "second@test.com", Password = "123456" };
            var result = await _controller.Create(req) as CreatedAtActionResult;
            dynamic user = result!.Value;
            Assert.IsFalse(user.isAdmin);
        }

        #endregion

        #region Update Tests

        [TestMethod]
        public async Task Update_UnauthorizedSession_ReturnsForbid()
        {
            var req = new UpdateUserRequest { Name = "X", Email = "x@test.com" };
            var result = await _controller.Update(1, req);
            Assert.IsInstanceOfType(result, typeof(ForbidResult));
        }

        [TestMethod]
        public async Task Update_InvalidName_ReturnsBadRequest()
        {
            _dbContext.Users.Add(new User { Id = 1, Name = "Al", Email = "a@test.com", Password = PasswordHasher.HashPassword("testpass") });
            await _dbContext.SaveChangesAsync();

            SetSessionUser(1);
            var req = new UpdateUserRequest { Name = "", Email = "a@test.com" };
            var result = await _controller.Update(1, req);
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        }

        [TestMethod]
        public async Task Update_EmailConflict_ReturnsConflict()
        {
            _dbContext.Users.Add(new User { Id = 1, Name = "Al", Email = "a@test.com", Password = PasswordHasher.HashPassword("testpass") });
            _dbContext.Users.Add(new User { Id = 2, Name = "Bo", Email = "b@test.com", Password = PasswordHasher.HashPassword("testpass") });
            await _dbContext.SaveChangesAsync();

            SetSessionUser(1);
            var req = new UpdateUserRequest { Name = "Al", Email = "b@test.com" };
            var result = await _controller.Update(1, req);
            Assert.IsInstanceOfType(result, typeof(ConflictObjectResult));
        }

        [TestMethod]
        public async Task Update_PasswordChange_WrongCurrentPassword_ReturnsUnauthorized()
        {
            _dbContext.Users.Add(new User { Id = 1, Name = "Al", Email = "a@test.com", Password = PasswordHasher.HashPassword("oldpass") });
            await _dbContext.SaveChangesAsync();

            SetSessionUser(1);
            var req = new UpdateUserRequest { Name = "Al", Email = "a@test.com", Password = "newpass", CurrentPassword = "wrong" };
            var result = await _controller.Update(1, req);
            Assert.IsInstanceOfType(result, typeof(UnauthorizedObjectResult));
        }

        [TestMethod]
        public async Task Update_PasswordChange_CorrectCurrentPassword_Success()
        {
            _dbContext.Users.Add(new User { Id = 1, Name = "Al", Email = "a@test.com", Password = PasswordHasher.HashPassword("oldpass") });
            await _dbContext.SaveChangesAsync();

            SetSessionUser(1);
            var req = new UpdateUserRequest { Name = "Al", Email = "a@test.com", Password = "newpass", CurrentPassword = "oldpass" };
            var result = await _controller.Update(1, req) as OkObjectResult;
            dynamic user = result!.Value;
            Assert.IsNotNull(user);
        }

        #endregion

        #region Delete Tests

        [TestMethod]
        public async Task Delete_Unauthorized_ReturnsForbid()
        {
            var result = await _controller.Delete(1);
            Assert.IsInstanceOfType(result, typeof(ForbidResult));
        }

        [TestMethod]
        public async Task Delete_NotFound_ReturnsNotFound()
        {
            SetSessionUser(999);
            var result = await _controller.Delete(999);
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Delete_Success_DeactivatesAndClearsSession()
        {
            _dbContext.Users.Add(new User { Id = 1, Name = "Al", Email = "a@test.com", Password = PasswordHasher.HashPassword("testpass") });
            await _dbContext.SaveChangesAsync();

            SetSessionUser(1);
            var result = await _controller.Delete(1) as OkObjectResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(true, _dbContext.Users.Find(1)!.IsDeactivated);
        }

        #endregion

        #region Session Helpers
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
        #endregion
    }
}
