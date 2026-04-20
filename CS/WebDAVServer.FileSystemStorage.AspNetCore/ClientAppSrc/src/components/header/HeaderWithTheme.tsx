import { useState } from 'react';
import { ThemeToggle } from './ThemeToggle';
import { Search } from '@/components/search/Search';
import type { AppShellServices } from '@/app/composition/createAppShellServices';
import type { FileBrowserViewModel } from '@/features/hooks/useFileBrowser';
import { t } from '@/shared/i18n/translate';

interface Props {
  fileBrowser: FileBrowserViewModel;
  appShellServices: AppShellServices;
}

export function HeaderWithTheme({ fileBrowser, appShellServices }: Props) {
  const [isOpen, setIsOpen] = useState(false);

  const handleFolderClick = () => {
    if (fileBrowser.currentFolderPath) {
      appShellServices.openFolderInOsFileManager(fileBrowser.currentFolderPath);
    }
  };

  const handleOpenTestsWindow = () => {
    const width = Math.round(screen.width * 0.5);
    const height = Math.round(screen.height * 0.8);
    window.open(
      `/AjaxFileBrowser/AjaxIntegrationTests.html#${appShellServices.websiteRootUrl}`,
      '',
      `menubar=1,location=1,status=1,scrollbars=1,resizable=1,width=${width},height=${height}`
    );
  };

  return (
    <>
      <div className="bg-surface-third pl-6 pr-4 py-1 border-light border-b border-b-border bg-light flex items-center gap-4">
        <div className="flex items-center gap-4 shrink-0">
          <i className="icon icon-logo w-8 h-8 block" />
          <h1 className="text-xl font-regular whitespace-nowrap text-primary-icon hidden md:block">
            IT Hit WebDAV Server Engine {appShellServices.webDavServerVersion}
          </h1>
        </div>

        <div className="flex-1 min-w-0 flex justify-center">
          <div className="w-full max-w-125">
            <Search fileBrowser={fileBrowser} />
          </div>
        </div>

        <div className="flex gap-3 shrink-0">
          <ThemeToggle />
          <button
            className="flex flex-col justify-center items-center w-9 h-9 gap-1.5 rounded hover:bg-surface-hover transition-colors"
            aria-expanded={isOpen}
            aria-label={t('phrases.header.toggleMenu')}
            onClick={() => setIsOpen(v => !v)}
          >
            <span
              className={`block w-5 h-0.5 bg-text transition-all duration-300 origin-center ${
                isOpen ? 'rotate-45 translate-y-2' : ''
              }`}
            />
            <span
              className={`block w-5 h-0.5 bg-text transition-all duration-300 ${
                isOpen ? 'opacity-0 scale-x-0' : ''
              }`}
            />
            <span
              className={`block w-5 h-0.5 bg-text transition-all duration-300 origin-center ${
                isOpen ? '-rotate-45 -translate-y-2' : ''
              }`}
            />
          </button>
        </div>
      </div>

      <div
        className={`transition-all duration-300 bg-surface border-b border-border ${
          isOpen ? 'max-h-[calc(100vh-3rem)] opacity-100 overflow-auto' : 'max-h-0 opacity-0'
        }`}
      >
        <div className="px-6 py-5">
          <p className="text-secondary mb-5">{t('phrases.header.pageDescription')}</p>

          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div className="panel-card">
              <ul className="mb-0 [&_a]:text-blue-600 dark:[&_a]:text-blue-400 [&_a]:underline [&_a]:hover:text-blue-800 dark:[&_a]:hover:text-blue-300">
                <li>
                  <a href="https://www.webdavsystem.com/server/" target="_blank" rel="noreferrer">
                    IT Hit WebDAV Server Engine for .NET
                  </a>
                  :{' '}
                  <span className="webdav-server-version">
                    {appShellServices.webDavServerVersion}
                  </span>
                </li>
                <li>
                  <a href="https://www.webdavsystem.com/ajax/" target="_blank" rel="noreferrer">
                    IT Hit WebDAV AJAX Library
                  </a>
                  : {appShellServices.webDavClientVersion}
                </li>
                {appShellServices.isIntegratedProject ? (
                  <li>
                    <a href="https://www.userfilesystem.com/" target="_blank" rel="noreferrer">
                      IT Hit User File System
                    </a>
                    : v9.0.29575
                  </li>
                ) : null}
                {appShellServices.isIntegratedProject ? (
                  <li>
                    <a href="https://www.webdavsystem.com/client/" target="_blank" rel="noreferrer">
                      IT Hit WebDAV Client Library
                    </a>
                    : v7.1.5051
                  </li>
                ) : null}
              </ul>
            </div>

            <div className="panel-card">
              <h3 className="text-lg font-semibold mb-2">{t('phrases.header.testYourServer')}</h3>
              <p className="mb-3">{t('phrases.header.testYourServerDescription')}</p>
              <button
                className="align-self-start btn btn-primary mt-auto"
                onClick={handleOpenTestsWindow}
              >
                {t('phrases.header.runIntegrationTests')}
              </button>
            </div>

            <div className="panel-card">
              <h3 className="text-lg font-semibold mb-2">{t('phrases.header.mountWebDAVDrive')}</h3>
              <p className="mb-3">{t('phrases.header.mountWebDAVDriveDescription')}</p>
              <button
                className="align-self-start btn btn-primary mt-auto"
                onClick={handleFolderClick}
              >
                {t('phrases.header.browseUsingOsFileManager')}
              </button>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}
