using DotNetEnv;
using EventFlow.WorkerService.Application.Consumers;
using EventFlow.WorkerService.Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;

Env.Load();

var host = Host.CreateDefaultBuilder(args)
    .UseSerilog((ctx, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Service", "worker-service")
        .WriteTo.Console())
    .ConfigureServices((ctx, services) =>
    {
        var connectionString =
            $"Host={Env.GetString("POSTGRES_HOST", "localhost")};" +
            $"Port={Env.GetString("POSTGRES_PORT", "5432")};" +
            $"Database={Env.GetString("POSTGRES_DB", "workerdb")};" +
            $"Username={Env.GetString("POSTGRES_USER", "postgres")};" +
            $"Password={Env.GetString("POSTGRES_PASSWORD", "postgres")}";

        services.AddDbContext<WorkerDbContext>(opt => opt.UseNpgsql(connectionString));

        services.AddMassTransit(x =>
        {
            x.AddConsumer<EventCreatedConsumer>();
            x.AddConsumer<EventUpdatedConsumer>();
            x.AddConsumer<EventDeletedConsumer>();

            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(Env.GetString("RABBITMQ_HOST", "localhost"), "/", h =>
                {
                    h.Username(Env.GetString("RABBITMQ_USER", "guest"));
                    h.Password(Env.GetString("RABBITMQ_PASS", "guest"));
                });

                cfg.ReceiveEndpoint("worker.queue", e =>
                {
                    e.UseMessageRetry(r => r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5)));
                    e.ConfigureConsumer<EventCreatedConsumer>(ctx);
                    e.ConfigureConsumer<EventUpdatedConsumer>(ctx);
                    e.ConfigureConsumer<EventDeletedConsumer>(ctx);
                    e.BindDeadLetterQueue("worker.dlq");
                });
            });
        });

        services.AddHealthChecks()
            .AddNpgsql(connectionString, name: "postgres");
    })
    .Build();

// Auto-migrate
if (Env.GetBool("RUN_MIGRATIONS", false))
{
    using var scope = host.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<WorkerDbContext>();
    await db.Database.MigrateAsync();
    Log.Information("Worker DB migrations applied.");
}

Log.Information("Worker Service starting on .NET 10");
await host.RunAsync();
