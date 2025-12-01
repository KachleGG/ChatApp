using Chatter.Data;
using Chatter.Models.DTOs;
using Chatter.Services;
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
    private readonly Chatter.Services.BackupService? _backupService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(ChatterDbContext dbContext, IConfiguration configuration, IWebHostEnvironment env, ILogger<AdminController> logger, Chatter.Services.BackupService? backupService = null)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _env = env;
        _backupService = backupService;
        _logger = logger;
    }

    // Schedule validation endpoint
    [HttpPost("validate-schedule")]
    public IActionResult ValidateSchedule([FromBody] JsonObject? body)
    {
        _logger.LogInformation("ValidateSchedule called by session user {UserId}", HttpContext.Session.GetInt32("UserId"));
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return Forbid();
        var user = _dbContext.Users.Find(userId.Value);
        if (user == null || user.IsDeactivated) { HttpContext.Session.Clear(); return Forbid(); }
        if (!user.IsAdmin)
            return Forbid();

        var schedule = body?["schedule"]?.GetValue<string?>();
        if (string.IsNullOrWhiteSpace(schedule))
            return BadRequest(new { valid = false, message = "Empty schedule" });
        try
        {
            var next = TimingService.GetNextUtc(schedule!, DateTime.UtcNow);
            if (next == null)
            {
                _logger.LogWarning("ValidateSchedule: could not parse schedule '{Schedule}'", schedule);
                return Ok(new { valid = false, message = "Could not parse cron expression" });
            }
            _logger.LogInformation("ValidateSchedule: parsed schedule '{Schedule}', next {Next}", schedule, next.Value.ToString("o"));
            return Ok(new { valid = true, message = "OK", next = next.Value.ToString("o") });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ValidateSchedule error for schedule '{Schedule}'", schedule);
            return Ok(new { valid = false, message = ex.Message });
        }
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
        var userGroupLimit = _configuration.GetValue<int>("ServerSettings:UserGroupLimit", 5);

        var httpUrl = _configuration.GetValue<string>("Kestrel:Endpoints:Http:Url");
        var httpsUrl = _configuration.GetValue<string>("Kestrel:Endpoints:Https:Url");

        dynamic resp = new System.Dynamic.ExpandoObject();
        resp.privateMode = privateMode;
        resp.prohibitGroups = prohibitGroups;
        resp.prohibitGeneral = prohibitGeneral;
        resp.userGroupLimit = userGroupLimit;
        resp.httpUrl = httpUrl;
        resp.httpsUrl = httpsUrl;
        // Backup settings
        resp.backupEnabled = _configuration.GetValue<bool?>("ServerSettings:BackupEnabled") ?? false;
        resp.backupSchedule = _configuration.GetValue<string?>("ServerSettings:BackupSchedule") ?? string.Empty;
        resp.backupPath = _configuration.GetValue<string?>("ServerSettings:BackupPath") ?? string.Empty;
        resp.backupRetention = _configuration.GetValue<int?>("ServerSettings:BackupRetention") ?? 7;
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

            if (request.UserGroupLimit.HasValue)
                serverNode["UserGroupLimit"] = request.UserGroupLimit.Value;

            node["ServerSettings"] = serverNode;

            // Backup settings
            if (request.BackupEnabled.HasValue)
                serverNode["BackupEnabled"] = request.BackupEnabled.Value;

            if (!string.IsNullOrWhiteSpace(request.BackupSchedule))
                serverNode["BackupSchedule"] = request.BackupSchedule;

            if (!string.IsNullOrWhiteSpace(request.BackupPath))
                serverNode["BackupPath"] = request.BackupPath;

            if (request.BackupRetention.HasValue)
                serverNode["BackupRetention"] = request.BackupRetention.Value;

            // Backup settings removed from config update
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

    // -------------------------
    // Backup endpoints
    // -------------------------



    // GET api/admin/backups/status
    [HttpGet("backups/status")]
    public async Task<IActionResult> BackupStatus()
    {
        _logger.LogInformation("BackupStatus called by session user {UserId}", HttpContext.Session.GetInt32("UserId"));
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return Forbid();
        var actingUser = await _dbContext.Users.FindAsync(userId.Value);
        if (actingUser == null || actingUser.IsDeactivated) { HttpContext.Session.Clear(); return Forbid(); }
        if (!actingUser.IsAdmin)
            return Forbid();

        if (_backupService != null)
        {
            _logger.LogDebug("Using running BackupService for status");
            var s = await _backupService.GetStatusAsync();
            DateTime? next = null;
            if (!string.IsNullOrWhiteSpace(s.Schedule))
                next = TimingService.GetNextUtc(s.Schedule!, DateTime.UtcNow);
            return Ok(new { enabled = s.Enabled, schedule = s.Schedule, retention = s.Retention, lastRun = s.LastRunUtc?.ToString("o"), nextRun = next?.ToString("o") });
        }

        _logger.LogDebug("BackupService not available, reading status from configuration and disk");
        // Fallback: read from configuration and last_run.txt if present
        var enabled = _configuration.GetValue<bool?>("ServerSettings:BackupEnabled") ?? false;
        var schedule = _configuration.GetValue<string?>("ServerSettings:BackupSchedule");
        var retention = _configuration.GetValue<int?>("ServerSettings:BackupRetention") ?? 7;
        var pathFallback = _configuration.GetValue<string?>("ServerSettings:BackupPath");
        if (string.IsNullOrWhiteSpace(pathFallback)) pathFallback = System.IO.Path.Combine(AppContext.BaseDirectory, "backups");
        pathFallback = System.IO.Path.GetFullPath(pathFallback);
        DateTime? last = null;
        try
        {
            var lastFile = System.IO.Path.Combine(pathFallback, "last_run.txt");
            if (System.IO.File.Exists(lastFile))
            {
                var s = System.IO.File.ReadAllText(lastFile);
                if (DateTime.TryParseExact(s, "o", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var dtExact))
                    last = dtExact;
                else if (DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var dt))
                    last = dt;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading last_run.txt at {Path}", pathFallback);
        }
        DateTime? nextRun = null;
        if (!string.IsNullOrWhiteSpace(schedule)) nextRun = TimingService.GetNextUtc(schedule!, DateTime.UtcNow);
        return Ok(new { enabled = enabled, schedule = schedule, retention = retention, lastRun = last?.ToString("o"), nextRun = nextRun?.ToString("o") });
    }

    // GET api/admin/backups
    [HttpGet("backups")]
    public async Task<IActionResult> ListBackups()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return Forbid();
        var actingUser = await _dbContext.Users.FindAsync(userId.Value);
        if (actingUser == null || actingUser.IsDeactivated) { HttpContext.Session.Clear(); return Forbid(); }
        if (!actingUser.IsAdmin)
            return Forbid();

        string? path;
        List<string> list;

        _logger.LogInformation("ListBackups called by session user {UserId}", userId);
        if (_backupService != null)
        {
            _logger.LogDebug("Using running BackupService to list backups");
            list = await _backupService.ListBackupsAsync();
            var status = await _backupService.GetStatusAsync();
            path = status.Path;
        }
        else
        {
            _logger.LogDebug("BackupService not available, falling back to config path {Path}", _configuration.GetValue<string?>("ServerSettings:BackupPath"));
            // Fallback: read backup path from configuration so UI can show existing backups
            path = _configuration.GetValue<string?>("ServerSettings:BackupPath");
            if (string.IsNullOrWhiteSpace(path)) path = System.IO.Path.Combine(AppContext.BaseDirectory, "backups");
            path = System.IO.Path.GetFullPath(path);
            if (!Directory.Exists(path)) return Ok(new List<object>());
            list = Directory.EnumerateFiles(path, "*.zip").Select(f => System.IO.Path.GetFileName(f)).OrderByDescending(n => n).ToList();
        }

        var items = list.Select(f =>
        {
            var full = System.IO.Path.Combine(path, f);
            var info = new System.IO.FileInfo(full);
            return new
            {
                fileName = f,
                timestamp = info.Exists ? info.CreationTimeUtc.ToString("o") : (string?)null,
                size = info.Exists ? info.Length : 0
            };
        }).ToList();

        return Ok(items);
    }

    // POST api/admin/backups/create
    [HttpPost("backups/create")]
    public async Task<IActionResult> CreateBackupNow()
    {
        _logger.LogInformation("CreateBackup called by session user {UserId}", HttpContext.Session.GetInt32("UserId"));
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return Forbid();
        var actingUser = await _dbContext.Users.FindAsync(userId.Value);
        if (actingUser == null || actingUser.IsDeactivated) { HttpContext.Session.Clear(); return Forbid(); }
        if (!actingUser.IsAdmin)
            return Forbid();

        if (_backupService == null)
        {
            _logger.LogWarning("CreateBackup attempted but BackupService is not available");
            return StatusCode(503, new { message = "Backup service not available" });
        }

        try
        {
            var ok = await _backupService.CreateBackupAsync(force: true);
            if (!ok)
            {
                _logger.LogError("CreateBackup failed (service returned false)");
                return StatusCode(500, new { message = "Create backup failed" });
            }
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during CreateBackup");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // POST api/admin/backups/restore/{file}
    [HttpPost("backups/restore/{fileName}")]
    public async Task<IActionResult> RestoreBackup(string fileName)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return Forbid();
        var actingUser = await _dbContext.Users.FindAsync(userId.Value);
        if (actingUser == null || actingUser.IsDeactivated) { HttpContext.Session.Clear(); return Forbid(); }
        if (!actingUser.IsAdmin)
            return Forbid();

        if (_backupService == null)
            return StatusCode(503, new { message = "Backup service not available" });

        var ok = await _backupService.RestoreBackupAsync(fileName);
        if (!ok)
            return StatusCode(500, new { message = "Restore failed" });
        return Ok(new { success = true });
    }

    // DELETE api/admin/backups/{file}
    [HttpDelete("backups/{fileName}")]
    public async Task<IActionResult> DeleteBackup(string fileName)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return Forbid();
        var actingUser = await _dbContext.Users.FindAsync(userId.Value);
        if (actingUser == null || actingUser.IsDeactivated) { HttpContext.Session.Clear(); return Forbid(); }
        if (!actingUser.IsAdmin)
            return Forbid();

        if (_backupService == null)
            return StatusCode(503, new { message = "Backup service not available" });

        var ok = await _backupService.DeleteBackupAsync(fileName);
        if (!ok)
            return NotFound();
        return Ok(new { success = true });
    }

    // Backup endpoints removed

    // Backup status endpoint removed

    // Create backup endpoint removed

    // Download backup endpoint removed

    // Restore backup endpoint removed

    // Delete backup endpoint removed
}
