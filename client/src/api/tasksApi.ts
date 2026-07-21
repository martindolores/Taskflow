import { apiClient } from './client'

export type TaskStatus = 'ToDo' | 'InProgress' | 'Done'
export type TaskPriority = 'Low' | 'Medium' | 'High'

export interface TaskListItem {
  id: string
  title: string
  status: TaskStatus
  priority: TaskPriority
  assigneeId: string | null
  assigneeName: string | null
  dueDate: string | null
  createdAt: string
}

export interface TaskListResult {
  items: TaskListItem[]
  total: number
  page: number
  pageSize: number
}

export interface TaskListParams {
  page: number
  pageSize: number
}

export async function getTasks(params: TaskListParams): Promise<TaskListResult> {
  const { data } = await apiClient.get<TaskListResult>('/api/tasks', { params })
  return data
}
