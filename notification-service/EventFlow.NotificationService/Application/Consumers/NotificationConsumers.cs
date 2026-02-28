using EventFlow.NotificationService.Application.Services;
using EventFlow.NotificationService.Domain.Entities;
using EventFlow.NotificationService.Infrastructure.Data;
using EventFlow.SharedKernel.Messaging;
using MassTransit;

namespace EventFlow.NotificationService.Application.Consumers;

public class EventCreatedNotificationConsumer : IConsumer<EventCreatedMessage>
{
    private readonly NotificationDbContext _db;
    private readonly IEmailService _email;
    private readonly ILogger<EventCreatedNotificationConsumer> _logger;

    public EventCreatedNotificationConsumer(NotificationDbContext db, IEmailService email, ILogger<EventCreatedNotificationConsumer> logger)
    {
        _db = db;
        _email = email;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EventCreatedMessage> context)
    {
        var msg = context.Message;
        var subject = $"New Event Created: {msg.Title}";
        var body = $"Your event '{msg.Title}' has been successfully created. Event ID: {msg.EventId}";

        // In real world, look up user email from auth service or a read model
        var recipientEmail = $"owner-{msg.OwnerId}@eventflow.local";

        var log = new NotificationLog
        {
            EventId = msg.EventId,
            RecipientEmail = recipientEmail,
            Subject = subject,
            Body = body,
            CorrelationId = msg.CorrelationId,
            Status = "Pending"
        };
        await _db.NotificationLogs.AddAsync(log, context.CancellationToken);

        try
        {
            await _email.SendAsync(recipientEmail, subject, body, context.CancellationToken);
            log.Status = "Sent";
            log.SentAt = DateTime.UtcNow;
            _logger.LogInformation("[{CorrelationId}] Notification sent for EventCreated {EventId}", msg.CorrelationId, msg.EventId);
        }
        catch (Exception ex)
        {
            log.Status = "Failed";
            log.ErrorMessage = ex.Message;
            _logger.LogError(ex, "[{CorrelationId}] Failed to send notification for EventCreated {EventId}", msg.CorrelationId, msg.EventId);
        }

        await _db.SaveChangesAsync(context.CancellationToken);
    }
}

public class EventUpdatedNotificationConsumer : IConsumer<EventUpdatedMessage>
{
    private readonly NotificationDbContext _db;
    private readonly IEmailService _email;
    private readonly ILogger<EventUpdatedNotificationConsumer> _logger;

    public EventUpdatedNotificationConsumer(NotificationDbContext db, IEmailService email, ILogger<EventUpdatedNotificationConsumer> logger)
    {
        _db = db;
        _email = email;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EventUpdatedMessage> context)
    {
        var msg = context.Message;
        var subject = $"Event Updated: {msg.Title}";
        var body = $"Event '{msg.Title}' has been updated. Event ID: {msg.EventId}";
        var recipientEmail = $"owner-{msg.OwnerId}@eventflow.local";

        var log = new NotificationLog
        {
            EventId = msg.EventId,
            RecipientEmail = recipientEmail,
            Subject = subject,
            Body = body,
            CorrelationId = msg.CorrelationId,
            Status = "Pending"
        };
        await _db.NotificationLogs.AddAsync(log, context.CancellationToken);

        try
        {
            await _email.SendAsync(recipientEmail, subject, body, context.CancellationToken);
            log.Status = "Sent";
            log.SentAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            log.Status = "Failed";
            log.ErrorMessage = ex.Message;
        }

        await _db.SaveChangesAsync(context.CancellationToken);
    }
}

public class EventDeletedNotificationConsumer : IConsumer<EventDeletedMessage>
{
    private readonly NotificationDbContext _db;
    private readonly IEmailService _email;
    private readonly ILogger<EventDeletedNotificationConsumer> _logger;

    public EventDeletedNotificationConsumer(NotificationDbContext db, IEmailService email, ILogger<EventDeletedNotificationConsumer> logger)
    {
        _db = db;
        _email = email;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EventDeletedMessage> context)
    {
        var msg = context.Message;
        var subject = "Event Deleted";
        var body = $"Event ID {msg.EventId} has been deleted.";
        var recipientEmail = $"owner-{msg.OwnerId}@eventflow.local";

        var log = new NotificationLog
        {
            EventId = msg.EventId,
            RecipientEmail = recipientEmail,
            Subject = subject,
            Body = body,
            CorrelationId = msg.CorrelationId,
            Status = "Pending"
        };
        await _db.NotificationLogs.AddAsync(log, context.CancellationToken);

        try
        {
            await _email.SendAsync(recipientEmail, subject, body, context.CancellationToken);
            log.Status = "Sent";
            log.SentAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            log.Status = "Failed";
            log.ErrorMessage = ex.Message;
        }

        await _db.SaveChangesAsync(context.CancellationToken);
    }
}
