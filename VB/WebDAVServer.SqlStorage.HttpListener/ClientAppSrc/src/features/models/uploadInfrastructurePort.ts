import type { IAsyncCallbackResult } from './openItemsCollectionResult';

export interface UploadInfrastructurePort {
  encodeUri: (url: string) => string;
  openItemCallback: (href: string, callback: (result: IAsyncCallbackResult) => void) => void;
}

export interface UploadErrorFactoryPort {
  createValidationError: (validationMessage: string, itemUrl: string) => unknown;
  createExistsCheckError: (originalError: unknown) => unknown;
}

export interface UploadValidationPort {
  validateName: (name: string) => string | undefined;
}
