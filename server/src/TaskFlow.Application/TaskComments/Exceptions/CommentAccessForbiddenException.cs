namespace TaskFlow.Application.TaskComments.Exceptions;

public sealed class CommentAccessForbiddenException(Guid commentId) : Exception($"Not permitted to modify comment '{commentId}'.");
