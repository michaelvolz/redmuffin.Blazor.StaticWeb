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
  buildStashInfo,
  type AheadBehind,
  type GitCounts,
  type StashInfo,
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
  stash: StashInfo | null;
}

const DB_PATH = pathResolve(homedir(), ".local/share/opencode/opencode.db");

// --- Status Dot constants ---
const SERVICE_NAME = "redmuffin.Blazor.StaticWeb-sass-dotnet-watch.service";
const SOLUTION_DIR = "/home/flynn/Projects/redmuffin.Blazor.StaticWeb";
const SERVICE_POLL_MS = 5000;

function isInSolution(dir: string | null): boolean {
  if (!dir) return false;
  return dir === SOLUTION_DIR || dir.startsWith(SOLUTION_DIR + "/");
}

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
// Timestamp of the last observed clean working tree (all changes committed).
// sessionFiles only counts files touched after this point — files touched
// before the last commit cannot explain current dirty state.
let lastCleanTimestamp = 0;
let storedApi: TuiPluginApi | null = null;

function normalizePath(rawPath: string): string {
  if (isAbsolute(rawPath)) return rawPath;
  const dir = storedApi?.state?.path?.directory ?? process.cwd();
  return pathResolve(dir, rawPath);
}

// Commands known to create, modify, or delete files on disk.
// Commands NOT in this list (grep, cat, ls, file, read, echo, etc.)
// must never contribute paths, even if they reference absolute paths.
const FILE_MODIFYING_CMD_RE = /\b(rm|git\s+rm|mv|cp|mkdir|touch|git\s+add|git\s+checkout|git\s+restore|git\s+mv|git\s+clean)\b/;

