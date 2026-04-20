import { WebDavSettings } from '@/infrastructure/config/webDavSettings';
import { getTail } from '@/shared/utils/urlCodec';

function normalizePathname(pathName: string) {
  if (!pathName || pathName === '/') {
    return '/';
  }

  const normalized = pathName.startsWith('/') ? pathName : `/${pathName}`;
  return normalized.replace(/\/+$/, '') || '/';
}

export function getServerOrigin() {
  return WebDavSettings.WebsiteRootUrl
    ? WebDavSettings.WebsiteRootUrl.replace(getServerRootFolder(), '')
    : window.location.origin;
}

export function getServerRootUrl() {
  return WebDavSettings.WebsiteRootUrl
    ? WebDavSettings.WebsiteRootUrl
    : window.location.origin;
}

function getServerRootFolder() {
  if (!WebDavSettings.WebsiteRootUrl) {
    return '';
  }

  return WebDavSettings.WebsiteRootUrl.replace(
    new URL(WebDavSettings.WebsiteRootUrl).origin,
    ''
  );
}

export function getAppPathFromServerUrl(serverUrl: string) {
  const appPathTail = getTail(serverUrl, getServerOrigin());
  return normalizePathname(appPathTail);
}

export function getServerUrl(pathName: string) {
  const normalizedPath = normalizePathname(pathName);
  const serverRootFolder = normalizePathname(getServerRootFolder());

  if (normalizedPath === '/') {
    return getServerRootUrl();
  }

  if (
    serverRootFolder !== '/'
    && (normalizedPath === serverRootFolder || normalizedPath.startsWith(`${serverRootFolder}/`))
  ) {
    return `${getServerOrigin()}${normalizedPath}`;
  }

  return `${getServerRootUrl()}${normalizedPath}`;
}
