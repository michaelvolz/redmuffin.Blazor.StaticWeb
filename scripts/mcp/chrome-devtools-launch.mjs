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
  linux: '/usr/bin/brave'
};

// Get executable path - fall back to brave or chrome
const getExecutablePath = () => {
  const currentPlatform = platform();

  if (currentPlatform === 'win32') {
    // Try to get Brave path from Windows registry
    try {
      const { execSync } = require('node:child_process');
      const path = execSync('powershell -Command "Get-ItemProperty \'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\Brave.exe\' -ErrorAction SilentlyContinue | Select-Object -ExpandProperty \'(default)\'"', { encoding: 'utf8' }).trim();
      if (path && existsSync(path)) {
        return path;
      }
    } catch (e) {
      // Fall back to search
    }

    // Search for brave.exe in common locations
    const searchPaths = [
      'C:\\Program Files\\BraveSoftware\\Brave-Browser\\Application\\brave.exe',
      'C:\\Program Files (x86)\\BraveSoftware\\Brave-Browser\\Application\\brave.exe',
      'C:\\Users\\${process.env.USERNAME}\\AppData\\Local\\BraveSoftware\\Brave-Browser\\Application\\brave.exe'
    ];
    for (const p of searchPaths) {
      if (existsSync(p)) {
        return p;
      }
    }
  }

  const osBrowserPaths = browserPaths[currentPlatform] ?? browserPaths.linux;
  if (existsSync(osBrowserPaths)) {
    return osBrowserPaths;
  }
  return osBrowserPaths;
};

const executablePath = getExecutablePath();
const npmCmd = platform() === 'win32' ? 'npx.cmd' : 'npx';

// Build args for npx
const args = ['-y', 'chrome-devtools-mcp'];
if (executablePath) {
  // Quote the path if it contains spaces
  const quotedPath = executablePath.includes(' ') ? `"${executablePath}"` : executablePath;
  args.push('--executablePath', quotedPath);
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