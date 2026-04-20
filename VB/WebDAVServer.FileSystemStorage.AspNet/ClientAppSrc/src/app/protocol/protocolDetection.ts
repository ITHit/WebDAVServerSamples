export type BrowserName = 'chrome' | 'firefox' | 'edge' | 'ie' | 'other';
export type OsName = 'windows' | 'mac' | 'linux' | 'other';

export function detectBrowserName(agent: string, hasChromeObject = false): BrowserName {
  switch (true) {
    case agent.indexOf('edge') > -1:
      return 'edge';
    case agent.indexOf('edg/') > -1:
      return 'edge';
    case agent.indexOf('chrome') > -1 && hasChromeObject:
      return 'chrome';
    case agent.indexOf('msie') > -1 || !!agent.match(/trident.*rv:11\./):
      return 'ie';
    case agent.indexOf('firefox') > -1:
      return 'firefox';
    default:
      return 'other';
  }
}

export function detectOsName(agent: string): OsName {
  switch (true) {
    case agent.indexOf('Win') > -1:
      return 'windows';
    case agent.indexOf('Mac') > -1:
      return 'mac';
    case agent.indexOf('Linux') > -1:
      return 'linux';
    default:
      return 'other';
  }
}
