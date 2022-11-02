﻿import $ from "jquery";
import { BaseButton } from "./webdav-basebutton";
import { ITHit } from "webdav.client";
import { webDavSettings } from "../webdav-settings";

function HerarhyItemPrintController(toolbar) {
  this.Toolbar = toolbar;
}

HerarhyItemPrintController.prototype = {
  /**
   * Print documents.
   * @param {string} sDocumentUrls Array of document URLs
   */
  PrintDocs: function (sDocumentUrls) {
    ITHit.WebDAV.Client.DocManager.DavProtocolEditDocument(
      sDocumentUrls,
      this.Toolbar.WebDAV.GetMountUrl(),
      this.Toolbar.WebDAV._ProtocolInstallMessage.bind(this.Toolbar.WebDAV),
      null,
      webDavSettings.EditDocAuth.SearchIn,
      webDavSettings.EditDocAuth.CookieNames,
      webDavSettings.EditDocAuth.LoginUrl,
      "Print"
    );
  },
  ExecutePrint: function () {
    var self = this;
    self.Toolbar.ConfirmModal.Confirm(
      "Are you sure want to print selected items?",
      function () {
        var filesUrls = [];
        $.each(self.Toolbar.FolderGrid.selectedItems, function () {
          if (!this.IsFolder()) {
            filesUrls.push(this.Href);
          }
        });

        self.PrintDocs(filesUrls);
      }
    );
  },
};

export function ToolbarPrintButton(name, toolbar) {
  BaseButton.call(
    this,
    name,
    '<i class="icon  icon-print-items"></i><span class="d-none d-xl-inline">Print</span>'
  );

  this.Render = function () {
    this.$Button.on("click", function () {
      new HerarhyItemPrintController(toolbar).ExecutePrint();
    });
  };
}
