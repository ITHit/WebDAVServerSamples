export const EVENT_TYPES = {
  UPDATED: "updated",
  LOCKED: "locked",
  UNLOCKED: "unlocked",
  CREATED: "created",
  MOVED: "moved",
  DELETED: "deleted",
} as const;

export type EventType = (typeof EVENT_TYPES)[keyof typeof EVENT_TYPES];

export type WebSocketEvent = {
  EventType: EventType;
  ItemPath: string;
  TargetPath?: string;
};

export type WebSocketEventHandler = (
  event: WebSocketEvent
) => Promise<void> | void;
