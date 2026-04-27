import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '../api/client';

export function useDataSources() {
  const qc = useQueryClient();
  const query = useQuery({ queryKey: ['datasources'], queryFn: api.datasources.list });

  const create = useMutation({
    mutationFn: api.datasources.create,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['datasources'] })
  });

  const remove = useMutation({
    mutationFn: (id: string) => api.datasources.delete(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['datasources'] })
  });

  return { ...query, create, remove };
}
