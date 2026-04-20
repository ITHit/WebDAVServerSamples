import type { AppEventBus, AppEventPayloads } from '@/shared/contracts/appEventBus';

class InMemoryAppEventBus implements AppEventBus {
  private events: Map<string, ((...args: unknown[]) => void)[]> = new Map();

  on<K extends keyof AppEventPayloads>(event: K, callback: (...args: AppEventPayloads[K]) => void): void {
    if (!this.events.has(event)) {
      this.events.set(event, []);
    }
    const handlers = this.events.get(event);
    if (handlers) {
      handlers.push(callback as (...args: unknown[]) => void);
    }
  }

  off<K extends keyof AppEventPayloads>(event: K, callback: (...args: AppEventPayloads[K]) => void): void {
    const callbacks = this.events.get(event);
    if (!callbacks) {
      return;
    }

    const index = callbacks.indexOf(callback as (...args: unknown[]) => void);
    if (index > -1) {
      callbacks.splice(index, 1);
    }
  }

  emit<K extends keyof AppEventPayloads>(event: K, ...args: AppEventPayloads[K]): void {
    const callbacks = this.events.get(event);
    if (callbacks) {
      callbacks.forEach(callback => callback(...args));
    }
  }

  clear(): void {
    this.events.clear();
  }
}

export const appEventBus = new InMemoryAppEventBus();
