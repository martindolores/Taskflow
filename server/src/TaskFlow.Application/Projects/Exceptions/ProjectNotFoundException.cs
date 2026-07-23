namespace TaskFlow.Application.Projects.Exceptions;

public sealed class ProjectNotFoundException(Guid projectId) : Exception($"Project '{projectId}' was not found.");
