import { ITHit } from "webdav.client";

function initializateLicense() {
  if (window.webDavSettings && window.webDavSettings.LicenseId) {
    ITHit.WebDAV.Client.LicenseId = window.webDavSettings.LicenseId;
  }
}
initializateLicense();
