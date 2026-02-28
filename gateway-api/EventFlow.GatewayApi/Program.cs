using System.Text;
using DotNetEnv;
using EventFlow.GatewayApi.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using StackExchange.Redis;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "gateway-api")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [GATEWAY] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// ── Redis ─────────────────────────────────────────────────────────────────
var redisHost = Env.GetString("REDIS_HOST", "localhost");
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(redisHost));

// ── Authentication (central JWT validation) ───────────────────────────────
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

// ── YARP Reverse Proxy ────────────────────────────────────────────────────
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// ── CORS ──────────────────────────────────────────────────────────────────
builder.Services.AddCors(opt => opt.AddPolicy("Default", p =>
    p.WithOrigins(Env.GetString("ALLOWED_ORIGINS", "http://localhost:3000").Split(','))
     .AllowAnyHeader().AllowAnyMethod()));

// ── OpenTelemetry ─────────────────────────────────────────────────────────
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("gateway-api"))
    .WithTracing(t => t.AddAspNetCoreInstrumentation());

builder.Services.AddHealthChecks()
    .AddRedis(redisHost, name: "redis");

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
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
    ctx.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    await next();
});

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapReverseProxy();

Log.Information("API Gateway starting on .NET 10 — listening on port 8080");
app.Run();
