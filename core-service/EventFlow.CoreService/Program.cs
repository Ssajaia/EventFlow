using System.Text;
using DotNetEnv;
using EventFlow.CoreService.API.Middleware;
using EventFlow.CoreService.Application.Interfaces;
using EventFlow.CoreService.Application.Services;
using EventFlow.CoreService.Application.Validators;
using EventFlow.CoreService.Infrastructure.Data;
using EventFlow.CoreService.Infrastructure.Messaging;
using EventFlow.CoreService.Infrastructure.Repositories;
using FluentValidation;
using FluentValidation.AspNetCore;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "core-service")
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

var connectionString =
    $"Host={Env.GetString("POSTGRES_HOST", "localhost")};" +
    $"Port={Env.GetString("POSTGRES_PORT", "5432")};" +
    $"Database={Env.GetString("POSTGRES_DB", "coredb")};" +
    $"Username={Env.GetString("POSTGRES_USER", "postgres")};" +
    $"Password={Env.GetString("POSTGRES_PASSWORD", "postgres")}";

builder.Services.AddDbContext<CoreDbContext>(opt => opt.UseNpgsql(connectionString));

// JWT Auth (validates tokens issued by auth-service)
var jwtSecret = Env.GetString("JWT_SECRET", "change-me-super-secret-key-32-chars!!");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = Env.GetString("JWT_ISSUER", "eventflow-auth"),
            ValidateAudience = true,
            ValidAudience = Env.GetString("JWT_AUDIENCE", "eventflow"),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

// MassTransit + RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(Env.GetString("RABBITMQ_HOST", "localhost"), "/", h =>
        {
            h.Username(Env.GetString("RABBITMQ_USER", "guest"));
            h.Password(Env.GetString("RABBITMQ_PASS", "guest"));
        });
        cfg.ConfigureEndpoints(ctx);
    });
});

builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IEventPublisher, MassTransitEventPublisher>();
builder.Services.AddScoped<IEventService, EventService>();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateEventRequestValidator>();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("core-service"))
    .WithTracing(t => t.AddAspNetCoreInstrumentation());

builder.Services.AddCors(opt => opt.AddPolicy("Default", p =>
    p.WithOrigins(Env.GetString("ALLOWED_ORIGINS", "http://localhost:3000").Split(','))
     .AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "EventFlow Core Service", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new()
    {
        { new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } }, [] }
    });
});

builder.Services.AddHealthChecks()
    .AddNpgsql(connectionString, name: "postgres");

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseCors("Default");
app.UseAuthentication();
app.UseAuthorization();

app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.Append("X-Frame-Options", "DENY");
    ctx.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    await next();
});

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Core Service v1"));

app.MapControllers();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

if (Env.GetBool("RUN_MIGRATIONS", false))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
    await db.Database.MigrateAsync();
    Log.Information("Core DB migrations applied.");
}

Log.Information("Core Service starting on .NET 10");
app.Run();

public partial class Program { }
