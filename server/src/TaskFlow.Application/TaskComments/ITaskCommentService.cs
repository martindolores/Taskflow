using TaskFlow.Application.TaskComments.Dtos;

namespace TaskFlow.Application.TaskComments;

public interface ITaskCommentService
{
    Task<IReadOnlyList<CommentResponse>> GetCommentsAsync(Guid taskId, CancellationToken cancellationToken);

    Task<CommentResponse> CreateCommentAsync(Guid taskId, CreateCommentRequest request, CancellationToken cancellationToken);

    Task DeleteCommentAsync(Guid taskId, Guid commentId, CancellationToken cancellationToken);
}
