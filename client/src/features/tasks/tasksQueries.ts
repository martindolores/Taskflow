import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import * as tasksApi from '@/api/tasksApi'
import type { TaskFormPayload, TaskListParams, UpdateTaskPayload } from '@/api/tasksApi'
import { showToast } from '@/components/toast'

export const taskKeys = {
  lists: ['tasks', 'list'] as const,
  list: (params: TaskListParams) => ['tasks', 'list', params] as const,
  detail: (id: string) => ['tasks', 'detail', id] as const,
}

export function useTasksQuery(params: TaskListParams, options?: { enabled?: boolean }) {
  return useQuery({
    queryKey: taskKeys.list(params),
    queryFn: () => tasksApi.getTasks(params),
    placeholderData: (previousData) => previousData,
    enabled: options?.enabled ?? true,
  })
}

export function useTaskQuery(id: string) {
  return useQuery({
    queryKey: taskKeys.detail(id),
    queryFn: () => tasksApi.getTask(id),
  })
}

export function useCreateTaskMutation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (payload: TaskFormPayload) => tasksApi.createTask(payload),
    meta: { suppressErrorToast: true },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: taskKeys.lists })
      showToast('Task created')
    },
  })
}

export function useUpdateTaskMutation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateTaskPayload }) =>
      tasksApi.updateTask(id, payload),
    meta: { suppressErrorToast: true },
    onSuccess: (_data, { id }) => {
      void queryClient.invalidateQueries({ queryKey: taskKeys.lists })
      void queryClient.invalidateQueries({ queryKey: taskKeys.detail(id) })
      showToast('Task updated')
    },
  })
}

export function useDeleteTaskMutation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => tasksApi.deleteTask(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: taskKeys.lists })
      showToast('Task deleted')
    },
  })
}
