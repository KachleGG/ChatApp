using Chatter.Models;
using Microsoft.EntityFrameworkCore;

namespace Chatter.Data;

public class ChatterDbContext : DbContext
{
    public ChatterDbContext(DbContextOptions<ChatterDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<Group> Groups { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
            entity.Property(e => e.SentAt).IsRequired();

            // Configure relationship: Message belongs to User
            entity.HasOne(e => e.SentFrom)
                  .WithMany()
                  .IsRequired()
                  .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

            // Configure relationship: Message belongs to Group
            entity.HasOne(e => e.Group)
                  .WithMany()
                  .HasForeignKey(e => e.GroupId)
                  .IsRequired()
                  .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete
        });

        // Configure Group entity
        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsDeactivated).HasDefaultValue(false);

            // Configure relationship: Group belongs to Owner (User)
            entity.HasOne(e => e.Owner)
                  .WithMany()
                  .HasForeignKey(e => e.OwnerId)
                  .IsRequired()
                  .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

            // Seed General group with Id = 1
            entity.HasData(new Group
            {
                Id = 1,
                Name = "General",
                OwnerId = 1, // Assume first user (admin) owns General
                IsDeactivated = false,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        });
    }
}
