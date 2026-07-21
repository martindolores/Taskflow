using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentTenantService currentTenantService) : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<User> Users => Set<User>();

    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    public DbSet<TaskComment> TaskComments => Set<TaskComment>();

    public DbSet<Invitation> Invitations => Set<Invitation>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Entity<User>().HasQueryFilter(u => u.OrganizationId == currentTenantService.OrganizationId);
        modelBuilder.Entity<TaskItem>().HasQueryFilter(t => t.OrganizationId == currentTenantService.OrganizationId);
        modelBuilder.Entity<TaskComment>().HasQueryFilter(c => c.OrganizationId == currentTenantService.OrganizationId);
        modelBuilder.Entity<Invitation>().HasQueryFilter(i => i.OrganizationId == currentTenantService.OrganizationId);
    }
}
