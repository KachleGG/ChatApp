using Chatter.Data;
using Chatter.Helpers;
using Chatter.Models;
using Chatter.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Chatter.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly ChatterDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public UsersController(ChatterDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
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

        // If server is in private mode, require an invite code and consume it atomically
        var privateMode = _configuration.GetValue<bool>("ServerSettings:PrivateMode");

        if (privateMode)
        {
            if (string.IsNullOrWhiteSpace(request.InviteCode))
            {
                return Forbid("Registration is by invite only. Provide a valid invite code.");
            }

            // Try to create user and consume invite within a transaction; handle concurrency on invite usage
            var maxAttempts = 4;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                using var tx = await _dbContext.Database.BeginTransactionAsync();
                try
                {
                    var code = request.InviteCode!.Trim().ToUpperInvariant();
                    var invite = await _dbContext.Invites.SingleOrDefaultAsync(i => i.Code == code);
                    if (invite == null || invite.IsRevoked)
                    {
                        return BadRequest("Invalid or revoked invite code.");
                    }
                    if (invite.ExpiresAt.HasValue && invite.ExpiresAt.Value < DateTime.UtcNow)
                    {
                        return BadRequest("Invite code has expired.");
                    }
                    if (invite.MaxUses > 0 && invite.UsesCount >= invite.MaxUses)
                    {
                        return BadRequest("Invite code has been used up.");
                    }

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

                    // Record invite usage and increment count
                    var usage = new InviteUsage
                    {
                        InviteId = invite.Id,
                        UserId = user.Id,
                        UsedAt = DateTime.UtcNow,
                        SourceIp = HttpContext.Connection.RemoteIpAddress?.ToString()
                    };
                    _dbContext.InviteUsages.Add(usage);

                    // increment count
                    invite.UsesCount += 1;
                    _dbContext.Invites.Update(invite);

                    await _dbContext.SaveChangesAsync();

                    await tx.CommitAsync();

                    return CreatedAtAction(nameof(Create), new { id = user.Id }, new
                    {
                        id = user.Id,
                        name = user.Name,
                        email = user.Email,
                        isAdmin = user.IsAdmin,
                        isDeactivated = user.IsDeactivated
                    });
                }
                catch (DbUpdateConcurrencyException)
                {
                    // concurrency on invite row; retry a few times
                    await tx.RollbackAsync();
                    if (attempt == maxAttempts - 1) throw;
                    // small delay before retry
                    await Task.Delay(50);
                    continue;
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            }

            // If we get here, something unexpected happened
            return StatusCode(500, "Failed to consume invite, please try again.");
        }

        // Non-private mode: create the user normally
        var userNormal = new User
        {
            Name = sanitizedName,
            Email = sanitizedEmail,
            Password = hashedPassword,
            IsAdmin = isAdmin,
            IsDeactivated = false
        };

        _dbContext.Users.Add(userNormal);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(Create), new { id = userNormal.Id }, new
        {
            id = userNormal.Id,
            name = userNormal.Name,
            email = userNormal.Email,
            isAdmin = userNormal.IsAdmin,
            isDeactivated = userNormal.IsDeactivated
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
