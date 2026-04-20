import { DownloadDriveModal } from '@/components/modals/DownloadDriveModal';
import type { AppShellServices } from '@/app/composition/createAppShellServices';
import { showModalComponent } from '@/shared/composables/useModalRegistry';
import { t } from '@/shared/i18n/translate';

interface Props {
  currentFolderPath: string;
  appShellServices: AppShellServices;
}

export function WebDavDriveButtons({ currentFolderPath, appShellServices }: Props) {
  const driveName = appShellServices.driveProjectName;

  const handleDownloadClick = () => {
    if (appShellServices.isIntegratedProject) {
      void showModalComponent(DownloadDriveModal as never, {
        appName: driveName,
        serverRootUrl: appShellServices.websiteRootUrl,
      });
      return;
    }

    window.open(appShellServices.getInstallerFileUrl(), '_blank', 'noopener,noreferrer');
  };

  const handleFolderClick = () => {
    if (!currentFolderPath) {
      return;
    }
    appShellServices.openFolderInOsFileManager(currentFolderPath);
  };

  return (
    <div className="flex gap-3">
      <button
        className="btn btn-primary text-sm font-medium hidden lg:block"
        onClick={handleFolderClick}
      >
        {t('phrases.header.browseUsingOsFileManager')}
      </button>
      <button
        id="ithit-webdav-drive"
        className="inline-flex items-center overflow-hidden rounded-md bg-primary hover:bg-primary-hover text-white text-sm font-medium transition-colors duration-200 cursor-pointer"
        type="button"
        title={t('phrases.downloadDrive.downloadTitle', { appName: driveName })}
        onClick={handleDownloadClick}
      >
        <span className="flex items-center justify-center px-2.5 py-1.5 bg-black/20 h-full">
          <i className="icon icon-webdav-drive bg-white!" />
        </span>
        <span className="hidden lg:inline-block px-3 py-1.5">
          {t('phrases.downloadDrive.downloadTitle', { appName: driveName })}
        </span>
      </button>
    </div>
  );
}
