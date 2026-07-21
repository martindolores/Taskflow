namespace TaskFlow.Application.Tasks.Exceptions;

public sealed class InvalidAssigneeException(Guid assigneeId) : Exception($"User '{assigneeId}' cannot be assigned to this task.");
