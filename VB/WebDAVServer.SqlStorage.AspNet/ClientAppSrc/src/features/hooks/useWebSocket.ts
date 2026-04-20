import { useEffect, useRef, useState } from 'react';
import {
  EVENT_TYPES,
  type WebSocketConnectionState,
  type WebSocketEvent,
  type WebSocketServicePort,
} from '@/shared/types/webSocket';
import type { FileBrowserDomainEventPort } from '@/features/models/fileBrowserDomainEvents';
import { routeWebSocketEvent } from '@/features/models/webSocketEventRouter';

type ServiceLeaseState = {
  refCount: number;
  disconnectTimerId: number | null;
};

const serviceLeases = new WeakMap<WebSocketServicePort, ServiceLeaseState>();
const DISCONNECT_GRACE_MS = 250;

function getLeaseState(service: WebSocketServicePort): ServiceLeaseState {
  const existing = serviceLeases.get(service);
  if (existing) {
    return existing;
  }

  const created: ServiceLeaseState = {
    refCount: 0,
    disconnectTimerId: null,
  };
  serviceLeases.set(service, created);
  return created;
}

function acquireServiceLease(service: WebSocketServicePort): void {
  const lease = getLeaseState(service);

  if (lease.disconnectTimerId !== null) {
    window.clearTimeout(lease.disconnectTimerId);
    lease.disconnectTimerId = null;
  }

  lease.refCount += 1;
}

function releaseServiceLease(service: WebSocketServicePort): void {
  const lease = getLeaseState(service);
  lease.refCount = Math.max(lease.refCount - 1, 0);

  if (lease.refCount > 0) {
    return;
  }

  lease.disconnectTimerId = window.setTimeout(() => {
    const latestLease = getLeaseState(service);
    if (latestLease.refCount === 0) {
      service.disconnect();
    }
    latestLease.disconnectTimerId = null;
  }, DISCONNECT_GRACE_MS);
}

export interface UseWebSocketDeps {
  webSocketService: WebSocketServicePort;
  getServerRootUrl: () => string;
  decode: (value: string) => string;
  getPathname?: () => string;
  enabled?: boolean;
}

export function useWebSocket(events: FileBrowserDomainEventPort, deps: UseWebSocketDeps) {
  const [connectionState, setConnectionState] = useState<WebSocketConnectionState>(() =>
    deps.webSocketService.getConnectionState()
  );

  const depsRef = useRef(deps);
  const eventsRef = useRef(events);

  // Keep refs in sync with current props
  useEffect(() => {
    depsRef.current = deps;
    eventsRef.current = events;
  }, [deps, events]);

  const { isConnected, error, reconnectAttempts } = connectionState;
  const isConnecting = !isConnected && reconnectAttempts > 0;

  useEffect(() => {
    if (deps.enabled === false) {
      return;
    }

    acquireServiceLease(deps.webSocketService);

    const handleConnectionStateChange = (nextState: WebSocketConnectionState) => {
      setConnectionState(nextState);
    };

    const handleItemUpdate = (event: WebSocketEvent) => {
      try {
        const action = routeWebSocketEvent(event, {
          pathname: depsRef.current.getPathname ? depsRef.current.getPathname() : window.location.pathname,
          serverOrigin: new URL(depsRef.current.getServerRootUrl()).origin,
          decode: depsRef.current.decode,
        });

        if (action.type === 'item-updated') {
          eventsRef.current.onItemUpdated(action.fullPath);
        }
      } catch (err) {
        eventsRef.current.onError(err);
      }
    };

    const handleFolderChange = (event: WebSocketEvent) => {
      try {
        const action = routeWebSocketEvent(event, {
          pathname: depsRef.current.getPathname ? depsRef.current.getPathname() : window.location.pathname,
          serverOrigin: new URL(depsRef.current.getServerRootUrl()).origin,
          decode: depsRef.current.decode,
        });

        if (action.type === 'folder-refresh') {
          eventsRef.current.onFolderRefreshRequested();
        }
      } catch (err) {
        eventsRef.current.onError(err);
      }
    };

    try {
      deps.webSocketService.addConnectionStateListener(handleConnectionStateChange);

      deps.webSocketService.addEventListener(EVENT_TYPES.UPDATED, handleItemUpdate);
      deps.webSocketService.addEventListener(EVENT_TYPES.LOCKED, handleItemUpdate);
      deps.webSocketService.addEventListener(EVENT_TYPES.UNLOCKED, handleItemUpdate);
      deps.webSocketService.addEventListener(EVENT_TYPES.CREATED, handleFolderChange);
      deps.webSocketService.addEventListener(EVENT_TYPES.MOVED, handleFolderChange);
      deps.webSocketService.addEventListener(EVENT_TYPES.DELETED, handleFolderChange);

      deps.webSocketService.connect();
    } catch (err) {
      eventsRef.current.onError(err);
    }

    return () => {
      try {
        deps.webSocketService.removeConnectionStateListener(handleConnectionStateChange);
        deps.webSocketService.removeEventListener(EVENT_TYPES.UPDATED, handleItemUpdate);
        deps.webSocketService.removeEventListener(EVENT_TYPES.LOCKED, handleItemUpdate);
        deps.webSocketService.removeEventListener(EVENT_TYPES.UNLOCKED, handleItemUpdate);
        deps.webSocketService.removeEventListener(EVENT_TYPES.CREATED, handleFolderChange);
        deps.webSocketService.removeEventListener(EVENT_TYPES.MOVED, handleFolderChange);
        deps.webSocketService.removeEventListener(EVENT_TYPES.DELETED, handleFolderChange);
        releaseServiceLease(deps.webSocketService);
      } catch (err) {
        eventsRef.current.onError(err);
      }
    };
  }, [deps.webSocketService, deps.enabled]);

  return {
    isConnected,
    error,
    reconnectAttempts,
    isConnecting,
  };
}
