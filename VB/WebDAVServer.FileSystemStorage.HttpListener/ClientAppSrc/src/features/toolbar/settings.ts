import { getI18n } from "react-i18next";
const i18n = getI18n();

class ToolbarConfig {
  hideDisabledOnMobile: boolean;
  buttons: ButtonConfig[];

  constructor(hideDisabledOnMobile: boolean, buttons: ButtonConfig[]) {
    this.hideDisabledOnMobile = hideDisabledOnMobile;
    this.buttons = buttons;
  }
}

export class ButtonConfig {
  name: string;
  title: string;
  iconClassName: string;
  innerHtml: string;

  constructor(
    name: string,
    title: string,
    iconClassName: string,
    innerHtml: string
  ) {
    this.name = name;
    this.title = title;
    this.iconClassName = iconClassName;
    this.innerHtml = innerHtml;
  }
}

export const toolbarConfig = new ToolbarConfig(true, [
  new ButtonConfig(
    "createFolderButton",
    i18n.t("phrases.toolbar.createFolderButton"),
    "icon-create-folder",
    '<span class="d-none d-lg-inline text-nowrap">' +
      i18n.t("phrases.toolbar.createFolderButton") +
      "</span>"
  ),
  new ButtonConfig(
    "downloadButton",
    i18n.t("phrases.toolbar.downloadButton"),
    "icon-download-items",
    '<span class="d-none d-xl-inline">' +
      i18n.t("phrases.toolbar.downloadButton") +
      "</span>"
  ),
  new ButtonConfig(
    "uploadButton",
    i18n.t("phrases.toolbar.uploadButton"),
    "icon-upload-items",
    '<span class="d-none d-xl-inline">' +
      i18n.t("phrases.toolbar.uploadButton") +
      "</span>"
  ),
  new ButtonConfig(
    "renameButton",
    i18n.t("phrases.toolbar.renameButton"),
    "icon-rename-item",
    ""
  ),
  new ButtonConfig(
    "copyButton",
    i18n.t("phrases.toolbar.copyButton"),
    "icon-copy-items",
    ""
  ),
  new ButtonConfig(
    "pasteButton",
    i18n.t("phrases.toolbar.pasteButton"),
    "icon-paste-items",
    ""
  ),
  new ButtonConfig(
    "cutButton",
    i18n.t("phrases.toolbar.cutButton"),
    "icon-cut-items",
    ""
  ),
  new ButtonConfig(
    "reloadButton",
    i18n.t("phrases.toolbar.reloadButton"),
    "icon-reload-items",
    ""
  ),
  new ButtonConfig(
    "printButton",
    i18n.t("phrases.toolbar.printButton"),
    "icon-print-items",
    '<span class="d-none d-lg-inline">' +
      i18n.t("phrases.toolbar.printButton") +
      "</span>"
  ),
  new ButtonConfig(
    "deleteButton",
    i18n.t("phrases.toolbar.deleteButton"),
    "icon-delete-items",
    '<span class="d-none d-lg-inline">' +
      i18n.t("phrases.toolbar.deleteButton") +
      "</span>"
  ),
]);
