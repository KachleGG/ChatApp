using Chatter.Models;
using Microsoft.EntityFrameworkCore;

namespace Chatter.Data;

public class ChatterDbContext : DbContext
{
    public ChatterDbContext(DbContextOptions<ChatterDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Message> Messages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Email).IsUnique(); // Ensure unique emails at database level
            entity.Property(e => e.Password).IsRequired();
            entity.Property(e => e.IsAdmin).HasDefaultValue(false);
            entity.Property(e => e.IsDeactivated).HasDefaultValue(false);
        });

        // Configure Message entity
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Text).IsRequired();

            // Configure relationship: Message belongs to User
            entity.HasOne(e => e.SentFrom)
                  .WithMany()
                  .IsRequired()
                  .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete
        });
    }
}
