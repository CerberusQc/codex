import { useState } from 'react';
import { useDataSources } from '../hooks/useDataSources';

type Creds = { host: string; port: string; user: string; password: string; url: string };
type FormState = { id: string; type: string; displayName: string } & Creds;

const emptyForm: FormState = {
  id: '', type: 'postgres', displayName: '',
  host: '', port: '5432', user: '', password: '', url: ''
};

function buildCredentials(f: FormState): Record<string, unknown> {
  if (f.type === 'http') return { url: f.url };
  return { host: f.host, port: Number(f.port) || 5432, user: f.user, password: f.password };
}

export default function DataSources() {
  const { data: sources = [], create, remove } = useDataSources();
  const [form, setForm] = useState<FormState | null>(null);
  const [saving, setSaving] = useState(false);

  function set<K extends keyof FormState>(key: K, value: FormState[K]) {
    setForm(prev => prev ? { ...prev, [key]: value } : null);
  }

  async function submit() {
    if (!form) return;
    setSaving(true);
    try {
      await create.mutateAsync({
        id: form.id,
        type: form.type,
        displayName: form.displayName,
        credentials: buildCredentials(form)
      });
      setForm(null);
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="max-w-3xl">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Data Sources</h1>
        <button
          className="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700 text-sm transition-colors"
          onClick={() => setForm({ ...emptyForm })}
        >
          + New Source
        </button>
      </div>

      <div className="flex flex-col gap-3">
        {sources.map(s => (
          <div key={s.id} className="border border-gray-200 rounded-lg p-4 flex justify-between items-center bg-white shadow-sm">
            <div>
              <span className="font-medium text-gray-900">{s.displayName}</span>
              <span className="ml-2 text-xs text-gray-400 uppercase bg-gray-100 px-1.5 py-0.5 rounded">{s.type}</span>
              <p className="text-xs text-gray-400 font-mono mt-0.5">{s.id}</p>
            </div>
            <button
              className="text-red-500 text-sm hover:underline"
              onClick={() => remove.mutate(s.id)}
            >
              Delete
            </button>
          </div>
        ))}
        {sources.length === 0 && (
          <p className="text-gray-400 text-sm">No datasources configured. Modules that require datasources will show MissingDatasource status.</p>
        )}
      </div>

      {form && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg shadow-xl w-96 p-6 flex flex-col gap-4">
            <h2 className="font-semibold text-lg text-gray-900">New Data Source</h2>

            <label className="flex flex-col gap-1">
              <span className="text-xs text-gray-500 uppercase font-medium">ID</span>
              <input
                className="border rounded px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="e.g. postgres:prod"
                value={form.id}
                onChange={e => set('id', e.target.value)}
              />
            </label>

            <label className="flex flex-col gap-1">
              <span className="text-xs text-gray-500 uppercase font-medium">Display Name</span>
              <input
                className="border rounded px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                value={form.displayName}
                onChange={e => set('displayName', e.target.value)}
              />
            </label>

            <label className="flex flex-col gap-1">
              <span className="text-xs text-gray-500 uppercase font-medium">Type</span>
              <select
                className="border rounded px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                value={form.type}
                onChange={e => set('type', e.target.value)}
              >
                <option value="postgres">PostgreSQL</option>
                <option value="mongodb">MongoDB</option>
                <option value="http">HTTP</option>
              </select>
            </label>

            {form.type === 'http' ? (
              <label className="flex flex-col gap-1">
                <span className="text-xs text-gray-500 uppercase font-medium">URL</span>
                <input
                  className="border rounded px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  placeholder="https://api.example.com"
                  value={form.url}
                  onChange={e => set('url', e.target.value)}
                />
              </label>
            ) : (
              <>
                {(['host', 'port', 'user'] as const).map(field => (
                  <label key={field} className="flex flex-col gap-1">
                    <span className="text-xs text-gray-500 uppercase font-medium">{field}</span>
                    <input
                      className="border rounded px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                      value={form[field]}
                      onChange={e => set(field, e.target.value)}
                    />
                  </label>
                ))}
                <label className="flex flex-col gap-1">
                  <span className="text-xs text-gray-500 uppercase font-medium">Password</span>
                  <input
                    type="password"
                    className="border rounded px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                    value={form.password}
                    onChange={e => set('password', e.target.value)}
                  />
                </label>
              </>
            )}

            <div className="flex justify-end gap-2 mt-2">
              <button
                className="px-4 py-1.5 rounded text-sm border hover:bg-gray-50 transition-colors"
                onClick={() => setForm(null)}
                disabled={saving}
              >
                Cancel
              </button>
              <button
                className="px-4 py-1.5 rounded text-sm bg-blue-600 text-white hover:bg-blue-700 disabled:opacity-50 transition-colors"
                onClick={submit}
                disabled={saving || !form.id || !form.displayName}
              >
                {saving ? 'Saving...' : 'Save'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
