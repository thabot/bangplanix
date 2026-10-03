<?xml version="1.0" encoding="UTF-8"?>
<report xmlns="http://developer.cognos.com/schemas/report/14.0/" name="CognosFinancialSummary">
  <queries>
    <query name="MainQuery">
      <source>
        <sqlQuery name="SQL1">
          <sqlText>SELECT AccountCode, Balance FROM GeneralLedger</sqlText>
        </sqlQuery>
      </source>
      <selection>
        <dataItem name="AccountCode">
          <expression>[AccountCode]</expression>
        </dataItem>
        <dataItem name="Balance">
          <expression>[Balance]</expression>
        </dataItem>
      </selection>
    </query>
  </queries>
  <layouts>
    <layout>
      <reportPages>
        <page name="Page1">
          <pageBody>
            <contents>
              <list name="List1" refQuery="MainQuery">
                <listColumns>
                  <listColumn name="col1" refDataItem="AccountCode"/>
                  <listColumn name="col2" refDataItem="Balance"/>
                </listColumns>
              </list>
            </contents>
          </pageBody>
        </page>
      </reportPages>
    </layout>
  </layouts>
</report>
