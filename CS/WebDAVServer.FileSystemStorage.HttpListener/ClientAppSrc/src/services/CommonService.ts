import { formatDistanceToNow } from "date-fns";
import { getI18n } from "react-i18next";
const i18n = getI18n();

const sFileNameSpecialCharactersRestrictionFormat =
  i18n.t("phrases.validations.notContainFollowingCharacters") + ": {0}";
const sForbiddenNameChars = '\\/:*?"<>|';
export class CommonService {
  static getFileExtension(fileName: string) {
    const index = fileName.lastIndexOf(".");
    return index !== -1 ? fileName.substr(index + 1).toLowerCase() : "";
  }

  static formatFileSize(iSize: number) {
    if (!iSize) {
      return "0.00 B";
    }
    const i = Math.floor(Math.log(iSize) / Math.log(1024));
    return (
      (iSize / Math.pow(1024, i)).toFixed(2) +
      " " +
      ["B", "kB", "MB", "GB", "TB"][i]
    );
  }

  static formatDate(oDate: Date) {
    return formatDistanceToNow(oDate, { addSuffix: true });
  }

  static validateName(sName: string) {
    const oRegExp = new RegExp("[" + sForbiddenNameChars + "]", "g");
    if (oRegExp.test(sName)) {
      const sMessage = this.pasteFormat(
        sFileNameSpecialCharactersRestrictionFormat,
        sForbiddenNameChars.replace(/\\?(.)/g, "$1 ")
      );
      return sMessage;
    }
  }

  static formatSnippet(html: string | null) {
    if (html) {
      const safePrefix = "__b__tag" + new Date().getTime();
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

  static pasteFormat(sPhrase: string, ...args: string[]) {
    class CallbackReplace {
      private _arguments: string[];

      constructor(oArguments: string[]) {
        this._arguments = oArguments;
      }

      Replace(sPlaceholder: string): string {
        const iIndex = parseInt(sPlaceholder.substr(1, sPlaceholder.length - 2), 10);
        return this._arguments[iIndex] !== undefined ? this._arguments[iIndex] : sPlaceholder;
      }
    }

    if (/\{\d+?\}/.test(sPhrase)) {
      const oReplace = new CallbackReplace(args);
      sPhrase = sPhrase.replace(/\{(\d+?)\}/g, function (match) {
        return oReplace.Replace(match);
      });
    }

    return sPhrase;
  }

  /**
   * Gets name with copy suffix or number of copies
   */
  static getCopySuffix(itemName: string, bWithCopySuffix: boolean) {
    const sCopySuffixName = "Copy";

    const aExtensionMatches = /\.[^\\.]+$/.exec(itemName);
    let sName =
      aExtensionMatches !== null
        ? itemName.replace(aExtensionMatches[0], "")
        : itemName;
    const sDotAndExtension =
      aExtensionMatches !== null ? aExtensionMatches[0] : "";

    const sLangCopy = sCopySuffixName;
    const oSuffixPattern = new RegExp(
      "- " + sLangCopy + "( \\(([0-9]+)\\))?$",
      "i"
    );

    const aSuffixMatches = oSuffixPattern.exec(sName);
    if (aSuffixMatches === null && bWithCopySuffix) {
      sName += " - " + sLangCopy;
    } else if (aSuffixMatches !== null && !aSuffixMatches[1]) {
      sName += " (2)";
    } else if (aSuffixMatches !== null) {
      const iNextNumber = parseInt(aSuffixMatches[2]) + 1;
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
    const hours = Math.floor(iSeconds / 3600);
    const minutes = Math.floor((iSeconds - hours * 3600) / 60);
    const seconds = iSeconds - hours * 3600 - minutes * 60;
    let sResult = "";
    if (hours) sResult += hours + "h ";
    if (minutes) sResult += minutes + "m ";
    sResult += seconds + "s ";
    return sResult;
  }
}
