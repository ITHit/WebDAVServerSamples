﻿import $ from "jquery";
import { Modal } from "bootstrap";
import { BaseButton } from "./webdav-basebutton";
import { ITHit } from "webdav.client";
import { WebdavCommon } from "../webdav-common";

function CreateFolderController(toolbar) {
  this.Toolbar = toolbar;
}

CreateFolderController.prototype = {
  CreateFolder: function (sFolderName, fCallback) {
    this.Toolbar.WebDAV.CurrentFolder.CreateFolderAsync(
      sFolderName,
      null,
      null,
      function (oAsyncResult) {
        fCallback(oAsyncResult);
      }
    );
  },
};

///////////////////
// Create Folder Bootstrap Modal
function CreateFolderModal(selector, createFolderController) {
  var sCreateFolderErrorMessage = "Create folder error.";

  var self = this;
  this.bsModal = new Modal(document.querySelector(selector));
  this.$modal = $(selector);
  this.$txt = this.$modal.find('input[type="text"]');
  this.$submitButton = this.$modal.find(".btn-submit");
  this.$alert = this.$modal.find(".alert-danger");

  this.$modal.on("shown.bs.modal", function () {
    self.$txt.focus();
  });

  this.$modal.find("form").submit(function () {
    self.$alert.addClass("d-none");
    if (self.$txt.val() !== null && self.$txt.val().match(/^ *$/) === null) {
      var oValidationMessage = WebdavCommon.Validators.ValidateName(
        self.$txt.val()
      );
      if (oValidationMessage) {
        self.$alert.removeClass("d-none").text(oValidationMessage);
        return false;
      }

      self.$txt.blur();
      self.$submitButton.attr("disabled", "disabled");
      createFolderController.CreateFolder(
        self.$txt.val().trim(),
        function (oAsyncResult) {
          if (!oAsyncResult.IsSuccess) {
            if (
              oAsyncResult.Error instanceof
              ITHit.WebDAV.Client.Exceptions.MethodNotAllowedException
            ) {
              self.$alert
                .removeClass("d-none")
                .text(
                  oAsyncResult.Error.Error.Description
                    ? oAsyncResult.Error.Error.Description
                    : "Folder already exists."
                );
            } else {
              WebdavCommon.ErrorModal.Show(
                sCreateFolderErrorMessage,
                oAsyncResult.Error
              );
            }
          } else {
            self.bsModal.hide();
          }
          self.$submitButton.removeAttr("disabled");
        }
      );
    } else {
      self.$alert.removeClass("d-none").text("Name is required!");
    }
    return false;
  });
}

export function ToolbarCreateFolderButton(name, toolbar) {
  BaseButton.call(
    this,
    name,
    '<i class="icon icon-create-folder"></i><span><span class="d-none d-xl-inline text-nowrap">Create Folder</span></span>'
  );
  var oCreateFolderModal = new CreateFolderModal(
    "#CreateFolderModal",
    new CreateFolderController(toolbar)
  );

  this.Render = function () {
    this.$Button.on("click", function () {
      oCreateFolderModal.$txt.val("");
      oCreateFolderModal.$alert.addClass("d-none");
      oCreateFolderModal.bsModal.show();
    });
  };
}
