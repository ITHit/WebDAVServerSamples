﻿import $ from "jquery";

/**
 * This class represents button that occurred on client.
 * @class
 * @param {string} sName - The name of button.
 * @param {string} innerHtml - This innerHtml will be inserted into html.
 * @property {string} Name
 * @property {string} CssClass
 */
export function BaseButton(sName, innerHtml) {
  this.Name = sName;
  this.CssClass = "btn-tool";
  this.InnerHtmlContent = innerHtml;

  this.Create = function ($toolbarContainer) {
    this.$Button = $(document.createElement("button")).prop({
      type: "button",
      innerHTML: this.InnerHtmlContent,
      class: this.CssClass,
      title: this.Name,
    });

    $toolbarContainer.append(this.$Button);
  };

  this.Disable = function () {
    this.$Button.attr("disabled", true);
  };

  this.Activate = function () {
    this.$Button.attr("disabled", false);
  };

  this.HideOnMobile = function () {
    this.$Button.addClass("d-none d-md-inline d-hide-medium");
  };

  this.ShowOnMobile = function () {
    this.$Button.removeClass("d-none d-md-inline d-hide-medium");
  };
}
