import type { FileBrowserContract } from '@/shared/contracts/fileBrowserContract';

/**
 * The shape returned by useFileBrowser() that we need to adapt.
 * Extend this as more methods are available.
 */
export type ReactFileBrowserContractSource = FileBrowserContract;

/**
 * Identity adapter — the React hook already returns the shape FileBrowserContract expects.
 */
export function createReactFileBrowserContract(
  source: ReactFileBrowserContractSource
): FileBrowserContract {
  return source;
}
