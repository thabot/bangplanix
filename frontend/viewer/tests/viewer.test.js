import { test, describe, beforeEach } from 'node:test';
import assert from 'node:assert/strict';
import { BangplanixViewerCore } from '../viewer-core.js';

describe('Bangplanix Viewer Core & State Tests', () => {
  let viewer;

  beforeEach(() => {
    viewer = new BangplanixViewerCore({
      serverUrl: 'http://localhost:9545',
      template: 'schema/v1/samples/invoice.bpx'
    });
  });

  describe('Initialization', () => {
    test('should initialize with correct default properties', () => {
      assert.equal(viewer.zoomLevel, 100);
      assert.equal(viewer.zoomMode, 'manual');
      assert.equal(viewer.searchOpen, false);
      assert.equal(viewer.searchQuery, '');
      assert.equal(viewer.searchResults.length, 0);
      assert.equal(viewer.isFullscreen, false);
      assert.equal(viewer.parameterPanelOpen, false);
      assert.equal(viewer.mobileActionsOpen, false);
    });
  });

  describe('Mobile Touch & Gestures Operations', () => {
    test('calculatePinchZoom should scale zoom smoothly based on pinch distance ratio', () => {
      // Zoom in: current distance is 2x initial distance
      const zoomedIn = viewer.calculatePinchZoom(100, 200, 100);
      assert.equal(zoomedIn, 200);
      assert.equal(viewer.zoomLevel, 200);

      // Zoom out: current distance is half
      const zoomedOut = viewer.calculatePinchZoom(200, 100, 200);
      assert.equal(zoomedOut, 100);
      assert.equal(viewer.zoomLevel, 100);
    });

    test('calculatePinchZoom should enforce min (25%) and max (500%) boundaries', () => {
      // Extreme pinch in -> clamps to 25%
      const minClamped = viewer.calculatePinchZoom(1000, 10, 100);
      assert.equal(minClamped, 25);
      assert.equal(viewer.zoomLevel, 25);

      // Extreme pinch out -> clamps to 500%
      const maxClamped = viewer.calculatePinchZoom(10, 1000, 100);
      assert.equal(maxClamped, 500);
      assert.equal(viewer.zoomLevel, 500);
    });

    test('calculatePinchZoom should gracefully handle zero or negative distances', () => {
      const initial = viewer.zoomLevel;
      assert.equal(viewer.calculatePinchZoom(0, 100, initial), initial);
      assert.equal(viewer.calculatePinchZoom(100, 0, initial), initial);
      assert.equal(viewer.calculatePinchZoom(-50, 100, initial), initial);
    });

    test('handleDoubleTapZoom should toggle fit-width on mobile screen and reset to 100%', () => {
      // First double tap when at 100% zoom -> fits to mobile width (380px)
      const fittedZoom = viewer.handleDoubleTapZoom(380, 794);
      assert.equal(viewer.zoomMode, 'fit-width');
      assert.ok(fittedZoom > 0);

      // Second double tap when zoomed -> resets to 100%
      viewer.setZoom(150);
      const reset = viewer.handleDoubleTapZoom(380, 794);
      assert.equal(reset, 100);
      assert.equal(viewer.zoomLevel, 100);
    });

    test('handleDoubleTapZoom should compute correct scale for various mobile screen widths', () => {
      // iPhone standard viewport width (~390px)
      const iphoneZoom = viewer.handleDoubleTapZoom(390, 794);
      assert.equal(viewer.zoomMode, 'fit-width');
      assert.equal(iphoneZoom, 45); // (390-32)/794 * 100 = 45%

      // Android viewport width (~412px)
      viewer.setZoom(100);
      const androidZoom = viewer.handleDoubleTapZoom(412, 794);
      assert.equal(androidZoom, 48); // (412-32)/794 * 100 = 48%

      // Tablet portrait width (~768px)
      viewer.setZoom(100);
      const tabletZoom = viewer.handleDoubleTapZoom(768, 794);
      assert.equal(tabletZoom, 93); // (768-32)/794 * 100 = 93%
    });

    test('toggleMobileActionsMenu should toggle mobile actions popup state and support explicit open/close', () => {
      assert.equal(viewer.toggleMobileActionsMenu(), true);
      assert.equal(viewer.mobileActionsOpen, true);

      // Explicit close
      assert.equal(viewer.toggleMobileActionsMenu(false), false);
      assert.equal(viewer.mobileActionsOpen, false);

      // Explicit open
      assert.equal(viewer.toggleMobileActionsMenu(true), true);
      assert.equal(viewer.mobileActionsOpen, true);
    });
  });

  describe('Interactive Parameter Panel & Cascading Filters', () => {
    test('should load parameter definitions and populate default values', () => {
      const defs = [
        { name: 'Country', label: 'Country', type: 'String', defaultValue: 'TH', isRequired: true, availableValues: [{ label: 'Thailand', value: 'TH' }, { label: 'Japan', value: 'JP' }] },
        { name: 'Province', label: 'Province', type: 'String', cascadingParent: 'Country', availableValues: [{ label: 'Bangkok', value: 'TH_BKK' }, { label: 'Tokyo', value: 'JP_TYO' }] },
        { name: 'MinAmount', label: 'Min Amount', type: 'Number', defaultValue: 100, minValue: 10, maxValue: 10000 },
        { name: 'TaxId', label: 'Tax ID', type: 'String', validationPattern: '^\\d{13}$' }
      ];

      viewer.setParameterDefinitions(defs);
      assert.equal(viewer.parameters['Country'], 'TH');
      assert.equal(viewer.parameters['MinAmount'], 100);
    });

    test('setParameter should trigger cascading filter for child parameter', () => {
      const defs = [
        { name: 'Country', label: 'Country', type: 'String', availableValues: [{ label: 'Thailand', value: 'TH' }, { label: 'Japan', value: 'JP' }] },
        { name: 'Province', label: 'Province', type: 'String', cascadingParent: 'Country', availableValues: [{ label: 'Bangkok', value: 'TH_BKK' }, { label: 'Chiang Mai', value: 'TH_CNX' }, { label: 'Tokyo', value: 'JP_TYO' }] }
      ];

      viewer.setParameterDefinitions(defs);
      viewer.setParameter('Country', 'TH');

      const filteredProvinces = viewer.parameterOptionsMap['Province'];
      assert.equal(filteredProvinces.length, 2);
      assert.ok(filteredProvinces.every(p => p.value.startsWith('TH')));
      assert.equal(viewer.parameters['Province'], 'TH_BKK');

      // Switch Country to JP
      viewer.setParameter('Country', 'JP');
      const jpProvinces = viewer.parameterOptionsMap['Province'];
      assert.equal(jpProvinces.length, 1);
      assert.equal(jpProvinces[0].value, 'JP_TYO');
    });

    test('validateParameters should catch required, min/max, and regex errors', () => {
      const defs = [
        { name: 'RequiredField', label: 'Required Field', type: 'String', isRequired: true },
        { name: 'Age', label: 'Age', type: 'Number', minValue: 18, maxValue: 65 },
        { name: 'TaxId', label: 'Tax ID', type: 'String', validationPattern: '^\\d{13}$' }
      ];

      viewer.setParameterDefinitions(defs);

      // 1. Missing Required
      let val = viewer.validateParameters();
      assert.equal(val.isValid, false);
      assert.ok(val.errors.some(e => e.includes('Required Field')));

      // 2. Out of range number & invalid regex
      viewer.setParameter('RequiredField', 'Filled');
      viewer.setParameter('Age', 10);
      viewer.setParameter('TaxId', '12345'); // only 5 digits instead of 13

      val = viewer.validateParameters();
      assert.equal(val.isValid, false);
      assert.ok(val.errors.some(e => e.includes('>= 18')));
      assert.ok(val.errors.some(e => e.includes('format is invalid')));

      // 3. Valid parameters
      viewer.setParameter('Age', 25);
      viewer.setParameter('TaxId', '1234567890123');
      val = viewer.validateParameters();
      assert.equal(val.isValid, true);
      assert.equal(val.errors.length, 0);
    });

    test('resetParametersToDefaults should clear custom inputs and restore defaults', () => {
      const defs = [
        { name: 'Country', type: 'String', defaultValue: 'TH' },
        { name: 'Amount', type: 'Number', defaultValue: 500 }
      ];

      viewer.setParameterDefinitions(defs);
      viewer.setParameter('Country', 'US');
      viewer.setParameter('Amount', 9999);

      viewer.resetParametersToDefaults();
      assert.equal(viewer.parameters['Country'], 'TH');
      assert.equal(viewer.parameters['Amount'], 500);
    });
  });

  describe('High-Resolution Vector Zoom Operations', () => {
    test('zoomIn and zoomOut should step zoom level correctly', () => {
      viewer.zoomIn(25);
      assert.equal(viewer.zoomLevel, 125);

      viewer.zoomOut(50);
      assert.equal(viewer.zoomLevel, 75);
    });

    test('setZoom should clamp zoom between 25% and 500%', () => {
      viewer.setZoom(10);
      assert.equal(viewer.zoomLevel, 25);

      viewer.setZoom(600);
      assert.equal(viewer.zoomLevel, 500);
    });

    test('resetZoom should revert zoom to 100%', () => {
      viewer.setZoom(250);
      assert.equal(viewer.zoomLevel, 250);

      viewer.resetZoom();
      assert.equal(viewer.zoomLevel, 100);
      assert.equal(viewer.zoomMode, 'manual');
    });

    test('fitToWidth should compute correct percentage based on container width', () => {
      const zoom = viewer.fitToWidth(1200, 794);
      assert.equal(viewer.zoomMode, 'fit-width');
      assert.equal(zoom, 147);
      assert.equal(viewer.zoomLevel, 147);
    });

    test('fitToPage should compute correct percentage based on container height', () => {
      const zoom = viewer.fitToPage(900, 1123);
      assert.equal(viewer.zoomMode, 'fit-page');
      assert.equal(zoom, 76);
      assert.equal(viewer.zoomLevel, 76);
    });
  });

  describe('Document Text Search Operations', () => {
    const mockCorpus = [
      'Bangplanix Enterprise Invoice 2026. Customer: Acme Corp. Subtotal: 10,000 THB',
      'Page 2: Terms and Conditions. Contact Acme Corp for support. Total Tax: 700 THB'
    ];

    test('toggleSearch should toggle searchOpen state and reset query on close', () => {
      assert.equal(viewer.toggleSearch(), true);
      viewer.executeSearch('Acme', mockCorpus);
      assert.equal(viewer.searchResults.length, 2);

      assert.equal(viewer.toggleSearch(), false);
      assert.equal(viewer.searchQuery, '');
      assert.equal(viewer.searchResults.length, 0);
    });

    test('executeSearch should find all matching occurrences across pages case-insensitively', () => {
      viewer.toggleSearch(true);
      const res = viewer.executeSearch('acme', mockCorpus);

      assert.equal(res.total, 2);
      assert.equal(res.current, 1);
      assert.equal(res.activeMatch.pageNumber, 1);
      assert.equal(res.activeMatch.charIndex, 46);
    });

    test('nextSearchResult and previousSearchResult should cycle circularly through matches', () => {
      viewer.toggleSearch(true);
      viewer.executeSearch('corp', mockCorpus);
      assert.equal(viewer.searchResults.length, 2);

      const secondMatch = viewer.nextSearchResult();
      assert.equal(secondMatch.current, 2);
      assert.equal(secondMatch.activeMatch.pageNumber, 2);

      const backToFirst = viewer.nextSearchResult();
      assert.equal(backToFirst.current, 1);
      assert.equal(backToFirst.activeMatch.pageNumber, 1);

      const prevMatch = viewer.previousSearchResult();
      assert.equal(prevMatch.current, 2);
      assert.equal(prevMatch.activeMatch.pageNumber, 2);
    });

    test('executeSearch with empty or not found query should return 0 results', () => {
      const resEmpty = viewer.executeSearch('', mockCorpus);
      assert.equal(resEmpty.total, 0);
      assert.equal(resEmpty.current, 0);

      const resNotFound = viewer.executeSearch('NonExistentKeyword', mockCorpus);
      assert.equal(resNotFound.total, 0);
      assert.equal(resNotFound.current, 0);
    });
  });

  describe('Fullscreen Operations', () => {
    test('toggleFullscreen should toggle state correctly', () => {
      assert.equal(viewer.toggleFullscreen(), true);
      assert.equal(viewer.isFullscreen, true);

      assert.equal(viewer.toggleFullscreen(), false);
      assert.equal(viewer.isFullscreen, false);
    });
  });

  describe('E2E Parameter Panel & Drawer Workflow', () => {
    test('full parameter lifecycle should handle open, fill, cascade, validate, and reset', () => {
      // 1. Open parameter panel
      assert.equal(viewer.toggleParameterPanel(true), true);
      assert.equal(viewer.parameterPanelOpen, true);

      // 2. Load parameters
      viewer.setParameterDefinitions([
        { name: 'Region', type: 'String', defaultValue: 'North', isRequired: true },
        { 
          name: 'City', 
          type: 'String', 
          defaultValue: 'north_cm', 
          isRequired: true,
          cascadingParent: 'Region',
          availableValues: [
            { value: 'north_cm', label: 'North - Chiang Mai' },
            { value: 'south_pk', label: 'South - Phuket' }
          ]
        }
      ]);
      assert.equal(viewer.parameters['Region'], 'North');
      assert.equal(viewer.parameters['City'], 'north_cm');

      // 3. User updates Region -> triggers cascade
      viewer.setParameter('Region', 'South');
      assert.equal(viewer.parameters['Region'], 'South');
      assert.equal(viewer.parameters['City'], 'south_pk', 'Dependent parameter should update to matched cascade option');

      // 4. Validate -> valid
      let valRes = viewer.validateParameters();
      assert.equal(valRes.isValid, true);
      assert.equal(valRes.errors.length, 0);

      // 5. Empty required field and validate
      viewer.setParameter('Region', '');
      valRes = viewer.validateParameters();
      assert.equal(valRes.isValid, false);
      assert.ok(valRes.errors.length > 0);

      // 6. Reset to defaults
      viewer.resetParametersToDefaults();
      assert.equal(viewer.parameters['Region'], 'North');
    });
  });
});
