export const APP_EVENTS = {
  FOLDER_REFRESH_REQUESTED: 'folder:refresh:requested',
  ITEM_UPDATED: 'item:updated',
  ERROR_OCCURRED: 'error:occurred',
} as const;

export type AppEventPayloads = {
  [APP_EVENTS.FOLDER_REFRESH_REQUESTED]: [];
  [APP_EVENTS.ITEM_UPDATED]: [fullPath: string];
  [APP_EVENTS.ERROR_OCCURRED]: [error: unknown];
};

export interface AppEventBus {
  on<K extends keyof AppEventPayloads>(event: K, callback: (...args: AppEventPayloads[K]) => void): void;
  off<K extends keyof AppEventPayloads>(event: K, callback: (...args: AppEventPayloads[K]) => void): void;
  emit<K extends keyof AppEventPayloads>(event: K, ...args: AppEventPayloads[K]): void;
  clear(): void;
}