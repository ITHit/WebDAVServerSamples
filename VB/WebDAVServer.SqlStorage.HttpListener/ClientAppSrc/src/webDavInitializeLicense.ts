import { ITHit } from "webdav.client";

function initializeLicense() {
    if (import.meta.env.VITE_LICENSE_ID) {
        ITHit.WebDAV.Client.LicenseId = import.meta.env.VITE_LICENSE_ID;
    }
    else if (window.webDavSettings && window.webDavSettings.LicenseId) {
        ITHit.WebDAV.Client.LicenseId = window.webDavSettings.LicenseId;
    }

    if (window.webDavSettings && window.webDavSettings.ProtocolName) {
        ITHit.WebDAV.Client.DocManager.ProtocolName = window.webDavSettings.ProtocolName;
    }
}
initializeLicense();
