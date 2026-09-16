## 🎯 Goal Overview
ยกระดับฟังก์ชัน AI ของระบบ Bangplanix ตาม Roadmap Milestone v1.1.0 โดยเชื่อมต่อ Core AI Engine ที่มีอยู่แล้ว (`ReportAiGenerator`, `AiExpressionAssistant`, `SelfHealingReportAgent`, `AiExecutiveSummaryEngine`) เข้ากับ:
1. **Server-Side REST AI Proxy API (`Bangplanix.Server`)**: รองรับ Endpoint สำหรับ Browser clients และ Enterprise integration โดยไม่ต้องเปิดเผย Provider API Keys ใน Client
2. **Interactive Visual AI Designer UI Integration (`<bangplanix-designer>`)**: เพิ่มปุ่มควบคุม Ribbon Toolbar, กล่องแชทสนทนาสร้างรายงานด้วย Natural Language (Prompt-to-Report), กล่องแก้ไขสูตร Roslyn C# Expression, ตัวตรวจซ่อมแซม Template (Auto-Fix), และ Modal จัดการ Provider Keys (BYOK / Server Proxy)

### 💡 Core Design Philosophy (แนวคิดหลัก: AI ทำงานแทนคน ล้อกับหน้าจอ Designer เดิม 100%)
- **เปรียบเสมือนมี Designer คนหนึ่งมานั่งทำแทนผู้ใช้**: AI จะไม่ได้สร้างระบบ UI ขึ้นมาใหม่แยกต่างหาก แต่จะทำหน้าที่เป็น "ผู้ช่วยอัจฉริยะ" ที่จัดวาง element ต่างๆ ลงบนโครงสร้าง Designer เดิมที่มีอยู่แล้ว 100%
- **Manipulate Canvas โดยตรง**: สิ่งที่ AI สร้าง ไม่ว่าจะเป็น Bands (`PageHeader`, `Detail`, `PageFooter`), Components (`Text`, `Table`, `Chart`, `Barcode`, `QrCode`), หรือ Formula Expressions จะถูกลงทะเบียนและแสดงผลลงใน Canvas เดิมของ `<bangplanix-designer>` อย่างสมบูรณ์
- **User สามารถคลิกแก้ไขต่อได้ทันที**: เมื่อ AI วางเลย์เอาต์เสร็จ ผู้ใช้สามารถคลิกลากย้าย (Drag & Drop), ปรับขนาด (Resize), เปลี่ยนฟอนต์/สีใน Property Inspector ด้านขวา หรือแก้ไขสูตรต่อด้วยมือตามปกติอย่างไร้รอยต่อ
- **Undo / Redo 100% Parity**: การกระทำของ AI จะถูก push เข้า Undo/Redo History Stack ของ Designer เดิม เพื่อให้กด `Ctrl+Z` ยกเลิกหรือย้อนกลับได้เสมอเหมือนคนทำเอง

---

## 🔍 User Review Required
> [!IMPORTANT]
> - **Security & Privacy:** การประมวลผล AI จะผ่าน `ZeroDataRetentionGuard` และ `AiSqlSafetyValidator` เสมอ เพื่อห้ามส่ง PII / Raw Data ออกนอกระบบ และบล็อกคำสั่ง SQL ที่ไม่ปลอดภัย (DDL/DML) 100%
> - **Zero Disruption to Existing UI:** การเพิ่มปุ่ม Ribbon และ Modal ใน `<bangplanix-designer>` จะไม่มีการลบหรือซ่อนปุ่มเดิมที่มีอยู่ตามกฎ Governance
> - **Testing & Branching:** หลังพัฒนาและรันชุดทดสอบผ่าน 100% จะทำการ Commit และ Push ขึ้นเฉพาะ branch `uat` ตามกฎของ Repository

---

## 📐 Proposed Changes

