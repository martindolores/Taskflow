namespace TaskFlow.Application.Tasks.Exceptions;

public sealed class TaskNotFoundException(Guid taskId) : Exception($"Task '{taskId}' was not found.");
