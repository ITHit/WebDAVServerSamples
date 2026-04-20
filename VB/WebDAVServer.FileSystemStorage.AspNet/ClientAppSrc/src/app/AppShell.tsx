import { useMemo } from 'react';
import { getAppServices } from '@/app/appServices';
import { createAppShellServices } from '@/app/composition/createAppShellServices';
import { HeaderWithTheme } from '@/components/header/HeaderWithTheme';
import { InnerContainer } from '@/components/InnerContainer';
import { useFileBrowser } from '@/features/hooks/useFileBrowser';

export function AppShell() {
  const appServices = useMemo(() => getAppServices(), []);
  const services = useMemo(() => createAppShellServices(appServices), [appServices]);
  const fileBrowser = useFileBrowser();

  return (
    <div className="flex flex-col h-dvh overflow-hidden">
      <header className="flex-none">
        <HeaderWithTheme fileBrowser={fileBrowser} appShellServices={services} />
      </header>
      <div className="flex-1 min-h-0 w-full px-4 flex flex-col">
        <InnerContainer fileBrowser={fileBrowser} />
      </div>
    </div>
  );
}
