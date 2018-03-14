if (location.protocol === "https:") {
    var socketSource = new WebSocket("wss://" + location.host);
} else {
    var socketSource = new WebSocket("ws://" + location.host);
}


socketSource.addEventListener('message', function (e) {
    var notifyObject = JSON.parse(e.data);

    // Removing domain and trailing slash.
    var currentLocation = location.pathname.replace(/^\/|\/$/g, '');
    // Checking message type after receiving.
    if (notifyObject.EventType === "refresh") {
        // Refresh folder structure if any item in this folder is updated or new item is created.
        if (currentLocation.toUpperCase() === notifyObject.FolderPath.toUpperCase()) {
            WebDAVController.Reload();
        }
    } else if (notifyObject.EventType === "delete") {
        if (notifyObject.FolderPath.substring(0, notifyObject.FolderPath.lastIndexOf('/')).toUpperCase() === currentLocation.toUpperCase()) {
            // Refresh folder structure if any item in this folder is deleted.
            WebDAVController.Reload();
        } else if (currentLocation.toUpperCase().indexOf(notifyObject.FolderPath.toUpperCase()) === 0) {
            // Redirect client to the root folder if current path is being deleted.
            var originPath = location.origin + "/";
            history.pushState({ Url: originPath }, '', originPath);
            WebDAVController.NavigateFolder(originPath);
        }
    }
}, false);