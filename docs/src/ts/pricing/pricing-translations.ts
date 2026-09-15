/**
 * Multi-Language Translations for Bangplanix Pricing Page
 * Supports 5 languages matching the Getting Started Guides:
 * 🇬🇧 English (en), 🇹🇭 ไทย (th), 🇨🇳 简体中文 (zh), 🇯🇵 日本語 (ja), 🇪🇸 Español (es)
 */

export interface PricingTranslation {
  heroTitle: string;
  heroSubtitle: string;
  monthlyBilling: string;
  annualBilling: string;
  saveBadge: string;
  perYear: string;
  perMonth: string;
  forever: string;
  freeRevText: string;
  popularBadge: string;
  buyBtnLabel: string;
  contactSales: string;
  downloadGithub: string;
  globalNoticeTitle: string;
  globalNoticeDesc: string;
  requestQuoteBtn: string;
  matrixTitle: string;
  matrixSubtitle: string;
  colFeature: string;
  colPricing: string;
  colTarget: string;
  colCores: string;
  colWatermark: string;
  colDesigner: string;
  colSdks: string;
  colAot: string;
  colBursting: string;
  colSignatures: string;
  colRedaction: string;
  colOem: string;
  colSupport: string;
  faqTitle: string;
}

export const PRICING_TRANSLATIONS: Record<string, PricingTranslation> = {
  en: {
    heroTitle: 'Head-to-Head Enterprise Value & Commercial Licensing',
    heroSubtitle: 'Sub-millisecond Native AOT document generation. Exact same pricing as QuestPDF with 10x more features: Web Visual Designer, Polyglot SDKs, and Built-in AI Suite.',
    monthlyBilling: 'Monthly Billing',
    annualBilling: 'Annual Billing',
    saveBadge: 'SAVE ~15%',
    perYear: '/ year',
    perMonth: '/ month',
    forever: '/ forever',
    freeRevText: 'Free Forever (< $1M Rev)',
    popularBadge: 'MOST POPULAR',
    buyBtnLabel: 'Buy',
    contactSales: '🏢 Contact Enterprise Sales',
    downloadGithub: 'Download on GitHub',
    globalNoticeTitle: '🏢 Global & Regional Corporate Invoicing (US, EU, UK, TH, APAC)',
    globalNoticeDesc: 'Lemon Squeezy acts as our Global Merchant of Record (MoR), supporting automatic sales tax/VAT compliance across 130+ countries, corporate purchase orders (PO), W-8BEN forms, and international wire transfers. For Thai entities requiring formal Tax Invoices (ภ.พ.20) or 3% Withholding Tax certificates (ภ.ง.ด. 53), our local entity fulfills inquiries directly.',
    requestQuoteBtn: '📄 Request Corporate PO / Quote',
    matrixTitle: 'Complete 4-Tier Feature Matrix',
    matrixSubtitle: 'Detailed feature breakdown and entitlements across all Bangplanix commercial licensing tiers',
    colFeature: 'Entitlement / Feature',
    colPricing: 'Pricing (Annual / Monthly)',
    colTarget: 'Target Audience',
    colCores: 'CPU Core Quota',
    colWatermark: 'PDF Watermark',
    colDesigner: 'Web Visual Designer',
    colSdks: 'Polyglot SDKs (5 Languages)',
    colAot: 'Native AOT Cold Start',
    colBursting: 'Report Bursting & Cloud Delivery',
    colSignatures: 'e-Tax & Digital Signatures (PAdES)',
    colRedaction: 'True Vector Redaction',
    colOem: 'Commercial Redistribution (OEM)',
    colSupport: 'Technical Support SLA',
    faqTitle: 'Frequently Asked Questions'
  },
  th: {
    heroTitle: 'ความคุ้มค่าระดับองค์กรชน QuestPDF พร้อมสิทธิ์เชิงพาณิชย์',
    heroSubtitle: 'สร้างเอกสารความเร็วสูงระดับ Sub-millisecond ด้วย Native AOT ราคาเท่ากับ QuestPDF ทุกระดับราคา แต่ได้ฟีเจอร์มากกว่า 10 เท่า ทั้ง Web Visual Designer, SDK 5 ภาษา และ AI Suite ในตัว',
    monthlyBilling: 'ชำระรายเดือน',
    annualBilling: 'ชำระรายปี',
    saveBadge: 'ประหยัด ~15%',
    perYear: '/ ปี',
    perMonth: '/ เดือน',
    forever: '/ ตลอดชีพ',
    freeRevText: 'ฟรีตลอดชีพ (รายได้ < 35 ล้านบาท)',
    popularBadge: 'ยอดนิยมสูงสุด',
    buyBtnLabel: 'สั่งซื้อแพ็กเกจ',
    contactSales: '🏢 ติดต่อฝ่ายขายองค์กร',
    downloadGithub: 'ดาวน์โหลดบน GitHub',
    globalNoticeTitle: '🏢 การออกใบกำกับภาษีสำหรับนิติบุคคลไทยและทั่วโลก (US, EU, UK, TH)',
    globalNoticeDesc: 'Lemon Squeezy ทำหน้าที่เป็นตัวแทนจำหน่ายสากล (Merchant of Record) รองรับการคำนวณภาษีมูลค่าเพิ่มใน 130+ ประเทศ สำหรับนิติบุคคลในประเทศไทยที่ต้องการใบเสนอราคา, ใบกำกับภาษีเต็มรูป (ภ.พ.20) และเอกสารหักภาษี ณ ที่จ่าย 3% (ภ.ง.ด. 53) สามารถติดต่อทีมงานในไทยเพื่อสั่งซื้อได้โดยตรง',
    requestQuoteBtn: '📄 ขอใบเสนอราคา / ใบกำกับภาษีไทย',
    matrixTitle: 'ตารางเปรียบเทียบคุณสมบัติทั้ง 4 Tiers อย่างละเอียด',
    matrixSubtitle: 'รายละเอียดสิทธิ์การใช้งานและฟังก์ชันครบถ้วนในแต่ละระดับลิขสิทธิ์ของ Bangplanix',
    colFeature: 'ฟังก์ชัน / สิทธิ์การใช้งาน',
    colPricing: 'ราคา (รายปี / รายเดือน)',
    colTarget: 'กลุ่มผู้ใช้งานที่เหมาะสม',
    colCores: 'โควตา CPU Cores',
    colWatermark: 'ลายน้ำบน PDF',
    colDesigner: 'Web Visual Designer (ลากวาง)',
    colSdks: 'SDKs รองรับ 5 ภาษา',
    colAot: 'ความเร็ว Native AOT Cold Start',
    colBursting: 'ระบบตัดยอดบิลอัตโนมัติ (Bursting)',
    colSignatures: 'e-Tax สรรพากร & ลายเซ็น PAdES',
    colRedaction: 'ถมดำลบเวกเตอร์ถาวร (Redaction)',
    colOem: 'สิทธิ์ฝังในซอฟต์แวร์ขายต่อ (OEM)',
    colSupport: 'ระดับการบริการซัพพอร์ต (SLA)',
    faqTitle: 'คำถามที่พบบ่อย (FAQ)'
  },
  zh: {
    heroTitle: '企业级性价比：对标 QuestPDF 的商业许可',
    heroSubtitle: '基于 Native AOT 的亚毫秒级文档生成。价格与 QuestPDF 完全一致，功能多达 10 倍：内置 Web 可视化设计器、5 种多语言 SDK 及 AI 套件。',
    monthlyBilling: '按月计费',
    annualBilling: '按年计费',
    saveBadge: '立省 ~15%',
    perYear: '/ 年',
    perMonth: '/ 月',
    forever: '/ 终身免费',
    freeRevText: '终身免费（年营收 < 100 万美元）',
    popularBadge: '最受欢迎',
    buyBtnLabel: '购买',
    contactSales: '🏢 联系企业销售',
    downloadGithub: '在 GitHub 上下载',
    globalNoticeTitle: '🏢 全球及区域企业开票支持（欧美、亚太及跨境）',
    globalNoticeDesc: 'Lemon Squeezy 作为全球名义商户（MoR），支持 130 多个国家/地区的增值税合规、企业采购订单 (PO)、W-8BEN 表格及国际电汇。支持各国对公账户采购开票。',
    requestQuoteBtn: '📄 申请企业报价单 / 发票',
    matrixTitle: '完整的 4 级商业许可功能对比矩阵',
    matrixSubtitle: '全面比较 Bangplanix 各许可级别的权限、核心配额与技术支持服务',
    colFeature: '功能与权限',
    colPricing: '价格（年付 / 月付）',
    colTarget: '适用对象',
    colCores: 'CPU 核心配额',
    colWatermark: 'PDF 水印',
    colDesigner: 'Web 可视化设计器',
    colSdks: '多语言 SDK（5 种语言）',
    colAot: 'Native AOT 冷启动速度',
    colBursting: '多渠道批量分发 (Bursting)',
    colSignatures: '电子发票及数字签名 (PAdES)',
    colRedaction: '永久矢量脱敏 (Redaction)',
    colOem: '商业再分发许可 (OEM)',
    colSupport: '技术支持 SLA',
    faqTitle: '常见问题解答 (FAQ)'
  },
  ja: {
    heroTitle: 'QuestPDF に匹敵する企業向けバリュー＆商用ライセンス',
    heroSubtitle: 'Native AOT によるミリ秒未満の高速ドキュメント生成。QuestPDF と同価格でありながら、Web ビジュアルデザイナー、5 言語 SDK、AI Suite 搭載で 10 倍の機能を誇ります。',
    monthlyBilling: '月払い',
    annualBilling: '年払い',
    saveBadge: '約15%お得',
    perYear: '/ 年',
    perMonth: '/ 月',
    forever: '/ 永久無料',
    freeRevText: '永久無料（年間売上高 100 万ドル未満）',
    popularBadge: '一番人気',
    buyBtnLabel: '購入する',
    contactSales: '🏢 法人営業へ問い合わせ',
    downloadGithub: 'GitHub で見る',
    globalNoticeTitle: '🏢 グローバル法人請求書発行対応（日本、米国、EU、アジア）',
    globalNoticeDesc: 'Lemon Squeezy がグローバル Merchant of Record (MoR) として機能し、世界 130 か国以上での消費税/VAT コンプライアンス、法人購買注文 (PO)、W-8BEN に対応しています。日本法人の請求書払いにも対応可能です。',
    requestQuoteBtn: '📄 お見積書・請求書の発行依頼',
    matrixTitle: '4 つのライセンス機能比較マトリクス',
    matrixSubtitle: 'Bangplanix のすべての商用ライセンス階層における詳細な機能と権限',
    colFeature: '機能・権限',
    colPricing: '価格（年払い / 月払い）',
    colTarget: '対象ユーザー',
    colCores: 'CPU コア制限',
    colWatermark: 'PDF 透かし (ウォーターマーク)',
    colDesigner: 'Web ビジュアルデザイナー',
    colSdks: 'マルチ言語 SDK (5 言語)',
    colAot: 'Native AOT コールドスタート',
    colBursting: '帳票バッチ自動配信 (Bursting)',
    colSignatures: '電子インボイス＆デジタル署名',
    colRedaction: '完全ベクター墨消し (Redaction)',
    colOem: '商用再配布権 (OEM)',
    colSupport: 'サポート SLA',
    faqTitle: 'よくある質問 (FAQ)'
  },
  es: {
    heroTitle: 'Valor Empresarial Competitivo y Licencias Comerciales',
    heroSubtitle: 'Generación de documentos sub-milisegundo con Native AOT. Exactamente el mismo precio que QuestPDF con 10 veces más funciones: Diseñador Web Visual, SDKs en 5 lenguajes y Suite de IA integrada.',
    monthlyBilling: 'Facturación Mensual',
    annualBilling: 'Facturación Anual',
    saveBadge: 'AHORRE ~15%',
    perYear: '/ año',
    perMonth: '/ mes',
    forever: '/ siempre',
    freeRevText: 'Gratis Siempre (< $1M Ingresos)',
    popularBadge: 'MÁS POPULAR',
    buyBtnLabel: 'Comprar',
    contactSales: '🏢 Contactar Ventas Corporativas',
    downloadGithub: 'Descargar en GitHub',
    globalNoticeTitle: '🏢 Facturación Corporativa Global (EE. UU., UE, LatAm, España)',
    globalNoticeDesc: 'Lemon Squeezy actúa como nuestro comerciante de registro global (MoR), gestionando impuestos de ventas, IVA de la UE, formularios W-8BEN y órdenes de compra corporativas (PO) en más de 130 países.',
    requestQuoteBtn: '📄 Solicitar Cotización / Factura PO',
    matrixTitle: 'Matriz Completa de Características en 4 Niveles',
    matrixSubtitle: 'Desglose detallado de características y derechos en todos los niveles de licencias comerciales de Bangplanix',
    colFeature: 'Característica / Capacidad',
    colPricing: 'Precio (Anual / Mensual)',
    colTarget: 'Público Objetivo',
    colCores: 'Cuota de Núcleos CPU',
    colWatermark: 'Marca de Agua PDF',
    colDesigner: 'Diseñador Web Visual',
    colSdks: 'SDKs en 5 Lenguajes',
    colAot: 'Arranque en Frío Native AOT',
    colBursting: 'Distribución Masiva de Reportes',
    colSignatures: 'Factura Electrónica y Firmas (PAdES)',
    colRedaction: 'Censura Vectorial Verdadera',
    colOem: 'Redistribución Comercial (OEM)',
    colSupport: 'SLA de Soporte Técnico',
    faqTitle: 'Preguntas Frecuentes (FAQ)'
  }
};
