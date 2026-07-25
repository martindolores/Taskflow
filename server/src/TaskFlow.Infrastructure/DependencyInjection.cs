using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Activity;
using TaskFlow.Application.Auth;
using TaskFlow.Application.Common;
using TaskFlow.Application.Organizations;
using TaskFlow.Application.Projects;
using TaskFlow.Application.TaskComments;
using TaskFlow.Application.Tasks;
using TaskFlow.Application.Users;
using TaskFlow.Infrastructure.Activity;
using TaskFlow.Infrastructure.Auth;
using TaskFlow.Infrastructure.Email;
using TaskFlow.Infrastructure.Organizations;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.Infrastructure.Projects;
using TaskFlow.Infrastructure.TaskComments;
using TaskFlow.Infrastructure.Tasks;
using TaskFlow.Infrastructure.Users;

namespace TaskFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'ConnectionStrings:Default' is not configured.");

        services.AddDbContext<AppDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<ITaskCommentService, TaskCommentService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IActivityService, ActivityService>();

        services.Configure<EmailOptions>(options =>
        {
            configuration.GetSection("Email").Bind(options);
            options.FrontendBaseUrl = configuration["Frontend:BaseUrl"] ?? string.Empty;
        });

        if (string.IsNullOrWhiteSpace(configuration["Email:Brevo:ApiKey"]))
        {
            services.AddSingleton<IEmailService, NullEmailService>();
        }
        else
        {
            services.AddHttpClient<IEmailService, BrevoEmailService>();
        }

        return services;
    }
}
