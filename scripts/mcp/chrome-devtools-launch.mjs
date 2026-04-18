#!/usr/bin/env node
/**
 * Cross-platform Chrome DevTools MCP launcher
 * Works on Windows (win32), Linux, and macOS (darwin)
 * 
 * Note: chrome-devtools-mcp only has npm package, no Docker image
 */
import { spawn } from 'node:child_process';
import { platform, arch } from 'node:os';
import { existsSync } from 'node:fs';

// Cross-platform browser paths
const browserPaths = {
  win32: 'C:\\Program Files\\BraveSoftware\\Brave-Browser\\Application\\brave.exe',
  linux: '/usr/bin/brave',
  darwin: '/Applications/Brave Browser.app/Contents/MacOS/Brave Browser'
};

// Get executable path - fall back to brave or chrome
const getExecutablePath = () => {
  const osBrowserPaths = browserPaths[platform()] ?? browserPaths.linux;
  if (existsSync(osBrowserPaths)) {
    return osBrowserPaths;
  }
  // Fallback to Chrome on Linux
  if (platform() === 'linux') {
    const chromePaths = [
      '/usr/bin/google-chrome',
      '/usr/bin/chromium',
      '/usr/bin/chromium-browser'
    ];
    for (const p of chromePaths) {
      if (existsSync(p)) return p;
    }
  }
  return osBrowserPaths;
};

const executablePath = getExecutablePath();
const npmCmd = platform() === 'win32' ? 'npx.cmd' : 'npx';

// Build args for npx
const args = ['-y', 'chrome-devtools-mcp'];
if (executablePath) {
  args.push('--executablePath', executablePath);
}

console.error(`[chrome-devtools] Platform: ${platform()}, Arch: ${arch()}`);
console.error(`[chrome-devtools] Browser: ${executablePath}`);

// Use shell: true on Windows to fix spawn EINVAL
const child = spawn(npmCmd, args, {
  stdio: ['pipe', 'pipe', 'pipe'],
  shell: platform() === 'win32'
});

// Forward MCP protocol stdio
process.stdin.pipe(child.stdin);
child.stdout.pipe(process.stdout);
child.stderr.pipe(process.stderr);

child.on('error', (err) => {
  console.error(`[chrome-devtools] Error: ${err.message}`);
  process.exit(1);
});

child.on('close', (code) => {
  process.exit(code ?? 0);
});