# 🗺️ Bangplanix — Product Roadmap & Future Milestones

This document tracks planned architectural milestones, upcoming capabilities, and future feature backlogs for the Bangplanix enterprise reporting ecosystem.

---

## 🚀 Upcoming Release: v1.1.0 (Phase 5: Interactive AI Designer & Smart Assistant)

### 🤖 1. Bangplanix AI Suite — Web UI & Interactive Designer Integration

While the core AI Engine (`ReportAiGenerator`, `HybridLlmGateway`, `AiSqlSafetyValidator`, `SelfHealingReportAgent`, `AiExpressionAssistant`) is fully implemented in the .NET engine and JavaScript core logic, the visual frontend controls will be introduced in v1.1.0:

#### 1.1 AI Provider & API Key Configuration Dialog (`<bangplanix-designer>`)
- [ ] **Multi-Provider Settings Modal:**
  - Support for **Google Gemini** (Gemini 2.5 Flash / Pro / Gemini 3.x)
  - Support for **Azure OpenAI** (GPT-4o / GPT-4o-mini)
  - Support for **Anthropic Claude** (Claude 3.5 Sonnet / Claude 3.7)
  - Support for **Local / On-Premise Ollama** (e.g., `http://localhost:11434`, Llama 3.3 / DeepSeek-R1 / Qwen 2.5)
- [ ] **Key & Credential Management:**
  - Secure encrypted client-side credential storage (`localStorage` / `sessionStorage`).
  - Option for team/enterprise managed credentials via backend server proxy.
  - Zero telemetry or third-party credential leakage.
- [ ] **Connectivity & Model Health Check:**
  - Integrated *"Test Connection"* / *"Ping Model"* button with latency measurement and quota status check.

#### 1.2 Interactive AI Chat & Prompt-to-Report Generator Modal
- [ ] **Ribbon Toolbar Integration:**
  - Prominent **"✨ AI Assistant"** and **"🤖 Generate with AI"** action buttons on the top ribbon toolbar of `<bangplanix-designer>`.
- [ ] **Conversational Chat Dialog:**
  - Interactive chat interface supporting natural language prompts in **Thai** and **English**.
  - Example Thai prompt: *"สร้างรายงานใบเสร็จรับเงิน มีรหัสสินค้า ชื่อสินค้า จำนวน ราคา ยอดรวม พร้อม QR Code PromptPay และยอดเงินบาทถ้วน"*
  - Example English prompt: *"Generate a modern executive sales dashboard with revenue column chart, customer table, and KPI cards."*
- [ ] **One-Click Apply & Visual Schema Diff:**
  - Preview generated `.bpx` schema with visual side-by-side or overlay diff before applying to canvas.
  - Seamless integration with Undo/Redo history stack (`Ctrl+Z` / `Ctrl+Y`).
- [ ] **AI Auto-Fix & Diagnostics Button:**
  - One-click trigger for `SelfHealingReportAgent.DiagnoseAndRepair()` to automatically fix broken bands, missing page setups, or corrupted JSON directly in the designer.
- [ ] **AI Expression Assistant Popup:**
  - Contextual prompt popup in the Property Inspector and Bottom Editor to translate formula intent into sandboxed C# Roslyn expressions (e.g., *"คำนวณภาษี VAT 7%"* ➔ `=Convert.ToDecimal(Fields["Amount"]) * 0.07m`).

---

### 🌐 2. Server-Side REST AI Proxy / Gateway API (`Bangplanix.Server`)

To allow browser clients to leverage AI features in enterprise environments without exposing provider API keys on the client:

- [ ] **`POST /api/v1/ai/generate`**:
  - Accepts natural language prompt and optional table schemas.
  - Passes through `PromptInjectionFirewall` and server-side `HybridLlmGateway`.
  - Returns safe, validated `.bpx` JSON schema with read-only SQL query.
- [ ] **`POST /api/v1/ai/expression`**:
  - Translates business calculation prompts into sandboxed Roslyn C# expressions.
- [ ] **`POST /api/v1/ai/diagnose-repair`**:
  - Diagnoses and auto-repairs broken `.bpx` template payloads via `SelfHealingReportAgent`.
- [ ] **`POST /api/v1/ai/executive-summary`**:
  - Generates analytical markdown summaries and IQR/Z-score outlier detection from dataset arrays.

---

## 🔮 Future Backlog (v1.2.0+)

### 📊 3. Autonomous Analytics & Visual Insights
- [ ] **AI Chart Recommendation:** Automatically inspects dataset column types and suggests optimal chart types (Bar, Line, Radar, Waterfall, Sparkline).
- [ ] **Natural Language Drill-Down Queries:** Click any chart element to ask follow-up questions in natural language.

### 🏢 4. Multi-Tenant Enterprise AI Hub
- [ ] Centralized Admin Console for enterprise prompt audit logs, DLP violation alarms, and tenant token consumption dashboards.
- [ ] Fine-tuned local domain models for Thai Government / Tax Invoicing regulations.