### 1. Backend REST AI Proxy (`Bangplanix.Server`)
#### [MODIFY] [Program.cs](file:///d:/thabot/git/gitlab/thabot/bangplanix/src/Bangplanix.Server/Program.cs)
เพิ่ม AI Service Endpoints ภายใต้ Base Route `/api/v1/ai/*`:
- `POST /api/v1/ai/generate`:
  - Request: `{ prompt: string, tableSchemas?: TableSchemaDescriptor[], tenantId?: string }`
  - Logic: เรียก `ReportAiGenerator.GenerateReportAsync()` เพื่อสร้าง `.bpx` JSON Schema และ SQL ปลอดภัย
  - Response: `{ success: bool, bpxJson: string, reportDefinition?: object, generatedSql?: string, errorMessage?: string, elapsedMs: double }`
- `POST /api/v1/ai/expression`:
  - Request: `{ intent: string, availableFields?: string[], datasetName?: string }`
  - Logic: เรียก `AiExpressionAssistant.GenerateExpressionAsync()` แปลงความต้องการทางธุรกิจเป็น Roslyn C# expression
  - Response: `{ success: bool, expression: string, explanation: string, returnType: string }`
- `POST /api/v1/ai/diagnose-repair`:
  - Request: `{ bpxJson: string }`
  - Logic: เรียก `SelfHealingReportAgent.DiagnoseAndRepairAsync()` วิเคราะห์และซ่อมแซม schema ที่ชำรุด
  - Response: `{ success: bool, repairedBpxJson: string, fixesApplied: string[] }`
- `POST /api/v1/ai/executive-summary`:
  - Request: `{ dataset: object[], metricColumns?: string[], dimensionColumn?: string }`
  - Logic: เรียก `AiExecutiveSummaryEngine.GenerateSummaryAsync()` สร้าง Markdown Summary และตรวจจับ Outlier ด้วย IQR/Z-score
  - Response: `{ success: bool, markdownSummary: string, anomalies: object[], kpis: object }`
- `POST /api/v1/ai/test-connection`:
  - Request: `{ provider: string, apiKey: string, modelName: string, endpointUrl?: string }`
  - Logic: ทดสอบ Ping ไปยัง LLM และวัด Latency
- `GET /api/v1/ai/history`:
  - ดึงรายการประวัติการสั่งงาน AI ย้อนหลัง (Prompt text, generated summary, snapshot id, timestamp) จากฐานข้อมูล SQLite
- `POST /api/v1/ai/history/undo`:
  - Request: `{ historyId: long }`
  - Logic: โหลด Template Snapshot ดั้งเดิมก่อนที่ AI จะทำการแก้ไขจาก SQLite เพื่อทำการ Rollback/Undo คืนสถานะ 100%

#### 🗄️ 1.1 SQLite AI History & Undo Storage
- เพิ่มตารางใน SQLite Portal Database (`portal.db`):
  ```sql
  CREATE TABLE IF NOT EXISTS portal_ai_history (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      username TEXT NOT NULL DEFAULT 'guest',
      prompt_text TEXT NOT NULL,
      provider TEXT NOT NULL,
      model_name TEXT NOT NULL,
      original_bpx_snapshot TEXT,
      generated_bpx TEXT NOT NULL,
      generated_sql TEXT,
      created_at_utc TEXT NOT NULL
  );
  ```
- เพิ่ม API รองรับการค้นหาประวัติและกู้คืน (Undo) ย้อนหลังได้อย่างแม่นยำ

#### 🔌 1.2 Bangplanix MCP (Model Context Protocol) Server Integration
เพิ่มความสามารถในการเป็น **MCP Server** (เปิดให้ Claude Desktop, Cursor, AI IDEs, หรือ LLM Agents ต่างๆ เชื่อมต่อและเรียกใช้งาน Bangplanix เป็น Tool อัตโนมัติ):
- **Transport Modes:**
  1. **Standard I/O (Stdio):** รันผ่าน CLI เช่น `bangplanix mcp` สำหรับ Claude Desktop / Cursor / Local IDEs
  2. **SSE / HTTP Transport:** Endpoint `POST /api/v1/mcp` และ `GET /api/v1/mcp/sse` บน Bangplanix.Server
