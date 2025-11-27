using Chatter.Data;
using Chatter.Models;
using Chatter.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
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

        var groups = await _dbContext.Groups
            .Include(g => g.Owner)
            .Where(g => !g.IsDeactivated)
            .Select(g => new
            {
                g.Id,
                g.Name,
                g.OwnerId,
                OwnerName = g.Owner.Name,
                g.CreatedAt,
                g.IsDeactivated
            })
            .ToListAsync();

        // Filter out General group if prohibited
        if (prohibitGeneral)
        {
            groups = groups.Where(g => g.Id != 1).ToList();
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

        var group = await _dbContext.Groups
            .Include(g => g.Owner)
            .Where(g => g.Id == id)
            .Select(g => new
            {
                g.Id,
                g.Name,
                g.OwnerId,
                OwnerName = g.Owner.Name,
                g.CreatedAt,
                g.IsDeactivated
            })
            .FirstOrDefaultAsync();

        if (group == null)
            return NotFound(new { message = "Group not found" });

        return Ok(group);
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

        return CreatedAtAction(nameof(GetGroup), new { id = newGroup.Id }, new
        {
            newGroup.Id,
            newGroup.Name,
            newGroup.OwnerId,
            OwnerName = user.Name,
            newGroup.CreatedAt,
            newGroup.IsDeactivated
        });
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

        return Ok(new
        {
            group.Id,
            group.Name,
            group.OwnerId,
            OwnerName = user.Name,
            group.CreatedAt,
            group.IsDeactivated
        });
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

        return Ok(new { message = "Group deactivated successfully" });
    }
}

