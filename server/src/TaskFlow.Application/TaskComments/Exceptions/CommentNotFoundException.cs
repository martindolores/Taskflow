namespace TaskFlow.Application.TaskComments.Exceptions;

public sealed class CommentNotFoundException(Guid commentId) : Exception($"Comment '{commentId}' was not found.");
