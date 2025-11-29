using Chatter.Data;
using Chatter.Helpers;
using Chatter.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chatter.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly ChatterDbContext _dbContext;

    public AuthController(ChatterDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // GET api/auth/check
    // Returns the current authentication state and user info if authenticated
    [HttpGet("check")]
    public async Task<IActionResult> Check()
    {
        var userId = HttpContext.Session.GetInt32("UserId");

        if (userId == null)
        {
            dynamic noAuth = new System.Dynamic.ExpandoObject();
            noAuth.authenticated = false;
            return Ok(noAuth);
        }

        var user = await _dbContext.Users.FindAsync(userId.Value);

        if (user == null || user.IsDeactivated)
        {
            // User no longer exists or is deactivated - clear session
            HttpContext.Session.Clear();
            dynamic noAuth = new System.Dynamic.ExpandoObject();
            noAuth.authenticated = false;
            return Ok(noAuth);
        }

        dynamic resp = new System.Dynamic.ExpandoObject();
        resp.authenticated = true;
        dynamic u = new System.Dynamic.ExpandoObject();
        u.id = user.Id;
        u.name = user.Name;
        u.email = user.Email;
        u.isAdmin = user.IsAdmin;
        resp.user = u;
        return Ok(resp);
    }

    // POST api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest? request)
    {
        // Validate request
        if (request == null)
        {
            return BadRequest("Login credentials must be provided.");
        }

        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return BadRequest("Username/Email is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Password is required.");
        }

        // Find user by email or username (name field)
        var sanitizedInput = request.Username.Trim();
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == sanitizedInput || u.Name == sanitizedInput);

        if (user == null)
        {
            // Don't reveal whether user exists or not (security best practice)
            return Unauthorized(new { message = "Invalid username/email or password." });
        }

        // Check if account is deactivated
        if (user.IsDeactivated)
        {
            return Unauthorized(new { message = "This account has been deactivated." });
        }

        // Verify password
        if (!PasswordHasher.VerifyPassword(request.Password, user.Password))
        {
            return Unauthorized(new { message = "Invalid username/email or password." });
        }

        // Create session
        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("UserEmail", user.Email);
        HttpContext.Session.SetString("UserName", user.Name);

        // Return success with user info (without password)
        dynamic resp = new System.Dynamic.ExpandoObject();
        resp.message = "Login successful";
        dynamic u = new System.Dynamic.ExpandoObject();
        u.id = user.Id;
        u.name = user.Name;
        u.email = user.Email;
        u.isAdmin = user.IsAdmin;
        resp.user = u;
        return Ok(resp);
    }

    // POST api/auth/logout
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // Check if user is logged in
        var userId = HttpContext.Session.GetInt32("UserId");

        if (userId == null)
        {
            return BadRequest(new { message = "No active session found." });
        }

        // Clear session
        HttpContext.Session.Clear();

        return Ok(new { message = "Logout successful" });
    }
}