- **MCP Tools Exposed (เครื่องมือที่ AI ภายนอกเรียกใช้ได้):**
  - `generate_report_bpx`: สร้างหรือปรับแต่ง `.bpx` Report Template ตาม Natural Language
  - `validate_report_bpx`: ตรวจสอบความถูกต้องของ Schema
  - `render_report_pdf`: สั่ง Render Template และ Data เป็นไฟล์ PDF
  - `render_report_xlsx`: สั่ง Render เป็น Excel Spreadsheet
  - `repair_report_bpx`: ซ่อมแซม Template ที่พังอัตโนมัติ (Self-Healing)
  - `list_container_templates`: ดูรายการเทมเพลตที่มีอยู่ในระบบ

#### [NEW] [AiProxyTests.cs](file:///d:/thabot/git/gitlab/thabot/bangplanix/tests/Bangplanix.Server.Tests/AiProxyTests.cs)
- เพิ่ม Integration / Unit Test สำหรับทดสอบ Endpoint `/api/v1/ai/*` และ MCP Tools ทั้งกรณีปกติและ Error cases

#### [MODIFY] [GETTING-STARTED.th.md](file:///d:/thabot/git/gitlab/thabot/bangplanix/GETTING-STARTED.th.md) & [GETTING-STARTED.md](file:///d:/thabot/git/gitlab/thabot/bangplanix/GETTING-STARTED.md)
- **เพิ่มบทใหม่: "คู่มือการใช้งาน ASP.NET Core REST API & AI Backend Endpoints"**
  1. **Core REST API Reference:**
     - ตารางสรุป HTTP Method, Endpoint, Request Payload, Response Format ของระบบหลัก (`/api/v1/render/pdf`, `/api/v1/render/excel`, `/api/v1/convert`, `/api/v1/files`, `/api/v1/auth/*`, `/health`, `/metrics`)
  2. **AI Backend REST API Reference:**
     - `POST /api/v1/ai/generate`: การสร้าง `.bpx` จาก Prompt / คำสั่งเสียง
     - `POST /api/v1/ai/expression`: การแปลงภาษาธรรมชาติเป็น C# Roslyn formula
     - `POST /api/v1/ai/diagnose-repair`: การส่งตรวจซ่อมแซม Template อัตโนมัติ
     - `POST /api/v1/ai/test-connection`: การทดสอบเชื่อมต่อ Provider & Model
     - อธิบายการส่ง Headers สำหรับ BYOK (`X-Bangplanix-Ai-Provider`, `X-Bangplanix-Ai-Key`, `X-Bangplanix-Ai-Model`)
  3. **คู่มือการเชื่อมต่อ Model Context Protocol (MCP) เต็มรูปแบบ:**
     - **บทนำและสถาปัตยกรรม:** อธิบายหลักการทำงานของ Bangplanix MCP Server ที่เปิดให้ AI Agents ควบคุม Engine ได้โดยตรง
     - **การตั้งค่า Claude Desktop:** ตัวอย่างการเพิ่มคอนฟิกใน `claude_desktop_config.json` (ทั้งโหมด Stdio Command และ HTTP/SSE Server)
     - **การตั้งค่าสำหรับ Cursor / VS Code / Windsurf:** วิธีตั้งค่า MCP Server ใน Extension และ Settings ของ AI IDEs
     - **คู่มือพารามิเตอร์ MCP Tools:** อธิบายโครงสร้าง JSON Schema, Input Parameters, และ Expected Output ของ Tool แต่ละตัว (`generate_report_bpx`, `validate_report_bpx`, `render_report_pdf`, `render_report_xlsx`, `repair_report_bpx`, `list_container_templates`)
     - **ตัวอย่าง Prompt ทดสอบ:** ตัวอย่างบทสนทนากับ Claude/Cursor เพื่อสั่งให้สร้างและเรนเดอร์รายงานอัตโนมัติ

