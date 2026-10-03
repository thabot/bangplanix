<?xml version="1.0" encoding="UTF-8"?>
<report name="Oracle General Ledger">
  <dataTemplate name="GL_DATA">
    <dataQuery>
      <sqlStatement name="Q_GL">SELECT ACCT_NO, AMOUNT FROM GL_LINES</sqlStatement>
    </dataQuery>
  </dataTemplate>
  <layout>
    <header height="40">
      <text name="lblTitle">Oracle General Ledger</text>
    </header>
    <repeater source="Q_GL" height="20">
      <field name="fldAcct" source="ACCT_NO" x="10" y="0" width="80" height="18"/>
    </repeater>
  </layout>
</report>
