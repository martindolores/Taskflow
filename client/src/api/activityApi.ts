import { apiClient } from './client'

export type ActivityType =
  'TaskCreated' | 'TaskStatusChanged' | 'TaskAssigned' | 'CommentAdded' | 'MemberInvited'

export interface ActivityItem {
  id: string
  actorId: string
  actorName: string
  taskId: string | null
  type: ActivityType
  summary: string
  createdAt: string
}

export async function getActivity(params?: { limit?: number }): Promise<ActivityItem[]> {
  const { data } = await apiClient.get<ActivityItem[]>('/api/activity', { params })
  return data
}