---

### 2. Frontend Visual AI Designer (`<bangplanix-designer>`)
#### [MODIFY] [bangplanix-designer.ts](file:///d:/thabot/git/gitlab/thabot/bangplanix/frontend/designer/bangplanix-designer.ts)
- **Top Ribbon Toolbar:**
  - เพิ่มปุ่ม **"✨ AI Assistant"** (เปิด Chat & Auto-Fix Modal)
  - เพิ่มปุ่ม **"🤖 Prompt to Report"** (สร้าง Template อัตโนมัติจากข้อความ)
  - เพิ่มปุ่ม **"⚙️ AI Settings"** (ตั้งค่า Provider / Keys / Server Proxy mode)
- **Interactive AI Chat & Generator Modal:**
  - รองรับ Prompt ทั้งภาษาไทยและอังกฤษ
  - **🎙️ Voice-to-Text Microphone Support (พิมพ์ด้วยเสียงพูดหลายภาษาทั่วโลก):**
    - มีปุ่มไอคอนไมโครโฟน **"🎙️ Speech to Text"** ข้างกล่องป้อน Prompt พร้อมตัวเลือกสลับภาษาเสียงพูด (**Voice Language Dropdown**)
    - ใช้ **Web Speech API (`SpeechRecognition` / `webkitSpeechRecognition`)** รองรับรหัสภาษามาตรฐาน BCP-47 เต็มรูปแบบ ครอบคลุมภาษาสากลยอดนิยม:
      - 🇹🇭 **ไทย (Thai)**: `th-TH`
      - 🇺🇸 **English (US)**: `en-US` / 🇬🇧 **English (UK)**: `en-GB`
      - 🇨🇳 **中文 (Chinese Simplified)**: `zh-CN` / 🇹🇼 **中文 (Traditional)**: `zh-TW`
      - 🇯🇵 **日本語 (Japanese)**: `ja-JP`
      - 🇰🇷 **한국어 (Korean)**: `ko-KR`
      - 🇻🇳 **Tiếng Việt (Vietnamese)**: `vi-VN`
      - 🇮🇩 **Bahasa Indonesia**: `id-ID`
      - 🇪🇸 **Español (Spanish)**: `es-ES`
      - 🇩🇪 **Deutsch (German)**: `de-DE`
      - 🇫🇷 **Français (French)**: `fr-FR`
      - 🇸🇦 **العربية (Arabic)**: `ar-SA`
      - 🇮🇳 **हिन्दी (Hindi)**: `hi-IN`
    - บันทึกภาษาที่ผู้ใช้เลือกล่าสุดลงใน `localStorage` จำไว้ใช้ในครั้งถัดไปอัตโนมัติ
    - แสดงสถานะ Pulse Animation ขณะกำลังรับฟัง พร้อมแปลงเสียงพูด Real-time ลงในช่อง Prompt ทันที
    - มี Fallback แจ้งเตือนสวยงามหากเบราว์เซอร์หรือสภาพแวดล้อมไม่รองรับ Web Speech API / ไมโครโฟน
  - **📋 Quick Starter Prompt Cards (ตัวอย่างชุดคำสั่งสำเร็จรูปตามภาษาของระบบ):**
    - ปรับเปลี่ยนข้อความตัวอย่างคำสั่งให้อัตโนมัติตามภาษาของเว็บ/ภาษาที่ผู้ใช้เลือก (Active Web/Speech Language):
      - 🇹🇭 **ไทย (Thai)**:
        - 🧾 *"สร้างใบเสร็จรับเงิน พร้อม QR PromptPay และตารางรายการสินค้า"*
        - 📊 *"สร้างแดชบอร์ดสรุปยอดขายรายเดือน พร้อมกราฟแท่ง Column Chart"*
        - 🏷️ *"สร้างใบปะหน้าพัสดุและฉลากบาร์โค้ด Code 128"*
        - 📑 *"สร้างใบกำกับภาษีเต็มรูป มีชื่อผู้ซื้อ-ผู้ขาย และคำนวณภาษี VAT 7%"*
      - 🇺🇸 / 🇬🇧 **English**:
        - 🧾 *"Generate a sales receipt with payment QR and itemized table"*
        - 📊 *"Create monthly executive sales dashboard with revenue column chart"*
        - 🏷️ *"Generate shipping logistics label with Code 128 barcode"*
        - 📑 *"Create commercial VAT invoice with subtotal, tax 7%, and total"*
      - 🇨🇳 / 🇹🇼 **中文 (Chinese)**:
        - 🧾 *"生成包含商品明细表与二维码的销售收据"*
        - 📊 *"创建月度销售执行仪表盘，包含柱状图与关键指标"*
      - 🇯🇵 **日本語 (Japanese)**:
        - 🧾 *"商品明細表とQRコード付きの領収書・レシートを作成"*
        - 📊 *"売上カラムチャートとKPIカードを含む月次ダッシュボード作成"*
      - 🇪🇸 **Español**:
        - 🧾 *"Generar recibo de venta con tabla de productos y código QR"*
        - 📊 *"Crear panel de ventas mensual con gráfico de columnas"*
    - คลิกครั้งเดียว ข้อความจะถูกกรอกลงในกล่อง Prompt ให้ทันทีตามภาษาที่เลือก พร้อมส่งสร้างหรือแก้ไขต่อได้สะดวก
  - **📜 SQLite AI Prompt History & Instant Undo (ประวัติการสั่ง AI และระบบกู้คืน):**
    - แถบประวัติ (History Drawer) โหลดรายการคำสั่ง Prompt ที่เคยสั่งไว้จาก SQLite Database
    - แสดง Prompt ล่าสุด, โมเดลที่ใช้, และเวลาที่สั่ง
    - มีปุ่ม **"↺ Undo / Rollback"**: คลิกเพื่อย้อนคืนค่า `.bpx` Snapshot ดั้งเดิมก่อนที่ AI จะปรับแต่งได้ตลอดเวลา แม้ปิดเบราว์เซอร์ไปแล้วเปิดใหม่
    - มีปุ่ม **"Re-run"**: คลิกเพื่อดึงข้อความ Prompt เก่ามาใช้งานหรือปรับปรุงต่อได้ทันที
  - **⏳ Loading State & Cancel Generation Button (สถานะกำลังประมวลผลและปุ่มกดยกเลิก):**
    - แสดง Skeleton shimmer / Animated glowing spinner บน Canvas และใน Modal ขณะรอ LLM ตอบกลับ
    - มีข้อความแสดงสเต็ปการทำงาน Real-time (เช่น *"กำลังวิเคราะห์คำสั่ง..."*, *"กำลังจัดวางองค์ประกอบลงบน Bands..."*, *"กำลังตรวจสอบความปลอดภัยของ SQL..."*)
    - มีปุ่ม **"⏹️ Cancel Generation"** ให้ User กดยกเลิกการประมวลผลได้ทันทีหากเปลี่ยนใจ โดยใช้ `AbortController` ตัดการเชื่อมต่อ HTTP ทันที เพื่อไม่ให้ระบบค้างหรือเปลืองโควตา
  - มีระบบแสดง JSON Preview / Diff เปรียบเทียบก่อนกด Apply เข้า Canvas
  - รองรับ Undo/Redo stack เมื่อกด Apply
