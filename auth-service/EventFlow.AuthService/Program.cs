using System.Text;
using DotNetEnv;
using EventFlow.AuthService.API.Middleware;
using EventFlow.AuthService.Application.Interfaces;
using EventFlow.AuthService.Application.Services;
using EventFlow.AuthService.Application.Validators;
using EventFlow.AuthService.Infrastructure.Data;
using EventFlow.AuthService.Infrastructure.Repositories;
using EventFlow.AuthService.Infrastructure.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using StackExchange.Redis;

// Load .env file
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ──────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "auth-service")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Service}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// ── Database ─────────────────────────────────────────────────────────────
var connectionString =
    $"Host={Env.GetString("POSTGRES_HOST", "localhost")};" +
    $"Port={Env.GetString("POSTGRES_PORT", "5432")};" +
    $"Database={Env.GetString("POSTGRES_DB", "authdb")};" +
    $"Username={Env.GetString("POSTGRES_USER", "postgres")};" +
    $"Password={Env.GetString("POSTGRES_PASSWORD", "postgres")}";

builder.Services.AddDbContext<AuthDbContext>(opt =>
    opt.UseNpgsql(connectionString));

// ── Redis ─────────────────────────────────────────────────────────────────
var redisHost = Env.GetString("REDIS_HOST", "localhost");
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(redisHost));

// ── Authentication ────────────────────────────────────────────────────────
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

// ── Application Services ──────────────────────────────────────────────────
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IAuthService, EventFlow.AuthService.Application.Services.AuthService>();

// ── Validation ────────────────────────────────────────────────────────────
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

// ── OpenTelemetry ─────────────────────────────────────────────────────────
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("auth-service"))
    .WithTracing(t => t.AddAspNetCoreInstrumentation());

// ── CORS ──────────────────────────────────────────────────────────────────
builder.Services.AddCors(opt => opt.AddPolicy("Default", p =>
    p.WithOrigins(Env.GetString("ALLOWED_ORIGINS", "http://localhost:3000").Split(','))
     .AllowAnyHeader()
     .AllowAnyMethod()));

// ── Rate Limiting (via middleware, Redis-backed) ───────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "EventFlow Auth Service", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
            []
        }
    });
});

builder.Services.AddHealthChecks()
    .AddNpgsql(connectionString, name: "postgres")
    .AddRedis(redisHost, name: "redis");

var app = builder.Build();

// ── Middleware Pipeline ───────────────────────────────────────────────────
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseCors("Default");
app.UseAuthentication();
app.UseAuthorization();

// Secure headers
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.Append("X-Frame-Options", "DENY");
    ctx.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    ctx.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    await next();
});

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth Service v1"));

app.MapControllers();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

// ── Auto-migration on startup ─────────────────────────────────────────────
if (Env.GetBool("RUN_MIGRATIONS", false))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await db.Database.MigrateAsync();
    Log.Information("Database migrations applied.");
}

Log.Information("Auth Service starting on .NET 10");
app.Run();

public partial class Program { }
