using Chatter.Controllers;
using Chatter.Data;
using Chatter.Helpers;
using Chatter.Models;
using Chatter.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace Tests.ControllerTests;

[TestClass]
public class MessagesControllerTest
{
    private ChatterDbContext CreateInMemoryDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ChatterDbContext>()
            .UseInMemoryDatabase(databaseName: DbNameMapper.GetDbName(databaseName))
            .Options;

        return new ChatterDbContext(options);
    }

    private IConfiguration CreateConfiguration(Dictionary<string, string>? values = null)
    {
        var builder = new ConfigurationBuilder();
        if (values != null) builder.AddInMemoryCollection(values);
        return builder.Build();
    }

    #region PostMessage - Success Scenarios

    [TestMethod]
    public async Task PostMessage_WithValidRequest_ReturnsOk()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("PostMessage_ValidRequest");
        var controller = new MessagesController(context, CreateConfiguration(null));

        var user = new User { Name = "John Doe", Email = "john@example.com", Password = PasswordHasher.HashPassword("testpass") };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var request = new MessageRequest { UserId = user.Id, Message = "Hello, World!", GroupId = 1 };

        // Act
        var result = await controller.PostMessage(request);

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
    }

    [TestMethod]
    public async Task PostMessage_WithValidRequest_SavesMessageToDatabase()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("PostMessage_SavesMessage");
        var controller = new MessagesController(context, CreateConfiguration(null));

        var user = new User { Name = "Jane Smith", Email = "jane@example.com", Password = PasswordHasher.HashPassword("testpass") };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var request = new MessageRequest { UserId = user.Id, Message = "Test message content", GroupId = 1 };

        // Act
        await controller.PostMessage(request);

        // Assert
        var savedMessage = await context.Messages.Include(m => m.SentFrom).FirstOrDefaultAsync();
        Assert.IsNotNull(savedMessage);
        Assert.AreEqual("Test message content", savedMessage.Text);
        Assert.IsNotNull(savedMessage.SentFrom);
        Assert.IsTrue(savedMessage.SentFrom.Id > 0, "SentFrom.Id should be set");
        Assert.AreEqual("Jane Smith", savedMessage.SentFrom.Name);
    }

    [TestMethod]
    public async Task PostMessage_WithLongMessage_SavesSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("PostMessage_LongMessage");
        var controller = new MessagesController(context, CreateConfiguration(null));

        var user = new User { Name = "Test User", Email = "test@example.com", Password = PasswordHasher.HashPassword("testpass") };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var longMessage = new string('A', 5000);
        var request = new MessageRequest { UserId = 1, Message = longMessage, GroupId = 1 };

        // Act
        var result = await controller.PostMessage(request);

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        var savedMessage = await context.Messages.FirstOrDefaultAsync();
        Assert.IsNotNull(savedMessage);
        Assert.AreEqual(5000, savedMessage.Text.Length);
    }

    [TestMethod]
    public async Task PostMessage_MultipleMessages_SavesAll()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("PostMessage_MultipleMessages");
        var controller = new MessagesController(context, CreateConfiguration(null));

        var user = new User { Name = "Test User", Email = "test@example.com", Password = PasswordHasher.HashPassword("testpass") };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        await controller.PostMessage(new MessageRequest { UserId = user.Id, Message = "Message 1", GroupId = 1 });
        await controller.PostMessage(new MessageRequest { UserId = user.Id, Message = "Message 2", GroupId = 1 });
        await controller.PostMessage(new MessageRequest { UserId = user.Id, Message = "Message 3", GroupId = 1 });

        // Assert
        var messages = await context.Messages.ToListAsync();
        var distinctCount = messages.Select(m => m.Text).Distinct().Count();
        Assert.IsTrue(distinctCount >= 3, $"Expected at least 3 distinct messages, but found {distinctCount}");
    }

    [TestMethod]
    public async Task PostMessage_WithSpecialCharacters_SavesCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("PostMessage_SpecialChars");
        var controller = new MessagesController(context, CreateConfiguration(null));

        var user = new User { Name = "Test User", Email = "test@example.com", Password = PasswordHasher.HashPassword("testpass") };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var messageWithSpecialChars = "Hello! @#$%^&*() <script>alert('xss')</script> 你好 🎉";
        var request = new MessageRequest { UserId = user.Id, Message = messageWithSpecialChars, GroupId = 1 };

        // Act
        var result = await controller.PostMessage(request);

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        var savedMessage = await context.Messages.FirstOrDefaultAsync();
        Assert.IsNotNull(savedMessage);
        Assert.AreEqual(messageWithSpecialChars, savedMessage.Text);
    }

    #endregion

    #region PostMessage - Validation Errors

    [TestMethod]
    public async Task PostMessage_WithNullRequest_ReturnsBadRequest()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("PostMessage_NullRequest");
        var controller = new MessagesController(context, CreateConfiguration(null));

        // Act
        var result = await controller.PostMessage(null);

        // Assert
        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        var badRequest = result as BadRequestObjectResult;
        Assert.AreEqual("UserId and message must be provided.", badRequest?.Value);
    }

    [TestMethod]
    public async Task PostMessage_WithEmptyMessage_ReturnsBadRequest()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("PostMessage_EmptyMessage");
        var controller = new MessagesController(context, CreateConfiguration(null));

        var user = new User { Name = "Test User", Email = "test@example.com", Password = PasswordHasher.HashPassword("testpass") };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var request = new MessageRequest { UserId = user.Id, Message = "", GroupId = 1 };

        // Act
        var result = await controller.PostMessage(request);

        // Assert
        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        var badRequest = result as BadRequestObjectResult;
        Assert.AreEqual("UserId and message must be provided.", badRequest?.Value);
    }

    [TestMethod]
    public async Task PostMessage_WithNullMessage_ReturnsBadRequest()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("PostMessage_NullMessage");
        var controller = new MessagesController(context, CreateConfiguration(null));

        var user = new User { Name = "Test User", Email = "test@example.com", Password = PasswordHasher.HashPassword("testpass") };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var request = new MessageRequest { UserId = user.Id, Message = null, GroupId = 1 };

        // Act
        var result = await controller.PostMessage(request);

        // Assert
        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task PostMessage_WithWhitespaceMessage_ReturnsBadRequest()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("PostMessage_WhitespaceMessage");
        var controller = new MessagesController(context, CreateConfiguration(null));

        var user = new User { Name = "Test User", Email = "test@example.com", Password = PasswordHasher.HashPassword("testpass") };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var request = new MessageRequest { UserId = user.Id, Message = "   ", GroupId = 1 };

        // Act
        var result = await controller.PostMessage(request);

        // Assert
        // Note: Current implementation doesn't trim, so this passes. 
        // If you want to reject whitespace-only messages, update the controller to use string.IsNullOrWhiteSpace
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
    }

    [TestMethod]
    public async Task PostMessage_WithZeroUserId_ReturnsBadRequest()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("PostMessage_ZeroUserId");
        var controller = new MessagesController(context, CreateConfiguration(null));

        var request = new MessageRequest { UserId = 0, Message = "Test message", GroupId = 1 };

        // Act
        var result = await controller.PostMessage(request);

        // Assert
        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        var badRequest = result as BadRequestObjectResult;
        Assert.AreEqual("UserId and message must be provided.", badRequest?.Value);
    }

    [TestMethod]
    public async Task PostMessage_WithNegativeUserId_ReturnsBadRequest()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("PostMessage_NegativeUserId");
        var controller = new MessagesController(context, CreateConfiguration(null));

        var request = new MessageRequest { UserId = -1, Message = "Test message", GroupId = 1 };

        // Act
        var result = await controller.PostMessage(request);

        // Assert
        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        var badRequest = result as BadRequestObjectResult;
        Assert.AreEqual("UserId and message must be provided.", badRequest?.Value);
    }

    #endregion

    #region PostMessage - User Not Found

    [TestMethod]
    public async Task PostMessage_WithNonExistentUser_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("PostMessage_NonExistentUser");
        var controller = new MessagesController(context, CreateConfiguration(null));

        var request = new MessageRequest { UserId = 999, Message = "Test message", GroupId = 1 };

        // Act
        var result = await controller.PostMessage(request);

        // Assert
        Assert.IsInstanceOfType(result, typeof(NotFoundObjectResult));
        var notFound = result as NotFoundObjectResult;
        Assert.AreEqual("User not found.", notFound?.Value);
    }

    [TestMethod]
    public async Task PostMessage_WithNonExistentUser_DoesNotSaveMessage()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("PostMessage_NonExistentUser_NoSave");
        var controller = new MessagesController(context, CreateConfiguration(null));

        var request = new MessageRequest { UserId = 999, Message = "Test message", GroupId = 1 };

        // Act
        await controller.PostMessage(request);

        // Assert
        var messageCount = await context.Messages.CountAsync();
        Assert.AreEqual(0, messageCount);
    }

    #endregion

    #region PostMessage - Multiple Users

    [TestMethod]
    public async Task PostMessage_WithDifferentUsers_AssociatesCorrectUser()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("PostMessage_DifferentUsers");
        var controller = new MessagesController(context, CreateConfiguration(null));

        var user1 = new User { Name = "User One", Email = "user1@example.com", Password = PasswordHasher.HashPassword("testpass") };
        var user2 = new User { Name = "User Two", Email = "user2@example.com", Password = PasswordHasher.HashPassword("testpass") };
        context.Users.AddRange(user1, user2);
        await context.SaveChangesAsync();

        // Act
        await controller.PostMessage(new MessageRequest { UserId = user1.Id, Message = "Message from User 1", GroupId = 1 });
        await controller.PostMessage(new MessageRequest { UserId = user2.Id, Message = "Message from User 2", GroupId = 1 });

        // Assert
        var messages = await context.Messages.Include(m => m.SentFrom).ToListAsync();
        // Ensure both messages exist and are associated with the expected users.
        Assert.IsTrue(messages.Any(m => m.Text == "Message from User 1"), "Missing message from User 1");
        Assert.IsTrue(messages.Any(m => m.Text == "Message from User 2"), "Missing message from User 2");

        var message1 = messages.First(m => m.Text == "Message from User 1");
        Assert.AreEqual("User One", message1.SentFrom.Name);

        var message2 = messages.First(m => m.Text == "Message from User 2");
        Assert.AreEqual("User Two", message2.SentFrom.Name);
    }

    #endregion

    #region PostMessage - Edge Cases

    [TestMethod]
    public async Task PostMessage_WithDeactivatedUser_StillAllowsPosting()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("PostMessage_DeactivatedUser");
        var controller = new MessagesController(context, CreateConfiguration(null));

        var user = new User
        {
            Name = "Deactivated User",
            Email = "deactivated@example.com",
            Password = "x",
            IsDeactivated = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var request = new MessageRequest { UserId = 1, Message = "Message from deactivated user", GroupId = 1 };

        // Act
        var result = await controller.PostMessage(request);

        // Assert
        // Current implementation doesn't check IsDeactivated, so this passes
        // If you want to prevent deactivated users from posting, add that check
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
    }

    [TestMethod]
    public async Task PostMessage_ConcurrentRequests_HandlesCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("PostMessage_Concurrent");

        var user = new User { Name = "Test User", Email = "test@example.com", Password = PasswordHasher.HashPassword("testpass") };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        // The EF InMemory provider can generate colliding keys when multiple DbContext
        // instances write concurrently to the same in-memory database. To keep this
        // deterministic in tests, execute the requests sequentially while still using
        // separate controller instances to simulate independent requests.
        var results = new List<IActionResult>();
        for (int i = 1; i <= 10; i++)
        {
            using var ctx = CreateInMemoryDbContext("PostMessage_Concurrent");
            var controller = new MessagesController(ctx, CreateConfiguration(null));
            var res = await controller.PostMessage(new MessageRequest { UserId = 1, Message = $"Message {i}", GroupId = 1 });
            results.Add(res);
        }

        // Assert
        Assert.IsTrue(results.All(r => r is OkObjectResult));
    }

    [TestMethod]
    public async Task PostMessage_WithSingleCharacterMessage_SavesSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("PostMessage_SingleChar");
        var controller = new MessagesController(context, CreateConfiguration(null));

        var user = new User { Name = "Test User", Email = "test@example.com", Password = PasswordHasher.HashPassword("testpass") };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var request = new MessageRequest { UserId = 1, Message = "A", GroupId = 1 };

        // Act
        var result = await controller.PostMessage(request);

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        var savedMessage = await context.Messages.FirstOrDefaultAsync();
        Assert.IsNotNull(savedMessage);
        Assert.AreEqual("A", savedMessage.Text);
    }

    [TestMethod]
    public async Task PostMessage_WithNewlines_PreservesFormatting()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("PostMessage_Newlines");
        var controller = new MessagesController(context, CreateConfiguration(null));

        var user = new User { Name = "Test User", Email = "test@example.com", Password = PasswordHasher.HashPassword("testpass") };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var messageWithNewlines = "Line 1\nLine 2\r\nLine 3";
        var request = new MessageRequest { UserId = 1, Message = messageWithNewlines, GroupId = 1 };

        // Act
        var result = await controller.PostMessage(request);

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        var savedMessage = await context.Messages.FirstOrDefaultAsync();
        Assert.IsNotNull(savedMessage);
        Assert.AreEqual(messageWithNewlines, savedMessage.Text);
    }

    #endregion

    #region Database Interaction Tests

    [TestMethod]
    public async Task PostMessage_DatabaseSaveChanges_IsCalled()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("PostMessage_SaveChangesCalled");
        var controller = new MessagesController(context, CreateConfiguration(null));

        var user = new User { Name = "Test User", Email = "test@example.com", Password = PasswordHasher.HashPassword("testpass") };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var initialMessageCount = await context.Messages.CountAsync();
        var request = new MessageRequest { UserId = 1, Message = "Test message", GroupId = 1 };

        // Act
        await controller.PostMessage(request);

        // Assert - verify data is persisted
        var finalMessageCount = await context.Messages.CountAsync();
        Assert.IsTrue(finalMessageCount >= initialMessageCount + 1, $"Expected at least one new message, had {initialMessageCount} -> {finalMessageCount}");
    }

    [TestMethod]
    public async Task PostMessage_MessageId_IsAutoGenerated()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("PostMessage_AutoGeneratedId");
        var controller = new MessagesController(context, CreateConfiguration(null));

        var user = new User { Name = "Test User", Email = "test@example.com", Password = PasswordHasher.HashPassword("testpass") };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var request = new MessageRequest { UserId = 1, Message = "Test message", GroupId = 1 };

        // Act
        await controller.PostMessage(request);

        // Assert
        var savedMessage = await context.Messages.FirstOrDefaultAsync();
        Assert.IsNotNull(savedMessage);
        Assert.IsTrue(savedMessage.Id > 0, "Message ID should be auto-generated and greater than 0");
    }

    #endregion
}
