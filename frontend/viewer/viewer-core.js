/**
 * Core state management and calculations for Bangplanix Viewer (with Parameters, Cascading Filters & Mobile Touch Support)
 */
export class BangplanixViewerCore {
  constructor(options = {}) {
    this.serverUrl = options.serverUrl || 'http://localhost:9545';
    this.template = options.template || '';
    this.data = options.data || '';
    this.pdfUrl = options.src || null;
    this.zoomLevel = 100;
    this.zoomMode = 'manual'; // 'manual' | 'fit-width' | 'fit-page'
    this.isLoading = false;
    this.errorMessage = null;

    // Search State
    this.searchOpen = false;
    this.searchQuery = '';
    this.searchResults = [];
    this.currentSearchIndex = -1;
    this.isFullscreen = false;

    // Mobile Actions Menu State
    this.mobileActionsOpen = false;

    // Parameter & Cascading Filter State
    this.parameterPanelOpen = false;
    this.parameterDefs = options.parameterDefs || [];
    this.parameters = { ...(options.parameters || {}) };
    this.parameterOptionsMap = {}; // paramName -> list of {label, value}
    this.validationErrors = [];
  }

  // Mobile Action Menu Operations
  toggleMobileActionsMenu(open = null) {
    this.mobileActionsOpen = open !== null ? open : !this.mobileActionsOpen;
    return this.mobileActionsOpen;
  }

  // Mobile Touch & Pinch Zoom Calculations
  calculatePinchZoom(initialDistance, currentDistance, initialZoom = 100) {
    if (initialDistance <= 0 || currentDistance <= 0) return this.zoomLevel;
    const scale = currentDistance / initialDistance;
    return this.setZoom(initialZoom * scale);
  }

  handleDoubleTapZoom(containerWidth = 400, pageNaturalWidth = 794) {
    if (this.zoomLevel <= 100) {
      return this.fitToWidth(containerWidth, pageNaturalWidth);
    } else {
      return this.resetZoom();
    }
  }

  // Parameter Operations
  toggleParameterPanel(open = null) {
    this.parameterPanelOpen = open !== null ? open : !this.parameterPanelOpen;
    return this.parameterPanelOpen;
  }

  setParameterDefinitions(defs) {
    this.parameterDefs = defs || [];
    this.parameterDefs.forEach(p => {
      if (this.parameters[p.name] === undefined && p.defaultValue !== undefined) {
        this.parameters[p.name] = p.defaultValue;
      }
      if (p.availableValues) {
        this.parameterOptionsMap[p.name] = p.availableValues;
      }
    });
  }

  setParameter(name, value) {
    this.parameters[name] = value;

    // Handle Cascading Children
    this.parameterDefs
      .filter(p => p.cascadingParent === name)
      .forEach(child => {
        this.updateCascadingChild(child, value);
      });
  }

  updateCascadingChild(childDef, parentValue) {
    if (!parentValue) {
      this.parameterOptionsMap[childDef.name] = [];
      this.parameters[childDef.name] = null;
      return;
    }

    const allOptions = childDef.availableValues || [];
    const parentStr = String(parentValue).trim().toLowerCase();

    const filtered = allOptions.filter(opt => {
      const optValStr = String(opt.value || '').toLowerCase();
      const optLabelStr = String(opt.label || '').toLowerCase();
      return optValStr.startsWith(parentStr) || optLabelStr.startsWith(parentStr);
    });

    this.parameterOptionsMap[childDef.name] = filtered;

    const currentVal = this.parameters[childDef.name];
    if (!currentVal || !filtered.some(f => f.value === currentVal)) {
      this.parameters[childDef.name] = filtered.length > 0 ? filtered[0].value : null;
    }
  }

  validateParameters() {
    this.validationErrors = [];

    this.parameterDefs.forEach(p => {
      const val = this.parameters[p.name];

      // 1. Required Check
      if (p.isRequired && (val === null || val === undefined || (typeof val === 'string' && val.trim() === ''))) {
        this.validationErrors.push(`Field '${p.label || p.name}' is required.`);
        return;
      }

      if (val === null || val === undefined || val === '') return;

      // 2. Type & Pattern Check
      if (p.type === 'Number') {
        const num = Number(val);
        if (isNaN(num)) {
          this.validationErrors.push(`Field '${p.label || p.name}' must be a number.`);
        } else {
          if (p.minValue !== undefined && num < p.minValue) {
            this.validationErrors.push(`Field '${p.label || p.name}' must be >= ${p.minValue}.`);
          }
          if (p.maxValue !== undefined && num > p.maxValue) {
            this.validationErrors.push(`Field '${p.label || p.name}' must be <= ${p.maxValue}.`);
          }
        }
      }

      if (p.validationPattern && typeof val === 'string') {
        const regex = new RegExp(p.validationPattern);
        if (!regex.test(val)) {
          this.validationErrors.push(`Field '${p.label || p.name}' format is invalid.`);
        }
      }
    });

    return {
      isValid: this.validationErrors.length === 0,
      errors: this.validationErrors
    };
  }

