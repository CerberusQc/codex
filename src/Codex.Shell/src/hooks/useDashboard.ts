import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '../api/client';
import type { DashboardPage } from '../types/api';

export function useDashboard() {
  const qc = useQueryClient();
  const query = useQuery({ queryKey: ['dashboard'], queryFn: api.dashboard.getPages });

  const setPages = useMutation({
    mutationFn: (pages: DashboardPage[]) => api.dashboard.setPages(pages),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['dashboard'] })
  });

  const enable = useMutation({
    mutationFn: (id: string) => api.dashboard.enable(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['dashboard'] })
  });

  const disable = useMutation({
    mutationFn: (id: string) => api.dashboard.disable(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['dashboard'] })
  });

  return { ...query, setPages, enable, disable };
}
