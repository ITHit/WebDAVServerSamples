import { ITHit } from 'webdav.client';

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
    if (!this.error) return "";
    if (this.error instanceof ITHit.WebDAV.Client.Exceptions.WebDavException) {
      return this.error.Message.toString();
    }
    if (this.error instanceof ITHit.WebDAV.Client.Error) {
      return this.error.Description.toString();
    }
    return "";
  }

  /**
   * Check if error is an HTTP exception (type guard to avoid exposing ITHit types)
   */
  isHttpError(): boolean {
    return this.error instanceof ITHit.WebDAV.Client.Exceptions.WebDavHttpException;
  }

  /**
   * Get HTTP status code if this is an HTTP error
   */
  private getHttpErrorDetails(): { Status?: number; Uri?: string } | null {
    if (!this.isHttpError()) return null;
    return this.error as ITHit.WebDAV.Client.Exceptions.WebDavHttpException & {
      Status?: number;
      Uri?: string;
    };
  }

  getHttpStatusCode(): number | null {
    return this.getHttpErrorDetails()?.Status ?? null;
  }

  /**
   * Get the error message (sanitized and formatted)
   */
  getErrorMessage(): string {
    return this.errorMessage;
  }

  /**
   * Get the URI that caused the error (for HTTP errors)
   */
  getUri(): string | null {
    return this.getHttpErrorDetails()?.Uri ?? null;
  }
}