- **AI Expression Assistant Popup:**
  - ปุ่มตัวช่วย AI ข้างกล่องป้อน Expression ใน Property Inspector เพื่อแปลงความต้องการเป็นโค้ด Roslyn
- **Self-Healing One-Click Repair:**
  - ปุ่มกด "Auto-Fix Report" เพื่อส่งตรวจสอบและซ่อมโครงสร้างรายงานที่พัง
- **Provider Settings Dialog:**
  - **Option 1: Server Proxy Mode (Default / Enterprise Managed)**
    - เรียกผ่าน `/api/v1/ai/*` ของ Bangplanix.Server โดยอัตโนมัติ ไม่ต้องกรอก Key ใดๆ ใน Browser (เหมาะสำหรับองค์กรที่ดูแล Key จากส่วนกลาง)
  - **Option 2: Client BYOK Mode (Bring Your Own Key)**
    - ลูกค้ากรอก API Key ของตนเองผ่านหน้าจอ UI จัดเก็บเข้ารหัสใน `localStorage` ของผู้ใช้:
      1. **Provider Selector**: สลับเลือก Google Gemini, OpenAI, Azure OpenAI, Anthropic Claude, หรือ Ollama (Local)
      2. **Model Name (Free-Text Input)**: ช่องพิมพ์ชื่อ Model เองได้อย่างอิสระ ไม่จำกัดเฉพาะใน Dropdown List เพื่อรองรับโมเดลใหม่ๆ ที่อัปเดตตลอดเวลา (เช่น `gemini-2.5-flash`, `gemini-3-flash`, `gpt-4o`, `claude-3-7-sonnet`, `deepseek-r1`) พร้อมมี Datalist แนะนำค่าเริ่มต้น
      3. **API Key Input**: ช่องกรอก Key พร้อมปุ่ม Mask/Unmask (`••••••`)
      4. **Endpoint URL (Optional)**: ป้อน Custom Endpoint สำหรับ Ollama / Azure / Private Proxy
      5. **Test Connection Button**: ปุ่มกดทดสอบ Ping ตรวจสอบว่า Key และ Model เชื่อมต่อสำเร็จจริงหรือไม่
      6. **Zero-Retention Forwarding**: ส่ง Key ไปประมวลผลเฉพาะ Request นั้นๆ ไม่มีการเก็บบันทึกบนเซิร์ฟเวอร์
  - **Option 3: Web Management Portal Central Setting (`GET /portal`)**
    - มีหน้าจอแท็บใน Portal ให้ผู้ดูแลระบบระดับ Tenant สามารถทดสอบและบันทึก Key ส่วนกลางได้เช่นกัน

