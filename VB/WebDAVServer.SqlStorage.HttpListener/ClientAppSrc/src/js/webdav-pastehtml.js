import $ from "jquery";
import { ITHit } from "webdav.client";

(function () {
  var $app = $("#app");
  if ($app) {
    var templatePath = $app.attr("data-template");
    if (templatePath) {
      $app.load(templatePath, function (responseText, textStatus, xhr) {
        if (textStatus == "error") {
          alert(
            "Error: " +
              xhr.status +
              ": " +
              xhr.statusText +
              " (file path: " +
              templatePath +
              ");"
          );
        } else {
          if (window.webDavSettings && window.webDavSettings.LicenseId) {
            ITHit.WebDAV.Client.LicenseId = window.webDavSettings.LicenseId;
          }

          if (window.webDavSettings && window.webDavSettings.WebDavServerPath) {
            let serverPath = window.webDavSettings.WebDavServerPath;
            if (serverPath[0] == "/") {
              window.webDavSettings.WebDavServerPath =
                window.location.origin + serverPath;
            }
          }
          require("./webdav-settings");
          require("./webdav-common");
          require("./webdav-uploader");
          require("./webdav-gridview");
          require("./webdav-websocket");
        }
      });
    } else {
      alert("'data-template' attribute not found!");
    }
  } else {
    alert("container with id 'app' not found!");
  }
})();
