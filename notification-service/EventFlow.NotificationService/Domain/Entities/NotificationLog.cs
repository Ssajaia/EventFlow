namespace EventFlow.NotificationService.Domain.Entities;

public class NotificationLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Sent, Failed
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
