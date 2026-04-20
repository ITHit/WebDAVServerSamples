import { IUploaderCore } from "@/infrastructure/webdav/WebDavClient";
import type { UploaderHandlers } from "./uploaderHandlers";

export class Uploader {
  private uploader: IUploaderCore;
  private isDropzoneAdded = false;
  private handlers: UploaderHandlers;
  private decodeUrl: (url: string) => string;


  public constructor(
    url: string,
    handlers: UploaderHandlers,
    uploaderCore: IUploaderCore,
    decodeUrl: (url: string) => string
  ) {
    this.uploader = uploaderCore;
    this.decodeUrl = decodeUrl;
    this.setUploadUrl(url);
    this.handlers = handlers;
    this.uploader.Queue.AddListener("OnQueueChanged", handlers.onQueueChanged, this);
    this.uploader.Queue.AddListener("OnUploadItemsCreated", handlers.onUploadItemsCreated, this);
  }

  public destroy() {
    if (this.uploader !== null) {
      this.uploader.Queue.RemoveListener("OnQueueChanged", this.handlers.onQueueChanged, this);
      this.uploader.Queue.RemoveListener("OnUploadItemsCreated", this.handlers.onUploadItemsCreated, this);
    }
  }

  public setUploadUrl(url: string) {
    this.uploader?.SetUploadUrl(this.decodeUrl(url));
  }

  public addDropzone() {
    if (!this.isDropzoneAdded) {
      this.uploader.Inputs.AddById("ithit-hidden-input");
      this.uploader.DropZones.AddById("ithit-dropzone");
      this.isDropzoneAdded = true;
    }
  }

  public addInput(inputId: string) {
    this.uploader.Inputs.AddById(inputId);
  }
}
