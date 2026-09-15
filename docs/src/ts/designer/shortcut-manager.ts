/**
 * Keyboard Shortcuts & HUD Manager for Bangplanix Designer
 * Captures Ctrl+Z, Ctrl+Y, Delete, Arrow keys, and Ctrl+S
 */
import { ShortcutItem } from '../core/types.js';

export const SHORTCUT_LIST: ShortcutItem[] = [
  { key: 'Ctrl + Z', macKey: 'Cmd + Z', action: 'undo', description: 'Undo last change' },
  { key: 'Ctrl + Y', macKey: 'Cmd + Shift + Z', action: 'redo', description: 'Redo last undone change' },
  { key: 'Delete / Backspace', macKey: 'Delete', action: 'delete', description: 'Delete selected element' },
  { key: 'Arrow Keys (↑↓←→)', macKey: 'Arrow Keys', action: 'nudge_1px', description: 'Nudge element by 1px' },
  { key: 'Shift + Arrow Keys', macKey: 'Shift + Arrow Keys', action: 'nudge_10px', description: 'Jump element by 10px' },
  { key: 'Ctrl + S', macKey: 'Cmd + S', action: 'save', description: 'Save draft to browser storage' },
  { key: 'Ctrl + P', macKey: 'Cmd + P', action: 'export_pdf', description: 'Instant PDF export download' },
  { key: 'Escape', macKey: 'Escape', action: 'deselect', description: 'Deselect all elements' }
];

export class ShortcutManager {
  private handlers: Map<string, () => void> = new Map();

  register(action: string, callback: () => void): void {
    this.handlers.set(action, callback);
  }

  handleKeyDown(e: KeyboardEvent): boolean {
    const isMac = typeof navigator !== 'undefined' && /Mac|iPod|iPhone|iPad/.test(navigator.platform);
    const modKey = isMac ? e.metaKey : e.ctrlKey;

    // Ctrl + S (Save)
    if (modKey && (e.key === 's' || e.key === 'S')) {
      e.preventDefault();
      this.trigger('save');
      return true;
    }

    // Ctrl + Z (Undo)
    if (modKey && !e.shiftKey && (e.key === 'z' || e.key === 'Z')) {
      e.preventDefault();
      this.trigger('undo');
      return true;
    }

    // Ctrl + Y or Ctrl + Shift + Z (Redo)
    if ((modKey && (e.key === 'y' || e.key === 'Y')) || (modKey && e.shiftKey && (e.key === 'z' || e.key === 'Z'))) {
      e.preventDefault();
      this.trigger('redo');
      return true;
    }

    // Delete or Backspace (Delete selected)
    if (e.key === 'Delete' || e.key === 'Backspace') {
      const target = e.target as HTMLElement | null;
      let activeEl: Element | null = typeof document !== 'undefined' ? document.activeElement : null;
      if (!activeEl && (target as any)?.shadowRoot?.activeElement) {
        activeEl = (target as any).shadowRoot.activeElement;
      }
      while (activeEl && (activeEl as HTMLElement).shadowRoot && (activeEl as HTMLElement).shadowRoot?.activeElement) {
        activeEl = (activeEl as HTMLElement).shadowRoot!.activeElement;
      }
      
      const path = typeof e.composedPath === 'function' ? e.composedPath() : [];
      const isInput = (activeEl && (activeEl.tagName === 'INPUT' || activeEl.tagName === 'TEXTAREA' || (activeEl as HTMLElement).isContentEditable)) ||
                      (target && (target.tagName === 'INPUT' || target.tagName === 'TEXTAREA' || target.isContentEditable)) ||
                      path.some((el: any) => el?.tagName === 'INPUT' || el?.tagName === 'TEXTAREA' || el?.isContentEditable);

      if (isInput) {
        return false; // Don't delete canvas element when typing in text input inside or outside Shadow DOM
      }
      e.preventDefault();
      this.trigger('delete');
      return true;
    }

    // Escape
    if (e.key === 'Escape') {
      this.trigger('deselect');
      return true;
    }

    return false;
  }

  private trigger(action: string): void {
    const handler = this.handlers.get(action);
    if (handler) handler();
  }

  getShortcuts(): ShortcutItem[] {
    return SHORTCUT_LIST;
  }
}
