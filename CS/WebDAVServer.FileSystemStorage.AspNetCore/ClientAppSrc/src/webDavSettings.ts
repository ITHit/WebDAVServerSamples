class EditDocAuth {
  Authentication: string | null;
  CookieNames: string | null;
  SearchIn: string | null;
  LoginUrl: string | null;
  constructor(
    authentication: string | null,
    cookieNames: string | null,
    searchIn: string | null,
    loginUrl: string | null
  ) {
    this.Authentication = authentication;
    this.CookieNames = cookieNames;
    this.SearchIn = searchIn;
    this.LoginUrl = loginUrl;
  }
}

export class WebDavSettings {
  static WebsiteRootUrl: string =
    window.webDavSettings && window.webDavSettings.WebDavServerPath
      ? WebDavSettings._getWebsiteRootUrl(
          window.webDavSettings.WebDavServerPath
        )
      : "";
  static WebSocketPath: string =
    window.webDavSettings && window.webDavSettings.WebSocketPath
      ? window.webDavSettings.WebSocketPath
      : "";
  static EditDocAuth: EditDocAuth = {
    Authentication: window.webDavSettings.EditDocAuth.Authentication,
    CookieNames: window.webDavSettings.EditDocAuth.CookieNames,
    SearchIn: window.webDavSettings.EditDocAuth.SearchIn,
    LoginUrl: window.webDavSettings.EditDocAuth.LoginUrl,
  };

  static ApplicationProtocolsPath: string =
    window.webDavSettings && window.webDavSettings.ApplicationProtocolsPath
      ? window.webDavSettings.ApplicationProtocolsPath
      : "";
  static _sliceLastSymbol(str: string, symbol: string) {
    var strVal = str;
    if (strVal) {
      var lastChar = strVal.slice(-1);
      if (lastChar === symbol) {
        strVal = strVal.slice(0, -1);
      }
    }
    return strVal;
  }

  static WebDavServerVersion: string =
    window.webDavSettings && window.webDavSettings.WebDavServerVersion
      ? window.webDavSettings.WebDavServerVersion
      : "";

  static _getWebsiteRootUrl(url: string) {
    if (url && url[0] === "/") {
      return window.location.origin + WebDavSettings._sliceLastSymbol(url, "/");
    }

    if (!url) {
      return window.location.origin;
    }

    return WebDavSettings._sliceLastSymbol(url, "/");
  }
}
