/**
 * Upload state enum - domain representation of upload item states
 * This replaces ITHit.WebDAV.Client.Upload.State to avoid external library coupling
 */
export enum UploadState {
  /** Upload is queued but not yet started */
  Queued = 'Queued',

  /** Upload is currently in progress */
  Uploading = 'Uploading',

  /** Upload has been paused by user */
  Paused = 'Paused',

  /** Upload has completed successfully */
  Completed = 'Completed',

  /** Upload has been canceled by user */
  Canceled = 'Canceled',

  /** Upload has failed due to error */
  Failed = 'Failed'
}

/**
 * Convert ITHit Upload State to domain UploadState
 * @param ithitState - ITHit SDK state string
 * @returns Domain UploadState enum value
 */
export function fromITHitState(ithitState: string): UploadState {
  // ITHit uses string state names like "Queued", "Uploading", etc.
  // Map them to our domain enum
  switch (ithitState) {
    case 'Queued':
      return UploadState.Queued;
    case 'Uploading':
      return UploadState.Uploading;
    case 'Paused':
      return UploadState.Paused;
    case 'Completed':
      return UploadState.Completed;
    case 'Canceled':
      return UploadState.Canceled;
    case 'Failed':
      return UploadState.Failed;
    default:
      console.warn(`Unknown ITHit upload state: ${ithitState}`);
      return UploadState.Queued;
  }
}
