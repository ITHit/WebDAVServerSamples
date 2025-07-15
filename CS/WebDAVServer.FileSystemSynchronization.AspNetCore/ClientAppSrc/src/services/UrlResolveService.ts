import { WebDavSettings } from "../webDavSettings";
export class UrlResolveService {
  /**
   * Gets location origin
   */
  static getOrigin() {
    return WebDavSettings.WebsiteRootUrl
      ? WebDavSettings.WebsiteRootUrl.replace(this.getRootFolder(), "")
      : window.location.origin;
  }
  /**
   * Gets root url
   */
  static getRootUrl() {
    return WebDavSettings.WebsiteRootUrl
      ? WebDavSettings.WebsiteRootUrl
      : window.location.origin;
  }
  /**
   * Gets root folder
   */
  static getRootFolder() {
    return WebDavSettings.WebsiteRootUrl.replace(
      new URL(WebDavSettings.WebsiteRootUrl).origin,
      ""
    );
  }

  static getServerOrigin() {
    return WebDavSettings.WebsiteRootUrl
      ? WebDavSettings.WebsiteRootUrl.replace(this.getServerRootFolder(), "")
      : window.location.origin;
  }

  static getServerRootUrl() {
    return WebDavSettings.WebsiteRootUrl
      ? WebDavSettings.WebsiteRootUrl
      : window.location.origin;
  }

  static getServerRootFolder() {
    return WebDavSettings.WebsiteRootUrl.replace(
      new URL(WebDavSettings.WebsiteRootUrl).origin,
      ""
    );
  }

  static getTail(url1: string, url2: string) {
    return url1.includes(url2) ? url1.replace(url2, "") : "";
  }

  static encodeUri(text: string) {
    if (!text) {
      return text;
    }

    return encodeURI(text).replace(/%25/g, "%");
  }

  static decodeUri(text: string) {
    if (!text) {
      return text;
    }

    return decodeURI(
      text.replace(/%([^0-9A-F]|.(?:[^0-9A-F]|$)|$)/gi, "%25$1")
    );
  }

  static decode(path: string) {
    if (!path) {
      return path;
    }

    const res = path
      .replace(/%7E/gi, "~")
      .replace(/%21/g, "!")
      .replace(/%40/g, "@")
      .replace(/%23/g, "#")
      .replace(/%24/g, "$")
      .replace(/%26/g, "&")
      .replace(/%2A/gi, "*")
      .replace(/%28/g, "(")
      .replace(/%29/g, ")")
      .replace(/%2D/gi, "-")
      .replace(/%5F/gi, "_")
      .replace(/%2B/gi, "+")
      .replace(/%3D/gi, "=")
      .replace(/%27/g, "'")
      .replace(/%3B/gi, ";")
      .replace(/%2E/gi, ".")
      .replace(/%2C/gi, ",")
      .replace(/%3F/gi, "?");

    return this.decodeUri(res);
  }

  static getParentFolderUrl(currentUrl: string): string {
    currentUrl = currentUrl.replace(/\/$/, "");

    const rootUrl = this.getServerRootUrl().replace(/\/$/, "");
    if (currentUrl === rootUrl) {
      return rootUrl;
    }

    const parentUrl = currentUrl.substring(0, currentUrl.lastIndexOf("/"));
    return parentUrl || rootUrl;
  }
}
