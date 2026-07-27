using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using TaskFlow.Api.Endpoints;
using TaskFlow.Api.Middleware;
using TaskFlow.Api.RateLimiting;
using TaskFlow.Api.Services;
using TaskFlow.Application.Auth.Validators;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Enums;
using TaskFlow.Infrastructure;
using TaskFlow.Infrastructure.Auth;
using TaskFlow.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ICurrentTenantService, CurrentTenantService>();

builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt configuration section is not configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            RoleClaimType = "role",
            NameClaimType = "sub",
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy => policy.RequireRole(nameof(UserRole.Admin)));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

const string CorsPolicyName = "Default";
var allowedOrigins = builder.Configuration
    .GetValue<string>("Cors:AllowedOrigins")?
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.Configure<RegisterRateLimitOptions>(builder.Configuration.GetSection("RateLimiting:Register"));

builder.Services.AddRateLimiter(options =>
{
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        await context.HttpContext.Response.WriteAsJsonAsync(
            new { title = "Too many requests. Please try again later.", status = StatusCodes.Status429TooManyRequests },
            cancellationToken);
    };

    options.AddPolicy("register", httpContext =>
    {
        var registerRateLimitOptions = httpContext.RequestServices
            .GetRequiredService<IOptions<RegisterRateLimitOptions>>().Value;

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = registerRateLimitOptions.PermitLimit,
                Window = TimeSpan.FromMinutes(registerRateLimitOptions.WindowMinutes),
                QueueLimit = 0,
            });
    });
});

var app = builder.Build();

if (app.Environment.IsProduction() && app.Configuration.GetValue<bool>("ApplyMigrationsOnStartup"))
{
    using var migrationScope = app.Services.CreateScope();
    migrationScope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

app.UseSerilogRequestLogging();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(CorsPolicyName);

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

var appVersion = typeof(Program).Assembly
    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
    ?? "unknown";

app.MapGet("/", () => Results.Ok());
app.MapGet("/health", () => Results.Ok(new { status = "healthy", version = appVersion }));

app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapOrganizationEndpoints();
app.MapTaskEndpoints();
app.MapTaskCommentEndpoints();
app.MapProjectEndpoints();
app.MapActivityEndpoints();

app.Run();

public partial class Program;
