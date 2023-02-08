import $ from "jquery";
import { ToolbarCreateFolderButton } from "./webdav-createfolderbutton";
import { ToolbarDownloadButton } from "./webdav-downloadbutton";
import { ToolbarRenameButton } from "./webdav-renamebutton";
import { CopyPasteButtonsControl } from "./webdav-copypastecutbuttons";
import { ToolbarReloadButton } from "./webdav-reloadbutton";
import { ToolbarPrintButton } from "./webdav-printbutton";
import { ToolbarDeleteButton } from "./webdav-deletebutton";
import { ITHit } from "webdav.client";

export function Toolbar(
  selectorTableToolbar,
  folderGrid,
  confirmModal,
  webDAVController
) {
  this.ToolbarName = selectorTableToolbar;
  this.$Toolbar = $(selectorTableToolbar);
  this.FolderGrid = folderGrid;
  this.ConfirmModal = confirmModal;
  this.WebDAV = webDAVController;
  this.buttons = [];

  var self = this;

  var createFolderButton = new ToolbarCreateFolderButton("Create Folder", this);
  this.buttons.push(createFolderButton);
  createFolderButton.Create(
    $(self.$Toolbar).find(".toolbar-section[data-index='1']")
  );

  var downloadButton = new ToolbarDownloadButton("Dwonload", this);
  this.buttons.push(downloadButton);
  downloadButton.Create(
    $(self.$Toolbar).find(".toolbar-section[data-index='2']")
  );

  var renameButton = new ToolbarRenameButton("Rename", this);
  this.buttons.push(renameButton);
  renameButton.Create(
    $(self.$Toolbar).find(".toolbar-section[data-index='3']")
  );

  var copyPasteButtons = new CopyPasteButtonsControl(this);
  this.buttons.push(copyPasteButtons);
  copyPasteButtons.Create(
    $(self.$Toolbar).find(".toolbar-section[data-index='4']")
  );

  var reloadButton = new ToolbarReloadButton("Reload", this);
  this.buttons.push(reloadButton);
  reloadButton.Create(
    $(self.$Toolbar).find(".toolbar-section[data-index='5']")
  );

  var printButton = new ToolbarPrintButton("Print", this);
  this.buttons.push(printButton);
  printButton.Create($(self.$Toolbar).find(".toolbar-section[data-index='6']"));

  var deleteButton = new ToolbarDeleteButton("Delete", this);
  this.buttons.push(deleteButton);
  deleteButton.Create(
    $(self.$Toolbar).find(".toolbar-section[data-index='6']")
  );

  $.each(self.buttons, function (index) {
    this.Render();
  });

  this.UpdateToolbarButtons();
}

Toolbar.prototype = {
  UpdateToolbarButtons: function () {
    var self = this;

    $.each(self.buttons, function (index) {
      if (this instanceof ToolbarCreateFolderButton) {
        self.FolderGrid.selectedItems.length == 0
          ? this.ShowOnMobile()
          : this.HideOnMobile();
      }
      if (this instanceof ToolbarDeleteButton) {
        if (self.FolderGrid.selectedItems.length == 0) {
          this.Disable();
          this.HideOnMobile();
        } else {
          this.Activate();
          this.ShowOnMobile();
        }
      }
      if (this instanceof ToolbarRenameButton) {
        if (
          self.FolderGrid.selectedItems.length == 0 ||
          self.FolderGrid.selectedItems.length != 1
        ) {
          this.Disable();
          this.HideOnMobile();
        } else {
          this.Activate();
          this.ShowOnMobile();
        }
      }
      if (this instanceof ToolbarDownloadButton) {
        if (
          self.FolderGrid.selectedItems.length == 0 ||
          !self.FolderGrid.selectedItems.some((el) => !el.IsFolder())
        ) {
          this.Disable();
          this.HideOnMobile();
        } else {
          this.Activate();
          this.ShowOnMobile();
        }
      }
      if (this instanceof CopyPasteButtonsControl) {
        if (self.FolderGrid.selectedItems.length == 0) {
          this.CopyButton.Disable();
          this.CopyButton.HideOnMobile();

          this.CutButton.Disable();
          this.CutButton.HideOnMobile();
        } else {
          this.CopyButton.Activate();
          this.CopyButton.ShowOnMobile();

          this.CutButton.Activate();
          this.CutButton.ShowOnMobile();
        }

        if (this.storedItems.length == 0) {
          this.PasteButton.Disable();
          this.PasteButton.HideOnMobile();
        } else {
          this.PasteButton.Activate();
          this.PasteButton.ShowOnMobile();
        }
      }
      if (
        ITHit.Environment.OS == "Windows" &&
        this instanceof ToolbarPrintButton
      ) {
        if (
          self.FolderGrid.selectedItems.filter(function (item) {
            return !item.IsFolder();
          }).length == 0
        ) {
          this.Disable();
          this.HideOnMobile();
        } else {
          this.Activate();
          this.ShowOnMobile();
        }
      }
    });
  },

  ResetToolbar: function () {
    this.FolderGrid.UncheckTableCheckboxs();
    this.UpdateToolbarButtons();
  },
};
