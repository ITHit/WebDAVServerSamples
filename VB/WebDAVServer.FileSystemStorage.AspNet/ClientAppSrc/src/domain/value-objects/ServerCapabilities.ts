/**
 * Domain value object representing server capabilities
 * Framework-agnostic representation of what features the server supports
 */
export class ServerCapabilities {
  constructor(
    public readonly supportsSearch: boolean,
    public readonly supportsLocking: boolean,
    public readonly supportsVersioning: boolean,
    public readonly supportsResumableUpload: boolean
  ) { }

  static createEmpty(): ServerCapabilities {
    return new ServerCapabilities(false, false, false, false);
  }
}
