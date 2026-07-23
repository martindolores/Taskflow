import { useQuery } from '@tanstack/react-query'
import * as activityApi from '@/api/activityApi'

export const activityKeys = {
  list: (limit: number) => ['activity', 'list', limit] as const,
}

export function useActivityQuery(limit = 20) {
  return useQuery({
    queryKey: activityKeys.list(limit),
    queryFn: () => activityApi.getActivity({ limit }),
  })
}