#### [MODIFY] [tests/designer.test.js](file:///d:/thabot/git/gitlab/thabot/bangplanix/frontend/designer/tests/designer.test.js)
- เพิ่ม Automated Tests ทดสอบ AI Toolbar Buttons, Modal Interactions, และ Expression generation events

---

## 📋 Granular TaskList Breakdown (รายการงานละเอียดย่อยทุกสเต็ป)

### 🔹 Module 1: Backend REST AI Proxy & SQLite History (`Bangplanix.Server`)
- [ ] **Task 1.1: SQLite AI History & Undo Schema Migration**
  - [ ] เพิ่ม DDL `portal_ai_history` ใน `SqlitePortalDatabase.cs` พร้อมฟิลด์ `original_bpx_snapshot`, `generated_bpx`, `provider`, `model_name`
  - [ ] อัปเดต `IPortalDatabase.cs` เพิ่มเมธอด `SaveAiHistoryAsync()`, `GetAiHistoryAsync()`, `GetAiSnapshotAsync()`
  - [ ] เพิ่ม Unit Test การบันทึกและดึงข้อมูล History/Snapshot ใน SQLite
- [ ] **Task 1.2: Server-Side REST AI Proxy Endpoints**
  - [ ] แมป Route `POST /api/v1/ai/generate` รองรับ Prompt + Schema + Tenant + BYOK Headers (`X-Bangplanix-Ai-*`)
  - [ ] แมป Route `POST /api/v1/ai/expression` แปลงภาษาพูดเป็น Roslyn C# Expression
  - [ ] แมป Route `POST /api/v1/ai/diagnose-repair` ซ่อมแซม `.bpx` เสียหายผ่าน `SelfHealingReportAgent`
  - [ ] แมป Route `POST /api/v1/ai/executive-summary` ตรวจจับ Outlier ด้วย IQR/Z-score + Markdown
  - [ ] แมป Route `POST /api/v1/ai/test-connection` สำหรับ Ping ตรวจสอบ API Key & Model
  - [ ] แมป Route `GET /api/v1/ai/history` และ `POST /api/v1/ai/history/undo` สำหรับดึงประวัติและกู้คืน
