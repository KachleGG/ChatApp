using Chatter.Data;
using Chatter.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Nodes;

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
        if (userId == null)
            return Forbid();

        var user = _dbContext.Users.Find(userId.Value);
        if (user == null || user.IsDeactivated)
        {
            HttpContext.Session.Clear();
            return Forbid();
        }

        if (!user.IsAdmin)
            return Forbid();

        var privateMode = _configuration.GetValue<bool>("ServerSettings:PrivateMode");
        var prohibitGroups = _configuration.GetValue<bool>("ServerSettings:ProhibitGroups");
        var prohibitGeneral = _configuration.GetValue<bool>("ServerSettings:ProhibitGeneral");

        var httpUrl = _configuration.GetValue<string>("Kestrel:Endpoints:Http:Url");
        var httpsUrl = _configuration.GetValue<string>("Kestrel:Endpoints:Https:Url");

        dynamic resp = new System.Dynamic.ExpandoObject();
        resp.privateMode = privateMode;
        resp.prohibitGroups = prohibitGroups;
        resp.prohibitGeneral = prohibitGeneral;
        resp.httpUrl = httpUrl;
        resp.httpsUrl = httpsUrl;
        return Ok(resp);
    }

    // PUT api/admin/config
    [HttpPut("config")]
    public IActionResult UpdateConfig([FromBody] AdminConfigRequest? request)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return Forbid();

        var user = _dbContext.Users.Find(userId.Value);
        if (user == null || user.IsDeactivated)
        {
            HttpContext.Session.Clear();
            return Forbid();
        }

        if (!user.IsAdmin)
            return Forbid();

        if (request == null)
            return BadRequest(new { message = "Request body required" });

        // Determine which config file to update based on environment
        var fileName = _env.IsDevelopment() ? "appsettings.Development.json" : "appsettings.json";
        var path = Path.Combine(_env.ContentRootPath, fileName);
        if (!System.IO.File.Exists(path))
            return NotFound(new { message = $"{fileName} not found" });

        try
        {
            var text = System.IO.File.ReadAllText(path);
            var node = JsonNode.Parse(text) ?? new JsonObject();

            var serverNode = node["ServerSettings"] as JsonObject ?? new JsonObject();

            if (request.PrivateMode.HasValue)
                serverNode["PrivateMode"] = request.PrivateMode.Value;

            if (request.ProhibitGroups.HasValue)
                serverNode["ProhibitGroups"] = request.ProhibitGroups.Value;

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

            if (endpointsNode.Count > 0)
                kestrelNode["Endpoints"] = endpointsNode;
            if (kestrelNode.Count > 0)
                node["Kestrel"] = kestrelNode;

            var writeOpts = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            System.IO.File.WriteAllText(path, node.ToJsonString(writeOpts));

            return Ok(new { success = true, serverSettings = serverNode, kestrel = node["Kestrel"] });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to update config", error = ex.Message });
        }
    }

    // POST api/admin/users/{id}/promote - promote a user to admin (admin only)
    [HttpPost("users/{id}/promote")]
    public async Task<IActionResult> PromoteUser(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return Forbid();

        var actingUser = await _dbContext.Users.FindAsync(userId.Value);
        if (actingUser == null || actingUser.IsDeactivated)
        {
            HttpContext.Session.Clear();
            return Forbid();
        }

        if (!actingUser.IsAdmin)
            return Forbid();

        var user = await _dbContext.Users.FindAsync(id);
        if (user == null || user.IsDeactivated)
            return NotFound(new { message = "User not found" });

        if (user.IsAdmin)
            return Conflict(new { message = "User is already an admin" });

        user.IsAdmin = true;
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();

        dynamic resp = new System.Dynamic.ExpandoObject();
        resp.id = user.Id;
        resp.name = user.Name;
        resp.email = user.Email;
        resp.isAdmin = user.IsAdmin;
        return Ok(resp);
    }

    // POST api/admin/users/{id}/demote - demote an admin to regular user (admin only)
    [HttpPost("users/{id}/demote")]
    public async Task<IActionResult> DemoteUser(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return Forbid();

        var actingUser = await _dbContext.Users.FindAsync(userId.Value);
        if (actingUser == null || actingUser.IsDeactivated)
        {
            HttpContext.Session.Clear();
            return Forbid();
        }

        if (!actingUser.IsAdmin)
            return Forbid();

        // Protect the owner/admin account with Id == 1 from being demoted
        if (id == 1)
            return BadRequest(new { message = "The owner account cannot be demoted." });

        var user = await _dbContext.Users.FindAsync(id);
        if (user == null || user.IsDeactivated)
            return NotFound(new { message = "User not found" });

        if (!user.IsAdmin)
            return Conflict(new { message = "User is not an admin" });

        user.IsAdmin = false;
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();

        dynamic resp = new System.Dynamic.ExpandoObject();
        resp.id = user.Id;
        resp.name = user.Name;
        resp.email = user.Email;
        resp.isAdmin = user.IsAdmin;
        return Ok(resp);
    }

    // GET api/admin/users - list users (admin only)
    [HttpGet("users")]
    public async Task<IActionResult> ListUsers()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return Forbid();

        var actingUser = await _dbContext.Users.FindAsync(userId.Value);
        if (actingUser == null || actingUser.IsDeactivated)
        {
            HttpContext.Session.Clear();
            return Forbid();
        }

        if (!actingUser.IsAdmin)
            return Forbid();

        var users = await _dbContext.Users
            .OrderBy(u => u.Id)
            .Select(u => new
            {
                id = u.Id,
                name = u.Name,
                email = u.Email,
                isAdmin = u.IsAdmin,
                isDeactivated = u.IsDeactivated
            })
            .ToListAsync();

        return Ok(users);
    }
}
