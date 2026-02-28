namespace EventFlow.CoreService.Domain.Entities;

public class Event
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Location { get; private set; } = string.Empty;
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public Guid OwnerId { get; private set; }
    public bool IsPublic { get; private set; }
    public EventStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public ICollection<EventMetadata> Metadata { get; private set; } = [];

    private Event() { }

    public static Event Create(string title, string description, string location,
        DateTime startDate, DateTime endDate, Guid ownerId, bool isPublic = true)
    {
        if (endDate <= startDate)
            throw new ArgumentException("End date must be after start date.");

        return new Event
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            Location = location,
            StartDate = startDate,
            EndDate = endDate,
            OwnerId = ownerId,
            IsPublic = isPublic,
            Status = EventStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string title, string description, string location,
        DateTime startDate, DateTime endDate, bool isPublic)
    {
        if (endDate <= startDate)
            throw new ArgumentException("End date must be after start date.");

        Title = title;
        Description = description;
        Location = location;
        StartDate = startDate;
        EndDate = endDate;
        IsPublic = isPublic;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Publish() { Status = EventStatus.Published; UpdatedAt = DateTime.UtcNow; }
    public void Cancel() { Status = EventStatus.Cancelled; UpdatedAt = DateTime.UtcNow; }

    public bool IsOwnedBy(Guid userId) => OwnerId == userId;
}

public enum EventStatus { Draft, Published, Cancelled }
