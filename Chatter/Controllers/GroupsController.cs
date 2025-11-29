using Chatter.Data;
using Chatter.Models;
using Chatter.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;

namespace Chatter.Controllers;

[Route("api/[controller]")]
[ApiController]
public class GroupsController : ControllerBase
{
    private readonly ChatterDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public GroupsController(ChatterDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    // GET api/groups - Get all active groups (excludes deactivated)
    [HttpGet]
    public async Task<IActionResult> GetGroups()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return Unauthorized(new { message = "Not authenticated" });

        var user = await _dbContext.Users.FindAsync(userId.Value);
        if (user == null || user.IsDeactivated)
        {
            HttpContext.Session.Clear();
            return Unauthorized(new { message = "User not found or deactivated" });
        }

        var prohibitGeneral = _configuration.GetValue<bool>("ServerSettings:ProhibitGeneral");

        // Find group ids where the user is a member
        var memberGroupIds = await _dbContext.GroupMemberships
            .Where(m => m.UserId == userId.Value)
            .Select(m => m.GroupId)
            .ToListAsync();

        // Load groups that the user should see:
        // - General (id==1) only when it's not prohibited
        // - For non-General groups, include if the user owns them or is a member
        var groupEntities = await _dbContext.Groups
            .Include(g => g.Owner)
            .Where(g => !g.IsDeactivated && (
                (g.Id == 1 && !prohibitGeneral) ||
                (g.Id != 1 && (g.OwnerId == userId.Value || memberGroupIds.Contains(g.Id)))
            ))
            .ToListAsync();

        var groups = new List<object>();
        foreach (var g in groupEntities)
        {
            dynamic item = new System.Dynamic.ExpandoObject();
            item.id = g.Id;
            item.name = g.Name;
            item.ownerId = g.OwnerId;
            item.ownerName = g.Owner?.Name;
            item.createdAt = g.CreatedAt;
            item.isDeactivated = g.IsDeactivated;
            item.code = (g.OwnerId == userId.Value || user.IsAdmin) ? g.Code : null;
            groups.Add(item);
        }

        return Ok(groups);
    }

    // GET api/groups/{id} - Get specific group
    [HttpGet("{id}")]
    public async Task<IActionResult> GetGroup(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return Unauthorized(new { message = "Not authenticated" });

        var user = await _dbContext.Users.FindAsync(userId.Value);
        if (user == null || user.IsDeactivated)
        {
            HttpContext.Session.Clear();
            return Unauthorized(new { message = "User not found or deactivated" });
        }

        var groupEntity = await _dbContext.Groups
            .Include(g => g.Owner)
            .Where(g => g.Id == id)
            .FirstOrDefaultAsync();

        if (groupEntity == null || groupEntity.IsDeactivated)
            return NotFound(new { message = "Group not found" });

        var prohibitGeneral = _configuration.GetValue<bool>("ServerSettings:ProhibitGeneral");

        // Check access. General group (id==1) is only visible when not prohibited.
        var isOwner = groupEntity.OwnerId == userId.Value;
        var isAdmin = user.IsAdmin;
        var isMember = await _dbContext.GroupMemberships.AnyAsync(m => m.GroupId == id && m.UserId == userId.Value);

        if (groupEntity.Id == 1)
        {
            if (prohibitGeneral)
            {
                // Hide General entirely when prohibited
                return NotFound(new { message = "Group not found" });
            }
            // Otherwise General is visible to any authenticated user
        }
        else
        {
            if (!(isOwner || isAdmin || isMember))
            {
                // Hide group from users who are not members/owners/admins
                return NotFound(new { message = "Group not found" });
            }
        }

        dynamic resp = new System.Dynamic.ExpandoObject();
        resp.id = groupEntity.Id;
        resp.name = groupEntity.Name;
        resp.ownerId = groupEntity.OwnerId;
        resp.ownerName = groupEntity.Owner?.Name;
        resp.createdAt = groupEntity.CreatedAt;
        resp.isDeactivated = groupEntity.IsDeactivated;
        resp.code = (isOwner || isAdmin) ? groupEntity.Code : null;
        return Ok(resp);
    }