- [ ] **Task 1.3: Bangplanix MCP Server Integration**
  - [ ] พัฒนา MCP Protocol Handler (Stdio CLI mode + SSE/HTTP mode at `/api/v1/mcp`)
  - [ ] ลงทะเบียน 6 Tools (`generate_report_bpx`, `validate_report_bpx`, `render_report_pdf`, `render_report_xlsx`, `repair_report_bpx`, `list_container_templates`)

---

### 🔹 Module 2: Visual AI Designer UI Integration (`<bangplanix-designer>`)
- [ ] **Task 2.1: Ribbon Toolbar & Design Parity Elements**
  - [ ] เพิ่มปุ่ม Ribbon: `✨ AI Assistant`, `🤖 Prompt to Report`, `⚙️ AI Settings` โดยรักษาปุ่มเดิม 100%
  - [ ] เพิ่ม AI Expression Button ใน Property Inspector ข้างช่อง Formula
  - [ ] เชื่อมต่อ Undo/Redo Stack (`core.undo()`, `core.redo()`) เมื่อ AI วางองค์ประกอบลง Canvas
- [ ] **Task 2.2: Interactive AI Chat & Generator Modal**
  - [ ] หน้าต่าง Prompt Dialog รองรับทั้งพิมพ์และแชท
  - [ ] **Quick Starter Prompt Cards:** แถบการ์ดตัวอย่างคำสั่งยอดนิยม สลับภาษาอัตโนมัติตาม Active Language (ไทย, อังกฤษ, จีน, ญี่ปุ่น, สเปน)
  - [ ] **Voice-to-Text Microphone:** ปุ่มไมโครโฟน ถอดเสียงพูดด้วย Web Speech API รองรับ 12+ ภาษาสากล พร้อม Visual Pulse
  - [ ] **Loading State & Cancel Button:** แสดง Skeleton Shimmer และปุ่ม `⏹️ Cancel Generation` ตัดการทำงานผ่าน `AbortController`
  - [ ] **Schema Diff & Preview:** แสดง Visual Diff เปรียบเทียบก่อนกด Apply เข้า Canvas
- [ ] **Task 2.3: SQLite History Drawer & Instant Undo UI**
  - [ ] แผง Drawer แสดงประวัติคำสั่ง Prompt ล่าสุดจาก SQLite
  - [ ] ปุ่ม `↺ Undo / Rollback` เรียกคืน Snapshot เก่าก่อน AI ปรับแต่ง
  - [ ] ปุ่ม `Re-run` นำ Prompt เก่ากลับมาสั่งงานซ้ำหรือต่อยอด
- [ ] **Task 2.4: BYOK Settings Modal & Free-Text Model Input**
  - [ ] Dialog เลือก Provider (Gemini, OpenAI, Claude, Azure, Ollama)
  - [ ] ช่อง Model Name แบบ Free-Text ให้ User พิมพ์ชื่อรุ่นได้อย่างอิสระ พร้อม Datalist
  - [ ] ช่องกรอก Key แบบ Mask/Unmask (`••••••`) + Endpoint URL
  - [ ] ปุ่ม Test Connection แสดงสถานะ Ping และ Latency (ms)

