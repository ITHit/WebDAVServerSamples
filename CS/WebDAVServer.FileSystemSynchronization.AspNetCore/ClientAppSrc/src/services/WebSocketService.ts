import { WebDavSettings } from "../webDavSettings";
import { StoreWorker } from "../app/storeWorker";
import { UrlResolveService } from "./UrlResolveService";
export function WebSocketConnect() {
  let socketSource: WebSocket | undefined;

  if (WebDavSettings.WebSocketPath) {
    socketSource = new WebSocket(WebDavSettings.WebSocketPath);
  } else {
    let host = window.location.host;

    if (WebDavSettings.WebsiteRootUrl) {
      const oAppPath = new URL(WebDavSettings.WebsiteRootUrl);
      host = oAppPath.host;
    }

    if (window.location.protocol === "https:") {
      socketSource = new WebSocket("wss://" + host);
    } else {
      socketSource = new WebSocket("ws://" + host);
    }
  }

  socketSource.addEventListener(
    "message",
    function (e: MessageEvent) {
      const notifyObject = JSON.parse(e.data);

      // Removing domain and trailing slash.
      const currentLocation = window.location.pathname.replace(/^\/|\/$/g, "");

      // Checking message type after receiving.
      if (
        notifyObject.EventType === "updated" ||
        notifyObject.EventType === "locked" ||
        notifyObject.EventType === "unlocked"
      ) {
        if (
          notifyObject.ItemPath.substring(
            0,
            notifyObject.ItemPath.lastIndexOf("/")
          ).toUpperCase() === currentLocation.toUpperCase()
        ) {
          StoreWorker.updateItem(
            new URL(UrlResolveService.getRootUrl()).origin +
            "/" +
            notifyObject.ItemPath
          );
        }
      } else if (notifyObject.EventType === "created") {
        // Refresh folder structure if any item in this folder is updated or new item is created.
        if (
          notifyObject.ItemPath.substring(
            0,
            notifyObject.ItemPath.lastIndexOf("/")
          ).toUpperCase() === currentLocation.toUpperCase()
        ) {
          StoreWorker.refreshCurrentItems();
        }
      } else if (notifyObject.EventType === "moved") {
        // Refresh folder structure if file or folder is moved.
        if (
          notifyObject.ItemPath.substring(
            0,
            notifyObject.ItemPath.lastIndexOf("/")
          ).toUpperCase() === currentLocation.toUpperCase() ||
          notifyObject.TargetPath.substring(
            0,
            notifyObject.TargetPath.lastIndexOf("/")
          ).toUpperCase() === currentLocation.toUpperCase()
        ) {
          StoreWorker.refreshCurrentItems();
        }
      } else if (notifyObject.EventType === "deleted") {
        if (
          notifyObject.ItemPath.substring(
            0,
            notifyObject.ItemPath.lastIndexOf("/")
          ).toUpperCase() === currentLocation.toUpperCase()
        ) {
          // Refresh folder structure if any item in this folder is deleted.
          StoreWorker.refreshCurrentItems();
        } else if (
          currentLocation
            .toUpperCase()
            .indexOf(notifyObject.ItemPath.toUpperCase()) === 0
        ) {
          // Redirect client to the root folder if current path is being deleted.
          const originPath = window.location.origin + "/";
          window.history.pushState({ Url: originPath }, "", originPath);
          StoreWorker.refresh(originPath);
        }
      }
    },
    false
  );
  socketSource.addEventListener("error", function (err: Event) {
    console.error(
      "Socket encountered error: ",
      (err as ErrorEvent).message,
      "Closing socket"
    );
    socketSource?.close();
  });

  socketSource.addEventListener("close", function (e: CloseEvent) {
    console.log(
      "Socket is closed. Reconnect will be attempted in 5 seconds.",
      e.reason
    );
    setTimeout(function () {
      WebSocketConnect();
    }, 5000);
  });
}
