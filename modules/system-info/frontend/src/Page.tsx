import { useEffect, useState } from 'react';

interface Stats {
  machineName: string;
  processorCount: number;
  osVersion: string;
  uptimeSeconds: number;
  workingSetMb: number;
  dotnetVersion: string;
}

export default function Page() {
  const [stats, setStats] = useState<Stats | null>(null);

  function load() {
    fetch('/api/mod/system-info/stats').then(r => r.json()).then(setStats);
  }

  useEffect(() => { load(); }, []);

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-4">
        <h1 className="text-2xl font-bold">System Info</h1>
        <button onClick={load} className="text-sm bg-gray-100 px-3 py-1 rounded hover:bg-gray-200">Refresh</button>
      </div>
      {stats ? (
        <div className="grid grid-cols-2 gap-4 max-w-lg">
          {Object.entries(stats).map(([k, v]) => (
            <div key={k} className="bg-gray-50 border rounded p-3">
              <p className="text-xs text-gray-400 uppercase">{k}</p>
              <p className="font-mono text-sm mt-1">{String(v)}</p>
            </div>
          ))}
        </div>
      ) : (
        <p className="text-gray-400">Loading...</p>
      )}
    </div>
  );
}
