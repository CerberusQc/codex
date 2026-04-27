import { Suspense, useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';

// Avoid re-init for already-registered remotes
const registeredModules = new Set<string>();

async function loadModulePage(moduleId: string): Promise<React.ComponentType> {
  const { init, loadRemote } = await import('@module-federation/runtime');

  if (!registeredModules.has(moduleId)) {
    await init({
      name: 'codex-shell',
      remotes: [
        {
          name: moduleId,
          entry: `/assets/modules/${moduleId}/remoteEntry.js`
        }
      ],
      shared: {
        react: { version: '18.0.0', strategy: 'loaded-first' },
        'react-dom': { version: '18.0.0', strategy: 'loaded-first' }
      }
    });
    registeredModules.add(moduleId);
  }

  const mod = await loadRemote<{ default: React.ComponentType }>(`${moduleId}/Page`);
  if (!mod) throw new Error(`Module ${moduleId} did not export a Page component`);
  return mod.default;
}

export default function ModulePageLoader() {
  const { moduleId } = useParams<{ moduleId: string }>();
  const [Page, setPage] = useState<React.ComponentType | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!moduleId) return;
    setPage(null);
    setError(null);
    loadModulePage(moduleId)
      .then(Component => setPage(() => Component))
      .catch(e => setError(e instanceof Error ? e.message : String(e)));
  }, [moduleId]);

  if (error) {
    return (
      <div className="p-6">
        <div className="bg-red-50 border border-red-200 rounded p-4 text-red-700">
          <p className="font-medium">Failed to load module</p>
          <p className="text-sm mt-1 font-mono">{error}</p>
        </div>
      </div>
    );
  }

  if (!Page) {
    return <div className="p-6 text-gray-400 animate-pulse">Loading module...</div>;
  }

  return (
    <Suspense fallback={<div className="p-6 text-gray-400">Loading...</div>}>
      <Page />
    </Suspense>
  );
}
