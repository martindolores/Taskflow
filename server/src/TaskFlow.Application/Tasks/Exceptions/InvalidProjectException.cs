namespace TaskFlow.Application.Tasks.Exceptions;

public sealed class InvalidProjectException(Guid projectId) : Exception($"Project '{projectId}' cannot be assigned to this task.");
