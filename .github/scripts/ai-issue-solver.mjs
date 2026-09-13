#!/usr/bin/env node
/**
 * Bangplanix Autonomous AI Issue Solver Agent
 * Powered by Google Gemini 2.0 Flash & GitHub Actions
 */

import fs from 'node:fs';
import path from 'node:path';
import { execSync } from 'node:child_process';

const GEMINI_API_KEY = process.env.GEMINI_API_KEY;
const GITHUB_TOKEN = process.env.GITHUB_TOKEN;
const GITHUB_REPOSITORY = process.env.GITHUB_REPOSITORY;
const ISSUE_NUMBER = process.env.ISSUE_NUMBER;
const ISSUE_TITLE = process.env.ISSUE_TITLE || '';
const ISSUE_BODY = process.env.ISSUE_BODY || '';

if (!GEMINI_API_KEY) {
  console.error('Error: GEMINI_API_KEY is not set.');
  process.exit(1);
}

if (!GITHUB_TOKEN || !GITHUB_REPOSITORY || !ISSUE_NUMBER) {
  console.error('Error: Missing GitHub context (GITHUB_TOKEN, GITHUB_REPOSITORY, ISSUE_NUMBER).');
  process.exit(1);
}

const ROOT_DIR = process.cwd();

function runCommand(cmd, ignoreError = false) {
  try {
    const stdout = execSync(cmd, { cwd: ROOT_DIR, encoding: 'utf8', stdio: ['pipe', 'pipe', 'pipe'] });
    return { success: true, output: stdout };
  } catch (error) {
    if (!ignoreError) {
      console.warn(`Command failed: ${cmd}\n${error.stdout || ''}\n${error.stderr || ''}`);
    }
    return {
      success: false,
      output: `${error.stdout || ''}\n${error.stderr || ''}`.trim()
    };
  }
}

async function postIssueComment(message) {
  try {
    const url = `https://api.github.com/repos/${GITHUB_REPOSITORY}/issues/${ISSUE_NUMBER}/comments`;
    const res = await fetch(url, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${GITHUB_TOKEN}`,
        Accept: 'application/vnd.github+json',
        'User-Agent': 'bangplanix-ai-agent',
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ body: message })
    });
    if (!res.ok) {
      console.warn(`Failed to post issue comment: ${res.status} ${await res.text()}`);
    }
  } catch (err) {
    console.warn('Error posting issue comment:', err.message);
  }
}

function getCodebaseFileList(dir = '.', currentList = []) {
  const ignoredDirs = new Set(['.git', 'bin', 'obj', 'node_modules', '.vs', '.idea', 'dist', 'output']);
  const files = fs.readdirSync(path.join(ROOT_DIR, dir), { withFileTypes: true });

  for (const file of files) {
    const relPath = path.join(dir, file.name).replace(/\\/g, '/');
    if (file.isDirectory()) {
      if (!ignoredDirs.has(file.name)) {
        getCodebaseFileList(relPath, currentList);
      }
    } else if (file.isFile()) {
      // Include relevant source/config files
      if (/\.(cs|js|mjs|ts|json|md|bpx|proto|props|targets|slnx|csproj)$/i.test(file.name)) {
        currentList.push(relPath);
      }
    }
  }
  return currentList;
}

async function callGemini(prompt, systemInstruction = '') {
  const candidateModels = ['gemini-3.6-flash', 'gemini-flash-latest', 'gemini-3.8-flash'];
  let lastError = null;

  for (const modelName of candidateModels) {
    try {
      const endpoint = `https://generativelanguage.googleapis.com/v1beta/models/${modelName}:generateContent?key=${GEMINI_API_KEY}`;
      const payload = {
        contents: [{ role: 'user', parts: [{ text: prompt }] }],
        generationConfig: {
          temperature: 0.2,
          responseMimeType: 'application/json'
        }
      };

      if (systemInstruction) {
        payload.systemInstruction = {
          parts: [{ text: systemInstruction }]
        };
      }

      const res = await fetch(endpoint, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });

      if (!res.ok) {
        const errText = await res.text();
        console.warn(`Model ${modelName} returned status ${res.status}: ${errText.slice(0, 150)}... Trying fallback if available.`);
        lastError = new Error(`Gemini API error [${res.status}] on ${modelName}: ${errText}`);
        continue; // try next candidate model
      }

      const data = await res.json();
      const text = data.candidates?.[0]?.content?.parts?.[0]?.text;
      if (!text) {
        throw new Error(`Gemini returned empty response from ${modelName}`);
      }

      try {
        return JSON.parse(text);
      } catch (err) {
        const clean = text.replace(/^```json\s*/i, '').replace(/\s*```$/i, '').trim();
        return JSON.parse(clean);
      }
    } catch (err) {
      lastError = err;
      console.warn(`Error using model ${modelName}: ${err.message}. Trying next candidate...`);
    }
  }

  throw lastError || new Error('All candidate Gemini models failed.');
}

