using Chatter.Data;
using Chatter.Helpers;
using Chatter.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chatter.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MessagesController : ControllerBase
{
    private readonly ChatterDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public MessagesController(ChatterDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    [HttpPost]
    public async Task<IActionResult> PostMessage([FromBody] MessageRequest request)
    {
        if (request == null)
        {
            return BadRequest("Request data must be provided.");
        }

        if (!Validator.IsValidMessage(request.Message))
        {
            return BadRequest("Message must be provided.");
        }

        // Check if posting to General group (Id = 1)
        if (request.GroupId == 1)
        {
            var prohibitGeneral = _configuration.GetValue<bool>("ServerSettings:ProhibitGeneral");
            if (prohibitGeneral)
            {
                return Forbid("The general chat is currently prohibited.");
            }
        }

        // Verify group exists and is not deactivated
        var group = await _dbContext.Groups.FindAsync(request.GroupId);
        if (group == null)
        {
            return NotFound("Group not found.");
        }

        if (group.IsDeactivated)
        {
            return BadRequest("This group is deactivated.");
        }

        if (!Validator.IsValidUserId(request.UserId))
        {
            return BadRequest("Valid UserId must be provided.");
        }

        var user = await _dbContext.Users.FindAsync(request.UserId);
        if (user == null)
        {
            return NotFound("User not found.");
        }

        var message = new Models.Message
        {
            Text = request.Message,
            SentFrom = user,
            SentAt = DateTime.UtcNow,
            GroupId = request.GroupId
        };

        _dbContext.Messages.Add(message);
        await _dbContext.SaveChangesAsync();

        return Ok(new { id = message.Id, text = message.Text, sentFrom = new { id = user.Id, name = user.Name }, sentAt = message.SentAt, groupId = message.GroupId });
    }

    // GET api/messages?limit=20&beforeId=123&groupId=1
    // Returns latest messages for a specific group ordered desc by Id (newest first). For infinite scroll, pass beforeId to load older messages.
    [HttpGet]
    public async Task<IActionResult> GetMessages([FromQuery] int limit = 20, [FromQuery] int? beforeId = null, [FromQuery] int groupId = 1)
    {
        limit = Math.Clamp(limit, 1, 100);

        // Verify group exists
        var group = await _dbContext.Groups.FindAsync(groupId);
        if (group == null)
        {
            return NotFound("Group not found.");
        }

        if (group.IsDeactivated)
        {
            return BadRequest("This group is deactivated.");
        }

        var query = _dbContext.Messages
            .Include(m => m.SentFrom)
            .Where(m => m.GroupId == groupId)
            .OrderByDescending(m => m.Id)
            .AsQueryable();

        if (beforeId.HasValue && beforeId.Value > 0)
        {
            query = query.Where(m => m.Id < beforeId.Value);
        }

        var messages = await query.Take(limit).ToListAsync();

        // Return in chronological order (oldest first) for client display when appending older messages
        var result = messages
            .OrderBy(m => m.Id)
            .Select(m => new
            {
                id = m.Id,
                text = m.Text,
                sentFrom = new { id = m.SentFrom.Id, name = m.SentFrom.Name },
                sentAt = m.SentAt,
                groupId = m.GroupId
            })
            .ToList();

        return Ok(result);
    }
}