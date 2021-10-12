
if (location.protocol === "https:") {
    var socketSource = new WebSocket("wss://" + location.host + "/dav");
} else {
    var socketSource = new WebSocket("ws://" + location.host + "/dav");
}


socketSource.addEventListener('message', function (e) {
    var notifyObject = JSON.parse(e.data);

    // Removing domain and trailing slash.
    var currentLocation = location.pathname.replace(/^\/|\/$/g, '');
    // Checking message type after receiving.
    if (notifyObject.EventType === "updated" || notifyObject.EventType === "created" || notifyObject.EventType === "locked" ||
        notifyObject.EventType === "unlocked") {
        // Refresh folder structure if any item in this folder is updated or new item is created.
        if (notifyObject.ItemPath.substring(0, notifyObject.ItemPath.lastIndexOf('/')).toUpperCase() === currentLocation.toUpperCase()) {
            WebDAVController.Reload();
        }
    } else if (notifyObject.EventType === "moved") {
        // Refresh folder structure if file or folder is moved.
        if (notifyObject.ItemPath.substring(0, notifyObject.ItemPath.lastIndexOf('/')).toUpperCase() === currentLocation.toUpperCase() ||
            notifyObject.TargetPath.substring(0, notifyObject.TargetPath.lastIndexOf('/')).toUpperCase() === currentLocation.toUpperCase()) {
            WebDAVController.Reload();
        }

    } else if (notifyObject.EventType === "deleted") {
        if (notifyObject.ItemPath.substring(0, notifyObject.ItemPath.lastIndexOf('/')).toUpperCase() === currentLocation.toUpperCase()) {
            // Refresh folder structure if any item in this folder is deleted.
            WebDAVController.Reload();
        } else if (currentLocation.toUpperCase().indexOf(notifyObject.ItemPath.toUpperCase()) === 0) {
            // Redirect client to the root folder if current path is being deleted.
            var originPath = location.origin + "/";
            history.pushState({ Url: originPath }, '', originPath);
            WebDAVController.NavigateFolder(originPath);
        }
    }
}, false);