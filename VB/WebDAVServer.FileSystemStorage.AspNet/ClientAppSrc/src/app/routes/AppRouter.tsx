import { Route, Routes } from 'react-router-dom';
import { AppShell } from '@/app/AppShell';

export function AppRouter() {
  return (
    <Routes>
      <Route path="/*" element={<AppShell />} />
    </Routes>
  );
}
