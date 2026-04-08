#!/usr/bin/env node
import { spawn } from 'node:child_process';
import { platform } from 'node:os';

const browserPaths = {
  win32: 'C:\\Program Files\\BraveSoftware\\Brave-Browser\\Application\\brave.exe',
  linux: '/usr/bin/brave',
  darwin: '/Applications/Brave Browser.app/Contents/MacOS/Brave Browser'
};

const executablePath = browserPaths[platform()]
  ?? '/usr/bin/brave';

const child = spawn('npx', ['chrome-devtools-mcp', '--executablePath', executablePath], {
  stdio: ['pipe', 'pipe', 'pipe']
});

// Forward stdio for MCP protocol
process.stdin.pipe(child.stdin);
child.stdout.pipe(process.stdout);
child.stderr.pipe(process.stderr);