﻿import { BaseButton } from "./webdav-basebutton";

export function ToolbarReloadButton(name, toolbar) {
  BaseButton.call(this, name, '<i class="icon  icon-reload-items"></i>');

  this.Render = function () {
    this.$Button.on("click", function () {
      toolbar.WebDAV.Reload();
    });
  };
}
