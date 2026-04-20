import { EVENT_TYPES, WebSocketEvent } from '@/shared/types/webSocket';

export type WebSocketRouteAction =
  | { type: 'item-updated'; fullPath: string }
  | { type: 'folder-refresh' }
  | { type: 'none' };

function normalizePath(path: string): string {
  return path.replace(/^\/|\/$/g, '').toUpperCase();
}

function getItemDirectory(path: string): string {
  return path.substring(0, path.lastIndexOf('/')).toUpperCase();
}

function isCurrentLocation(path: string, currentLocation: string, decode: (value: string) => string): boolean {
  return path.toUpperCase() === currentLocation || decode(path.toUpperCase()) === decode(currentLocation);
}

function shouldRefreshOnChange(
  event: WebSocketEvent,
  currentLocation: string,
  decode: (value: string) => string
): boolean {
  const itemDir = getItemDirectory(event.ItemPath);
  const targetDir = event.TargetPath ? getItemDirectory(event.TargetPath) : null;

  return isCurrentLocation(itemDir, currentLocation, decode) ||
    Boolean(targetDir && isCurrentLocation(targetDir, currentLocation, decode));
}

function shouldRefreshOnDelete(
  event: WebSocketEvent,
  currentLocation: string,
  decode: (value: string) => string
): boolean {
  const itemDir = getItemDirectory(event.ItemPath);

  return isCurrentLocation(itemDir, currentLocation, decode) ||
    currentLocation.startsWith(event.ItemPath.toUpperCase()) ||
    decode(currentLocation).startsWith(decode(event.ItemPath.toUpperCase()));
}

export interface RouteWebSocketEventContext {
  pathname: string;
  serverOrigin: string;
  decode: (value: string) => string;
}

export function routeWebSocketEvent(
  event: WebSocketEvent,
  context: RouteWebSocketEventContext
): WebSocketRouteAction {
  const currentLocation = normalizePath(context.pathname);

  if (
    event.EventType === EVENT_TYPES.UPDATED ||
    event.EventType === EVENT_TYPES.LOCKED ||
    event.EventType === EVENT_TYPES.UNLOCKED
  ) {
    const itemDir = getItemDirectory(event.ItemPath);

    if (isCurrentLocation(itemDir, currentLocation, context.decode)) {
      return {
        type: 'item-updated',
        fullPath: `${context.serverOrigin}/${event.ItemPath}`,
      };
    }

    return { type: 'none' };
  }

  if (event.EventType === EVENT_TYPES.CREATED || event.EventType === EVENT_TYPES.MOVED) {
    return shouldRefreshOnChange(event, currentLocation, context.decode)
      ? { type: 'folder-refresh' }
      : { type: 'none' };
  }

  if (event.EventType === EVENT_TYPES.DELETED) {
    return shouldRefreshOnDelete(event, currentLocation, context.decode)
      ? { type: 'folder-refresh' }
      : { type: 'none' };
  }

  return { type: 'none' };
}
