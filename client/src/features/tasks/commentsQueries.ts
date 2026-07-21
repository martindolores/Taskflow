import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import * as commentsApi from '@/api/commentsApi'

export const commentKeys = {
  list: (taskId: string) => ['tasks', taskId, 'comments'] as const,
}

export function useCommentsQuery(taskId: string) {
  return useQuery({
    queryKey: commentKeys.list(taskId),
    queryFn: () => commentsApi.getComments(taskId),
  })
}

export function useCreateCommentMutation(taskId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: string) => commentsApi.createComment(taskId, body),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: commentKeys.list(taskId) })
    },
  })
}

export function useDeleteCommentMutation(taskId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (commentId: string) => commentsApi.deleteComment(taskId, commentId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: commentKeys.list(taskId) })
    },
  })
}
