namespace EventFlow.SharedKernel.Messaging;

public record EventCreatedMessage
{
    public Guid EventId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Guid OwnerId { get; init; }
    public DateTime CreatedAt { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
}

public record EventUpdatedMessage
{
    public Guid EventId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Guid OwnerId { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
}

public record EventDeletedMessage
{
    public Guid EventId { get; init; }
    public Guid OwnerId { get; init; }
    public DateTime DeletedAt { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
}
