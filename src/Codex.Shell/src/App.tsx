import { Navigate, Route, Routes } from 'react-router-dom';
import Layout from './components/Layout';
import ModulePageLoader from './components/ModulePageLoader';
import Store from './pages/Store';

function DataSources() { return <div className="text-2xl font-semibold text-gray-700">Data Sources</div>; }

export default function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route index element={<Navigate to="/store" replace />} />
        <Route path="store" element={<Store />} />
        <Route path="datasources" element={<DataSources />} />
        <Route path="mod/:moduleId" element={<ModulePageLoader />} />
      </Route>
    </Routes>
  );
}
