export interface UploadPreflightItem {
  GetOverwrite(): boolean;
  IsFolder(): boolean;
  CustomData: unknown;
}

export interface UploadCollectionItem {
  IsFolder(): boolean;
  SetDeleteOnCancel(value: boolean): void;
  SetOverwrite(value: boolean): void;
  GetRelativePath(): string;
  CustomData: unknown;
}

export interface ExistsCheckResult<T> {
  Result: T;
  IsSuccess: boolean;
  Error: unknown;
  Status: { Code: number };
}

export interface OpenItemCheckResult<T> {
  uploadItem: T;
  asyncResult: ExistsCheckResult<unknown>;
}

export function hasVerifiedExistence(customData: unknown): boolean {
  return Boolean((customData as { FileExistanceVerified?: boolean })?.FileExistanceVerified);
}

export function shouldUploadImmediately(item: UploadPreflightItem): boolean {
  return item.GetOverwrite() || item.IsFolder() || hasVerifiedExistence(item.CustomData);
}

export function toExistsCheckResult<T extends UploadCollectionItem>(
  results: OpenItemCheckResult<T>[]
): ExistsCheckResult<T[]> {
  const failedResult = results.find(
    (result) => !(result.asyncResult.IsSuccess || result.asyncResult.Status.Code === 404)
  );

  if (failedResult) {
    return {
      Result: [],
      IsSuccess: false,
      Error: failedResult.asyncResult.Error,
      Status: failedResult.asyncResult.Status,
    };
  }

  const existingItems = results
    .filter((result) => result.asyncResult.IsSuccess)
    .map((result) => result.uploadItem);

  return {
    Result: existingItems,
    IsSuccess: true,
    Error: null,
    Status: { Code: 0 },
  };
}

export function prepareExistingItemsForRewrite<T extends UploadCollectionItem>(
  existingItems: T[]
): string[] {
  const itemPaths: string[] = [];

  existingItems.forEach((item) => {
    if (!item.IsFolder()) {
      item.SetDeleteOnCancel(false);
    }

    (item.CustomData as { FileExistanceVerified?: boolean }).FileExistanceVerified = true;
    itemPaths.push(item.GetRelativePath());
  });

  return itemPaths;
}

export function markItemsForOverwrite<T extends UploadCollectionItem>(existingItems: T[]): void {
  existingItems.forEach((item) => {
    if (item.IsFolder()) {
      return;
    }

    item.SetOverwrite(true);
  });
}

export function getNotExistingItems<T extends UploadCollectionItem>(items: T[], existingItems: T[]): T[] {
  return items.filter((item) => !existingItems.includes(item));
}

export interface UploadRetryHandlers {
  setRetryMessage: (timeLeftMs: number) => void;
  removeRetryMessage: () => void;
  incrementRetry: () => void;
  onRetry: () => void;
  onSkip: () => void;
  setCancelRetryCallback: (callback: () => void) => void;
}

export interface UploadRetryOptions {
  currentRetry: number;
  maxRetry: number;
  retryDelaySeconds: number;
}

export function startUploadRetryTimer(
  options: UploadRetryOptions,
  handlers: UploadRetryHandlers,
  scheduleInterval: (callback: () => void, ms: number) => ReturnType<typeof setInterval> = setInterval,
  cancelInterval: (timer: ReturnType<typeof setInterval>) => void = clearInterval,
  now: () => number = () => Date.now()
): void {
  if (options.maxRetry <= options.currentRetry) {
    handlers.onSkip();
    return;
  }

  const retryAt = now() + options.retryDelaySeconds * 1000;
  const timerId = scheduleInterval(() => {
    const timeLeft = retryAt - now();

    if (timeLeft > 0) {
      handlers.setRetryMessage(timeLeft);
      return;
    }

    cancelInterval(timerId);
    handlers.incrementRetry();
    handlers.removeRetryMessage();
    handlers.onRetry();
  }, 1000);

  handlers.setCancelRetryCallback(() => {
    cancelInterval(timerId);
    handlers.removeRetryMessage();
  });
}
