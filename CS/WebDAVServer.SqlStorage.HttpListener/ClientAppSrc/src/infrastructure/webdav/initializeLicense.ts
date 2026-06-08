import { ITHit } from 'webdav.client';

export const initializeLicense = (): void => {
  if (import.meta.env.VITE_LICENSE_ID) {
    ITHit.WebDAV.Client.LicenseId = import.meta.env.VITE_LICENSE_ID;
  } else if (window.webDavSettings && window.webDavSettings.LicenseId) {
    ITHit.WebDAV.Client.LicenseId = window.webDavSettings.LicenseId;
  }

  if (window.webDavSettings && window.webDavSettings.ProtocolName) {
    ITHit.WebDAV.Client.DocManager.ProtocolName = window.webDavSettings.ProtocolName;
  }
};

// Execute immediately on import so this module can be used as a side-effect pre-init.
initializeLicense();
