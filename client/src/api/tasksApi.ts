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
  projectId: string | null
  createdAt: string
}

export interface TaskListResult {
  items: TaskListItem[]
  total: number
  page: number
  pageSize: number
}

export interface TaskDetail {
  id: string
  title: string
  description: string | null
  status: TaskStatus
  priority: TaskPriority
  assigneeId: string | null
  dueDate: string | null
  projectId: string | null
  createdById: string
  createdAt: string
  updatedAt: string
}

export interface TaskListParams {
  page: number
  pageSize: number
  status?: TaskStatus
}

export interface TaskFormPayload {
  title: string
  description?: string
  priority: TaskPriority
  assigneeId?: string
  dueDate?: string
  projectId?: string
}

export interface UpdateTaskPayload extends TaskFormPayload {
  status: TaskStatus
}

export async function getTasks(params: TaskListParams): Promise<TaskListResult> {
  const { data } = await apiClient.get<TaskListResult>('/api/tasks', { params })
  return data
}

export async function createTask(payload: TaskFormPayload): Promise<TaskDetail> {
  const { data } = await apiClient.post<TaskDetail>('/api/tasks', payload)
  return data
}

export async function updateTask(id: string, payload: UpdateTaskPayload): Promise<TaskDetail> {
  const { data } = await apiClient.put<TaskDetail>(`/api/tasks/${id}`, payload)
  return data
}

export async function getTask(id: string): Promise<TaskDetail> {
  const { data } = await apiClient.get<TaskDetail>(`/api/tasks/${id}`)
  return data
}

export async function deleteTask(id: string): Promise<void> {
  await apiClient.delete(`/api/tasks/${id}`)
}
