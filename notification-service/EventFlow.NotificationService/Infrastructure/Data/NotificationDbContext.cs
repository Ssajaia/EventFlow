using EventFlow.NotificationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.NotificationService.Infrastructure.Data;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationLog>(e =>
        {
            e.HasKey(n => n.Id);
            e.HasIndex(n => n.EventId);
            e.HasIndex(n => n.SentAt);
            e.HasIndex(n => n.Status);
            e.Property(n => n.Subject).IsRequired().HasMaxLength(300);
            e.Property(n => n.RecipientEmail).IsRequired().HasMaxLength(256);
        });
    }
}
