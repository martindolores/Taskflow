import { apiClient } from './client'

export interface Comment {
  id: string
  body: string
  authorId: string
  authorName: string
  createdAt: string
}

export async function getComments(taskId: string): Promise<Comment[]> {
  const { data } = await apiClient.get<Comment[]>(`/api/tasks/${taskId}/comments`)
  return data
}

export async function createComment(taskId: string, body: string): Promise<Comment> {
  const { data } = await apiClient.post<Comment>(`/api/tasks/${taskId}/comments`, { body })
  return data
}

export async function deleteComment(taskId: string, commentId: string): Promise<void> {
  await apiClient.delete(`/api/tasks/${taskId}/comments/${commentId}`)
}
