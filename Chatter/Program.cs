using Chatter.Data;
using Chatter.Helpers;
using Chatter.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Chatter
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Determine base directory for published app
            var baseDir = AppContext.BaseDirectory; // will point to the publish folder when published

            // Get database path from environment variable or use default in publish folder
            var databasePathEnv = Environment.GetEnvironmentVariable("DATABASE_PATH");

            string databasePath;
            if (!string.IsNullOrWhiteSpace(databasePathEnv))
            {
                databasePath = databasePathEnv!;
            }
            else
            {
                // Default to a database placed next to the compiled files so publish folder is self-contained
                databasePath = Path.Combine(baseDir, "./data", "chatter.db");
            }

            // Normalize and ensure directory exists
            databasePath = Path.GetFullPath(databasePath);
            var dbDir = Path.GetDirectoryName(databasePath) ?? baseDir;
            if (!Directory.Exists(dbDir))
                Directory.CreateDirectory(dbDir);

            Console.WriteLine($"Using SQLite database path: {databasePath}");

            // Add services to the container.
            builder.Services.AddDbContext<ChatterDbContext>(options =>
                options.UseSqlite($"Data Source={databasePath}"));


            // Configure session support
            builder.Services.AddDistributedMemoryCache(); // Required for session

            var sessionTimeoutHours = int.TryParse(
                Environment.GetEnvironmentVariable("SESSION_TIMEOUT_HOURS"),
                out var hours) ? hours : 24;

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(sessionTimeoutHours);
                options.Cookie.HttpOnly = true; // Security: prevent JavaScript access
                options.Cookie.IsEssential = true; // Required for GDPR compliance
                options.Cookie.SameSite = SameSiteMode.Lax; // CSRF protection
            });

            builder.Services.AddControllers();
            builder.Services.AddOpenApi();

            // Configure a default authentication scheme so Authorization/Forbid/Challenge work.
            // The app uses session cookies for user session state; register cookie auth as the default.
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultForbidScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                // Keep defaults; ensure cookies are HttpOnly and SameSite to match session policy
                options.LoginPath = "/login"; // controller or client handles login route
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
            });

            // Register BackupService as a hosted service only when enabled in configuration.
            // This keeps the service out of the app unless the operator has explicitly enabled backups.
            try
            {
                var backupEnabled = builder.Configuration.GetValue<bool?>("ServerSettings:BackupEnabled") ?? false;
                if (backupEnabled)
                {
                    // register as a singleton and expose as IHostedService so controllers can also use it if needed
                    builder.Services.AddSingleton<Chatter.Services.BackupService>();
                    builder.Services.AddHostedService(sp => sp.GetRequiredService<Chatter.Services.BackupService>());
                    Console.WriteLine("BackupService registered (enabled via ServerSettings:BackupEnabled).");
                }
                else
                {
                    Console.WriteLine("BackupService not registered (ServerSettings:BackupEnabled is false).");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error registering BackupService: {ex.Message}");
            }

            var app = builder.Build();

            // Ensure database is created and migrated
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ChatterDbContext>();

                try
                {
                    dbContext.Database.Migrate();
                    Console.WriteLine("Database migration completed successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Database migration error: {ex.Message}");
                    throw;
                }

                // Idempotent admin seeding: create a default admin user if none exists.
                // This runs in any environment (including Production) but will not create duplicates.
                try
                {
                    // Read admin credentials from environment variables if provided; fall back to defaults.
                    var adminName = Environment.GetEnvironmentVariable("ADMIN_USERNAME") ?? "admin";
                    var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@admin.com";
                    var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "admin";

                    var existingAdmin = dbContext.Users.Any(u => u.IsAdmin || u.Name == adminName || u.Email == adminEmail);
                    if (!existingAdmin)
                    {
                        var adminUser = new User
                        {
                            Name = adminName,
                            Email = adminEmail,
                            Password = PasswordHasher.HashPassword(adminPassword),
                            IsAdmin = true
                        };

                        dbContext.Users.Add(adminUser);
                        dbContext.SaveChanges();
                        Console.WriteLine($"Seeded admin user '{adminName}' ('{adminEmail}')");
                    }
                    else
                    {
                        Console.WriteLine("Admin user already exists; skipping seeding.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Admin seeding error: {ex.Message}");
                    // Don't throw here — seeding failure shouldn't block app startup in most cases.
                }
            }

            AppConstants.EnsureFolderStructure();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseSession();

            // Ensure authentication middleware runs before authorization so
            // Forbid/Challenge and [Authorize] work correctly.
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.UseDefaultFiles();
            app.UseStaticFiles();

            // SPA fallback: serve index.html for any non-API route so client-side routing works
            // This is required when serving the built `dist` files from wwwroot.
            app.MapFallbackToFile("index.html");

            app.Run();
        }
    }
}
