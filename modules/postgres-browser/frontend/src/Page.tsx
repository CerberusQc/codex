import { useEffect, useState } from 'react';

interface Table { name: string; rowEstimate: number; }

export default function Page() {
  const [tables, setTables] = useState<Table[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetch('/api/mod/postgres-browser/tables')
      .then(r => r.json())
      .then(d => { if (d.error) setError(d.error); else setTables(d.tables); })
      .catch(e => setError(e.message));
  }, []);

  return (
    <div className="p-6">
      <h1 className="text-2xl font-bold mb-4">Postgres Browser</h1>
      {error && (
        <div className="bg-orange-50 border border-orange-200 rounded p-4 text-orange-700">
          {error} — configure the <strong>postgres:demo</strong> datasource in Sources.
        </div>
      )}
      {tables.length > 0 && (
        <table className="w-full border-collapse text-sm">
          <thead>
            <tr className="bg-gray-50 border-b">
              <th className="text-left px-4 py-2 font-medium text-gray-600">Table</th>
              <th className="text-right px-4 py-2 font-medium text-gray-600">~Rows</th>
            </tr>
          </thead>
          <tbody>
            {tables.map(t => (
              <tr key={t.name} className="border-b hover:bg-gray-50">
                <td className="px-4 py-2 font-mono">{t.name}</td>
                <td className="px-4 py-2 text-right text-gray-500">{t.rowEstimate.toLocaleString()}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      {!error && tables.length === 0 && <p className="text-gray-400">No tables found or datasource not configured.</p>}
    </div>
  );
}
