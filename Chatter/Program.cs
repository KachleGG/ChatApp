using Chatter.Data;
using Microsoft.EntityFrameworkCore;

namespace Chatter
{
    public class Program
    {
        public static void Main(string[] args) {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddDbContext<ChatterDbContext>(options =>
                options.UseSqlite("Data Source=chatter.db"));

            // Configure session support
            builder.Services.AddDistributedMemoryCache(); // Required for session
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(24); // Session timeout
                options.Cookie.HttpOnly = true; // Security: prevent JavaScript access
                options.Cookie.IsEssential = true; // Required for GDPR compliance
                options.Cookie.SameSite = SameSiteMode.Lax; // CSRF protection
            });

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Ensure database is created and migrated
            using (var scope = app.Services.CreateScope()) {
                var dbContext = scope.ServiceProvider.GetRequiredService<ChatterDbContext>();

                // Use migrations to update database schema while preserving data
                dbContext.Database.Migrate();
            }

            // Ensure AppData folder creation
            AppConstants.EnsureFolderStructure();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment()) {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            // Enable session middleware (must be before UseAuthorization)
            app.UseSession();

            app.UseAuthorization();

            app.MapControllers();

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.Run();
        }
    }
}
