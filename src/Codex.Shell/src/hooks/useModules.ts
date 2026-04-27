import { useQuery } from '@tanstack/react-query';
import { api } from '../api/client';

export function useModules() {
  return useQuery({
    queryKey: ['modules'],
    queryFn: api.modules.list,
    refetchInterval: 5000
  });
}
