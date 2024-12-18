import { ITHit } from "webdav.client";

function initializeLicense() {
  if (window.webDavSettings && window.webDavSettings.LicenseId) {
    ITHit.WebDAV.Client.LicenseId = window.webDavSettings.LicenseId;
  }
}
initializeLicense();
