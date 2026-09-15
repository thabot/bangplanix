/**
 * Type-safe Pub/Sub Event Bus for Bangplanix Web Component architecture
 */
export type EventHandler<T = any> = (payload: T) => void;

export class BangplanixEventBus {
  private listeners: Map<string, Set<EventHandler>> = new Map();

  subscribe<T = any>(event: string, handler: EventHandler<T>): () => void {
    if (!this.listeners.has(event)) {
      this.listeners.set(event, new Set());
    }
    this.listeners.get(event)!.add(handler as EventHandler);

    // Return un-subscribe function
    return () => this.unsubscribe(event, handler);
  }

  unsubscribe(event: string, handler: EventHandler): void {
    const handlers = this.listeners.get(event);
    if (handlers) {
      handlers.delete(handler);
      if (handlers.size === 0) {
        this.listeners.delete(event);
      }
    }
  }

  publish<T = any>(event: string, payload: T): void {
    const handlers = this.listeners.get(event);
    if (handlers) {
      handlers.forEach(handler => {
        try {
          handler(payload);
        } catch (err) {
          console.error(`[EventBus] Error in handler for event "${event}":`, err);
        }
      });
    }
  }

  clear(): void {
    this.listeners.clear();
  }
}

export const globalEventBus = new BangplanixEventBus();
