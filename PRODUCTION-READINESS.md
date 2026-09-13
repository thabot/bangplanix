# 🛡️ Bangplanix — Production Readiness Checklist & Deployment Runbook

เอกสารตรวจสอบความพร้อมก่อนขึ้นระบบใช้งานจริง (Production Go-Live Checklist) สำหรับ **Bangplanix Enterprise Reporting Engine v1.0.0**

---

## 📋 1. Pre-Flight Infrastructure Checklist

| หมวดหมู่ | รายการตรวจสอบ | ค่าแนะนำ (Production Default) | สถานะ |
| :--- | :--- | :--- | :---: |
| **Compute & OS** | Base Image / Architecture | Alpine Linux 3.20+ (x86_64 / ARM64) Non-Root (UID 10001) | [x] |
| **Memory Limit** | Pod / Container Memory Limit | Minimum 512Mi / Recommended 2Gi (Spill Buffer < 128MB) | [x] |
| **CPU Allocation** | CPU Limits / Cores | 2 to 8 vCPU (Supports HPA Scale Target 75% CPU) | [x] |
| **Network Ports** | REST / Web API Port | `9545` (HTTP/1.1 & HTTP/2 with TLS 1.3) | [x] |
| **gRPC Port** | Streaming Report Engine | `9546` (HTTP/2 Cleartext or TLS) | [x] |
| **Hardware Crypto** | AES-NI & SIMD Vectorization | Hardware Intrinsics AVX2 / AVX-512 / ARM Neon | [x] |

---

## 🔐 2. Security & Secret Management

- [x] **Master KMS Encryption Key:**
  - กำหนดตัวแปรสภาพแวดล้อม `THABOT_MASTER_KEY` (ความยาวขั้นต่ำ 32 ตัวอักษร สำหรับ AES-256-GCM Connection String Encryption)
  - ห้าม Hardcode Master Key ใน Dockerfile หรือ Codebase เด็ดขาด (แนะนำใช้ HashiCorp Vault, AWS Secrets Manager, หรือ Azure Key Vault)
- [x] **Network & SSRF Guard:**
  - ตรวจสอบให้แน่ใจว่า Anti-SSRF Guard ทำงานอยู่เสมอ (บล็อก Private IP Range: `10.0.0.0/8`, `172.16.0.0/12`, `192.168.0.0/16`, `127.0.0.1`, `169.254.169.254`)
- [x] **Kubernetes Security Context:**
  ```yaml
  securityContext:
    runAsNonRoot: true
    runAsUser: 10001
    runAsGroup: 10001
    readOnlyRootFilesystem: true
    allowPrivilegeEscalation: false
    capabilities:
      drop:
        - ALL
  ```

---

## 📈 3. Observability & Monitoring

1. **Health Probes:**
   - Liveness Probe: `GET http://<host>:9545/healthz` (Timeout: 2s, Interval: 10s)
   - Readiness Probe: `GET http://<host>:9545/healthz` (Initial Delay: 3s)
2. **Prometheus Metrics:**
   - Metric Scrape Endpoint: `GET http://<host>:9545/metrics`
   - แดชบอร์ด Grafana: Import ไฟล์ `deploy/grafana/bangplanix-dashboard.json`
3. **Audit Trail & SIEM:**
   - ส่งออก Security Logs ไปยัง Splunk HEC, Datadog หรือ Elastic ผ่าน `SiemNetworkDispatcher`

---

## 🔄 4. Disaster Recovery & KMS Rotation Procedure

### ขั้นตอนการหมุนเวียน Master Key (KMS Key Rotation Runbook):
1. สร้าง Master Key ใหม่ (เช่น `THABOT_MASTER_KEY_V2`)
2. รันคำสั่ง CLI ทำการ Re-encrypt ข้อมูล Connection Strings และ Saved Passwords:
   ```bash
   bangplanix migrate-keys --old-key "$THABOT_MASTER_KEY_OLD" --new-key "$THABOT_MASTER_KEY_NEW"
   ```
3. อัปเดต Secret ใน Kubernetes Cluster หรือ Vault
4. ทำการ Rolling Restart Pods ในระบบ (Zero-Downtime)
