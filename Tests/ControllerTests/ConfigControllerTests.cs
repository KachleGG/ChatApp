using Chatter.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Tests.ControllerTests;

[TestClass]
public class ConfigControllerTests
{
    private IConfiguration BuildConfig(Dictionary<string, string?> values) {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    [TestMethod]
    public void Get_ReturnsExpectedValues() {
        // Arrange
        var cfg = BuildConfig(new Dictionary<string, string?>
        {
            ["ServerSettings:ProhibitGroups"] = "true",
            ["ServerSettings:PrivateMode"] = "false",
            ["ServerSettings:ProhibitGeneral"] = "true",
            ["ServerSettings:UserGroupLimit"] = "12"
        });

        var ctrl = new ConfigController(cfg);

        // Act
        var result = ctrl.Get() as OkObjectResult;

        // Assert
        Assert.IsNotNull(result);
        dynamic body = result.Value!;

        Assert.AreEqual(true, (bool)body.prohibitGroups);
        Assert.AreEqual(false, (bool)body.privateMode);
        Assert.AreEqual(true, (bool)body.prohibitGeneral);
        Assert.AreEqual(12, (int)body.userGroupLimit);
    }

    [TestMethod]
    public void Get_UsesDefaultUserGroupLimit_WhenNotConfigured() {
        // Arrange — no UserGroupLimit
        var cfg = BuildConfig(new Dictionary<string, string?>
        {
            ["ServerSettings:ProhibitGroups"] = "false",
            ["ServerSettings:PrivateMode"] = "true",
            ["ServerSettings:ProhibitGeneral"] = "false"
        });

        var ctrl = new ConfigController(cfg);

        // Act
        var result = ctrl.Get() as OkObjectResult;

        // Assert
        Assert.IsNotNull(result);
        dynamic body = result.Value!;

        // Values provided
        Assert.AreEqual(false, (bool)body.prohibitGroups);
        Assert.AreEqual(true, (bool)body.privateMode);
        Assert.AreEqual(false, (bool)body.prohibitGeneral);

        // Default value (expected 5)
        Assert.AreEqual(5, (int)body.userGroupLimit);
    }

    [TestMethod]
    public void Get_MissingBooleans_DefaultToFalse() {
        // Arrange — nothing provided
        var cfg = BuildConfig(new Dictionary<string, string?>());

        var ctrl = new ConfigController(cfg);

        // Act
        var result = ctrl.Get() as OkObjectResult;

        // Assert
        Assert.IsNotNull(result);
        dynamic body = result.Value!;

        Assert.AreEqual(false, (bool)body.prohibitGroups);
        Assert.AreEqual(false, (bool)body.privateMode);
        Assert.AreEqual(false, (bool)body.prohibitGeneral);

        // Default still applies
        Assert.AreEqual(5, (int)body.userGroupLimit);
    }
}