async function main() {
  console.log(`🚀 Starting Autonomous AI Issue Solver for Issue #${ISSUE_NUMBER}: ${ISSUE_TITLE}`);
  await postIssueComment(
    `🤖 **Bangplanix AI Agent** ได้รับมอบหมายให้ตรวจสอบและแก้ไข Issue #${ISSUE_NUMBER} แล้ว\n\nกำลังเริ่มขั้นตอนการสแกนโค้ดและวิเคราะห์ปัญหา...`
  );

  const allFiles = getCodebaseFileList();
  console.log(`📁 Scanned ${allFiles.length} files in repository.`);

  // Step 1: Ask Gemini to identify relevant files
  const fileSelectPrompt = `
You are an expert engineer working on Bangplanix (C# 14 / .NET 10, Vanilla JS, Enterprise Reporting Engine).
We have an Issue:
Title: "${ISSUE_TITLE}"
Description:
${ISSUE_BODY}

Here is the list of files in the repository:
${allFiles.join('\n')}

Identify up to 7 most relevant files that need to be read or modified to investigate and solve this issue.
Return a JSON object with schema:
{
  "reasoning": "string",
  "relevantFiles": ["string"]
}
`;

  console.log('🔍 Identifying relevant files via Gemini...');
  const selectResult = await callGemini(fileSelectPrompt);
  const relevantFiles = selectResult.relevantFiles || [];
  console.log('Selected files:', relevantFiles);

  // Read contents of relevant files
  const fileContexts = [];
  for (const relPath of relevantFiles) {
    const fullPath = path.join(ROOT_DIR, relPath);
    if (fs.existsSync(fullPath)) {
      try {
        const content = fs.readFileSync(fullPath, 'utf8');
        // Truncate if file is overly huge
        const safeContent = content.length > 50000 ? content.slice(0, 50000) + '\n...[truncated]' : content;
        fileContexts.push(`--- File: ${relPath} ---\n${safeContent}\n--- End File ---`);
      } catch (e) {
        console.warn(`Could not read ${relPath}:`, e.message);
      }
    }
  }

  // Step 2: Solve and Self-Healing Loop
  let attempts = 0;
  const maxAttempts = 3;
  let testErrorLogs = '';
  let solutionSummary = '';
  let branchName = `fix/issue-${ISSUE_NUMBER}`;

  while (attempts < maxAttempts) {
    attempts++;
    console.log(`⚙️ Solution & Verification Attempt ${attempts}/${maxAttempts}...`);

    const solvePrompt = `
You are fixing an issue in Bangplanix.
Issue #${ISSUE_NUMBER}: "${ISSUE_TITLE}"
Description:
${ISSUE_BODY}

Relevant files and contents:
${fileContexts.join('\n\n')}

${testErrorLogs ? `PREVIOUS ATTEMPT FAILED WITH TEST ERRORS:\n${testErrorLogs}\nPlease analyze and fix the errors.` : ''}

Generate the exact complete code for any files that need changes or new test files to reproduce and verify the fix.
Preserve all existing comments, conventions, and functional parity.
Do NOT remove existing features.

Return a JSON object with this exact schema:
{
  "summary": "Clear explanation in Thai and English of the root cause and how it was fixed",
  "changes": [
    {
      "path": "relative/path/to/file",
      "content": "Full complete content of the file"
    }
  ]
}
`;

    const fixResult = await callGemini(solvePrompt);
    solutionSummary = fixResult.summary || 'Bug fix applied.';

    if (!fixResult.changes || fixResult.changes.length === 0) {
      console.log('Gemini suggested no file changes.');
      break;
    }

    // Apply changes
    for (const change of fixResult.changes) {
      const destPath = path.join(ROOT_DIR, change.path);
      fs.mkdirSync(path.dirname(destPath), { recursive: true });
      fs.writeFileSync(destPath, change.content, 'utf8');
      console.log(`✏️ Updated: ${change.path}`);
    }

    // Step 3: Verify with dotnet test
    console.log('🧪 Running automated tests: dotnet test...');
    const testRes = runCommand('dotnet test --nologo -v q');

    if (testRes.success) {
      console.log('✅ Automated tests passed successfully!');
      testErrorLogs = '';
      break;
    } else {
      console.warn(`❌ Tests failed on attempt ${attempts}. Capturing logs for retry...`);
      testErrorLogs = testRes.output.slice(-3000); // last 3000 chars of error
    }
  }

  if (testErrorLogs) {
    console.error('❌ Could not completely resolve tests within max attempts.');
    await postIssueComment(
      `⚠️ **Bangplanix AI Agent** ได้ทดลองแก้ไขปัญหาแล้ว แต่ชุดทดสอบ (\`dotnet test\`) ยังไม่ผ่าน 100%:\n\n\`\`\`\n${testErrorLogs.slice(0, 1500)}\n\`\`\`\n\nระบบจึงระงับการเปิด Pull Request เพื่อความปลอดภัย กรุณาตรวจสอบเพิ่มเติมครับ`
    );
    process.exit(1);
  }

  // Step 4: Create branch, commit, push, and open PR
  console.log('📦 Creating Git branch and Pull Request...');
  runCommand('git config user.name "bangplanix-ai-agent[bot]"');
  runCommand('git config user.email "ai-agent@bangplanix.io"');
  runCommand(`git checkout -B ${branchName}`);
  runCommand('git add .');

  const statusCheck = runCommand('git status --porcelain');
  if (!statusCheck.output.trim()) {
    console.log('No git changes detected.');
    await postIssueComment(`ℹ️ **Bangplanix AI Agent** ตรวจสอบแล้ว ไม่พบการแก้ไขไฟล์ที่จำเป็นสำหรับ Issue นี้ครับ`);
    return;
  }

  const commitMsg = `fix(issue-${ISSUE_NUMBER}): ${ISSUE_TITLE.replace(/"/g, '\\"')}`;
  runCommand(`git commit -m "${commitMsg}"`);
  runCommand(`git push origin ${branchName} --force`);

  // Create Pull Request via GitHub CLI (gh)
  const prBody = `
## 🤖 Autonomous AI Fix for Issue #${ISSUE_NUMBER}

### 📝 สรุปการแก้ไข (Summary)
${solutionSummary}

---
- **Linked Issue:** #${ISSUE_NUMBER}
- **Automated Verification:** \`dotnet test\` PASSED (100%)
- **Reviewer:** Please inspect diff and approve to merge.
`.trim();

  const prRes = runCommand(
    `gh pr create --title "fix: ${ISSUE_TITLE.replace(/"/g, '\\"')} (Issue #${ISSUE_NUMBER})" --body "${prBody.replace(/"/g, '\\"')}" --head ${branchName} --base main`,
    true
  );

  console.log('PR Output:', prRes.output);

  await postIssueComment(
    `✅ **Bangplanix AI Agent** ได้ดำเนินการแก้ไขปัญหาและทดสอบผ่าน 100% เรียบร้อยแล้ว!\n\n${solutionSummary}\n\n🔗 **Pull Request:** ดูรายการเปลี่ยนแปลงและกดอนุมัติได้ที่ PR ที่เปิดขึ้นล่าสุดครับ`
  );

  console.log('🎉 AI Issue Solver finished successfully!');
}

main().catch(async (err) => {
  console.error('Fatal error in AI Issue Solver:', err);
  await postIssueComment(`❌ **Bangplanix AI Agent** เกิดข้อผิดพลาดในการประมวลผล: \`${err.message}\``);
  process.exit(1);
});