  resetParametersToDefaults() {
    this.parameters = {};
    this.validationErrors = [];
    this.parameterDefs.forEach(p => {
      if (p.defaultValue !== undefined) {
        this.parameters[p.name] = p.defaultValue;
      }
      if (p.availableValues) {
        this.parameterOptionsMap[p.name] = p.availableValues;
      }
    });
    return this.parameters;
  }

  // Zoom Operations
  setZoom(level) {
    this.zoomLevel = Math.max(25, Math.min(500, Math.round(level)));
    this.zoomMode = 'manual';
    return this.zoomLevel;
  }

  zoomIn(step = 25) {
    return this.setZoom(this.zoomLevel + step);
  }

  zoomOut(step = 25) {
    return this.setZoom(this.zoomLevel - step);
  }

  resetZoom() {
    this.zoomMode = 'manual';
    this.zoomLevel = 100;
    return this.zoomLevel;
  }

  fitToWidth(containerWidth = 1000, pageNaturalWidth = 794) {
    if (pageNaturalWidth <= 0 || containerWidth <= 0) return this.zoomLevel;
    const padding = 32;
    const availableWidth = containerWidth - padding;
    const calculatedZoom = Math.round((availableWidth / pageNaturalWidth) * 100);
    this.zoomMode = 'fit-width';
    this.zoomLevel = Math.max(25, Math.min(500, calculatedZoom));
    return this.zoomLevel;
  }

  fitToPage(containerHeight = 800, pageNaturalHeight = 1123) {
    if (pageNaturalHeight <= 0 || containerHeight <= 0) return this.zoomLevel;
    const padding = 48;
    const availableHeight = containerHeight - padding;
    const calculatedZoom = Math.round((availableHeight / pageNaturalHeight) * 100);
    this.zoomMode = 'fit-page';
    this.zoomLevel = Math.max(25, Math.min(500, calculatedZoom));
    return this.zoomLevel;
  }

  // Search Operations
  toggleSearch(open = null) {
    this.searchOpen = open !== null ? open : !this.searchOpen;
    if (!this.searchOpen) {
      this.clearSearch();
    }
    return this.searchOpen;
  }

  executeSearch(query, documentTextCorpus = []) {
    this.searchQuery = (query || '').trim();
    this.searchResults = [];
    this.currentSearchIndex = -1;

    if (!this.searchQuery) {
      return { total: 0, current: 0, activeMatch: null };
    }

    const lowerQuery = this.searchQuery.toLowerCase();
    documentTextCorpus.forEach((pageText, pageIndex) => {
      let pos = 0;
      const lowerPage = pageText.toLowerCase();
      while ((pos = lowerPage.indexOf(lowerQuery, pos)) !== -1) {
        this.searchResults.push({
          pageNumber: pageIndex + 1,
          charIndex: pos,
          matchLength: lowerQuery.length
        });
        pos += lowerQuery.length;
      }
    });

    if (this.searchResults.length > 0) {
      this.currentSearchIndex = 0;
    }

    return {
      total: this.searchResults.length,
      current: this.currentSearchIndex >= 0 ? this.currentSearchIndex + 1 : 0,
      activeMatch: this.currentSearchIndex >= 0 ? this.searchResults[this.currentSearchIndex] : null
    };
  }

  nextSearchResult() {
    if (this.searchResults.length === 0) return null;
    this.currentSearchIndex = (this.currentSearchIndex + 1) % this.searchResults.length;
    return {
      total: this.searchResults.length,
      current: this.currentSearchIndex + 1,
      activeMatch: this.searchResults[this.currentSearchIndex]
    };
  }

  previousSearchResult() {
    if (this.searchResults.length === 0) return null;
    this.currentSearchIndex = (this.currentSearchIndex - 1 + this.searchResults.length) % this.searchResults.length;
    return {
      total: this.searchResults.length,
      current: this.currentSearchIndex + 1,
      activeMatch: this.searchResults[this.currentSearchIndex]
    };
  }

  clearSearch() {
    this.searchQuery = '';
    this.searchResults = [];
    this.currentSearchIndex = -1;
  }

  // Fullscreen
  toggleFullscreen(force = null) {
    this.isFullscreen = force !== null ? force : !this.isFullscreen;
    return this.isFullscreen;
  }
}