function extractPathsFromBashCommand(command: string): string[] {
  const dir = storedApi?.state?.path?.directory ?? process.cwd();
  const paths: string[] = [];

  // Guard: skip commands that do not create/modify/delete files.
  // Prevents false positives from grep, cat, ls, file, read, echo,
  // and any other read-only or non-file-mutating operation.
  if (!FILE_MODIFYING_CMD_RE.test(command)) return paths;

  // Absolute paths anywhere in the command
  const absRe = /(?:\s|^)(\/[^\s"'`;|&<>]+)/g;
  let m: RegExpExecArray | null;
  while ((m = absRe.exec(command)) !== null) {
    paths.push(m[1]);
  }

  // Known file-operation commands — extract relative path args
  const fileOps = ["rm ", "git rm ", "mv ", "cp ", "mkdir ", "touch "];
  for (const prefix of fileOps) {
    const idx = command.indexOf(prefix);
    if (idx === -1) continue;
    const args = command.slice(idx + prefix.length).trim();
    const tokens = args.match(/(?:[^\s"']+|"[^"]*"|'[^']*')+/g) || [];
    for (const t of tokens) {
      if (t.startsWith("-")) continue;
      const cleaned = t.replace(/^["']|["']$/g, "");
      if (cleaned.startsWith("/")) {
        paths.push(cleaned);
      } else if (cleaned.includes("/") || cleaned.includes(".")) {
        paths.push(pathResolve(dir, cleaned));
      }
    }
    break;
  }

  return paths;
}

function seedSessionFiles(sessionId: string) {
  if (!SESSION_ID_RE.test(sessionId)) return;
  if (sessionId !== storedSessionId) return;

  try {
    const since = lastCleanTimestamp > 0
      ? `AND time_created > ${lastCleanTimestamp * 1000}`
      : "";

    // Write + edit tool filePaths
    const writeOutput = execSync(
      `sqlite3 "${DB_PATH}" "SELECT DISTINCT json_extract(data, '$.state.input.filePath') FROM part WHERE session_id = '${sessionId}' AND json_extract(data, '$.type') = 'tool' AND json_extract(data, '$.tool') IN ('write', 'edit') AND json_extract(data, '$.state.input.filePath') IS NOT NULL ${since};"`,
      { encoding: "utf8", timeout: 5000 },
    );
    for (const p of writeOutput.trim().split("\n").filter(Boolean)) {
      sessionFiles.add(normalizePath(p));
    }

    // Bash tool commands — extract file paths
    const bashOutput = execSync(
      `sqlite3 "${DB_PATH}" "SELECT json_extract(data, '$.state.input.command') FROM part WHERE session_id = '${sessionId}' AND json_extract(data, '$.tool') = 'bash' AND json_extract(data, '$.state.input.command') LIKE '%/%' ${since};"`,
      { encoding: "utf8", timeout: 5000 },
    );
    for (const cmd of bashOutput.trim().split("\n").filter(Boolean)) {
      for (const p of extractPathsFromBashCommand(cmd)) {
        sessionFiles.add(normalizePath(p));
      }
    }

    // Patch entries — file arrays
    const patchOutput = execSync(
      `sqlite3 "${DB_PATH}" "SELECT DISTINCT json_extract(data, '$.files') FROM part WHERE session_id = '${sessionId}' AND json_extract(data, '$.type') = 'patch' ${since};"`,
      { encoding: "utf8", timeout: 5000 },
    );
    for (const line of patchOutput.trim().split("\n").filter(Boolean)) {
      try {
        const files: Array<string> = JSON.parse(line);
        for (const f of files) {
          sessionFiles.add(normalizePath(f));
        }
      } catch { /* skip */ }
    }
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

// --- Module-level state ---

// --- Status Dot state ---
type DotColor = "green" | "yellow" | "red" | "grey";
const [serviceDot, setServiceDot] = createSignal<DotColor>("grey");
let serviceInterval: ReturnType<typeof setInterval> | null = null;

function pollServiceStatus() {
  try {
    // || true suppresses non-zero exit so we read string output for all states
    const active = execSync(`systemctl --user is-active ${SERVICE_NAME} || true`, {
      encoding: "utf8",
      timeout: 3000,
    }).trim();

    if (active === "inactive" || active === "unknown") {
      setServiceDot("grey");
      return;
    }
    if (active === "failed") {
      setServiceDot("red");
      return;
    }
    // active — single journalctl call covering both error + warning
    const since = `-${Math.ceil(SERVICE_POLL_MS / 1000)}s`;
    const entries = execFileSync("journalctl", [
      "--user", "-u", SERVICE_NAME, "-p", "warning",
      "--since", since, "--no-pager", "-o", "json",
    ], { encoding: "utf8", timeout: 3000 }).trim();
    if (entries) {
      let hasError = false;
      for (const line of entries.split("\n")) {
        try {
          const pri = JSON.parse(line).PRIORITY;
          if (typeof pri === "number" && pri <= 3) { hasError = true; break; }
        } catch { /* skip malformed lines */ }
      }
      setServiceDot(hasError ? "red" : "yellow");
    } else {
      setServiceDot("green");
    }
  } catch {
    setServiceDot("grey");
  }
}

const [gitState, setGitState] = createSignal<GitState>({
  dir: null,
  error: false,
  counts: { ...EMPTY_COUNTS },
  sessionCounts: { ...EMPTY_COUNTS },
  total: 0,
  aheadBehind: null,
  stash: null,
});

const ERROR_STATE: GitState = {
  dir: null,
  error: true,
  counts: { ...EMPTY_COUNTS },
  sessionCounts: { ...EMPTY_COUNTS },
  total: 0,
  aheadBehind: null,
  stash: null,
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
    const total = categoryTotal(counts);

    // Clean-tree boundary — when all changes are committed, files touched
    // before this point are stale and cannot explain current dirty state.
    // On startup with dirty tree, do NOT set lastCleanTimestamp yet —
    // seedSessionFiles must load all historical tool calls first to
    // correctly attribute pre-existing dirty files to this session.
    // The timestamp is set after the first seed (in seedSessionFiles).
    if (total === 0) {
      lastCleanTimestamp = Date.now() / 1000;
      sessionFiles.clear();
    }

    // Refresh session file set from database on every poll
    if (sid) {
      const wasFirstSeed = lastCleanTimestamp === 0;
      seedSessionFiles(sid);
      // After the initial full-load seed, set the boundary so future
      // polls are incremental. This must happen AFTER seedSessionFiles
      // so the first seed runs without the since filter.
      if (wasFirstSeed) {
        lastCleanTimestamp = Date.now() / 1000;
      }
    }

    // Only use sessionFiles if the session hasn't changed since capture.
    // If it has, compute against an empty set — session counts are stale.
    const files = (storedSessionId === sid) ? sessionFiles : new Set<string>();
    const sessionCounts = computeSessionCounts(output, dir, files);

    // --- Stash poll (added to existing 10s interval) ---
    let stash: StashInfo | null = null;
    try {
      const stashList = execSync(`git -C "${dir}" stash list --format="%ct" 2>/dev/null || true`, {
        encoding: "utf8",
        timeout: 2000,
      });
      if (stashList.trim()) {
        const stashShow = execSync(`git -C "${dir}" stash show --name-only stash@{0} 2>/dev/null || true`, {
          encoding: "utf8",
          timeout: 2000,
        });
        stash = buildStashInfo(stashList, stashShow);
      }
    } catch {
      stash = null;
    }

    setGitState({ dir, error: false, counts, sessionCounts, total: categoryTotal(counts), aheadBehind, stash });
  } catch {
    setGitState({ dir: null, error: true, counts: { ...EMPTY_COUNTS }, sessionCounts: { ...EMPTY_COUNTS }, total: 0, aheadBehind: null, stash: null });
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
  // Service polling starts on first render when PWD enters solution dir

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

  // --- Shared badge renderer ---
  interface Segment {
    label: string;
    fg: RGBA;
  }

  function renderBadge(s: GitState, theme: { success: RGBA; warning: RGBA; error: RGBA; textMuted: RGBA }) {
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
  }

  // --- Slot registrations ---

  api.slots.register({
    order: 10,
    slots: {
      session_prompt_right(_ctx: unknown, _value: { session_id: string }) {
        // Session tracking init — sessionFiles must be scoped to this session's
        // tool calls. lastCleanTimestamp must NOT reset here — the clean-tree
        // boundary is independent of session identity and persists across sessions.
        if (_value.session_id !== storedSessionId) {
          storedSessionId = _value.session_id;
          sessionFiles = new Set<string>();
        }
        // Initial seed on session change — pollGitStatus handles periodic refresh.
        // lastCleanTimestamp === 0 means plugin just started (no clean boundary yet).
        // sessionFiles are empty from the reset above, so initial seed loads only
        // tool calls from the current session after the clean boundary.
        if (storedSessionId && lastCleanTimestamp === 0) {
          const sid = storedSessionId;
          setTimeout(() => seedSessionFiles(sid), 0);
        }

        const dir = api.state?.path?.directory ?? null;
        const inSolution = isInSolution(dir);

        // Start/stop service polling based on directory
        if (inSolution && serviceInterval === null) {
          pollServiceStatus();
          serviceInterval = setInterval(pollServiceStatus, SERVICE_POLL_MS);
        } else if (!inSolution && serviceInterval !== null) {
          clearInterval(serviceInterval);
          serviceInterval = null;
          setServiceDot("grey");
        }

        const dot = serviceDot();
        const dotFg: RGBA =
          dot === "green" ? api.theme.current.success
          : dot === "yellow" ? api.theme.current.warning
          : dot === "red" ? api.theme.current.error
          : api.theme.current.textMuted;

        // --- Stash indicator ---
        let stashFg: RGBA = api.theme.current.textMuted;
        let stashText: string | null = null;
        const s = gitState();
        if (s.stash && s.stash.count > 0) {
          const ageSeconds = s.stash.oldestTimestamp
            ? (Date.now() / 1000) - s.stash.oldestTimestamp
            : 0;
          const ageDays = ageSeconds / 86400;
          stashFg = ageDays >= 2 ? api.theme.current.error
            : ageDays >= 1 ? api.theme.current.warning
            : api.theme.current.success;
          stashText = `※${s.stash.count}[${s.stash.latestFileCount}f]`;
        }

        return (
          <box paddingLeft={0} flexDirection="row">
            {renderBadge(gitState(), api.theme.current)}
            {stashText && <text fg={stashFg}> {stashText}</text>}
            {inSolution && <text fg={dotFg}> ●</text>}
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
    if (serviceInterval !== null) {
      clearInterval(serviceInterval);
      serviceInterval = null;
    }
  });
};

const plugin: TuiPluginModule & { id: string } = {
  id,
  tui,
};

export default plugin;
