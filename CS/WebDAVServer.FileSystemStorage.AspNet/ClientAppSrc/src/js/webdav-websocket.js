import { webDavSettings } from "./webdav-settings";
export function WebSocketConnect() {
  var socketSource;

  if (webDavSettings.WebSocketPath) {
    socketSource = new WebSocket(webDavSettings.WebSocketPath);
  } else {
    var host = location.host;

    if (webDavSettings.WebDavServerPath) {
      var oAppPath = new URL(webDavSettings.WebDavServerPath);
      host = oAppPath.host;
    }

    if (location.protocol === "https:") {
      socketSource = new WebSocket("wss://" + host + "/dav");
    } else {
      socketSource = new WebSocket("ws://" + host + "/dav");
    }
  }

  socketSource.addEventListener(
    "message",
    function (e) {
      var notifyObject = JSON.parse(e.data);

      // Removing domain and trailing slash.
      var currentLocation = location.pathname.replace(/^\/|\/$/g, "");
      // Checking message type after receiving.
      if (
        notifyObject.EventType === "updated" ||
        notifyObject.EventType === "created" ||
        notifyObject.EventType === "locked" ||
        notifyObject.EventType === "unlocked"
      ) {
        // Refresh folder structure if any item in this folder is updated or new item is created.
        if (
          notifyObject.ItemPath.substring(
            0,
            notifyObject.ItemPath.lastIndexOf("/")
          ).toUpperCase() === currentLocation.toUpperCase()
        ) {
          window.WebDAVController.Reload();
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
          window.WebDAVController.Reload();
        }
      } else if (notifyObject.EventType === "deleted") {
        if (
          notifyObject.ItemPath.substring(
            0,
            notifyObject.ItemPath.lastIndexOf("/")
          ).toUpperCase() === currentLocation.toUpperCase()
        ) {
          // Refresh folder structure if any item in this folder is deleted.
          window.WebDAVController.Reload();
        } else if (
          currentLocation
            .toUpperCase()
            .indexOf(notifyObject.ItemPath.toUpperCase()) === 0
        ) {
          // Redirect client to the root folder if current path is being deleted.
          var originPath = location.origin + "/";
          history.pushState({ Url: originPath }, "", originPath);
          window.WebDAVController.NavigateFolder(originPath);
        }
      }
    },
    false
  );

  socketSource.addEventListener("error", function (err) {
    console.error("Socket encountered error: ", err.message, "Closing socket");
    socketSource.close();
  });

  socketSource.addEventListener("close", function (e) {
    console.log(
      "Socket is closed. Reconnect will be attempted in 5 seconds.",
      e.reason
    );
    setTimeout(function () {
      WebSocketConnect();
    }, 5000);
  });
}
WebSocketConnect();
