import moment from "moment";
import { getI18n } from "react-i18next";
const i18n = getI18n();

const sFileNameSpecialCharactersRestrictionFormat =
  i18n.t("phrases.validations.notContainFollowingCharacters") + ": {0}";
const sForbiddenNameChars = '\\/:*?"<>|';
export class CommonService {
  static getFileExtension(fileName: string) {
    var index = fileName.lastIndexOf(".");
    return index !== -1 ? fileName.substr(index + 1).toLowerCase() : "";
  }

  static formatFileSize(iSize: number) {
    if (!iSize) {
      return "0.00 B";
    }
    var i = Math.floor(Math.log(iSize) / Math.log(1024));
    return (
      (iSize / Math.pow(1024, i)).toFixed(2) +
      " " +
      ["B", "kB", "MB", "GB", "TB"][i]
    );
  }

  static formatDate(oDate: Date) {
    return moment(oDate).fromNow();
  }

  static validateName(sName: string) {
    var oRegExp = new RegExp("[" + sForbiddenNameChars + "]", "g");
    if (oRegExp.test(sName)) {
      var sMessage = this.pasteFormat(
        sFileNameSpecialCharactersRestrictionFormat,
        sForbiddenNameChars.replace(/\\?(.)/g, "$1 ")
      );
      return sMessage;
    }
  }

  static formatSnippet(html: string | null) {
    if (html) {
      var safePrefix = "__b__tag" + new Date().getTime();
      html = html
        .replace(/<b>/g, safePrefix + "_0")
        .replace(/<\/b>/g, safePrefix + "_1");
      html = "<div>" + html + "</div>";
      html = html
        .replace(new RegExp(safePrefix + "_0", "g"), "<b>")
        .replace(new RegExp(safePrefix + "_1", "g"), "</b>");
    }
    return html;
  }

  static pasteFormat(sPhrase: string, args: any) {
    var callbackReplace = function (this: any, oArguments: any) {
      this._arguments = oArguments;
    } as any as { new (oArguments: any): any };

    callbackReplace.prototype.Replace = function (sPlaceholder: string) {
      var iIndex = sPlaceholder.substr(1, sPlaceholder.length - 2);
      return "undefined" !== typeof this._arguments[iIndex]
        ? this._arguments[iIndex]
        : sPlaceholder;
    };

    if (/\{\d+?\}/.test(sPhrase)) {
      var oReplace = new callbackReplace(
        Array.prototype.slice.call(arguments, 1)
      );
      sPhrase = sPhrase.replace(/\{(\d+?)\}/g, function (args) {
        return oReplace.Replace(args);
      });
    }

    return sPhrase;
  }

  /**
   * Gets name with copy suffix or number of copies
   */
  static getCopySuffix(itemName: string, bWithCopySuffix: boolean) {
    var sCopySuffixName = "Copy";

    var aExtensionMatches = /\.[^\\.]+$/.exec(itemName);
    var sName =
      aExtensionMatches !== null
        ? itemName.replace(aExtensionMatches[0], "")
        : itemName;
    var sDotAndExtension =
      aExtensionMatches !== null ? aExtensionMatches[0] : "";

    var sLangCopy = sCopySuffixName;
    var oSuffixPattern = new RegExp(
      "- " + sLangCopy + "( \\(([0-9]+)\\))?$",
      "i"
    );

    var aSuffixMatches = oSuffixPattern.exec(sName);
    if (aSuffixMatches === null && bWithCopySuffix) {
      sName += " - " + sLangCopy;
    } else if (aSuffixMatches !== null && !aSuffixMatches[1]) {
      sName += " (2)";
    } else if (aSuffixMatches !== null) {
      var iNextNumber = parseInt(aSuffixMatches[2]) + 1;
      sName = sName.replace(
        oSuffixPattern,
        "- " + sLangCopy + " (" + iNextNumber + ")"
      );
    }

    itemName = sName + sDotAndExtension;
    return itemName;
  }

  static htmlEscape(text: string) {
    return String(text)
      .replace(/&/g, "&amp;")
      .replace(/"/g, "&quot;")
      .replace(/'/g, "&#39;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;");
  }

  static stringToBoolean(str: string) {
    switch (str.toLowerCase().trim()) {
      case "true":
      case "yes":
      case "1":
        return true;

      case "false":
      case "no":
      case "0":
      case null:
        return false;

      default:
        return Boolean(str);
    }
  }

  static getPropertyName<T extends object>(
    obj: T,
    expression: (x: { [Property in keyof T]: () => string }) => () => string
  ): string {
    const res: { [Property in keyof T]: () => string } = {} as {
      [Property in keyof T]: () => string;
    };

    Object.keys(obj).map((k) => (res[k as keyof T] = () => k));

    return expression(res)();
  }

  static timeSpan(iSeconds: number): string {
    var hours = Math.floor(iSeconds / 3600);
    var minutes = Math.floor((iSeconds - hours * 3600) / 60);
    var seconds = iSeconds - hours * 3600 - minutes * 60;
    var sResult = "";
    if (hours) sResult += hours + "h ";
    if (minutes) sResult += minutes + "m ";
    sResult += seconds + "s ";
    return sResult;
  }
}
