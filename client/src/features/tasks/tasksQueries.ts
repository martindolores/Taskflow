import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import * as tasksApi from '@/api/tasksApi'
import type { TaskFormPayload, TaskListParams, UpdateTaskPayload } from '@/api/tasksApi'

export const taskKeys = {
  lists: ['tasks', 'list'] as const,
  list: (params: TaskListParams) => ['tasks', 'list', params] as const,
}

export function useTasksQuery(params: TaskListParams) {
  return useQuery({
    queryKey: taskKeys.list(params),
    queryFn: () => tasksApi.getTasks(params),
    placeholderData: (previousData) => previousData,
  })
}

export function useCreateTaskMutation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (payload: TaskFormPayload) => tasksApi.createTask(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: taskKeys.lists })
    },
  })
}

export function useUpdateTaskMutation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateTaskPayload }) =>
      tasksApi.updateTask(id, payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: taskKeys.lists })
    },
  })
}
