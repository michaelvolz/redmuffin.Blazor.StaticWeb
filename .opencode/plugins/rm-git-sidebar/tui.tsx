/** @jsxImportSource @opentui/solid */
import type { TuiPlugin, TuiPluginApi, TuiPluginModule } from "@opencode-ai/plugin/tui";
import type { RGBA } from "@opentui/core";
import { createSignal } from "solid-js";
import { execSync, execFileSync } from "node:child_process";
import { homedir } from "node:os";
import { isAbsolute, resolve as pathResolve } from "node:path";
import {
  CATEGORIES,
  EMPTY_COUNTS,
  categorySome,
  categoryTotal,
  computeSessionCounts,
  parseGitCounts,
  parseAheadBehind,
  type AheadBehind,
  type GitCounts,
  type ThemeToken,
} from "./git";

const id = "rm-git-sidebar";

interface GitState {
  dir: string | null;
  error: boolean;
  counts: GitCounts;
  sessionCounts: GitCounts;
  total: number;
  aheadBehind: AheadBehind | null;
}

const DB_PATH = pathResolve(homedir(), ".local/share/opencode/opencode.db");

// --- Session file tracking ---

// sessionId format validated against known OpenCode prefix
const SESSION_ID_RE = /^ses_[a-zA-Z0-9]{16,}$/;

// Type for session.diff event — documents framework contract, forward-compatible
interface SessionDiffEvent {
  properties?: {
    sessionID?: string;
    diff?: Array<{ file: string; [key: string]: unknown }>;
  };
}

let sessionFiles = new Set<string>();
let storedSessionId: string | null = null;
let sessionFilesSeeded = false;
let storedApi: TuiPluginApi | null = null;

function normalizePath(rawPath: string): string {
  if (isAbsolute(rawPath)) return rawPath;
  const dir = storedApi?.state?.path?.directory ?? process.cwd();
  return pathResolve(dir, rawPath);
}

function seedSessionFiles(sessionId: string) {
  // Guard: only seed once per session, skip stale or empty
  if (sessionFilesSeeded) return;
  if (!SESSION_ID_RE.test(sessionId)) return;
  if (sessionId !== storedSessionId) return;

  try {
    const writeOutput = execSync(
      `sqlite3 "${DB_PATH}" "SELECT DISTINCT json_extract(data, '$.state.input.filePath') FROM part WHERE session_id = '${sessionId}' AND json_extract(data, '$.type') = 'tool' AND json_extract(data, '$.tool') IN ('write', 'edit') AND json_extract(data, '$.state.input.filePath') IS NOT NULL;"`,
      { encoding: "utf8", timeout: 5000 },
    );
    const patchOutput = execSync(
      `sqlite3 "${DB_PATH}" "SELECT DISTINCT json_extract(data, '$.files') FROM part WHERE session_id = '${sessionId}' AND json_extract(data, '$.type') = 'patch';"`,
      { encoding: "utf8", timeout: 5000 },
    );
    const paths = writeOutput.trim().split("\n").filter(Boolean);
    for (const p of paths) {
      sessionFiles.add(normalizePath(p));
    }
    const patchLines = patchOutput.trim().split("\n").filter(Boolean);
    for (const line of patchLines) {
      try {
        const files: Array<string> = JSON.parse(line);
        for (const f of files) {
          sessionFiles.add(normalizePath(f));
        }
      } catch { /* invalid JSON, skip */ }
    }
    sessionFilesSeeded = true;
  } catch (err) {
    storedApi?.app.log({
      body: {
        service: id,
        level: "error",
        message: "seedSessionFiles failed",
        extra: { error: err instanceof Error ? err.message : String(err) },
      },
    });
  }
}

// --- Module-level state ---

const [gitState, setGitState] = createSignal<GitState>({
  dir: null,
  error: false,
  counts: { ...EMPTY_COUNTS },
  sessionCounts: { ...EMPTY_COUNTS },
  total: 0,
  aheadBehind: null,
});

const ERROR_STATE: GitState = {
  dir: null,
  error: true,
  counts: { ...EMPTY_COUNTS },
  sessionCounts: { ...EMPTY_COUNTS },
  total: 0,
  aheadBehind: null,
};

let interval: ReturnType<typeof setInterval> | null = null;
let lastRefresh = 0;

function pollGitStatus() {
  lastRefresh = Date.now();
  const sid = storedSessionId;
  try {
    const dir = storedApi?.state?.path?.directory ?? null;
    if (!dir) {
      setGitState(ERROR_STATE);
      return;
    }
    const output = execFileSync("git", ["-C", dir, "status", "--porcelain=v1", "--branch"], {
      encoding: "utf8",
      timeout: 5000,
    });
    const counts = parseGitCounts(output);
    const aheadBehind = parseAheadBehind(output);
    // Only use sessionFiles if the session hasn't changed since capture.
    // If it has, compute against an empty set — session counts are stale.
    const files = (storedSessionId === sid) ? sessionFiles : new Set<string>();
    const sessionCounts = computeSessionCounts(output, dir, files);
    setGitState({ dir, error: false, counts, sessionCounts, total: categoryTotal(counts), aheadBehind });
  } catch {
    setGitState({ dir: null, error: true, counts: { ...EMPTY_COUNTS }, sessionCounts: { ...EMPTY_COUNTS }, total: 0, aheadBehind: null });
  }
}

