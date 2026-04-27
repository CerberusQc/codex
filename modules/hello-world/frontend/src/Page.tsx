import { useEffect, useState } from 'react';

export default function Page() {
  const [data, setData] = useState<{ message: string; timestamp: string } | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetch('/api/mod/hello-world/ping')
      .then(r => r.json())
      .then(setData)
      .catch(e => setError(e.message));
  }, []);

  return (
    <div className="p-6">
      <h1 className="text-2xl font-bold mb-4">Hello World</h1>
      {error && <p className="text-red-500">{error}</p>}
      {data && (
        <div className="bg-green-50 border border-green-200 rounded p-4">
          <p className="text-green-800 font-medium">{data.message}</p>
          <p className="text-green-600 text-sm mt-1">{data.timestamp}</p>
        </div>
      )}
      {!data && !error && <p className="text-gray-400">Loading...</p>}
    </div>
  );
}
