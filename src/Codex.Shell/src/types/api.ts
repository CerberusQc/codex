export interface ModuleEntry {
  id: string;
  displayName: string;
  version: string;
  description?: string;
  icon?: string;
  pageRoute: string;
  status: 'Discovered' | 'Building' | 'BuildFailed' | 'Loaded' | 'LoadFailed' | 'MissingDatasource';
  buildLog?: string;
  loadedAt?: string;
}

export interface DashboardPage {
  moduleId: string;
  order: number;
  isFavorite: boolean;
}

export interface DataSourceEntry {
  id: string;
  type: string;
  displayName: string;
}
