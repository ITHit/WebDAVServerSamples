import type { WindowWebDavSettings } from '@/infrastructure/config/windowWebDavSettings';

declare global {
  interface Window {
    webDavSettings: WindowWebDavSettings | null;
    chrome?: object;
  }
}

export { };
