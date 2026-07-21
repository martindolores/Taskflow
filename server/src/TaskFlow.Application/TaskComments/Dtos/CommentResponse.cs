namespace TaskFlow.Application.TaskComments.Dtos;

public sealed record CommentResponse(Guid Id, string Body, Guid AuthorId, string AuthorName, DateTime CreatedAt);
