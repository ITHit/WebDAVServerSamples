export interface WindowWebDavSettings {
  WebDavServerPath: string | null;
  WebSocketPath: string | null;
  EditDocAuth: {
    Authentication: string | null;
    CookieNames: string | null;
    SearchIn: string | null;
    LoginUrl: string | null;
  } | null;
  ApplicationProtocolsPath: string | null;
  WebDavServerVersion: string | null;
  ProtocolName: string | null;
  LicenseId: string | null;
}
