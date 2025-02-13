import { ITHit } from "webdav.client";

function initializeLicense() {
  if (window.webDavSettings && window.webDavSettings.LicenseId) {
    ITHit.WebDAV.Client.LicenseId = window.webDavSettings.LicenseId;
  }

  if (window.webDavSettings && window.webDavSettings.ProtocolName) {
    ITHit.WebDAV.Client.DocManager.ProtocolName = window.webDavSettings.ProtocolName;
  }
}
initializeLicense();
