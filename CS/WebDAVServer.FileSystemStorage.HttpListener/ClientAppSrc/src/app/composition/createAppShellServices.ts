import { getAppServices } from '@/app/appServices';
import { InstallProtocolModal } from '@/components/modals/InstallProtocolModal';
import { showModalComponent } from '@/shared/composables/useModalRegistry';

type AppServices = ReturnType<typeof getAppServices>;

export interface AppShellServices {
  webDavServerVersion: string;
  webDavClientVersion: string;
  websiteRootUrl: string;
  isIntegratedProject: boolean;
  driveProjectName: string;
  getInstallerFileUrl: () => string;
  openFolderInOsFileManager: (folderPath: string) => void;
}

export function createAppShellServices(appServices: AppServices): AppShellServices {
  return {
    webDavServerVersion: appServices.webDavServerVersion,
    webDavClientVersion: appServices.getWebDavClientVersion(),
    websiteRootUrl: appServices.websiteRootUrl,
    isIntegratedProject: appServices.isIntegratedProject,
    driveProjectName: appServices.driveProjectName,
    getInstallerFileUrl: appServices.getInstallerFileUrl,
    openFolderInOsFileManager: (folderPath: string) => {
      appServices.openFolderInOsFileManager(folderPath, () => {
        void showModalComponent(InstallProtocolModal);
      });
    },
  };
}
