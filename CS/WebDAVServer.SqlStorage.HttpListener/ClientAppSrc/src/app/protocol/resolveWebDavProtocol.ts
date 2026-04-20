import { detectBrowserName, detectOsName, type BrowserName, type OsName } from '@/app/protocol/protocolDetection';

export interface WebDavProtocolApp {
  id: string;
  name: string;
  downloadLink: string;
  fileName: string;
  cssClass: string;
}

export interface WebDavProtocol {
  currentBrowser: WebDavProtocolApp | null;
  currentOs: WebDavProtocolApp | null;
  otherBrowsers: WebDavProtocolApp[];
  otherOs: WebDavProtocolApp[];
}

function createOsApps(): WebDavProtocolApp[] {
  return [
    {
      id: 'windows',
      name: 'Windows: ',
      downloadLink: 'ms-windows-store://pdp/?ProductId=9nqb82r5hmnh',
      fileName: 'WebDAV Drive',
      cssClass: 'window',
    },
    {
      id: 'mac',
      name: 'Mac OS: ',
      downloadLink: 'https://apps.apple.com/us/app/webdav-drive/id6502366145',
      fileName: 'WebDAV Drive',
      cssClass: 'mac-os',
    },
  ];
}

function createBrowserApps(): WebDavProtocolApp[] {
  return [
    {
      id: 'chrome',
      name: '',
      downloadLink:
        'https://chrome.google.com/webstore/detail/it-hit-edit-doc-opener-5/nakgflbblpkdafokdokmjdfglijajhlp',
      fileName: 'Extension for Google Chrome.',
      cssClass: 'goole-chrome',
    },
    {
      id: 'firefox',
      name: '',
      downloadLink: 'https://addons.mozilla.org/en-CA/firefox/addon/it-hit-edit-doc-opener-5/',
      fileName: 'Extension for Mozilla Firefox.',
      cssClass: 'mozilla-firefox',
    },
    {
      id: 'edge',
      name: '',
      downloadLink: 'https://microsoftedge.microsoft.com/addons/detail/mdfaonmaoigngflemfmkboffllkopopm',
      fileName: 'Extension for Microsoft Edge Chromium.',
      cssClass: 'edge-chromium',
    },
  ];
}

function createInternetExplorerApp(): WebDavProtocolApp {
  return {
    id: 'ie',
    name: 'Not required for Internet Explorer',
    downloadLink: '',
    fileName: '',
    cssClass: 'not-required-internet-explorer',
  };
}

function findCurrentApp<T extends WebDavProtocolApp>(items: T[], id: string): T | null {
  return items.find(item => item.id === id) ?? null;
}

export function resolveWebDavProtocol(
  userAgent = navigator.userAgent,
  hasChromeObject = 'chrome' in window
): WebDavProtocol {
  const allOs = createOsApps();
  const allBrowsers = createBrowserApps();

  const currentOsId: OsName = detectOsName(userAgent);
  const currentBrowserId: BrowserName = detectBrowserName(userAgent.toLocaleLowerCase(), hasChromeObject);

  const currentOs = findCurrentApp(allOs, currentOsId);
  const currentBrowser =
    currentBrowserId === 'ie' ? createInternetExplorerApp() : findCurrentApp(allBrowsers, currentBrowserId);

  return {
    currentOs,
    currentBrowser,
    otherOs: currentOs ? allOs.filter(item => item.id !== currentOs.id) : allOs,
    otherBrowsers: currentBrowser ? allBrowsers.filter(item => item.id !== currentBrowser.id) : allBrowsers,
  };
}
