using Chatter.Data;
using Chatter.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Chatter.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AdminController : ControllerBase
{
    private readonly ChatterDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;

    public AdminController(ChatterDbContext dbContext, IConfiguration configuration, IWebHostEnvironment env)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _env = env;
    }


    // GET api/admin/config
    [HttpGet("config")]
    public IActionResult GetConfig()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return Forbid();

        var user = _dbContext.Users.Find(userId.Value);
        if (user == null || user.IsDeactivated)
        {
            HttpContext.Session.Clear();
            return Forbid();
        }

        if (!user.IsAdmin) return Forbid();

        var prohibit = _configuration.GetValue<bool>("ServerSettings:ProhibitGroups");
        var priv = _configuration.GetValue<bool>("ServerSettings:PrivateMode");
        var prohibitGeneral = _configuration.GetValue<bool>("ServerSettings:ProhibitGeneral");

        var httpUrl = _configuration.GetValue<string>("Kestrel:Endpoints:Http:Url");
        var httpsUrl = _configuration.GetValue<string>("Kestrel:Endpoints:Https:Url");

        return Ok(new { prohibitGroups = prohibit, privateMode = priv, prohibitGeneral, httpUrl, httpsUrl });
    }

    // PUT api/admin/config
    [HttpPut("config")]
    public IActionResult UpdateConfig([FromBody] AdminConfigRequest? request)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return Forbid();

        var user = _dbContext.Users.Find(userId.Value);
        if (user == null || user.IsDeactivated)
        {
            HttpContext.Session.Clear();
            return Forbid();
        }

        if (!user.IsAdmin) return Forbid();

        if (request == null)
            return BadRequest(new { message = "Request body required" });

        var path = Path.Combine(_env.ContentRootPath, "appsettings.json");
        if (!System.IO.File.Exists(path))
            return NotFound(new { message = "appsettings.json not found" });

        try
        {
            var text = System.IO.File.ReadAllText(path);
            var node = JsonNode.Parse(text) ?? new JsonObject();

            var serverNode = node["ServerSettings"] as JsonObject ?? new JsonObject();

            if (request.ProhibitGroups.HasValue)
                serverNode["ProhibitGroups"] = request.ProhibitGroups.Value;

            if (request.PrivateMode.HasValue)
                serverNode["PrivateMode"] = request.PrivateMode.Value;

            if (request.ProhibitGeneral.HasValue)
                serverNode["ProhibitGeneral"] = request.ProhibitGeneral.Value;

            node["ServerSettings"] = serverNode;

            // Update Kestrel endpoints if provided
            var kestrelNode = node["Kestrel"] as JsonObject ?? new JsonObject();
            var endpointsNode = kestrelNode["Endpoints"] as JsonObject ?? new JsonObject();

            if (!string.IsNullOrWhiteSpace(request.HttpUrl))
            {
                var httpNode = endpointsNode["Http"] as JsonObject ?? new JsonObject();
                httpNode["Url"] = request.HttpUrl;
                endpointsNode["Http"] = httpNode;
            }

            if (!string.IsNullOrWhiteSpace(request.HttpsUrl))
            {
                var httpsNode = endpointsNode["Https"] as JsonObject ?? new JsonObject();
                httpsNode["Url"] = request.HttpsUrl;
                endpointsNode["Https"] = httpsNode;
            }

            if (endpointsNode.Count > 0) kestrelNode["Endpoints"] = endpointsNode;
            if (kestrelNode.Count > 0) node["Kestrel"] = kestrelNode;

            var writeOpts = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            System.IO.File.WriteAllText(path, node.ToJsonString(writeOpts));

            return Ok(new { success = true, serverSettings = serverNode, kestrel = node["Kestrel"] });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to update config", error = ex.Message });
        }
    }
}
