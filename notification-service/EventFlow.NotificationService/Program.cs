using DotNetEnv;
using EventFlow.NotificationService.Application.Consumers;
using EventFlow.NotificationService.Application.Services;
using EventFlow.NotificationService.Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;

Env.Load();

var host = Host.CreateDefaultBuilder(args)
    .UseSerilog((ctx, cfg) => cfg
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Service", "notification-service")
        .WriteTo.Console())
    .ConfigureServices((ctx, services) =>
    {
        var connectionString =
            $"Host={Env.GetString("POSTGRES_HOST", "localhost")};" +
            $"Port={Env.GetString("POSTGRES_PORT", "5432")};" +
            $"Database={Env.GetString("POSTGRES_DB", "notificationdb")};" +
            $"Username={Env.GetString("POSTGRES_USER", "postgres")};" +
            $"Password={Env.GetString("POSTGRES_PASSWORD", "postgres")}";

        services.AddDbContext<NotificationDbContext>(opt => opt.UseNpgsql(connectionString));
        services.AddScoped<IEmailService, MockEmailService>();

        services.AddMassTransit(x =>
        {
            x.AddConsumer<EventCreatedNotificationConsumer>();
            x.AddConsumer<EventUpdatedNotificationConsumer>();
            x.AddConsumer<EventDeletedNotificationConsumer>();

            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(Env.GetString("RABBITMQ_HOST", "localhost"), "/", h =>
                {
                    h.Username(Env.GetString("RABBITMQ_USER", "guest"));
                    h.Password(Env.GetString("RABBITMQ_PASS", "guest"));
                });

                cfg.ReceiveEndpoint("notification.queue", e =>
                {
                    e.UseMessageRetry(r => r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5)));
                    e.ConfigureConsumer<EventCreatedNotificationConsumer>(ctx);
                    e.ConfigureConsumer<EventUpdatedNotificationConsumer>(ctx);
                    e.ConfigureConsumer<EventDeletedNotificationConsumer>(ctx);
                    e.BindDeadLetterQueue("notification.dlq");
                });
            });
        });

        services.AddHealthChecks().AddNpgSql(connectionString);
    })
    .Build();

if (Env.GetBool("RUN_MIGRATIONS", false))
{
    using var scope = host.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    await db.Database.MigrateAsync();
    Log.Information("Notification DB migrations applied.");
}

Log.Information("Notification Service starting on .NET 10");
await host.RunAsync();
