import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import * as projectsApi from '@/api/projectsApi'
import type { CreateProjectPayload } from '@/api/projectsApi'
import { showToast } from '@/components/toast'

export const projectKeys = {
  lists: ['projects', 'list'] as const,
}

export function useProjectsQuery(options?: { enabled?: boolean }) {
  return useQuery({
    queryKey: projectKeys.lists,
    queryFn: projectsApi.getProjects,
    enabled: options?.enabled ?? true,
  })
}

export function useCreateProjectMutation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (payload: CreateProjectPayload) => projectsApi.createProject(payload),
    meta: { suppressErrorToast: true },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: projectKeys.lists })
      showToast('Project created')
    },
  })
}

export function useDeleteProjectMutation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => projectsApi.deleteProject(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: projectKeys.lists })
      showToast('Project deleted')
    },
  })
}
