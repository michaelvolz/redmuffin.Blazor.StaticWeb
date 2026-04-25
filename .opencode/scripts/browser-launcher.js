#!/usr/bin/env node
/**
 * Cross-platform browser launcher for chrome-devtools-mcp
 * Detects OS and finds the correct browser without hardcoded paths.
 *
 * OS -> Browser mapping:
 *   Windows 11 (native) -> Brave    (via registry)
 *   WSL                 -> Chromium (via which / common paths)
 *   Linux               -> Brave    (via which / common paths)
 */

const { spawn, execSync } = require('child_process');
const fs = require('fs');
const os = require('os');
const path = require('path');

/* ------------------------------------------------------------------ */
/*  OS detection                                                       */
/* ------------------------------------------------------------------ */

function isWSL() {
  if (process.platform !== 'linux') return false;

  if (process.env.WSL_DISTRO_NAME || process.env.WSLENV) return true;

  try {
    const version = fs.readFileSync('/proc/version', 'utf8').toLowerCase();
    if (version.includes('microsoft') || version.includes('wsl')) return true;
  } catch {}

  try {
    fs.accessSync('/proc/sys/fs/binfmt_misc/WSLInterop');
    return true;
  } catch {}

  return false;
}

/* ------------------------------------------------------------------ */
/*  Windows registry helpers                                           */
/* ------------------------------------------------------------------ */

function parseRegQuery(output) {
  const lines = output.split(/\r?\n/);
  for (const line of lines) {
    const match = line.match(/\(Default\)\s+REG_SZ\s+(.+)/i);
    if (match) {
      let value = match[1].trim();
      // strip surrounding quotes
      if (value.startsWith('"') && value.endsWith('"')) {
        value = value.slice(1, -1);
      }
      return value;
    }
  }
  return null;
}

function findWindowsBrave() {
  const keys = [
    'HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\brave.exe',
    'HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\App Paths\\brave.exe',
    'HKLM\\SOFTWARE\\BraveSoftware\\Brave-Browser\\shell\\open\\command',
  ];

  for (const key of keys) {
    try {
      const output = execSync(`reg query "${key}" /ve`, {
        encoding: 'utf8',
        windowsHide: true,
        timeout: 5000,
      });
      let value = parseRegQuery(output);
      if (!value) continue;

      // shell\open\command returns a full command line: "path" --args %1
      if (key.includes('shell\\open\\command')) {
        const cmdMatch = value.match(/^"([^"]+)"/);
        if (cmdMatch) value = cmdMatch[1];
      }

      if (fs.existsSync(value)) return value;
    } catch {}
  }

  // Fallback: PATH lookup via where.exe
  try {
    const whereOut = execSync('where brave', {
      encoding: 'utf8',
      windowsHide: true,
      timeout: 5000,
    })
      .split(/\r?\n/)[0]
      .trim();
    if (fs.existsSync(whereOut)) return whereOut;
  } catch {}

  // Fallback: common installation directories
  const candidates = [
    path.join(process.env.LOCALAPPDATA || '', 'BraveSoftware', 'Brave-Browser', 'Application', 'brave.exe'),
    path.join(process.env.PROGRAMFILES || 'C:\\Program Files', 'BraveSoftware', 'Brave-Browser', 'Application', 'brave.exe'),
    path.join(process.env['PROGRAMFILES(X86)'] || 'C:\\Program Files (x86)', 'BraveSoftware', 'Brave-Browser', 'Application', 'brave.exe'),
  ];

  for (const c of candidates) {
    if (fs.existsSync(c)) return c;
  }

  return null;
}

/* ------------------------------------------------------------------ */
/*  Linux / WSL browser discovery                                      */
/* ------------------------------------------------------------------ */

function findByWhich(names) {
  for (const name of names) {
    try {
      const out = execSync(`which ${name}`, { encoding: 'utf8', timeout: 5000 }).trim();
      if (fs.existsSync(out)) return out;
    } catch {}
  }
  return null;
}

function findLinuxBrave() {
  const path = findByWhich(['brave', 'brave-browser', 'brave-browser-stable']);
  if (path) return path;

  const candidates = [
    '/usr/bin/brave',
    '/usr/bin/brave-browser',
    '/usr/bin/brave-browser-stable',
    '/usr/local/bin/brave',
    '/snap/bin/brave',
  ];
  for (const c of candidates) {
    if (fs.existsSync(c)) return c;
  }
  return null;
}

function findWSLChromium() {
  const path = findByWhich(['chromium', 'chromium-browser', 'google-chrome-stable', 'google-chrome', 'chrome']);
  if (path) return path;

  const candidates = [
    '/usr/bin/chromium',
    '/usr/bin/chromium-browser',
    '/usr/bin/google-chrome-stable',
    '/usr/bin/google-chrome',
    '/usr/bin/chrome',
  ];
  for (const c of candidates) {
    if (fs.existsSync(c)) return c;
  }
  return null;
}

/* ------------------------------------------------------------------ */
/*  Resolve npx (robust cross-platform)                                */
/* ------------------------------------------------------------------ */

