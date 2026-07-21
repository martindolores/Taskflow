import { useQuery } from '@tanstack/react-query'
import * as tasksApi from '@/api/tasksApi'
import type { TaskListParams } from '@/api/tasksApi'

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