---

### 🔹 Module 3: Comprehensive Unit & Integration Tests (ทดสอบครอบคลุม 100%)
- [ ] **Task 3.1: Backend C# Unit & Integration Tests (`tests/Bangplanix.Server.Tests/AiProxyTests.cs`)**
  - [ ] Test 1: `GenerateReport_ValidPrompt_ReturnsValidBpxAndSql`
  - [ ] Test 2: `GenerateReport_SqlInjectionAttempt_BlockedByValidator`
  - [ ] Test 3: `ExpressionAssist_NaturalLanguage_ReturnsSandboxedRoslynExpression`
  - [ ] Test 4: `DiagnoseRepair_CorruptedJson_AutoRepairsSuccessfully`
  - [ ] Test 5: `TestConnection_ValidKey_ReturnsSuccessWithLatency`
  - [ ] Test 6: `TestConnection_InvalidKey_ReturnsDescriptiveError`
  - [ ] Test 7: `AiHistory_SaveAndRetrieve_PersistsToSqlite`
  - [ ] Test 8: `AiHistory_UndoSnapshot_RestoresOriginalTemplate`
  - [ ] Test 9: `McpProtocol_ListToolsAndExecute_ReturnsValidOutput`
- [ ] **Task 3.2: Frontend Designer Unit Tests (`frontend/designer/tests/designer-ai.test.js`)**
  - [ ] Test 1: Ribbon toolbar renders AI action buttons without breaking existing tools
  - [ ] Test 2: Multi-language starter prompt cards switch language dynamically
  - [ ] Test 3: Voice-to-Text SpeechRecognition triggers transcription and fallback alert
  - [ ] Test 4: Loading state renders skeleton and Cancel button aborts HTTP request
  - [ ] Test 5: BYOK free-text model input accepts custom models and saves to localStorage
  - [ ] Test 6: Applying AI generated schema preserves Undo/Redo stack parity

---

### 🔹 Module 4: Documentation Updates (`GETTING-STARTED`)
- [ ] **Task 4.1: ASP.NET Core REST API Server Reference**
  - [ ] ตารางสรุป Endpoint ระบบหลัก (`/render/pdf`, `/render/excel`, `/files`, `/convert`, `/auth/*`)
- [ ] **Task 4.2: AI Backend REST API Reference**
  - [ ] ตัวอย่าง Payload & Headers สำหรับ `/api/v1/ai/*`
- [ ] **Task 4.3: Model Context Protocol (MCP) Integration Guide**
  - [ ] ตัวอย่างคอนฟิก `claude_desktop_config.json`, Cursor/VS Code, และคำอธิบาย Tools ทั้ง 6 ตัว

---

## 🧪 Verification Plan

### Automated Tests Execution
1. **.NET Server & Engine Tests:**
   ```powershell
   dotnet test tests/Bangplanix.Server.Tests/Bangplanix.Server.Tests.csproj
   dotnet test tests/Bangplanix.Engine.Tests/Bangplanix.Engine.Tests.csproj
   ```
2. **Frontend Component Tests:**
   ```powershell
   cd frontend/designer
   npm test
   ```
3. **Syntax Verification & Build:**
   ```powershell
   dotnet build src/Bangplanix.Server/Bangplanix.Server.csproj
   cd frontend/designer && npm run build
   ```

### Manual Verification
- รัน Server ที่พอร์ต 9545 และเปิดหน้า Designer บนเบราว์เซอร์
- ทดสอบคลิกปุ่ม Quick Starter Prompt และทดสอบกดไมโครโฟนสั่งงานด้วยเสียง
- ทดสอบกด Cancel ขณะกำลังประมวลผล
- ทดสอบกด Apply แล้วกดย้อนกลับด้วย Undo (`Ctrl+Z`) และปุ่ม Rollback จากประวัติ SQLite
