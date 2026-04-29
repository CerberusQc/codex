import { Suspense, useEffect, useState } from 'react';
import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { useParams } from 'react-router-dom';

// Share scope passed to every remote container so they use the shell's React
// rather than their own bundled copy. Two React instances break hooks because
// useState/useEffect rely on the active dispatcher set by the running renderer.
const reactVer = React.version;
const SHARE_SCOPE = {
  react: {
    [reactVer]: { version: reactVer, scope: ['default'], get: () => Promise.resolve(() => React), from: 'codex-shell', loaded: true }
  },
  'react-dom': {
    [reactVer]: { version: reactVer, scope: ['default'], get: () => Promise.resolve(() => ReactDOM), from: 'codex-shell', loaded: true }
  }
};

interface MFContainer {
  init: (shareScope: Record<string, unknown>, initScope?: unknown[]) => Promise<unknown>;
  get: (module: string) => Promise<() => { default: React.ComponentType }>;
}

const loadedContainers = new Map<string, MFContainer>();

async function loadModulePage(moduleId: string): Promise<React.ComponentType> {
  let container = loadedContainers.get(moduleId);

  if (!container) {
    const entryUrl = `/assets/modules/${moduleId}/remoteEntry.js`;
    container = await import(/* @vite-ignore */ entryUrl) as unknown as MFContainer;
    await container.init(SHARE_SCOPE).catch(() => {});
    loadedContainers.set(moduleId, container);
  }

  const factory = await container.get('./Page');
  const mod = factory();
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
