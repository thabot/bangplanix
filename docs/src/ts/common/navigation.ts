/**
 * Unified Global Navigation Header for Bangplanix Docs & Tools
 */
export function renderGlobalNav(activePage: 'designer' | 'playground' | 'pricing'): string {
  return `
    <header class="bpx-global-nav">
      <div class="nav-brand">
        <a href="index.html" class="brand-link">
          <span class="brand-icon">🚀</span>
          <span class="brand-name">Bangplanix</span>
          <span class="brand-badge">AOT ENGINE</span>
        </a>
      </div>
      <nav class="nav-links">
        <a href="designer.html" class="nav-item ${activePage === 'designer' ? 'active' : ''}">
          🎨 Visual Designer
        </a>
        <a href="index.html" class="nav-item ${activePage === 'playground' ? 'active' : ''}">
          ⚡ WASM Playground
        </a>
        <a href="pricing.html" class="nav-item ${activePage === 'pricing' ? 'active' : ''}">
          💎 Pricing & Plans
        </a>
      </nav>
      <div class="nav-actions">
        <span class="aot-badge desktop-only" title="390 Unit Tests Passing, Cold Start 0.05s">🛡️ 390 Tests Passed • 0.05s AOT</span>
        <a href="https://github.com/thabot/bangplanix" target="_blank" class="nav-btn github-btn" title="View on GitHub">
          ⭐ Star on GitHub
        </a>
      </div>
    </header>
  `;
}
