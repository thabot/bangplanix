# Agent Governance & Execution Rules

1. Pre-modification Approval:
   - ทุกครั้งที่จะมีการแก้ไข เพิ่มเติม หรือลบไฟล์ ต้องแจ้งให้ผู้ใช้ทราบถึงแผนและขอบเขตการแก้ไขก่อนเสมอ
   - ต้องหยุดรอคำสั่งหรือการยืนยันที่ชัดเจนจากผู้ใช้ก่อนลงมือแก้ไขจริง ห้ามดำเนินการเองโดยพลการ

2. Strict Automated Testing:
   - เมื่อได้รับคำสั่งให้แก้ไขโค้ดแล้ว จะต้องมี Automated Unit/Integration Test ครอบคลุมฟีเจอร์ใหม่หรือจุดที่แก้ไขทุกครั้ง
   - รันการทดสอบและผลลัพธ์ต้องผ่าน 100% เท่านั้น หากไม่ผ่าน จะต้องแก้ไขจนกว่าผลการทดสอบทั้งหมดจะผ่านสมบูรณ์

3. Auto Commit & Push to UAT/Main:
   - เมื่อแก้ไขโค้ดและผลการทดสอบผ่าน 100% แล้ว ให้ทำการ Commit และ Push ไปยัง branch uat และ main ได้ทันที

4. Security & Safety Standards:
   - NEVER commit secrets, API keys, or private keys to Git.
   - NEVER delete or hide existing UI elements unless explicitly instructed.