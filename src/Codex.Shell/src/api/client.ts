import type { ModuleEntry, DashboardPage, DataSourceEntry } from '../types/api';

const base = '/api/core';

async function req<T>(path: string, opts?: RequestInit): Promise<T> {
  const res = await fetch(`${base}${path}`, {
    headers: { 'Content-Type': 'application/json' },
    ...opts
  });
  if (!res.ok) throw new Error(await res.text());
  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}

export const api = {
  modules: {
    list: () => req<ModuleEntry[]>('/modules'),
    buildLog: (id: string) => req<{ buildLog: string }>(`/modules/${id}/buildlog`)
  },
  dashboard: {
    getPages: () => req<DashboardPage[]>('/dashboard/pages'),
    setPages: (pages: DashboardPage[]) =>
      req<void>('/dashboard/pages', { method: 'PUT', body: JSON.stringify(pages) }),
    enable: (id: string) => req<void>(`/dashboard/pages/${id}/enable`, { method: 'POST' }),
    disable: (id: string) => req<void>(`/dashboard/pages/${id}`, { method: 'DELETE' })
  },
  datasources: {
    list: () => req<DataSourceEntry[]>('/datasources'),
    create: (body: { id: string; type: string; displayName: string; credentials: Record<string, unknown> }) =>
      req<DataSourceEntry>('/datasources', { method: 'POST', body: JSON.stringify(body) }),
    update: (id: string, body: { displayName: string; credentials: Record<string, unknown> }) =>
      req<void>(`/datasources/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
    delete: (id: string) => req<void>(`/datasources/${id}`, { method: 'DELETE' })
  }
};
