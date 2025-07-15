import { useEffect } from "react";
import { webSocketService } from "../../services/WebSocketService";
import { EVENT_TYPES, WebSocketEvent } from "../../models/WebSocketTypes";
import { StoreWorker } from "../../app/storeWorker";
import { UrlResolveService } from "../../services/UrlResolveService";

export const useWebSocket = () => {
  const getCurrentLocation = (): string => {
    return window.location.pathname.replace(/^\/|\/$/g, "").toUpperCase();
  };

  const getItemDirectory = (path: string): string => {
    return path.substring(0, path.lastIndexOf("/")).toUpperCase();
  };

  const isCurrentLocation = (path: string): boolean => {
    return path.toUpperCase() === getCurrentLocation() ||
      UrlResolveService.decode(path.toUpperCase()) === UrlResolveService.decode(getCurrentLocation());
  };

  const handleItemUpdate = async (event: WebSocketEvent) => {
    const itemDir = getItemDirectory(event.ItemPath);
    if (isCurrentLocation(itemDir)) {
      const fullPath = `${new URL(UrlResolveService.getRootUrl()).origin}/${event.ItemPath
        }`;
      StoreWorker.updateItem(fullPath);
    }
  };

  const handleItemChange = async (event: WebSocketEvent) => {
    const itemDir = getItemDirectory(event.ItemPath);
    const targetDir = event.TargetPath
      ? getItemDirectory(event.TargetPath)
      : null;

    if (
      isCurrentLocation(itemDir) ||
      (targetDir && isCurrentLocation(targetDir))
    ) {
      StoreWorker.refreshFolder(true);
    }
  };

  const handleItemDelete = async (event: WebSocketEvent) => {
    const currentLocation = getCurrentLocation();
    const itemDir = getItemDirectory(event.ItemPath);
    if (
      isCurrentLocation(itemDir) ||
      currentLocation.startsWith(event.ItemPath.toUpperCase()) ||
      UrlResolveService.decode(currentLocation).startsWith(UrlResolveService.decode(event.ItemPath.toUpperCase()))) {
      StoreWorker.refreshFolder(true);
    }
  };

  useEffect(() => {
    const handlers = [
      { event: EVENT_TYPES.UPDATED, handler: handleItemUpdate },
      { event: EVENT_TYPES.LOCKED, handler: handleItemUpdate },
      { event: EVENT_TYPES.UNLOCKED, handler: handleItemUpdate },
      { event: EVENT_TYPES.CREATED, handler: handleItemChange },
      { event: EVENT_TYPES.MOVED, handler: handleItemChange },
      { event: EVENT_TYPES.DELETED, handler: handleItemDelete },
    ];

    handlers.forEach(({ event, handler }) => {
      webSocketService.addEventListener(event, handler);
    });

    webSocketService.connect();

    return () => {
      handlers.forEach(({ event, handler }) => {
        webSocketService.removeEventListener(event, handler);
      });
      webSocketService.disconnect();
    };
  }, []);
};
