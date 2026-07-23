using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common;
using TaskFlow.Application.Projects;
using TaskFlow.Application.Projects.Dtos;
using TaskFlow.Application.Projects.Exceptions;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Projects;

public sealed class ProjectService(AppDbContext db, ICurrentUserService currentUserService) : IProjectService
{
    public async Task<IReadOnlyList<ProjectResponse>> GetProjectsAsync(CancellationToken cancellationToken) =>
        await db.Projects
            .OrderBy(p => p.Name)
            .Select(p => new ProjectResponse(p.Id, p.Name, p.Color))
            .ToListAsync(cancellationToken);

    public async Task<ProjectResponse> CreateProjectAsync(CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            OrganizationId = currentUserService.OrganizationId!.Value,
            Name = request.Name,
            Color = request.Color,
        };

        db.Projects.Add(project);
        await db.SaveChangesAsync(cancellationToken);

        return new ProjectResponse(project.Id, project.Name, project.Color);
    }

    public async Task DeleteProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await db.Projects.SingleOrDefaultAsync(p => p.Id == projectId, cancellationToken)
            ?? throw new ProjectNotFoundException(projectId);

        db.Projects.Remove(project);
        await db.SaveChangesAsync(cancellationToken);
    }
}