    // POST api/groups - Create a new group
    [HttpPost]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest? request)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return Unauthorized(new { message = "Not authenticated" });

        var user = await _dbContext.Users.FindAsync(userId.Value);
        if (user == null || user.IsDeactivated)
        {
            HttpContext.Session.Clear();
            return Unauthorized(new { message = "User not found or deactivated" });
        }

        // Enforce per-user group limit (exclude deactivated groups) - skip for admins
        var userGroupLimit = _configuration.GetValue<int>("ServerSettings:UserGroupLimit", 5);
        if (!user.IsAdmin)
        {
            var ownedCount = await _dbContext.Groups.CountAsync(g => g.OwnerId == userId.Value && !g.IsDeactivated);
            if (ownedCount >= userGroupLimit)
            {
                return BadRequest(new { message = $"User group limit reached ({userGroupLimit})" });
            }
        }

        if (request == null || string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Group name is required" });

        if (request.Name.Length > 100)
            return BadRequest(new { message = "Group name must be 100 characters or less" });

        var newGroup = new Group
        {
            Name = request.Name.Trim(),
            OwnerId = userId.Value,
            CreatedAt = DateTime.UtcNow,
            IsDeactivated = false
        };

        _dbContext.Groups.Add(newGroup);
        await _dbContext.SaveChangesAsync();

        // Add owner as a member of the group
        try
        {
            _dbContext.GroupMemberships.Add(new GroupMembership { GroupId = newGroup.Id, UserId = userId.Value, JoinedAt = DateTime.UtcNow });
            await _dbContext.SaveChangesAsync();
        }
        catch
        {
            // membership add is best-effort; ignore failures here
        }

        dynamic created = new System.Dynamic.ExpandoObject();
        created.id = newGroup.Id;
        created.name = newGroup.Name;
        created.ownerId = newGroup.OwnerId;
        created.ownerName = user.Name;
        created.createdAt = newGroup.CreatedAt;
        created.isDeactivated = newGroup.IsDeactivated;
        return CreatedAtAction(nameof(GetGroup), new { id = newGroup.Id }, created);
    }

    // POST api/groups/{id}/code - generate a join code for the group (owner or admin)
    [HttpPost("{id}/code")]
    public async Task<IActionResult> GenerateCode(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return Unauthorized(new { message = "Not authenticated" });

        var user = await _dbContext.Users.FindAsync(userId.Value);
        if (user == null || user.IsDeactivated)
        {
            HttpContext.Session.Clear();
            return Unauthorized(new { message = "User not found or deactivated" });
        }

        var group = await _dbContext.Groups.FindAsync(id);
        if (group == null)
            return NotFound(new { message = "Group not found" });

        if (id == 1)
            return BadRequest(new { message = "Cannot generate code for General group" });

        if (group.OwnerId != userId.Value && !user.IsAdmin)
            return Forbid();

        // Generate unique code (retry few times on collision)
        string code = string.Empty;
        for (int attempt = 0; attempt < 6; attempt++)
        {
            code = GenerateJoinCode(8);
            var exists = await _dbContext.Groups.AnyAsync(g => g.Code == code);
            if (!exists) break;
        }

        group.Code = code;
        group.CodeGeneratedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        dynamic codeResp = new System.Dynamic.ExpandoObject();
        codeResp.code = group.Code;
        codeResp.generatedAt = group.CodeGeneratedAt;
        return Ok(codeResp);
    }

    // DELETE api/groups/{id}/code - revoke join code (owner or admin)
    [HttpDelete("{id}/code")]
    public async Task<IActionResult> RevokeCode(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return Unauthorized(new { message = "Not authenticated" });

        var user = await _dbContext.Users.FindAsync(userId.Value);
        if (user == null || user.IsDeactivated)
        {
            HttpContext.Session.Clear();
            return Unauthorized(new { message = "User not found or deactivated" });
        }

        var group = await _dbContext.Groups.FindAsync(id);
        if (group == null)
            return NotFound(new { message = "Group not found" });

        if (id == 1)
            return BadRequest(new { message = "Cannot revoke code for General group" });

        if (group.OwnerId != userId.Value && !user.IsAdmin)
            return Forbid();

        group.Code = null;
        group.CodeGeneratedAt = null;
        await _dbContext.SaveChangesAsync();

        dynamic revoked = new System.Dynamic.ExpandoObject();
        revoked.message = "Code revoked";
        return Ok(revoked);
    }

    // POST api/groups/join - join a group by code
    [HttpPost("join")]
    public async Task<IActionResult> JoinByCode([FromBody] Models.DTOs.JoinGroupRequest? request)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return Unauthorized(new { message = "Not authenticated" });

        var user = await _dbContext.Users.FindAsync(userId.Value);
        if (user == null || user.IsDeactivated)
        {
            HttpContext.Session.Clear();
            return Unauthorized(new { message = "User not found or deactivated" });
        }

        if (request == null || string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(new { message = "Code is required" });

        var code = request.Code.Trim().ToUpperInvariant();
        var group = await _dbContext.Groups.FirstOrDefaultAsync(g => g.Code == code && !g.IsDeactivated);
        if (group == null)
            return NotFound(new { message = "Group with provided code not found" });

        if (group.Id == 1)
            return BadRequest(new { message = "Cannot join General group by code" });

        // Check existing membership
        var existing = await _dbContext.GroupMemberships.FirstOrDefaultAsync(gm => gm.GroupId == group.Id && gm.UserId == userId.Value);
        if (existing != null)
        {
            dynamic already = new System.Dynamic.ExpandoObject();
            already.message = "Already a member";
            already.id = group.Id;
            already.name = group.Name;
            return Ok(already);
        }

        var membership = new GroupMembership { GroupId = group.Id, UserId = userId.Value, JoinedAt = DateTime.UtcNow };
        _dbContext.GroupMemberships.Add(membership);
        await _dbContext.SaveChangesAsync();

        dynamic joinResp = new System.Dynamic.ExpandoObject();
        joinResp.id = group.Id;
        joinResp.name = group.Name;
        joinResp.ownerId = group.OwnerId;
        return Ok(joinResp);
    }

    private static string GenerateJoinCode(int length = 8)
    {
        const string chars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789"; // avoid ambiguous characters
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rng.GetBytes(bytes);
        var result = new char[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = chars[bytes[i] % chars.Length];
        }
        return new string(result);
    }

    // PUT api/groups/{id} - Update group (name or deactivate)
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateGroup(int id, [FromBody] UpdateGroupRequest? request)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return Unauthorized(new { message = "Not authenticated" });

        var user = await _dbContext.Users.FindAsync(userId.Value);
        if (user == null || user.IsDeactivated)
        {
            HttpContext.Session.Clear();
            return Unauthorized(new { message = "User not found or deactivated" });
        }

        if (request == null)
            return BadRequest(new { message = "Request body required" });

        var group = await _dbContext.Groups.FindAsync(id);
        if (group == null)
            return NotFound(new { message = "Group not found" });

        // Cannot modify General group
        if (id == 1)
            return BadRequest(new { message = "Cannot modify General group" });

        // Only owner or admin can update group
        if (group.OwnerId != userId.Value && !user.IsAdmin)
            return Forbid();

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            if (request.Name.Length > 100)
                return BadRequest(new { message = "Group name must be 100 characters or less" });
            group.Name = request.Name.Trim();
        }

        if (request.IsDeactivated.HasValue)
        {
            group.IsDeactivated = request.IsDeactivated.Value;
        }

        await _dbContext.SaveChangesAsync();

        dynamic upd = new System.Dynamic.ExpandoObject();
        upd.id = group.Id;
        upd.name = group.Name;
        upd.ownerId = group.OwnerId;
        upd.ownerName = user.Name;
        upd.createdAt = group.CreatedAt;
        upd.isDeactivated = group.IsDeactivated;
        return Ok(upd);
    }

    // DELETE api/groups/{id} - Deactivate group
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGroup(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return Unauthorized(new { message = "Not authenticated" });

        var user = await _dbContext.Users.FindAsync(userId.Value);
        if (user == null || user.IsDeactivated)
        {
            HttpContext.Session.Clear();
            return Unauthorized(new { message = "User not found or deactivated" });
        }

        var group = await _dbContext.Groups.FindAsync(id);
        if (group == null)
            return NotFound(new { message = "Group not found" });

        // Cannot delete General group
        if (id == 1)
            return BadRequest(new { message = "Cannot delete General group" });

        // Only owner or admin can delete group
        if (group.OwnerId != userId.Value && !user.IsAdmin)
            return Forbid();

        group.IsDeactivated = true;
        await _dbContext.SaveChangesAsync();

        dynamic del = new System.Dynamic.ExpandoObject();
        del.message = "Group deactivated successfully";
        return Ok(del);
    }

    // POST api/groups/{id}/leave - leave a group (member request)
    [HttpPost("{id}/leave")]
    public async Task<IActionResult> LeaveGroup(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return Unauthorized(new { message = "Not authenticated" });

        var user = await _dbContext.Users.FindAsync(userId.Value);
        if (user == null || user.IsDeactivated)
        {
            HttpContext.Session.Clear();
            return Unauthorized(new { message = "User not found or deactivated" });
        }

        var group = await _dbContext.Groups.FindAsync(id);
        if (group == null || group.IsDeactivated)
            return NotFound(new { message = "Group not found" });

        if (id == 1)
            return BadRequest(new { message = "Cannot leave General group" });

        var membership = await _dbContext.GroupMemberships.FirstOrDefaultAsync(gm => gm.GroupId == id && gm.UserId == userId.Value);
        if (membership == null)
            return BadRequest(new { message = "You are not a member of this group" });

        // If the leaving user is the owner, attempt to transfer ownership to the next joined member
        if (group.OwnerId == userId.Value)
        {
            var otherMembers = await _dbContext.GroupMemberships
                .Where(gm => gm.GroupId == id && gm.UserId != userId.Value)
                .OrderBy(gm => gm.JoinedAt)
                .ToListAsync();

            if (otherMembers.Count == 0)
            {
                return BadRequest(new { message = "Owner cannot leave the group. Transfer ownership or deactivate the group first." });
            }

            var newOwnerId = otherMembers.First().UserId;
            group.OwnerId = newOwnerId;
            // Keep the new owner as a member; we'll remove the current owner's membership below.
        }

        _dbContext.GroupMemberships.Remove(membership);
        await _dbContext.SaveChangesAsync();

        dynamic left = new System.Dynamic.ExpandoObject();
        left.message = "Left group";
        left.id = group.Id;
        left.ownerId = group.OwnerId;
        return Ok(left);
    }
}

