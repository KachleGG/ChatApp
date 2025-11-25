using Chatter.Data;
using Chatter.Helpers;
using Chatter.Models;
using Chatter.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chatter.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly ChatterDbContext _dbContext;

    public UsersController(ChatterDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        if (request == null)
        {
            return BadRequest("User data must be provided.");
        }

        if (!Validator.IsValidName(request.Name))
        {
            return BadRequest("Name is required.");
        }

        if (!Validator.IsValidEmail(request.Email))
        {
            return BadRequest("Invalid email format.");
        }

        if (!Validator.IsValidPassword(request.Password))
        {
            return BadRequest("Password must be at least 6 characters long.");
        }

        // Sanitize inputs
        var sanitizedEmail = Validator.SanitizeEmail(request.Email);
        var sanitizedName = Validator.SanitizeName(request.Name);

        // Check if email already exists
        var existingUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == sanitizedEmail);

        if (existingUser != null)
        {
            return Conflict("A user with this email already exists.");
        }

        var hashedPassword = PasswordHasher.HashPassword(request.Password);

        // Determine if this is the first user -> make admin
        var anyUsers = await _dbContext.Users.AnyAsync();
        var isAdmin = !anyUsers; // first user becomes admin

        // Create the user
        var user = new User
        {
            Name = sanitizedName,
            Email = sanitizedEmail,
            Password = hashedPassword,
            IsAdmin = isAdmin,
            IsDeactivated = false
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Return created user (without password)
        return CreatedAtAction(nameof(Create), new { id = user.Id }, new
        {
            id = user.Id,
            name = user.Name,
            email = user.Email,
            isAdmin = user.IsAdmin,
            isDeactivated = user.IsDeactivated
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request)
    {
        // Authenticate: user must be logged in
        var sessionUserId = HttpContext.Session.GetInt32("UserId");
        if (sessionUserId == null || sessionUserId.Value != id)
        {
            return Forbid();
        }

        if (request == null) return BadRequest("User data must be provided.");

        if (!Validator.IsValidName(request.Name))
        {
            return BadRequest("Name is required.");
        }

        if (!Validator.IsValidEmail(request.Email))
        {
            return BadRequest("Invalid email format.");
        }

        var user = await _dbContext.Users.FindAsync(id);
        if (user == null) return NotFound("User not found.");

        var sanitizedEmail = Validator.SanitizeEmail(request.Email);
        var sanitizedName = Validator.SanitizeName(request.Name);

        // If email changed, ensure uniqueness
        if (!string.Equals(user.Email, sanitizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == sanitizedEmail);
            if (existing != null) return Conflict("A user with this email already exists.");
            user.Email = sanitizedEmail;
        }

        user.Name = sanitizedName;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            // If the user is attempting to change password, require CurrentPassword and verify it
            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
            {
                return BadRequest("Current password must be provided to change password.");
            }

            // Verify the provided current password matches stored password
            if (!PasswordHasher.VerifyPassword(request.CurrentPassword, user.Password))
            {
                return Unauthorized("Current password is incorrect.");
            }

            if (!Validator.IsValidPassword(request.Password))
            {
                return BadRequest("Password must be at least 6 characters long.");
            }

            user.Password = PasswordHasher.HashPassword(request.Password);
        }

        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            id = user.Id,
            name = user.Name,
            email = user.Email,
            isAdmin = user.IsAdmin,
            isDeactivated = user.IsDeactivated
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        // Authenticate: user must be logged in
        var sessionUserId = HttpContext.Session.GetInt32("UserId");
        if (sessionUserId == null || sessionUserId.Value != id)
        {
            return Forbid();
        }

        var user = await _dbContext.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.IsDeactivated = true;
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();

        // Clear session on delete
        HttpContext.Session.Clear();

        return Ok(new { message = "Account deactivated." });
    }
}
