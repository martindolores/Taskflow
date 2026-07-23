namespace TaskFlow.Application.Projects.Dtos;

public sealed record CreateProjectRequest(string Name, string Color, string? Description = null);
