import { EditDocAuth } from './editDocAuth';

const isDev = import.meta.env.MODE === 'development';

export class WebDavSettings {
  static WebsiteRootUrl: string = isDev
    ? WebDavSettings._getWebsiteRootUrl(import.meta.env.VITE_WEBDAV_SERVER_PATH)
    : window.webDavSettings && window.webDavSettings.WebDavServerPath
      ? WebDavSettings._getWebsiteRootUrl(window.webDavSettings.WebDavServerPath)
      : '';

  static WebSocketPath: string = isDev
    ? WebDavSettings._getWebsiteRootUrl(import.meta.env.VITE_WEBDAV_SOCKET_PATH)
    : window.webDavSettings && window.webDavSettings.WebSocketPath
      ? window.webDavSettings.WebSocketPath
      : '';

  static EditDocAuth: EditDocAuth = {
    Authentication: window.webDavSettings?.EditDocAuth?.Authentication ?? null,
    CookieNames: window.webDavSettings?.EditDocAuth?.CookieNames ?? null,
    SearchIn: window.webDavSettings?.EditDocAuth?.SearchIn ?? null,
    LoginUrl: window.webDavSettings?.EditDocAuth?.LoginUrl ?? null,
  };

  static DriveProjectName: string = import.meta.env.VITE_WEBDAV_DRIVE_NAME ?? 'Drive';

  static IsIntegratedProject: boolean = import.meta.env.VITE_IS_INTEGRATED_PROJECT
    ? import.meta.env.VITE_IS_INTEGRATED_PROJECT.toLowerCase() === 'true'
    : false;

  static ApplicationProtocolsPath: string =
    window.webDavSettings && window.webDavSettings.ApplicationProtocolsPath
      ? window.webDavSettings.ApplicationProtocolsPath
      : '';

  static WebDavServerVersion: string = isDev
    ? WebDavSettings._getWebsiteRootUrl(import.meta.env.VITE_WEBDAV_SERVER_VERSION)
    : window.webDavSettings && window.webDavSettings.WebDavServerVersion
      ? window.webDavSettings.WebDavServerVersion
      : '';

  static _sliceLastSymbol(str: string, symbol: string) {
    let value = str;
    if (value) {
      const lastChar = value.slice(-1);
      if (lastChar === symbol) {
        value = value.slice(0, -1);
      }
    }
    return value;
  }

  static _getWebsiteRootUrl(url: string) {
    if (url && url[0] === '/') {
      return window.location.origin + WebDavSettings._sliceLastSymbol(url, '/');
    }

    if (!url) {
      return window.location.origin;
    }

    return WebDavSettings._sliceLastSymbol(url, '/');
  }
}
