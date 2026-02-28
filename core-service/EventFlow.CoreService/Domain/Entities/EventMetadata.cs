namespace EventFlow.CoreService.Domain.Entities;

public class EventMetadata
{
    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public Event Event { get; private set; } = null!;
    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;

    private EventMetadata() { }

    public static EventMetadata Create(Guid eventId, string key, string value)
        => new() { Id = Guid.NewGuid(), EventId = eventId, Key = key, Value = value };
}
