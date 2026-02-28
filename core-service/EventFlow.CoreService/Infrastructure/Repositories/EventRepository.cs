using EventFlow.CoreService.Application.Interfaces;
using EventFlow.CoreService.Domain.Entities;
using EventFlow.CoreService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.CoreService.Infrastructure.Repositories;

public class EventRepository : IEventRepository
{
    private readonly CoreDbContext _db;
    public EventRepository(CoreDbContext db) => _db = db;

    public Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Events.Include(e => e.Metadata).FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<(IEnumerable<Event> Items, int Total)> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Events.Where(e => e.IsPublic).OrderByDescending(e => e.CreatedAt);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task<(IEnumerable<Event> Items, int Total)> GetByOwnerPagedAsync(Guid ownerId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Events.Where(e => e.OwnerId == ownerId).OrderByDescending(e => e.CreatedAt);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task AddAsync(Event ev, CancellationToken ct = default) =>
        await _db.Events.AddAsync(ev, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);

    public void Remove(Event ev) => _db.Events.Remove(ev);
}
