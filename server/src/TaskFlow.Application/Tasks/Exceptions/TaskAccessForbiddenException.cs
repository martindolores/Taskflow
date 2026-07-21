namespace TaskFlow.Application.Tasks.Exceptions;

public sealed class TaskAccessForbiddenException(Guid taskId) : Exception($"Not permitted to modify task '{taskId}'.");
