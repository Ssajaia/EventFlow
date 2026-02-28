namespace EventFlow.WorkerService.Domain.Entities;

public class EventAnalytics
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public string Action { get; set; } = string.Empty; // Created, Updated, Deleted
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public string CorrelationId { get; set; } = string.Empty;
    public string RawPayload { get; set; } = string.Empty;
}

public class AggregatedStats
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Period { get; set; } = string.Empty; // e.g. "2025-02"
    public string StatKey { get; set; } = string.Empty; // e.g. "events_created"
    public long Value { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
