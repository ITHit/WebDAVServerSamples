import { WebDavSettings } from '@/infrastructure/config/webDavSettings';
import type {
  WebSocketConnectionState,
  WebSocketConnectionStateHandler,
  WebSocketEvent,
  WebSocketEventHandler,
  WebSocketEventType,
  WebSocketServicePort,
} from '@/shared/types/webSocket';

export class WebSocketService implements WebSocketServicePort {
  private socket: WebSocket | null = null;
  private readonly eventHandlers = new Map<string, Set<WebSocketEventHandler>>();
  private readonly connectionStateHandlers = new Set<WebSocketConnectionStateHandler>();
  private readonly reconnectIntervalMs = 5000;
  private readonly maxReconnectAttempts = 10;
  private reconnectAttempts = 0;
  private intentionalClose = false;
  private lastError: Error | null = null;

  public addEventListener(eventType: WebSocketEventType, handler: WebSocketEventHandler): void {
    if (!this.eventHandlers.has(eventType)) {
      this.eventHandlers.set(eventType, new Set());
    }

    this.eventHandlers.get(eventType)?.add(handler);
  }

  public removeEventListener(eventType: WebSocketEventType, handler: WebSocketEventHandler): void {
    this.eventHandlers.get(eventType)?.delete(handler);
  }

  public addConnectionStateListener(handler: WebSocketConnectionStateHandler): void {
    this.connectionStateHandlers.add(handler);
  }

  public removeConnectionStateListener(handler: WebSocketConnectionStateHandler): void {
    this.connectionStateHandlers.delete(handler);
  }

  public getConnectionState(): WebSocketConnectionState {
    return {
      isConnected: this.socket !== null && this.socket.readyState === WebSocket.OPEN,
      reconnectAttempts: this.reconnectAttempts,
      error: this.lastError,
    };
  }

  public connect(): void {
    if (this.socket || typeof WebSocket === 'undefined') {
      return;
    }

    this.lastError = null;
    this.notifyConnectionState();
    this.socket = this.createWebSocket();
    this.socket.addEventListener('open', () => {
      this.reconnectAttempts = 0;
      this.lastError = null;
      this.notifyConnectionState();
    });
    this.socket.addEventListener('message', event => {
      void this.handleMessage(event);
    });
    this.socket.addEventListener('error', event => this.handleError(event));
    this.socket.addEventListener('close', () => this.handleClose());
  }

  public disconnect(): void {
    if (!this.socket) {
      return;
    }

    this.intentionalClose = true;
    this.socket.close();
    this.socket = null;
    this.notifyConnectionState();
  }

  private createWebSocket(): WebSocket {
    let url: string = '';
    if (WebDavSettings.WebSocketPath) {
      url = WebDavSettings.WebSocketPath;
    } else {
      const host = WebDavSettings.WebsiteRootUrl
        ? new URL(WebDavSettings.WebsiteRootUrl).host
        : window.location.host;
      url = window.location.protocol === 'https:' ? 'wss://' + host : 'ws://' + host;
    }
    return new WebSocket(url);
  }

  private async handleMessage(event: MessageEvent): Promise<void> {
    try {
      const data = JSON.parse(String(event.data)) as WebSocketEvent;
      const handlers = this.eventHandlers.get(data.EventType);

      if (!handlers) {
        return;
      }

      await Promise.all([...handlers].map(handler => handler(data)));
    } catch (error) {
      console.error('Error handling WebSocket message:', error);
    }
  }

  private handleError(error: Event): void {
    const message = (error as ErrorEvent).message || 'WebSocket connection failed.';
    this.lastError = new Error(message);
    console.error('WebSocket error:', message);
    this.intentionalClose = true;
    this.socket?.close();
    this.socket = null;
    this.notifyConnectionState();
    this.reconnect();
  }

  private handleClose(): void {
    const wasIntentional = this.intentionalClose;
    this.intentionalClose = false;
    this.socket = null;
    this.notifyConnectionState();

    if (!wasIntentional) {
      this.reconnect();
    }
  }

  private reconnect(): void {
    if (this.reconnectAttempts >= this.maxReconnectAttempts) {
      this.notifyConnectionState();
      return;
    }

    this.reconnectAttempts += 1;
    this.notifyConnectionState();
    setTimeout(() => this.connect(), this.reconnectIntervalMs);
  }

  private notifyConnectionState(): void {
    const state = this.getConnectionState();
    this.connectionStateHandlers.forEach(handler => {
      handler(state);
    });
  }
}

export const webSocketService = new WebSocketService();
