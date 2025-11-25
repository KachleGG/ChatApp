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

    public MessagesController(ChatterDbContext dbContext)
    {
        _dbContext = dbContext;
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
            SentAt = DateTime.UtcNow
        };

        _dbContext.Messages.Add(message);
        await _dbContext.SaveChangesAsync();

        return Ok(new { id = message.Id, text = message.Text, sentFrom = new { id = user.Id, name = user.Name }, sentAt = message.SentAt });
    }

    // GET api/messages?limit=20&beforeId=123
    // Returns latest messages ordered desc by Id (newest first). For infinite scroll, pass beforeId to load older messages.
    [HttpGet]
    public async Task<IActionResult> GetMessages([FromQuery] int limit = 20, [FromQuery] int? beforeId = null)
    {
        limit = Math.Clamp(limit, 1, 100);

        var query = _dbContext.Messages
            .Include(m => m.SentFrom)
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
                sentAt = m.SentAt
            })
            .ToList();

        return Ok(result);
    }
}