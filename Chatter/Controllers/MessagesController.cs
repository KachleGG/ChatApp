using Chatter.Data;
using Chatter.Helpers;
using Chatter.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Chatter.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MessagesController : ControllerBase
{
    private readonly ChatterDbContext _dbContext;

    public MessagesController(ChatterDbContext dbContext) {
        _dbContext = dbContext;
    }

    [HttpPost]
    public async Task<IActionResult> PostMessage([FromBody] MessageRequest request) {
        if (request == null) {
            return BadRequest("Request data must be provided.");
        }

        if (!Validator.IsValidMessage(request.Message)) {
            return BadRequest("Message must be provided.");
        }

        if (!Validator.IsValidUserId(request.UserId)) {
            return BadRequest("Valid UserId must be provided.");
        }

        var user = await _dbContext.Users.FindAsync(request.UserId);
        if (user == null) {
            return NotFound("User not found.");
        }

        _dbContext.Messages.Add(new Models.Message
        {
            Text = request.Message,
            SentFrom = user
        });

        await _dbContext.SaveChangesAsync();

        return Ok();
    }
}