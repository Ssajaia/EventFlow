using EventFlow.CoreService.Application.Interfaces;
using EventFlow.CoreService.Domain.Entities;
using EventFlow.SharedKernel.Messaging;
using MassTransit;

namespace EventFlow.CoreService.Infrastructure.Messaging;

public class MassTransitEventPublisher : IEventPublisher
{
    private readonly IPublishEndpoint _publish;
    private readonly ILogger<MassTransitEventPublisher> _logger;

    public MassTransitEventPublisher(IPublishEndpoint publish, ILogger<MassTransitEventPublisher> logger)
    {
        _publish = publish;
        _logger = logger;
    }

    public async Task PublishCreatedAsync(Event ev, string correlationId, CancellationToken ct = default)
    {
        var message = new EventCreatedMessage
        {
            EventId = ev.Id,
            Title = ev.Title,
            Description = ev.Description,
            OwnerId = ev.OwnerId,
            CreatedAt = ev.CreatedAt,
            CorrelationId = correlationId
        };
        await _publish.Publish(message, ct);
        _logger.LogInformation("[{CorrelationId}] Published EventCreatedMessage for {EventId}", correlationId, ev.Id);
    }

    public async Task PublishUpdatedAsync(Event ev, string correlationId, CancellationToken ct = default)
    {
        var message = new EventUpdatedMessage
        {
            EventId = ev.Id,
            Title = ev.Title,
            Description = ev.Description,
            OwnerId = ev.OwnerId,
            UpdatedAt = ev.UpdatedAt ?? DateTime.UtcNow,
            CorrelationId = correlationId
        };
        await _publish.Publish(message, ct);
        _logger.LogInformation("[{CorrelationId}] Published EventUpdatedMessage for {EventId}", correlationId, ev.Id);
    }

    public async Task PublishDeletedAsync(Guid eventId, Guid ownerId, string correlationId, CancellationToken ct = default)
    {
        var message = new EventDeletedMessage
        {
            EventId = eventId,
            OwnerId = ownerId,
            DeletedAt = DateTime.UtcNow,
            CorrelationId = correlationId
        };
        await _publish.Publish(message, ct);
        _logger.LogInformation("[{CorrelationId}] Published EventDeletedMessage for {EventId}", correlationId, eventId);
    }
}
