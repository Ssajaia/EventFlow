namespace EventFlow.NotificationService.Application.Services;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct = default);
}

/// <summary>
/// Mock SMTP email service — logs emails to console.
/// Replace with real SMTP (e.g. MailKit + SmtpClient) in production.
/// </summary>
public class MockEmailService : IEmailService
{
    private readonly ILogger<MockEmailService> _logger;

    public MockEmailService(ILogger<MockEmailService> logger) => _logger = logger;

    public Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "📧 [MOCK EMAIL] To: {To} | Subject: {Subject} | Body: {Body}",
            to, subject, body[..Math.Min(100, body.Length)]);

        // Simulate a small async delay (as a real SMTP would have)
        return Task.Delay(50, ct);
    }
}
