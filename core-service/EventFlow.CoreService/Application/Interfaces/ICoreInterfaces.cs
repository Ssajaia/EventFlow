using EventFlow.CoreService.Application.DTOs;
using EventFlow.CoreService.Domain.Entities;
using EventFlow.SharedKernel.DTOs;
using DomainEvent = EventFlow.CoreService.Domain.Entities.Event;

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
    Task<DomainEvent?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IEnumerable<DomainEvent> Items, int Total)> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<(IEnumerable<DomainEvent> Items, int Total)> GetByOwnerPagedAsync(Guid ownerId, int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(DomainEvent ev, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    void Remove(DomainEvent ev);
}

public interface IEventPublisher
{
    Task PublishCreatedAsync(DomainEvent ev, string correlationId, CancellationToken ct = default);
    Task PublishUpdatedAsync(DomainEvent ev, string correlationId, CancellationToken ct = default);
    Task PublishDeletedAsync(Guid eventId, Guid ownerId, string correlationId, CancellationToken ct = default);
}
