using TaskFlow.Application.Projects.Dtos;

namespace TaskFlow.Application.Projects;

public interface IProjectService
{
    Task<IReadOnlyList<ProjectResponse>> GetProjectsAsync(CancellationToken cancellationToken);

    Task<ProjectResponse> CreateProjectAsync(CreateProjectRequest request, CancellationToken cancellationToken);

    Task DeleteProjectAsync(Guid projectId, CancellationToken cancellationToken);
}
