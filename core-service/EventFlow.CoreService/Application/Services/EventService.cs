using EventFlow.CoreService.Application.DTOs;
using EventFlow.CoreService.Application.Interfaces;
using EventFlow.CoreService.Domain.Entities;
using EventFlow.SharedKernel.DTOs;

namespace EventFlow.CoreService.Application.Services;

public class EventService : IEventService
{
    private readonly IEventRepository _events;
    private readonly IEventPublisher _publisher;
    private readonly ILogger<EventService> _logger;

    public EventService(IEventRepository events, IEventPublisher publisher, ILogger<EventService> logger)
    {
        _events = events;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<EventDto> CreateAsync(CreateEventRequest request, Guid ownerId, string correlationId, CancellationToken ct = default)
    {
        var ev = Event.Create(request.Title, request.Description, request.Location,
            request.StartDate, request.EndDate, ownerId, request.IsPublic);

        await _events.AddAsync(ev, ct);
        await _events.SaveChangesAsync(ct);

        await _publisher.PublishCreatedAsync(ev, correlationId, ct);

        _logger.LogInformation("[{CorrelationId}] Event created: {EventId} by {OwnerId}", correlationId, ev.Id, ownerId);
        return EventDto.FromEntity(ev);
    }

    public async Task<EventDto> UpdateAsync(Guid id, UpdateEventRequest request, Guid ownerId, string correlationId, CancellationToken ct = default)
    {
        var ev = await _events.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Event {id} not found.");

        if (!ev.IsOwnedBy(ownerId))
            throw new UnauthorizedAccessException("You do not own this event.");

        ev.Update(request.Title, request.Description, request.Location,
            request.StartDate, request.EndDate, request.IsPublic);

        await _events.SaveChangesAsync(ct);
        await _publisher.PublishUpdatedAsync(ev, correlationId, ct);

        _logger.LogInformation("[{CorrelationId}] Event updated: {EventId}", correlationId, ev.Id);
        return EventDto.FromEntity(ev);
    }

    public async Task DeleteAsync(Guid id, Guid ownerId, string correlationId, CancellationToken ct = default)
    {
        var ev = await _events.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Event {id} not found.");

        if (!ev.IsOwnedBy(ownerId))
            throw new UnauthorizedAccessException("You do not own this event.");

        _events.Remove(ev);
        await _events.SaveChangesAsync(ct);
        await _publisher.PublishDeletedAsync(id, ownerId, correlationId, ct);

        _logger.LogInformation("[{CorrelationId}] Event deleted: {EventId}", correlationId, id);
    }

    public async Task<EventDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var ev = await _events.GetByIdAsync(id, ct);
        return ev is null ? null : EventDto.FromEntity(ev);
    }

    public async Task<PagedResult<EventDto>> GetAllAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var (items, total) = await _events.GetPagedAsync(page, pageSize, ct);
        return new PagedResult<EventDto>
        {
            Items = items.Select(EventDto.FromEntity),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<EventDto>> GetByOwnerAsync(Guid ownerId, int page, int pageSize, CancellationToken ct = default)
    {
        var (items, total) = await _events.GetByOwnerPagedAsync(ownerId, page, pageSize, ct);
        return new PagedResult<EventDto>
        {
            Items = items.Select(EventDto.FromEntity),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
