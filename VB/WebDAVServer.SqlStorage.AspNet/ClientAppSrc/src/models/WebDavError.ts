import { ITHit } from "webdav.client";
import { CommonService } from "../services/CommonService";
export class WebDavError {
  errorMessage: string;
  error:
    | ITHit.WebDAV.Client.Exceptions.WebDavException
    | ITHit.WebDAV.Client.Error
    | null;

  constructor(
    errorMessage: string,
    error:
      | ITHit.WebDAV.Client.Exceptions.WebDavException
      | ITHit.WebDAV.Client.Error
      | unknown
      | null
  ) {
    if (
      error instanceof ITHit.WebDAV.Client.Exceptions.WebDavException ||
      error instanceof ITHit.WebDAV.Client.Error
    ) {
      this.error = error;
    } else {
      this.error = null;
    }

    this.errorMessage = errorMessage;
  }

  getServerMessage(): string {
    let res = "";
    if (this.error) {
      if (
        this.error instanceof ITHit.WebDAV.Client.Exceptions.WebDavException
      ) {
        res = this.error?.Message.toString();
      } else if (this.error instanceof ITHit.WebDAV.Client.Error) {
        res = this.error?.Description.toString();
      }

      res = CommonService.htmlEscape(res)
        .replace(/\n/g, "<br />\n")
        .replace(/\t/g, "&nbsp;&nbsp;&nbsp;&nbsp;");
    }
    return res;
  }
}