function resolveNpx() {
  // 1. Try PATH lookup
  try {
    const cmd = process.platform === 'win32' ? 'where npx' : 'which npx';
    const found = execSync(cmd, { encoding: 'utf8', timeout: 5000, windowsHide: true }).trim().split(/\r?\n/)[0];
    if (found && fs.existsSync(found)) return found;
  } catch {}

  // 2. Platform-specific fallbacks
  if (process.platform === 'win32') {
    const candidates = [
      path.join(process.env.APPDATA || '', 'npm', 'npx.cmd'),
      path.join(process.env.LOCALAPPDATA || '', 'npm', 'npx.cmd'),
      path.join(process.env.PROGRAMFILES || 'C:\\Program Files', 'nodejs', 'npx.cmd'),
      path.join(process.env['PROGRAMFILES(X86)'] || 'C:\\Program Files (x86)', 'nodejs', 'npx.cmd'),
      'C:\\Program Files\\nodejs\\npx.cmd',
      'C:\\Program Files (x86)\\nodejs\\npx.cmd',
    ];
    for (const c of candidates) {
      if (fs.existsSync(c)) return c;
    }
  } else {
    const candidates = [
      '/usr/bin/npx',
      '/usr/local/bin/npx',
      '/opt/homebrew/bin/npx',
      path.join(os.homedir(), '.local', 'share', 'pnpm', 'npx'),
      path.join(os.homedir(), '.npm-global', 'bin', 'npx'),
    ];
    for (const c of candidates) {
      if (fs.existsSync(c)) return c;
    }
  }

  // 3. Last resort — hope the shell finds it
  console.error('[browser-launcher] WARN: npx not found in PATH or common locations, falling back to bare "npx"');
  return 'npx';
}

/* ------------------------------------------------------------------ */
/*  Main                                                               */
/* ------------------------------------------------------------------ */

function main() {
  const npxPath = resolveNpx();
  const wsl = isWSL();
  let browserPath = null;
  let browserLabel = '';

  if (wsl) {
    browserLabel = 'Chromium (WSL)';
    browserPath = findWSLChromium();
  } else if (process.platform === 'win32') {
    browserLabel = 'Brave (Windows)';
    browserPath = findWindowsBrave();
  } else if (process.platform === 'linux') {
    browserLabel = 'Brave (Linux)';
    browserPath = findLinuxBrave();
  } else if (process.platform === 'darwin') {
    browserLabel = 'Brave (macOS)';
    // best-effort macOS support
    try {
      const appPath = execSync(
        'mdfind "kMDItemCFBundleIdentifier == \'com.brave.Browser\'"',
        { encoding: 'utf8', timeout: 5000 }
      ).trim();
      if (appPath) {
        browserPath = path.join(appPath, 'Contents', 'MacOS', 'Brave Browser');
      }
    } catch {}
    if (!browserPath || !fs.existsSync(browserPath)) {
      const candidates = [
        '/Applications/Brave Browser.app/Contents/MacOS/Brave Browser',
        path.join(os.homedir(), 'Applications', 'Brave Browser.app', 'Contents', 'MacOS', 'Brave Browser'),
      ];
      for (const c of candidates) {
        if (fs.existsSync(c)) { browserPath = c; break; }
      }
    }
  }

  if (!browserPath) {
    console.error(`ERROR: Could not locate browser for ${browserLabel || 'this platform'}.`);
    console.error(`Platform: ${process.platform}${wsl ? ' (WSL)' : ''}`);
    process.exit(1);
  }

  if (!fs.existsSync(browserPath)) {
    console.error(`ERROR: Browser path does not exist: ${browserPath}`);
    process.exit(1);
  }

  console.error(`[browser-launcher] ${browserLabel} -> ${browserPath}`);

  // Build chrome-devtools-mcp arguments
  const args = [
    '-y',
    'chrome-devtools-mcp@latest',
    '--isolated',
    `--executable-path=${browserPath}`,
    '--chrome-arg=--incognito',
    '--chrome-arg=--no-first-run',
    '--chrome-arg=--no-default-browser-check',
  ];

  // Forward any extra CLI arguments to the MCP server
  if (process.argv.length > 2) {
    args.push(...process.argv.slice(2));
  }

  // On Windows both npxPath and browserPath may contain spaces.
  // Array-based spawn with shell:true does not quote the executable path,
  // so build a single properly-quoted command string instead.
  let child;
  if (process.platform === 'win32') {
    const quote = (s) => (s.includes(' ') ? `"${s}"` : s);
    const cmd = `${quote(npxPath)} ${args.map(quote).join(' ')}`;
    child = spawn(cmd, {
      stdio: 'inherit',
      shell: true,
      windowsHide: false,
    });
  } else {
    child = spawn(npxPath, args, {
      stdio: 'inherit',
      shell: false,
      windowsHide: false,
    });
  }

  child.on('error', (err) => {
    console.error(`ERROR: Failed to spawn MCP server: ${err.message}`);
    process.exit(1);
  });

  child.on('exit', (code, signal) => {
    if (signal) {
      process.kill(process.pid, signal);
    } else {
      process.exit(code ?? 0);
    }
  });
}

module.exports = { main };
main();
