import { ITHit } from "webdav.client";
import { WebDavSettings } from "../webDavSettings";
import { getI18n } from "react-i18next";
const i18n = getI18n();

export class WebDavProtocolApp {
  id = "";
  name = "";
  downloadLink = "";
  fileName = "";
  cssClass = "";
  constructor(
    id: string,
    name: string,
    downloadLink: string,
    fileName: string,
    cssClass: string
  ) {
    this.id = id;
    this.name = name;
    this.downloadLink = downloadLink;
    this.fileName = fileName;
    this.cssClass = cssClass;
  }
}

export class WebDavProtocol {
  currentBrowser: WebDavProtocolApp | null = null;
  currentOs: WebDavProtocolApp | null = null;
  otherBrowsers: WebDavProtocolApp[] = [];
  otherOs: WebDavProtocolApp[] = [];
  constructor(
    currentOs: WebDavProtocolApp | null,
    currentBrowser: WebDavProtocolApp | null,
    otherOs: WebDavProtocolApp[],
    otherBrowsers: WebDavProtocolApp[]
  ) {
    this.currentOs = currentOs;
    this.currentBrowser = currentBrowser;
    this.otherOs = otherOs;
    this.otherBrowsers = otherBrowsers;
  }
}

export const allOs = [
  new WebDavProtocolApp(
    "windows",
    "Windows: ",
    "ms-windows-store://pdp/?ProductId=9nqb82r5hmnh",
    "WebDAV Drive",
    "window"
  ),
  new WebDavProtocolApp(
    "mac",
    "Mac OS: ",
    "https://apps.apple.com/us/app/webdav-drive/id6502366145",
    "WebDAV Drive",
    "mac-os"
  ),
];

export const allBrowsers = [
  new WebDavProtocolApp(
    "chrome",
    "",
    "https://chrome.google.com/webstore/detail/it-hit-edit-doc-opener-5/nakgflbblpkdafokdokmjdfglijajhlp",
    i18n.t("phrases.extensionFor", { browser: "Google Chrome." }),
    "goole-chrome"
  ),
  new WebDavProtocolApp(
    "firefox",
    "",
    "https://addons.mozilla.org/en-CA/firefox/addon/it-hit-edit-doc-opener-5/",
    i18n.t("phrases.extensionFor", { browser: "Mozilla Firefox." }),
    "mozilla-firefox"
  ),
  new WebDavProtocolApp(
    "edge",
    "",
    "https://microsoftedge.microsoft.com/addons/detail/mdfaonmaoigngflemfmkboffllkopopm",
    i18n.t("phrases.extensionFor", { browser: "Microsoft Edge Chromium." }),
    "edge-chromium"
  ),
];
export class ProtocolService {
  static getProtocol() {
    const currentBrowserProtocol = this.getCurrentBrowserProtocolApp();
    const currentOsProtocol = this.getCurrentOsProtocolApp();

    const webDavProtocol = new WebDavProtocol(
      currentOsProtocol,
      currentBrowserProtocol,
      currentOsProtocol
        ? allOs.filter((c) => c.id !== currentOsProtocol?.id)
        : allOs,
      currentBrowserProtocol
        ? allBrowsers.filter((c) => c.id !== currentBrowserProtocol?.id)
        : allBrowsers
    );
    return webDavProtocol;
  }
  /**
   * Gets current browser Protocol item
   */
  static getCurrentBrowserProtocolApp() {
    const browserName = this.getBrowserName(
      navigator.userAgent.toLocaleLowerCase()
    );
    let currentBrowserProtocol = allBrowsers
      .filter((c) => c.id === browserName)
      .shift();

    if (browserName === "ie") {
      currentBrowserProtocol = new WebDavProtocolApp(
        "ie",
        "The extension is not required for Internet Explorer.",
        "",
        "",
        "not-required-internet-explorer"
      );
    }

    return currentBrowserProtocol ? currentBrowserProtocol : null;
  }

  /**
   * Gets current OS Protocol item
   */
  static getCurrentOsProtocolApp() {
    const currentOsProtocol = allOs
      .filter((c) => c.id === this.getOsName(navigator.userAgent))
      .shift();

    return currentOsProtocol ? currentOsProtocol : null;
  }

  /**
   * Is dav protocol supported
   */
  static isDavProtocolSupported() {
    return ITHit.WebDAV.Client.DocManager.IsDavProtocolSupported();
  }

  static isMicrosoftOfficeDocument(href: string) {
    return ITHit.WebDAV.Client.DocManager.IsMicrosoftOfficeDocument(href);
  }

  static getBrowserName(agent: string) {
    switch (true) {
      case agent.indexOf("edge") > -1:
        return "edge";
      case agent.indexOf("edg/") > -1:
        return "edge";
      case agent.indexOf("chrome") > -1 && !!window.chrome:
        return "chrome";
      case agent.indexOf("msie") > -1 || !!agent.match(/trident.*rv\\:11\./):
        return "ie";
      case agent.indexOf("firefox") > -1:
        return "firefox";
      default:
        return "other";
    }
  }

  static getOsName(agent: string) {
    switch (true) {
      case agent.indexOf("Win") > -1:
        return "windows";
      case agent.indexOf("Mac") > -1:
        return "mac";
      case agent.indexOf("Linux") > -1:
        return "linux";
      default:
        return "other";
    }
  }

  static getInstallerFileUrl() {
    if (this.getOsName(navigator.userAgent) === "windows") {
      return "ms-windows-store://pdp/?ProductId=9nqb82r5hmnh";
    } else if (this.getOsName(navigator.userAgent) === "mac") {
      return "https://apps.apple.com/us/app/webdav-drive/id6502366145";
    }
    return (
      WebDavSettings.ApplicationProtocolsPath +
      ITHit.WebDAV.Client.DocManager.GetProtocolInstallFileNames()[0]
    );
  }
}
