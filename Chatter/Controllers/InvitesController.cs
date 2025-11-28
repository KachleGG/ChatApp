using Chatter.Data;
using Chatter.Models;
using Chatter.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;

namespace Chatter.Controllers;

[Route("api/[controller]")]
[ApiController]
public class InvitesController : ControllerBase
{
    private readonly ChatterDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public InvitesController(ChatterDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    // Simple in-memory IP-based rate limiter for invite validation endpoints.
    // Keyed by client IP. Limits attempts within a time window.
    private static readonly ConcurrentDictionary<string, (int Attempts, DateTime WindowStart)> _inviteRate = new();
    private const int INVITE_MAX_ATTEMPTS = 6; // max attempts
    private static readonly TimeSpan INVITE_WINDOW = TimeSpan.FromMinutes(10);

    private bool IsRateLimited(string ip)
    {
        var now = DateTime.UtcNow;
        var entry = _inviteRate.GetOrAdd(ip, (0, now));
        if (now - entry.WindowStart > INVITE_WINDOW)
        {
            // reset window
            _inviteRate[ip] = (1, now);
            return false;
        }

        if (entry.Attempts >= INVITE_MAX_ATTEMPTS)
        {
            return true;
        }

        _inviteRate[ip] = (entry.Attempts + 1, entry.WindowStart);
        return false;
    }

    // POST api/invites - create an invite (admin only)
    [HttpPost]
    public async Task<IActionResult> CreateInvite([FromBody] CreateInviteRequest? request)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return Unauthorized(new { message = "Not authenticated" });

        var user = await _dbContext.Users.FindAsync(userId.Value);
        if (user == null || user.IsDeactivated) { HttpContext.Session.Clear(); return Unauthorized(new { message = "User not found or deactivated" }); }

        if (!user.IsAdmin)
            return Forbid();

        // Invites are only useful when private mode is enabled
        var privateMode = _configuration.GetValue<bool>("ServerSettings:PrivateMode");
        if (!privateMode)
            return BadRequest(new { message = "Server is not in private mode; invites are disabled." });

        if (request == null) return BadRequest(new { message = "Request body required" });

        var maxUses = Math.Max(0, request.MaxUses);
        DateTime? expires = null;
        if (request.ExpiresInSeconds.HasValue && request.ExpiresInSeconds.Value > 0)
        {
            expires = DateTime.UtcNow.AddSeconds(request.ExpiresInSeconds.Value);
        }

        string code = string.Empty;
        for (int attempt = 0; attempt < 8; attempt++)
        {
            code = GenerateInviteCode(10);
            var exists = await _dbContext.Invites.AnyAsync(i => i.Code == code);
            if (!exists) break;
        }

        var invite = new Invite
        {
            Code = code,
            CreatedByUserId = userId.Value,
            CreatedAt = DateTime.UtcNow,
            MaxUses = maxUses,
            UsesCount = 0,
            ExpiresAt = expires,
            IsRevoked = false,
            Note = request.Note?.Trim()
        };

        _dbContext.Invites.Add(invite);
        await _dbContext.SaveChangesAsync();

        return Ok(new { code = invite.Code, maxUses = invite.MaxUses, expiresAt = invite.ExpiresAt });
    }

    // GET api/invites - list invites (admin only)
    [HttpGet]
    public async Task<IActionResult> ListInvites()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return Unauthorized(new { message = "Not authenticated" });

        var user = await _dbContext.Users.FindAsync(userId.Value);
        if (user == null || user.IsDeactivated) { HttpContext.Session.Clear(); return Unauthorized(new { message = "User not found or deactivated" }); }
        if (!user.IsAdmin) return Forbid();

        var invites = await _dbContext.Invites
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new { i.Id, i.Code, i.MaxUses, i.UsesCount, i.ExpiresAt, i.IsRevoked, i.CreatedAt, i.Note })
            .ToListAsync();

        return Ok(invites);
    }

    // GET api/invites/{code} - get invite metadata (admin or used for validation)
    [HttpGet("{code}")]
    public async Task<IActionResult> GetInvite(string code)
    {
        // Rate limit by client IP to prevent brute-force attempts
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (IsRateLimited(ip))
        {
            return StatusCode(429, new { message = "Too many invite validation attempts, try again later." });
        }

        if (string.IsNullOrWhiteSpace(code)) return BadRequest(new { message = "Code required" });
        var invite = await _dbContext.Invites.SingleOrDefaultAsync(i => i.Code == code.ToUpperInvariant());
        if (invite == null) return NotFound(new { message = "Invite not found" });

        return Ok(new { invite.Id, invite.Code, invite.MaxUses, invite.UsesCount, invite.ExpiresAt, invite.IsRevoked, invite.CreatedAt, invite.Note });
    }

    // POST api/invites/{code}/revoke - revoke an invite (admin only)
    [HttpPost("{code}/revoke")]
    public async Task<IActionResult> RevokeInvite(string code)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return Unauthorized(new { message = "Not authenticated" });

        var user = await _dbContext.Users.FindAsync(userId.Value);
        if (user == null || user.IsDeactivated) { HttpContext.Session.Clear(); return Unauthorized(new { message = "User not found or deactivated" }); }
        if (!user.IsAdmin) return Forbid();

        var invite = await _dbContext.Invites.SingleOrDefaultAsync(i => i.Code == code.ToUpperInvariant());
        if (invite == null) return NotFound(new { message = "Invite not found" });

        invite.IsRevoked = true;
        _dbContext.Invites.Update(invite);
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Invite revoked" });
    }

    private static string GenerateInviteCode(int length = 10)
    {
        const string chars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rng.GetBytes(bytes);
        var result = new char[length];
        for (int i = 0; i < length; i++) result[i] = chars[bytes[i] % chars.Length];
        return new string(result);
    }
}
