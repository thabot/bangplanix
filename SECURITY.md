# Bangplanix Security Policy & Zero-Trust Standards

Bangplanix takes security seriously. As an enterprise-grade reporting engine, it is engineered with **Zero-Trust Security & 12 Defense Shields** against Remote Code Execution (RCE), Server-Side Request Forgery (SSRF), XML External Entity (XXE), and Data Exfiltration.

## Supported Versions

| Version | Supported |
| ------- | --------- |
| 1.x.x   | :white_check_mark: |
| < 1.0.0 | :x: |

## Reporting a Vulnerability

If you discover a security vulnerability within Bangplanix, please **DO NOT open a public issue**. 
Instead, please report it privately via email or the GitLab/GitHub Private Security Advisory:

- **Security Email:** thabot47@gmail.com (or via Project Security Advisory)
- **Response SLA:** Initial acknowledgment within 24 hours; triage within 48 hours.

## Security Guarantees & Built-in Protections
1. **Dynamic Expression Sandboxing:** Dynamic C# expressions are compiled via Roslyn with strict AST allow-listing. Reflection, File I/O, Network, and System namespaces are banned.
2. **Anti-SSRF Protection:** Remote image/font fetchers validate target IPs and block private/loopback CIDR ranges (RFC 1918 / 127.0.0.0/8 / 169.254.0.0/16 / IPv6 loopback & link-local).
3. **Zero XXE / XML Hardening:** XML parsers operate with `DtdProcessing = Prohibit` and `XmlResolver = null`.
4. **Denial-of-Service (DoS) Limits:** Strict recursion depth limits, canvas dimension caps (max 10000x10000 px), and timeout constraints per report render.
