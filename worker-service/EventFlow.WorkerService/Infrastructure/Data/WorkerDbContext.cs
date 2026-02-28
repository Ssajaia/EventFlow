using EventFlow.WorkerService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.WorkerService.Infrastructure.Data;

public class WorkerDbContext : DbContext
{
    public WorkerDbContext(DbContextOptions<WorkerDbContext> options) : base(options) { }

    public DbSet<EventAnalytics> EventAnalytics => Set<EventAnalytics>();
    public DbSet<AggregatedStats> AggregatedStats => Set<AggregatedStats>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventAnalytics>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => a.EventId);
            e.HasIndex(a => a.ProcessedAt);
            e.HasIndex(a => new { a.OwnerId, a.ProcessedAt });
        });

        modelBuilder.Entity<AggregatedStats>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => new { s.Period, s.StatKey }).IsUnique();
        });
    }
}
