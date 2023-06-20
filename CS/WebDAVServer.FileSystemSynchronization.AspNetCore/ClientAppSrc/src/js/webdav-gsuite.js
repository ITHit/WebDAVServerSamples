import $ from "jquery";
import { Tab } from "bootstrap";
import { ITHit } from "webdav.client";
import { WebdavCommon } from "./webdav-common";
import Split from "split.js";

const sGSuitePreviewErrorMessage =
  "Preview document with G Suite Online Tool error.";
const sGSuiteEditErrorMessage =
  "Edit document with G Suite Online Editor error.";

export function Spliter(selectorLeftPanel, selectorRightPanel) {
  // add spliter button
  Split([selectorLeftPanel, selectorRightPanel], {
    elementStyle: function (dimension, size, gutterSize) {
      $(window).trigger("resize");
      if (size < 1) {
        return { "flex-basis": "0px", height: "0px" };
      } else {
        return {
          "flex-basis": "calc(" + size + "% - " + gutterSize + "px)",
          height: "",
        };
      }
    },
    gutterStyle: function (dimension, gutterSize) {
      return { "flex-basis": gutterSize + "px" };
    },
    sizes: [60, 40],
    minSize: [400, 0],
    expandToMin: true,
    gutterSize: 30,
    cursor: "col-resize",
    onDragEnd: function (sizes) {
      $(selectorRightPanel).removeClass("disable-iframe-events");
    },
    onDragStart: function (sizes) {
      $(selectorRightPanel).addClass("disable-iframe-events");
    },
  });

  // handle resize window event
  $(window).resize(function () {
    // settimeout because resize event is triggered before applying 'flex-basis' css rule
    this.setTimeout(function () {
      var $pnl = $("#leftPanel");
      var classAttr = "col";
      if ($pnl.width() <= 566) {
        classAttr += " medium-point";
      } else if ($pnl.width() <= 692) {
        classAttr += " large-point";
      } else if ($pnl.width() <= 800) {
        classAttr += " extra-large-point";
      }
      $pnl.attr("class", classAttr);
    }, 10);
  });
}

export function GSuiteEditor(selectorGSuite) {
  this.$gSuiteTabs = $(selectorGSuite + " #gSuiteTabs");
  this.$gSuitePreviewPanel = $(selectorGSuite + " #gSuitePreview");
  this.$gSuitePreviewBackground = $(
    selectorGSuite + " #gSuitePreviewBackground"
  );
  this.$gSuiteEditPanel = $(selectorGSuite + " #gSuiteEdit");
  this.$gSuiteEditBackground = $(selectorGSuite + " #gSuiteEditBackground");
  this.$fileName = $(selectorGSuite + " #fileName");
}

GSuiteEditor.prototype = {
  Render: function (oItem) {
    var self = this;
    this.isEditorLoaded = false;
    this.isPreviewLoaded = false;
    this.$gSuitePreviewPanel.empty();
    this.$gSuiteEditPanel.empty();
    this.$gSuiteEditContainer = $('<div class="inner-container"/>').appendTo(
      this.$gSuiteEditPanel
    );
    this.$gSuitePreviewContainer = $('<div class="inner-container"/>').appendTo(
      this.$gSuitePreviewPanel
    );

    // display file name
    this.$fileName.text(oItem.DisplayName);

    if (
      window.WebDAVController.OptionsInfo.Features &
      ITHit.WebDAV.Client.Features.GSuite
    ) {
      let editTabSelector = document.querySelector("#edit-tab");
      let previewTabSelector = document.querySelector("#preview-tab");
      if (self.activeSelectedTab == "edit") {
        this._RenderEditor(oItem);
        var editTab = new Tab(editTabSelector);
        editTab.show();
      } else {
        this._RenderPreview(oItem);
        var previewTab = new Tab(previewTabSelector);
        previewTab.show();
      }

      previewTabSelector.addEventListener(
        "shown.bs.tab",
        function () {
          self._RenderPreview(oItem);
        },
        { once: true }
      );

      editTabSelector.addEventListener(
        "shown.bs.tab",
        function () {
          self._RenderEditor(oItem);
        },
        { once: true }
      );
    } else if (
      !(
        window.WebDAVController.OptionsInfo.Features &
        ITHit.WebDAV.Client.Features.GSuite
      )
    ) {
      this.$gSuitePreviewBackground.text(
        "GSuite preview and edit is not supported."
      );
    }
  },

  _RenderEditor: function (oItem) {
    var self = this;
    this.activeSelectedTab = "edit";

    if (ITHit.WebDAV.Client.DocManager.IsGSuiteDocument(oItem.Href)) {
      if (!this.isEditorLoaded) {
        this.$gSuiteEditBackground.text("Loading ...");
        window.WebDAVController.GSuiteEditDoc(
          oItem.Href,
          this.$gSuiteEditContainer[0],
          function (e) {
            self.$gSuiteEditPanel.empty();
            self.$gSuiteEditBackground.text("Select a document to edit.");
            if (e instanceof ITHit.WebDAV.Client.Exceptions.LockedException) {
              WebdavCommon.ErrorModal.Show(
                "The document is locked exclusively.<br/>" +
                  "You can not edit the document in G Suite in case of an exclusive lock.",
                e
              );
            } else {
              WebdavCommon.ErrorModal.Show(sGSuiteEditErrorMessage, e);
            }
          }
        );
        this.isEditorLoaded = true;
      }
    } else {
      this.$gSuiteEditBackground.text(
        "GSuite editor for this type of document is not available."
      );
    }
  },

  _RenderPreview: function (oItem) {
    var self = this;
    this.activeSelectedTab = "preview";

    if (!this.isPreviewLoaded) {
      this.$gSuitePreviewBackground.text("Loading preview...");
      window.WebDAVController.GSuitePreviewDoc(
        oItem.Href,
        this.$gSuitePreviewContainer[0],
        function (e) {
          self.$gSuitePreviewPanel.empty();
          self.$gSuitePreviewBackground.text("Select a document to preview.");
          WebdavCommon.ErrorModal.Show(sGSuitePreviewErrorMessage, e);
        }
      );
      this.isPreviewLoaded = true;
    }
  },
};
