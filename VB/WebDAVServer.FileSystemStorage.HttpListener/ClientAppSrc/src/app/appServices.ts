import { WebDavFileSystemRepository } from '@/infrastructure/repositories/WebDavFileSystemRepository';
import { WebDavSettings } from '@/infrastructure/config/webDavSettings';
import { WebDavClient } from '@/infrastructure/webdav/WebDavClient';

const webDavClient = new WebDavClient();
const fileSystemRepository = new WebDavFileSystemRepository(webDavClient);

export function getAppServices() {
  return {
    webDavClient,
    fileSystemRepository,
    webDavServerVersion: WebDavSettings.WebDavServerVersion,
    websiteRootUrl: WebDavSettings.WebsiteRootUrl,
    isIntegratedProject: WebDavSettings.IsIntegratedProject,
    driveProjectName: WebDavSettings.DriveProjectName || 'WebDAV Drive',
    searchInFolder: fileSystemRepository.searchInFolder.bind(fileSystemRepository),
    manageDocuments: webDavClient.manageDocuments.bind(webDavClient),
    isDavProtocolSupported: () => webDavClient.isDavProtocolSupported(),
    openItemCallback: webDavClient.openItemCallback.bind(webDavClient),
    createUploaderCore: webDavClient.createUploaderCore.bind(webDavClient),
    getWebDavClientVersion: () => WebDavClient.getWebdavClientVersion(),
    getInstallerFileUrl: () => WebDavClient.getInstallerFileUrl(),
    openFolderInOsFileManager: (folderPath: string, showProtocolInstallModal: () => void) =>
      WebDavClient.openFolderInOsFileManager(
        folderPath,
        WebDavSettings.WebsiteRootUrl,
        showProtocolInstallModal,
        null,
        WebDavSettings.EditDocAuth.SearchIn ?? '',
        WebDavSettings.EditDocAuth.CookieNames ?? '',
        WebDavSettings.EditDocAuth.LoginUrl ?? ''
      ),
  };
}
