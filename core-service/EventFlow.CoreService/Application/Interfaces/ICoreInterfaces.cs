using EventFlow.CoreService.Application.DTOs;
using EventFlow.CoreService.Domain.Entities;
using EventFlow.SharedKernel.DTOs;

namespace EventFlow.CoreService.Application.Interfaces;

public interface IEventService
{
    Task<EventDto> CreateAsync(CreateEventRequest request, Guid ownerId, string correlationId, CancellationToken ct = default);
    Task<EventDto> UpdateAsync(Guid id, UpdateEventRequest request, Guid ownerId, string correlationId, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid ownerId, string correlationId, CancellationToken ct = default);
    Task<EventDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<EventDto>> GetAllAsync(int page, int pageSize, CancellationToken ct = default);
    Task<PagedResult<EventDto>> GetByOwnerAsync(Guid ownerId, int page, int pageSize, CancellationToken ct = default);
}

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IEnumerable<Event> Items, int Total)> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<(IEnumerable<Event> Items, int Total)> GetByOwnerPagedAsync(Guid ownerId, int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(Event ev, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    void Remove(Event ev);
}

public interface IEventPublisher
{
    Task PublishCreatedAsync(Event ev, string correlationId, CancellationToken ct = default);
    Task PublishUpdatedAsync(Event ev, string correlationId, CancellationToken ct = default);
    Task PublishDeletedAsync(Guid eventId, Guid ownerId, string correlationId, CancellationToken ct = default);
}
