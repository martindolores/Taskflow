using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.IntegrationTests;

public class DatabaseConfigurationTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public void AppDbContext_IsRegisteredWithNpgsqlProvider()
    {
        using var scope = factory.Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", context.Database.ProviderName);
    }

    [Fact]
    public void AppDbContext_UsesConnectionStringFromConfiguration()
    {
        using var overriddenFactory = factory.WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:Default", "Host=db.example.com;Database=taskflow_test;Username=u;Password=p"));
        using var scope = overriddenFactory.Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Contains("db.example.com", context.Database.GetConnectionString());
    }
}
