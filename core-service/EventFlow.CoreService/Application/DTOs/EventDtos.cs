using EventFlow.CoreService.Domain.Entities;

namespace EventFlow.CoreService.Application.DTOs;

public record CreateEventRequest
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public bool IsPublic { get; init; } = true;
}

public record UpdateEventRequest
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public bool IsPublic { get; init; } = true;
}

public record EventDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public Guid OwnerId { get; init; }
    public bool IsPublic { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    public static EventDto FromEntity(Event e) => new()
    {
        Id = e.Id,
        Title = e.Title,
        Description = e.Description,
        Location = e.Location,
        StartDate = e.StartDate,
        EndDate = e.EndDate,
        OwnerId = e.OwnerId,
        IsPublic = e.IsPublic,
        Status = e.Status.ToString(),
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}
