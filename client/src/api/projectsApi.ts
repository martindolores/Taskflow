import { apiClient } from './client'

export interface Project {
  id: string
  name: string
  color: string
  description: string | null
}

export interface CreateProjectPayload {
  name: string
  color: string
  description?: string
}

export async function getProjects(): Promise<Project[]> {
  const { data } = await apiClient.get<Project[]>('/api/projects')
  return data
}

export async function createProject(payload: CreateProjectPayload): Promise<Project> {
  const { data } = await apiClient.post<Project>('/api/projects', payload)
  return data
}

export async function deleteProject(id: string): Promise<void> {
  await apiClient.delete(`/api/projects/${id}`)
}