// --- Theme helpers ---

function resolveToken(token: ThemeToken, theme: { success: RGBA; warning: RGBA; error: RGBA; textMuted: RGBA }): RGBA {
  return theme[token];
}

// --- Plugin ---

const tui: TuiPlugin = async (api) => {
  storedApi = api;
  pollGitStatus();
  interval = setInterval(pollGitStatus, 10000);

  // Event-driven refresh: fires when agent changes files
  api.event.on("session.diff", (event: SessionDiffEvent) => {
    // Guard: only process events for the currently displayed session
    if (event?.properties?.sessionID === storedSessionId) {
      if (event?.properties?.diff) {
        for (const d of event.properties.diff) {
          if (d.file) sessionFiles.add(normalizePath(d.file));
        }
      }
      // Debounce: skip if refreshed within last 2s
      if (Date.now() - lastRefresh > 2000) pollGitStatus();
    }
  });

  api.slots.register({
    order: 350,
    slots: {
      sidebar_content(_ctx: unknown, _value: { session_id: string }) {
        // Session tracking init
        if (_value.session_id !== storedSessionId) {
          storedSessionId = _value.session_id;
          sessionFiles = new Set<string>();
          sessionFilesSeeded = false;
        }
        if (!sessionFilesSeeded && storedSessionId) {
          const sid = storedSessionId;
          setTimeout(() => seedSessionFiles(sid), 0);
        }

        const s = gitState();
        const theme = api.theme.current;

        if (s.error) {
          return <text fg={theme.error}>git ?</text>;
        }

        const ab = s.aheadBehind;
        const hasAheadBehind = ab !== null && (ab.ahead > 0 || ab.behind > 0);

        // --- Clean working tree ---

        // Truly clean: no dirty files, in sync with remote
        if (s.total === 0 && !hasAheadBehind) {
          return <text fg={theme.success}>git ✓</text>;
        }

        // Clean working tree but ahead/behind
        if (s.total === 0 && hasAheadBehind) {
          const arrowFg = ab!.ahead > 0 && ab!.behind > 0 ? theme.error
            : ab!.ahead > 0 ? theme.success : theme.warning;
          return (
            <box paddingLeft={0} flexDirection="row">
              <text fg={theme.success}>git ✓</text>
              {ab!.ahead > 0 && <text fg={arrowFg}> ↑{ab!.ahead}</text>}
              {ab!.behind > 0 && <text fg={arrowFg}> ↓{ab!.behind}</text>}
            </box>
          );
        }

        // --- Dirty working tree — full badge ---

        interface Segment {
          label: string;
          fg: RGBA;
        }
        const allSegments: Array<Segment> = [];
        const sessionSegments: Array<Segment> = [];

        // Prefix
        allSegments.push({ label: "git", fg: theme.textMuted });

        // Dirty file counts
        for (const cat of CATEGORIES) {
          const n = s.counts[cat.key];
          if (n > 0) allSegments.push({ label: `${cat.label}${n}`, fg: resolveToken(cat.token, theme) });
        }

        // Ahead/behind — appended after dirty counts
        if (hasAheadBehind) {
          const arrowFg = ab!.ahead > 0 && ab!.behind > 0 ? theme.error
            : ab!.ahead > 0 ? theme.success : theme.warning;
          if (ab!.ahead > 0) allSegments.push({ label: `↑${ab!.ahead}`, fg: arrowFg });
          if (ab!.behind > 0) allSegments.push({ label: `↓${ab!.behind}`, fg: arrowFg });
        }

        // Session-specific counts
        if (categorySome(s.sessionCounts)) {
          for (const cat of CATEGORIES) {
            const n = s.sessionCounts[cat.key];
            if (n > 0) sessionSegments.push({ label: `${cat.label}${n}`, fg: resolveToken(cat.token, theme) });
          }
        }

        return (
          <box paddingLeft={0} flexDirection="row">
            {allSegments.map((p, i) => (
              <text fg={p.fg}>{i > 0 ? " " : ""}{p.label}</text>
            ))}
            {sessionSegments.length > 0 && (
              <>
                <text fg={theme.textMuted}> (</text>
                {sessionSegments.map((p, i) => (
                  <text fg={p.fg}>{i > 0 ? " " : ""}{p.label}</text>
                ))}
                <text fg={theme.textMuted}>)</text>
              </>
            )}
          </box>
        );
      },
    },
  });

  api.lifecycle.onDispose(() => {
    if (interval !== null) {
      clearInterval(interval);
      interval = null;
    }
  });
};

const plugin: TuiPluginModule & { id: string } = {
  id,
  tui,
};

export default plugin;
