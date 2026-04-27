import { useState } from 'react';
import { useModules } from '../hooks/useModules';
import { useDashboard } from '../hooks/useDashboard';
import { api } from '../api/client';

const statusColor: Record<string, string> = {
  Loaded: 'text-green-600',
  Building: 'text-yellow-500',
  BuildFailed: 'text-red-600',
  LoadFailed: 'text-red-600',
  MissingDatasource: 'text-orange-500',
  Discovered: 'text-gray-400',
  Unloading: 'text-gray-400',
};

export default function Store() {
  const { data: modules = [], isLoading } = useModules();
  const { data: pages = [], enable, disable } = useDashboard();
  const [buildLog, setBuildLog] = useState<{ id: string; log: string } | null>(null);
  const [logLoading, setLogLoading] = useState(false);

  const enabledIds = new Set(pages.map(p => p.moduleId));

  async function viewLog(id: string) {
    setLogLoading(true);
    try {
      const { buildLog: log } = await api.modules.buildLog(id);
      setBuildLog({ id, log: log ?? '(no log available)' });
    } finally {
      setLogLoading(false);
    }
  }

  return (
    <div className="max-w-3xl">
      <h1 className="text-2xl font-bold mb-6 text-gray-900">Module Store</h1>
      {isLoading && <p className="text-gray-400 text-sm">Loading modules...</p>}
      <div className="flex flex-col gap-4">
        {modules.map(m => (
          <div key={m.id} className="border border-gray-200 rounded-lg p-4 bg-white shadow-sm">
            <div className="flex items-start justify-between gap-4">
              <div className="min-w-0">
                <div className="flex items-center gap-2">
                  {m.icon && <span>{m.icon}</span>}
                  <span className="font-semibold text-gray-900">{m.displayName}</span>
                  <span className="text-xs text-gray-400">v{m.version}</span>
                </div>
                {m.description && <p className="text-sm text-gray-500 mt-1">{m.description}</p>}
                <p className="text-xs text-gray-300 font-mono mt-0.5">{m.id}</p>
              </div>
              <div className="flex gap-2 items-center shrink-0">
                <span className={`text-xs font-medium ${statusColor[m.status] ?? 'text-gray-500'}`}>
                  ● {m.status}
                </span>
                <button
                  className="text-xs underline text-blue-500 hover:text-blue-700"
                  onClick={() => viewLog(m.id)}
                  disabled={logLoading}
                >
                  Build log
                </button>
                {m.status === 'Loaded' && (
                  enabledIds.has(m.id) ? (
                    <button
                      className="text-xs bg-gray-200 px-2 py-1 rounded hover:bg-gray-300 transition-colors"
                      onClick={() => disable.mutate(m.id)}
                    >
                      Enabled ✓
                    </button>
                  ) : (
                    <button
                      className="text-xs bg-blue-600 text-white px-2 py-1 rounded hover:bg-blue-700 transition-colors"
                      onClick={() => enable.mutate(m.id)}
                    >
                      Enable
                    </button>
                  )
                )}
              </div>
            </div>
          </div>
        ))}
        {!isLoading && modules.length === 0 && (
          <p className="text-gray-400 text-sm">
            No modules discovered yet. The poller checks every 30 seconds.
          </p>
        )}
      </div>

      {/* Build log modal */}
      {buildLog && (
        <div
          className="fixed inset-0 bg-black/50 flex items-center justify-center z-50"
          onClick={() => setBuildLog(null)}
        >
          <div
            className="bg-white rounded-lg shadow-xl w-2/3 max-h-[75vh] flex flex-col"
            onClick={e => e.stopPropagation()}
          >
            <div className="flex justify-between items-center p-4 border-b">
              <h2 className="font-semibold text-gray-900">Build log: {buildLog.id}</h2>
              <button onClick={() => setBuildLog(null)} className="text-gray-400 hover:text-gray-800 text-xl">✕</button>
            </div>
            <pre className="p-4 text-xs overflow-auto flex-1 bg-gray-950 text-green-400 font-mono whitespace-pre-wrap">
              {buildLog.log}
            </pre>
          </div>
        </div>
      )}
    </div>
  );
}
