/**
 * BrowserStorage Engine for Bangplanix Designer
 * Supports LocalStorage with In-Memory / Safe Quota Fallback and Auto-Save
 */
import { BpxReportSchema, StorageItem } from './types.js';

export class BrowserStorageRepository {
  private prefix: string;
  private memoryFallback: Map<string, string> = new Map();
  private isLocalStorageAvailable: boolean;

  constructor(prefix = 'bangplanix:') {
    this.prefix = prefix;
    this.isLocalStorageAvailable = this.checkStorageAvailability();
  }

  private checkStorageAvailability(): boolean {
    try {
      if (typeof window === 'undefined' || !window.localStorage) return false;
      const testKey = `${this.prefix}__test__`;
      window.localStorage.setItem(testKey, '1');
      window.localStorage.removeItem(testKey);
      return true;
    } catch {
      return false;
    }
  }

  private getKey(key: string): string {
    return `${this.prefix}${key}`;
  }

  setItem(key: string, value: string): boolean {
    if (this.isLocalStorageAvailable) {
      try {
        window.localStorage.setItem(this.getKey(key), value);
        return true;
      } catch (err) {
        console.warn('[BrowserStorage] LocalStorage quota exceeded or denied, falling back to in-memory store:', err);
      }
    }
    this.memoryFallback.set(this.getKey(key), value);
    return true;
  }

  getItem(key: string): string | null {
    if (this.isLocalStorageAvailable) {
      try {
        const val = window.localStorage.getItem(this.getKey(key));
        if (val !== null) return val;
      } catch {
        // Fallback to memory
      }
    }
    return this.memoryFallback.get(this.getKey(key)) || null;
  }

  removeItem(key: string): void {
    if (this.isLocalStorageAvailable) {
      try {
        window.localStorage.removeItem(this.getKey(key));
      } catch {}
    }
    this.memoryFallback.delete(this.getKey(key));
  }

  // --- Auto-Save & Draft Management ---
  autoSaveDraft(report: BpxReportSchema): boolean {
    try {
      const payload: StorageItem = {
        id: 'current_draft',
        name: report.metadata?.title || 'Untitled Draft',
        savedAt: new Date().toISOString(),
        schema: report
      };
      return this.setItem('draft:current', JSON.stringify(payload));
    } catch {
      return false;
    }
  }

  loadDraft(): StorageItem | null {
    const raw = this.getItem('draft:current');
    if (!raw) return null;
    try {
      return JSON.parse(raw) as StorageItem;
    } catch {
      return null;
    }
  }

  clearDraft(): void {
    this.removeItem('draft:current');
  }

  // --- Template Library Manager ---
  saveNamedTemplate(name: string, report: BpxReportSchema): boolean {
    const list = this.getTemplateList();
    const id = `tpl_${Date.now()}`;
    const item: StorageItem = {
      id,
      name,
      savedAt: new Date().toISOString(),
      schema: report
    };

    const success = this.setItem(`template:${id}`, JSON.stringify(item));
    if (success) {
      const existingIdx = list.findIndex(t => t.name === name);
      if (existingIdx >= 0) {
        list[existingIdx] = { id, name, savedAt: item.savedAt };
      } else {
        list.push({ id, name, savedAt: item.savedAt });
      }
      this.setItem('template:catalog', JSON.stringify(list));
    }
    return success;
  }

  getTemplateList(): Array<{ id: string; name: string; savedAt: string }> {
    const raw = this.getItem('template:catalog');
    if (!raw) return [];
    try {
      return JSON.parse(raw);
    } catch {
      return [];
    }
  }

  loadNamedTemplate(id: string): StorageItem | null {
    const raw = this.getItem(`template:${id}`);
    if (!raw) return null;
    try {
      return JSON.parse(raw);
    } catch {
      return null;
    }
  }

  deleteNamedTemplate(id: string): boolean {
    this.removeItem(`template:${id}`);
    const list = this.getTemplateList().filter(t => t.id !== id);
    return this.setItem('template:catalog', JSON.stringify(list));
  }
}

export const globalStorage = new BrowserStorageRepository();
