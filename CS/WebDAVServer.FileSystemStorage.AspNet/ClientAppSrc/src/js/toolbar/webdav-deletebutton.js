﻿﻿import $ from "jquery";
import { BaseButton } from "./webdav-basebutton";

function HerarhyItemDeleteController(toolbar) {
  this.Toolbar = toolbar;
}

HerarhyItemDeleteController.prototype = {
  Delete: function () {
    var self = this;
    self.Toolbar.ConfirmModal.Confirm(
      "Are you sure want to delete selected items?",
      function () {
        var countDeleted = 0;
        self.Toolbar.WebDAV.AllowReloadGrid = false;
        $.each(self.Toolbar.FolderGrid.selectedItems, function (index) {
          self.Toolbar.FolderGrid.selectedItems[index].DeleteAsync(
            null,
            function () {
              if (
                ++countDeleted == self.Toolbar.FolderGrid.selectedItems.length
              ) {
                self.Toolbar.WebDAV.AllowReloadGrid = true;
                self.Toolbar.WebDAV.Reload();
                self.Toolbar.ResetToolbar();
              }
            }
          );
        });
      }
    );
  },
};

export function ToolbarDeleteButton(name, toolbar) {
  BaseButton.call(
    this,
    name,
    '<i class="icon  icon-delete-items"></i><span class="d-none d-xl-inline">Delete</span>'
  );

  this.Render = function () {
    this.$Button.on("click", function () {
      new HerarhyItemDeleteController(toolbar).Delete();
    });
  };
}