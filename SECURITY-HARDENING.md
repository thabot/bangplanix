# 🔒 Bangplanix — Enterprise Security Hardening Guide

แนวทางการตั้งค่าความปลอดภัยระดับองค์กร (Enterprise Security Hardening) สำหรับ **Bangplanix Enterprise Reporting Engine v1.0.0**

---

## 🛡️ 1. Sandbox Execution & Roslyn Compiler

- **Dynamic Expression Sandbox:**
  - สูตร C# Dynamic Expressions (`=Fields["Amount"] * 0.07`) ถูกประมวลผลผ่าน `RoslynSandbox` ที่สแกน AST Whitelist
  - บล็อกคลาสอันตรายทั้งหมด (`System.IO`, `System.Diagnostics.Process`, `System.Reflection`, `System.Net`, Unsafe pointers)
  - กำหนด Execution Timeout สูงสุดไม่เกิน 5,000ms ป้องกัน ReDoS และ Infinite Loops

---

## 🌐 2. Network & SSRF Mitigation

- **Anti-SSRF Filter & TOCTOU Prevention:**
  - ทำงานผ่าน `CreateSafeSocketsHttpHandler` ของ Bangplanix Core
  - ตรวจสอบ DNS Resolution ล่วงหน้าก่อนเปิด Socket Connection
  - บล็อก IP ในกลุ่ม RFC 1918 Private Subnets, Loopback, Link-Local, และ Cloud Metadata IP (`169.254.169.254`) 100%

---

## 🔑 3. Cryptographic Agility & Licensing

- **License Signature Verification:**
  - รองรับทั้ง **Ed25519** (Standard) และ **ML-DSA-65 (CRYSTALS-Dilithium)** (Post-Quantum Cryptography FIPS 204)
  - ไม่มีการส่งข้อมูล License Key ออกนอกเครือข่าย (100% Air-Gapped Verification)
- **Document Integrity & Tamper Seals:**
  - ลงลายมือชื่อดิจิทัล PAdES ISO 32000-2 ด้วย SHA-256 / SHA-512 RSA / ECDSA
  - ผนึก Merkle Tree Hash Seals ทุกหน้าเพื่อตรวจจับการแก้ไขเอกสาร

---

## 🤖 4. AI Firewall & Zero Data Retention

- **Prompt Injection Firewall:**
  - สแกนเจตนา Jailbreak, Instruction Override, และ Role-play Evasion ผ่าน `PromptInjectionFirewall`
- **Zero Data Retention:**
  - ส่งเฉพาะ Column Metadata & Schema ไปยัง LLM Providers; ไม่ส่ง Customer Row Data
- **Output DLP Redaction:**
  - เซนเซอร์ข้อมูลหลุด (เลขบัตรประชาชนไทย, หมายเลขบัตรเครดิต, อีเมล, หมายเลขโทรศัพท์) อัตโนมัติก่อนส่งให้ผู้ใช้งาน
