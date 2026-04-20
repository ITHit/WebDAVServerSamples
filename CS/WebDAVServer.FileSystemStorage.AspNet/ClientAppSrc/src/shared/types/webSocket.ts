export const EVENT_TYPES = {
  UPDATED: 'updated',
  LOCKED: 'locked',
  UNLOCKED: 'unlocked',
  CREATED: 'created',
  MOVED: 'moved',
  DELETED: 'deleted',
} as const;

export type WebSocketEventType = (typeof EVENT_TYPES)[keyof typeof EVENT_TYPES];

export type WebSocketEvent = {
  EventType: WebSocketEventType;
  ItemPath: string;
  TargetPath?: string;
};

export type WebSocketEventHandler = (event: WebSocketEvent) => Promise<void> | void;

export type WebSocketConnectionState = {
  isConnected: boolean;
  reconnectAttempts: number;
  error: Error | null;
};

export type WebSocketConnectionStateHandler = (state: WebSocketConnectionState) => void;

export interface WebSocketServicePort {
  addEventListener(eventType: WebSocketEventType, handler: WebSocketEventHandler): void;
  removeEventListener(eventType: WebSocketEventType, handler: WebSocketEventHandler): void;
  addConnectionStateListener(handler: WebSocketConnectionStateHandler): void;
  removeConnectionStateListener(handler: WebSocketConnectionStateHandler): void;
  getConnectionState(): WebSocketConnectionState;
  connect(): void;
  disconnect(): void;
}
