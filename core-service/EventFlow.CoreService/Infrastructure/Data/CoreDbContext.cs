using EventFlow.CoreService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.CoreService.Infrastructure.Data;

public class CoreDbContext : DbContext
{
    public CoreDbContext(DbContextOptions<CoreDbContext> options) : base(options) { }

    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventMetadata> EventMetadata => Set<EventMetadata>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Event>(e =>
        {
            e.HasKey(ev => ev.Id);
            e.Property(ev => ev.Title).IsRequired().HasMaxLength(200);
            e.Property(ev => ev.Description).IsRequired().HasMaxLength(5000);
            e.Property(ev => ev.Location).IsRequired().HasMaxLength(300);
            e.Property(ev => ev.Status).HasConversion<string>();
            e.HasIndex(ev => ev.OwnerId);
            e.HasIndex(ev => ev.CreatedAt).IsDescending();
            e.HasIndex(ev => new { ev.Status, ev.IsPublic });
        });

        modelBuilder.Entity<EventMetadata>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Key).IsRequired().HasMaxLength(100);
            e.Property(m => m.Value).IsRequired().HasMaxLength(2000);
            e.HasOne(m => m.Event).WithMany(ev => ev.Metadata).HasForeignKey(m => m.EventId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(m => new { m.EventId, m.Key });
        });
    }
}
