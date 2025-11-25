using Chatter.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chatter.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AdminController : ControllerBase
{
    private readonly ChatterDbContext _dbContext;

    public AdminController(ChatterDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // GET api/admin
    // Placeholder endpoint for future admin APIs. Requires an authenticated admin session.
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return Forbid();

        var user = await _dbContext.Users.FindAsync(userId.Value);
        if (user == null || user.IsDeactivated)
        {
            HttpContext.Session.Clear();
            return Forbid();
        }

        if (!user.IsAdmin) return Forbid();

        return Ok(new { message = "Admin endpoint placeholder" });
    }
}
