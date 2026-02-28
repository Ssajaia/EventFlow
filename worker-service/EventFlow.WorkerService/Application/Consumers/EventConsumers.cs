using EventFlow.SharedKernel.Messaging;
using EventFlow.WorkerService.Domain.Entities;
using EventFlow.WorkerService.Infrastructure.Data;
using MassTransit;
using System.Text.Json;

namespace EventFlow.WorkerService.Application.Consumers;

public class EventCreatedConsumer : IConsumer<EventCreatedMessage>
{
    private readonly WorkerDbContext _db;
    private readonly ILogger<EventCreatedConsumer> _logger;

    public EventCreatedConsumer(WorkerDbContext db, ILogger<EventCreatedConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EventCreatedMessage> context)
    {
        var msg = context.Message;
        _logger.LogInformation("[{CorrelationId}] Worker processing EventCreated: {EventId}", msg.CorrelationId, msg.EventId);

        var analytics = new EventAnalytics
        {
            EventId = msg.EventId,
            EventTitle = msg.Title,
            OwnerId = msg.OwnerId,
            Action = "Created",
            CorrelationId = msg.CorrelationId,
            RawPayload = JsonSerializer.Serialize(msg)
        };

        await _db.EventAnalytics.AddAsync(analytics, context.CancellationToken);

        // Update aggregated stats
        var period = msg.CreatedAt.ToString("yyyy-MM");
        var statKey = "events_created";
        var stat = await _db.AggregatedStats.FindAsync([period + ":" + statKey], context.CancellationToken);
        if (stat is null)
        {
            stat = new AggregatedStats { Period = period, StatKey = statKey, Value = 1 };
            await _db.AggregatedStats.AddAsync(stat, context.CancellationToken);
        }
        else
        {
            stat.Value++;
            stat.LastUpdated = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(context.CancellationToken);
        _logger.LogInformation("[{CorrelationId}] Worker analytics saved for EventId {EventId}", msg.CorrelationId, msg.EventId);
    }
}

public class EventUpdatedConsumer : IConsumer<EventUpdatedMessage>
{
    private readonly WorkerDbContext _db;
    private readonly ILogger<EventUpdatedConsumer> _logger;

    public EventUpdatedConsumer(WorkerDbContext db, ILogger<EventUpdatedConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EventUpdatedMessage> context)
    {
        var msg = context.Message;
        _logger.LogInformation("[{CorrelationId}] Worker processing EventUpdated: {EventId}", msg.CorrelationId, msg.EventId);

        var analytics = new EventAnalytics
        {
            EventId = msg.EventId,
            EventTitle = msg.Title,
            OwnerId = msg.OwnerId,
            Action = "Updated",
            CorrelationId = msg.CorrelationId,
            RawPayload = JsonSerializer.Serialize(msg)
        };

        await _db.EventAnalytics.AddAsync(analytics, context.CancellationToken);
        await _db.SaveChangesAsync(context.CancellationToken);
    }
}

public class EventDeletedConsumer : IConsumer<EventDeletedMessage>
{
    private readonly WorkerDbContext _db;
    private readonly ILogger<EventDeletedConsumer> _logger;

    public EventDeletedConsumer(WorkerDbContext db, ILogger<EventDeletedConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EventDeletedMessage> context)
    {
        var msg = context.Message;
        _logger.LogInformation("[{CorrelationId}] Worker processing EventDeleted: {EventId}", msg.CorrelationId, msg.EventId);

        var analytics = new EventAnalytics
        {
            EventId = msg.EventId,
            OwnerId = msg.OwnerId,
            Action = "Deleted",
            CorrelationId = msg.CorrelationId,
            RawPayload = JsonSerializer.Serialize(msg)
        };

        await _db.EventAnalytics.AddAsync(analytics, context.CancellationToken);
        await _db.SaveChangesAsync(context.CancellationToken);
    }
}
