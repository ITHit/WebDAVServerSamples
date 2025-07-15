import { WebDavSettings } from "../webDavSettings";
import {
  WebSocketEvent,
  WebSocketEventHandler,
  EventType,
} from "../models/WebSocketTypes";

export class WebSocketService {
  private socket: WebSocket | null = null;
  private eventHandlers: Map<string, Set<WebSocketEventHandler>> = new Map();
  private readonly RECONNECT_INTERVAL_MS = 5000;

  public addEventListener(
    eventType: EventType,
    handler: WebSocketEventHandler
  ): void {
    if (!this.eventHandlers.has(eventType)) {
      this.eventHandlers.set(eventType, new Set());
    }
    this.eventHandlers.get(eventType)?.add(handler);
  }

  public removeEventListener(
    eventType: EventType,
    handler: WebSocketEventHandler
  ): void {
    this.eventHandlers.get(eventType)?.delete(handler);
  }

  public connect(): void {
    if (this.socket) {
      return;
    }

    this.socket = this.createWebSocket();
    this.socket.addEventListener("message", (e) => this.handleMessage(e));
    this.socket.addEventListener("error", (e) => this.handleError(e));
    this.socket.addEventListener("close", () => this.handleClose());
  }

  public disconnect(): void {
    if (this.socket) {
      this.socket.close();
      this.socket = null;
    }
  }

  private createWebSocket(): WebSocket {
    let url: string = "";
    if (WebDavSettings.WebSocketPath) {
      url = WebDavSettings.WebSocketPath;
    } else {
      const host = WebDavSettings.WebsiteRootUrl
        ? new URL(WebDavSettings.WebsiteRootUrl).host
        : window.location.host;
      url =
        window.location.protocol === "https:"
          ? "wss://" + host
          : "ws://" + host;
    }
    console.log(`Creating WebSocket connection to ${url}`);
    return new WebSocket(`${url}`);
  }

  private async handleMessage(event: MessageEvent): Promise<void> {
    try {
      const data: WebSocketEvent = JSON.parse(event.data);
      const handlers = this.eventHandlers.get(data.EventType);

      if (handlers) {
        await Promise.all([...handlers].map((handler) => handler(data)));
      }
    } catch (error) {
      console.error("Error handling WebSocket message:", error);
    }
  }

  private handleError(error: Event): void {
    console.error("WebSocket error:", (error as ErrorEvent).message);
    this.disconnect();
    this.reconnect();
  }

  private handleClose(): void {
    this.socket = null;
    this.reconnect();
  }

  private reconnect(): void {
    setTimeout(() => this.connect(), this.RECONNECT_INTERVAL_MS);
  }
}

export const webSocketService = new WebSocketService();
