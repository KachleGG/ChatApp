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

    public UsersController(ChatterDbContext dbContext) {
        _dbContext = dbContext;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request) {
        if (request == null) {
            return BadRequest("User data must be provided.");
        }

        if (!Validator.IsValidName(request.Name)) {
            return BadRequest("Name is required.");
        }

        if (!Validator.IsValidEmail(request.Email)) {
            return BadRequest("Invalid email format.");
        }

        if (!Validator.IsValidPassword(request.Password)) {
            return BadRequest("Password must be at least 6 characters long.");
        }

        // Sanitize inputs
        var sanitizedEmail = Validator.SanitizeEmail(request.Email);
        var sanitizedName = Validator.SanitizeName(request.Name);

        // Check if email already exists
        var existingUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == sanitizedEmail);

        if (existingUser != null) {
            return Conflict("A user with this email already exists.");
        }

        var hashedPassword = PasswordHasher.HashPassword(request.Password);

        // Create the user
        var user = new User
        {
            Name = sanitizedName,
            Email = sanitizedEmail,
            Password = hashedPassword,
            IsAdmin = false,
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

    [HttpPut]
    public void Update() {
        // TODO: Implement user update
    }

    [HttpDelete]
    public void Delete() {
        // TODO: Implement user deletion
    }
}
