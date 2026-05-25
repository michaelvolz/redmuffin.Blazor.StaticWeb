// @bun
// tui.tsx
import { insert as _$insert } from "@opentui/solid";
import { memo as _$memo } from "@opentui/solid";
import { setProp as _$setProp } from "@opentui/solid";
import { effect as _$effect } from "@opentui/solid";
import { createTextNode as _$createTextNode } from "@opentui/solid";
import { insertNode as _$insertNode } from "@opentui/solid";
import { createElement as _$createElement } from "@opentui/solid";
import { createSignal } from "solid-js";
import { execSync, execFileSync } from "child_process";
import { homedir } from "os";
import { isAbsolute, resolve as pathResolve2 } from "path";

// git.ts
import { resolve as pathResolve, sep } from "path";
var CATEGORIES = [
  { key: "modified", statuses: ["M ", " M", "MM"], label: "M", token: "warning" },
  { key: "added", statuses: ["A ", "AM"], label: "A", token: "success" },
  { key: "deleted", statuses: ["D ", " D"], label: "D", token: "error" },
  { key: "renamed", statuses: ["R ", "RM", "RD"], label: "R", token: "success" },
  { key: "untracked", statuses: ["??"], label: "U", token: "success" },
  { key: "conflicting", statuses: ["DD", "AU", "UD", "UA", "DU", "AA", "UU"], label: "!", token: "error" },
  { key: "copied", statuses: ["C "], label: "C", token: "success" },
  { key: "typechanged", statuses: ["T "], label: "T", token: "warning" },
  { key: "ignored", statuses: ["!!"], label: "I", token: "textMuted" }
];
var STATUS_LABEL = {};
var LABEL_TO_KEY = {};
for (const cat of CATEGORIES) {
  for (const s of cat.statuses) {
    STATUS_LABEL[s] = cat.label;
  }
  LABEL_TO_KEY[cat.label] = cat.key;
}
var EMPTY_COUNTS = Object.fromEntries(CATEGORIES.map((c) => [c.key, 0]));
function categoryTotal(c) {
  return CATEGORIES.reduce((sum, cat) => sum + c[cat.key], 0);
}
function categorySome(c) {
  return CATEGORIES.some((cat) => c[cat.key] > 0);
}
function parseGitCounts(output) {
  const counts = { ...EMPTY_COUNTS };
  if (!output)
    return counts;
  const lines = output.trimEnd().split(`
`).filter(Boolean);
  for (const line of lines) {
    if (line.startsWith("## "))
      continue;
    const code = line.substring(0, 2);
    const label = STATUS_LABEL[code] || code.trim() || "?";
    const key = LABEL_TO_KEY[label];
    if (key)
      counts[key]++;
  }
  return counts;
}
function computeSessionCounts(output, dir, sessionFiles) {
  const counts = { ...EMPTY_COUNTS };
  if (!output || sessionFiles.size === 0)
    return counts;
  const lines = output.trimEnd().split(`
`).filter(Boolean);
  for (const line of lines) {
    if (line.startsWith("## "))
      continue;
    if (line.length < 4)
      continue;
    const code = line.substring(0, 2);
    const label = STATUS_LABEL[code] || code.trim() || "?";
    let relPath = line.substring(3).trim();
    if (relPath.includes(" -> ")) {
      relPath = relPath.split(" -> ")[1];
    }
    const absPath = pathResolve(dir, relPath);
    let matched = sessionFiles.has(absPath);
    if (!matched && relPath.endsWith(sep)) {
      const prefix = absPath + sep;
      for (const f of sessionFiles) {
        if (f.startsWith(prefix)) {
          matched = true;
          break;
        }
      }
    }
    if (matched) {
      const key = LABEL_TO_KEY[label];
      if (key)
        counts[key]++;
    }
  }
  return counts;
}
function buildStashInfo(timestampsOutput, latestShowOutput) {
  const timestamps = timestampsOutput.trim().split(`
`).filter(Boolean).map(Number).filter((n) => !isNaN(n));
  if (timestamps.length === 0)
    return null;
  const latestFileCount = latestShowOutput.trim() ? latestShowOutput.trim().split(`
`).length : 0;
  return {
    count: timestamps.length,
    latestFileCount,
    oldestTimestamp: Math.min(...timestamps)
  };
}
function parseAheadBehind(output) {
  if (!output)
    return null;
  const firstLine = output.split(`
`)[0];
  if (!firstLine || !firstLine.startsWith("## "))
    return null;
  if (!firstLine.includes("["))
    return null;
  const aheadMatch = firstLine.match(/ahead (\d+)/);
  const behindMatch = firstLine.match(/behind (\d+)/);
  const ahead = aheadMatch ? parseInt(aheadMatch[1], 10) : 0;
  const behind = behindMatch ? parseInt(behindMatch[1], 10) : 0;
  if (ahead === 0 && behind === 0)
    return null;
  return { ahead, behind };
}

// tui.tsx
var id = "rm-git-sidebar";
var DB_PATH = pathResolve2(homedir(), ".local/share/opencode/opencode.db");
var SERVICE_NAME = "redmuffin.Blazor.StaticWeb-sass-dotnet-watch.service";
var SOLUTION_DIR = "/home/flynn/Projects/redmuffin.Blazor.StaticWeb";
var SERVICE_POLL_MS = 5000;
function isInSolution(dir) {
  if (!dir)
    return false;
  return dir === SOLUTION_DIR || dir.startsWith(SOLUTION_DIR + "/");
}
var SESSION_ID_RE = /^ses_[a-zA-Z0-9]{16,}$/;
var sessionFiles = new Set;
var storedSessionId = null;
var lastCleanTimestamp = 0;
var storedApi = null;
function expandTilde(p) {
  if (p === "~")
    return homedir();
  if (p.startsWith("~/"))
    return pathResolve2(homedir(), p.slice(2));
  return p;
}
function normalizePath(rawPath) {
  const expanded = expandTilde(rawPath);
  if (isAbsolute(expanded))
    return expanded;
  const dir = storedApi?.state?.path?.directory ?? process.cwd();
  return pathResolve2(dir, expanded);
}
var FILE_MODIFYING_CMD_RE = /\b(rm|git\s+rm|mv|cp|mkdir|touch|git\s+add|git\s+checkout|git\s+restore|git\s+mv|git\s+clean)\b/;
function extractPathsFromBashCommand(command) {
  const dir = storedApi?.state?.path?.directory ?? process.cwd();
  const paths = [];
  if (!FILE_MODIFYING_CMD_RE.test(command))
    return paths;
  const absRe = /(?:\s|^)(\/[^\s"'`;|&<>]+|~[^\s"'`;|&<>]*)/g;
  let m;
  while ((m = absRe.exec(command)) !== null) {
    paths.push(expandTilde(m[1]));
  }
  const fileOps = ["rm ", "git rm ", "mv ", "cp ", "mkdir ", "touch "];
  for (const prefix of fileOps) {
    const idx = command.indexOf(prefix);
    if (idx === -1)
      continue;
    const args = command.slice(idx + prefix.length).trim();
    const tokens = args.match(/(?:[^\s"']+|"[^"]*"|'[^']*')+/g) || [];
    for (const t of tokens) {
      if (t.startsWith("-"))
        continue;
      const cleaned = expandTilde(t.replace(/^["']|["']$/g, ""));
      if (cleaned.startsWith("/")) {
        paths.push(cleaned);
      } else if (cleaned.includes("/") || cleaned.includes(".")) {
        paths.push(pathResolve2(dir, cleaned));
      }
    }
    break;
  }
  return paths;
}
function seedSessionFiles(sessionId) {
  if (!SESSION_ID_RE.test(sessionId))
    return;
  if (sessionId !== storedSessionId)
    return;
  try {
    const SEVEN_DAYS_MS = 7 * 24 * 60 * 60 * 1000;
    const sevenDaysAgo = (Date.now() - SEVEN_DAYS_MS) / 1000;
    let effectiveTimestamp = lastCleanTimestamp;
    if (lastCleanTimestamp === 0 || lastCleanTimestamp < sevenDaysAgo) {
      effectiveTimestamp = sevenDaysAgo;
    }
    const since = `AND time_created > ${effectiveTimestamp * 1000}`;
    const writeOutput = execSync(`sqlite3 "${DB_PATH}" "SELECT DISTINCT json_extract(data, '$.state.input.filePath') FROM part WHERE session_id = '${sessionId}' AND json_extract(data, '$.type') = 'tool' AND json_extract(data, '$.tool') IN ('write', 'edit') AND json_extract(data, '$.state.input.filePath') IS NOT NULL ${since};"`, {
      encoding: "utf8",
      timeout: 5000,
      maxBuffer: 10 * 1024 * 1024
    });
    for (const p of writeOutput.trim().split(`
`).filter(Boolean)) {
      sessionFiles.add(normalizePath(p));
    }
    const bashOutput = execSync(`sqlite3 "${DB_PATH}" "SELECT json_extract(data, '$.state.input.command') FROM part WHERE session_id = '${sessionId}' AND json_extract(data, '$.tool') = 'bash' AND json_extract(data, '$.state.input.command') LIKE '%/%' ${since};"`, {
      encoding: "utf8",
      timeout: 5000,
      maxBuffer: 10 * 1024 * 1024
    });
    for (const cmd of bashOutput.trim().split(`
`).filter(Boolean)) {
      for (const p of extractPathsFromBashCommand(cmd)) {
        sessionFiles.add(normalizePath(p));
      }
    }
    const patchOutput = execSync(`sqlite3 "${DB_PATH}" "SELECT DISTINCT json_extract(data, '$.files') FROM part WHERE session_id = '${sessionId}' AND json_extract(data, '$.type') = 'patch' ${since};"`, {
      encoding: "utf8",
      timeout: 5000,
      maxBuffer: 10 * 1024 * 1024
    });
    for (const line of patchOutput.trim().split(`
`).filter(Boolean)) {
      try {
        const files = JSON.parse(line);
        for (const f of files) {
          sessionFiles.add(normalizePath(f));
        }
      } catch {}
    }
  } catch (err) {
    const msg = err instanceof Error ? err.message : String(err);
    console.error(`[rm-git-sidebar] seedSessionFiles failed: ${msg}`);
  }
}
var [serviceDot, setServiceDot] = createSignal("grey");
var serviceInterval = null;
function pollServiceStatus() {
  try {
    const active = execSync(`systemctl --user is-active ${SERVICE_NAME} || true`, {
      encoding: "utf8",
      timeout: 3000
    }).trim();
    if (active === "inactive" || active === "unknown") {
      setServiceDot("grey");
      return;
    }
    if (active === "failed") {
      setServiceDot("red");
      return;
    }
    const since = `-${Math.ceil(SERVICE_POLL_MS / 1000)}s`;
    const entries = execFileSync("journalctl", ["--user", "-u", SERVICE_NAME, "-p", "warning", "--since", since, "--no-pager", "-o", "json"], {
      encoding: "utf8",
      timeout: 3000
    }).trim();
    if (entries) {
      let hasError = false;
      for (const line of entries.split(`
`)) {
        try {
          const pri = JSON.parse(line).PRIORITY;
          if (typeof pri === "number" && pri <= 3) {
            hasError = true;
            break;
          }
        } catch {}
      }
      setServiceDot(hasError ? "red" : "yellow");
    } else {
      setServiceDot("green");
    }
  } catch {
    setServiceDot("grey");
  }
}
var [gitState, setGitState] = createSignal({
  dir: null,
  error: false,
  counts: {
    ...EMPTY_COUNTS
  },
  sessionCounts: {
    ...EMPTY_COUNTS
  },
  total: 0,
  aheadBehind: null,
  stash: null
});
var ERROR_STATE = {
  dir: null,
  error: true,
  counts: {
    ...EMPTY_COUNTS
  },
  sessionCounts: {
    ...EMPTY_COUNTS
  },
  total: 0,
  aheadBehind: null,
  stash: null
};
var interval = null;
var lastRefresh = 0;
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
      timeout: 5000
    });
    const counts = parseGitCounts(output);
    const aheadBehind = parseAheadBehind(output);
    const total = categoryTotal(counts);
    if (total === 0) {
      const SEVEN_DAYS_AGO = (Date.now() - 7 * 24 * 60 * 60 * 1000) / 1000;
      lastCleanTimestamp = Math.max(Date.now() / 1000, SEVEN_DAYS_AGO);
      sessionFiles.clear();
    }
    let sessionCounts = {
      ...EMPTY_COUNTS
    };
    try {
      if (sid) {
        const wasFirstSeed = lastCleanTimestamp === 0;
        seedSessionFiles(sid);
        if (wasFirstSeed) {
          const SEVEN_DAYS_AGO = (Date.now() - 7 * 24 * 60 * 60 * 1000) / 1000;
          lastCleanTimestamp = Math.max(Date.now() / 1000, SEVEN_DAYS_AGO);
        }
      }
      const files = storedSessionId === sid ? sessionFiles : new Set;
      sessionCounts = computeSessionCounts(output, dir, files);
    } catch (err) {
      const msg = err instanceof Error ? err.message : String(err);
      console.error(`[rm-git-sidebar] Session tracking failed (non-fatal): ${msg}`);
      sessionCounts = {
        ...EMPTY_COUNTS
      };
    }
    let stash = null;
    try {
      const stashList = execSync(`git -C "${dir}" stash list --format="%ct" 2>/dev/null || true`, {
        encoding: "utf8",
        timeout: 2000
      });
      if (stashList.trim()) {
        const stashShow = execSync(`git -C "${dir}" stash show --name-only stash@{0} 2>/dev/null || true`, {
          encoding: "utf8",
          timeout: 2000
        });
        stash = buildStashInfo(stashList, stashShow);
      }
    } catch {
      stash = null;
    }
    setGitState({
      dir,
      error: false,
      counts,
      sessionCounts,
      total: categoryTotal(counts),
      aheadBehind,
      stash
    });
  } catch (err) {
    const msg = err instanceof Error ? err.message : String(err);
    console.error(`[rm-git-sidebar] pollGitStatus threw: ${msg}`);
    setGitState({
      dir: null,
      error: true,
      counts: {
        ...EMPTY_COUNTS
      },
      sessionCounts: {
        ...EMPTY_COUNTS
      },
      total: 0,
      aheadBehind: null,
      stash: null
    });
  }
}
function resolveToken(token, theme) {
  return theme[token];
}
var tui = async (api) => {
  storedApi = api;
  pollGitStatus();
  interval = setInterval(pollGitStatus, 1e4);
  api.event.on("session.diff", (event) => {
    if (event?.properties?.sessionID === storedSessionId) {
      if (event?.properties?.diff) {
        for (const d of event.properties.diff) {
          if (d.file)
            sessionFiles.add(normalizePath(d.file));
        }
      }
      if (Date.now() - lastRefresh > 2000)
        pollGitStatus();
    }
  });
  function renderBadge(s, theme) {
    if (s.error) {
      return (() => {
        var _el$ = _$createElement("text");
        _$insertNode(_el$, _$createTextNode(`git ?`));
        _$effect((_$p) => _$setProp(_el$, "fg", theme.error, _$p));
        return _el$;
      })();
    }
    const ab = s.aheadBehind;
    const hasAheadBehind = ab !== null && (ab.ahead > 0 || ab.behind > 0);
    if (s.total === 0 && !hasAheadBehind) {
      return (() => {
        var _el$3 = _$createElement("text");
        _$insertNode(_el$3, _$createTextNode(`git \u2713`));
        _$effect((_$p) => _$setProp(_el$3, "fg", theme.success, _$p));
        return _el$3;
      })();
    }
    if (s.total === 0 && hasAheadBehind) {
      const arrowFg = ab.ahead > 0 && ab.behind > 0 ? theme.error : ab.ahead > 0 ? theme.success : theme.warning;
      return (() => {
        var _el$5 = _$createElement("box"), _el$6 = _$createElement("text");
        _$insertNode(_el$5, _el$6);
        _$setProp(_el$5, "paddingLeft", 0);
        _$setProp(_el$5, "flexDirection", "row");
        _$insertNode(_el$6, _$createTextNode(`git \u2713`));
        _$insert(_el$5, (() => {
          var _c$ = _$memo(() => ab.ahead > 0);
          return () => _c$() && (() => {
            var _el$8 = _$createElement("text"), _el$9 = _$createTextNode(` \u2191`);
            _$insertNode(_el$8, _el$9);
            _$setProp(_el$8, "fg", arrowFg);
            _$insert(_el$8, () => ab.ahead, null);
            return _el$8;
          })();
        })(), null);
        _$insert(_el$5, (() => {
          var _c$2 = _$memo(() => ab.behind > 0);
          return () => _c$2() && (() => {
            var _el$0 = _$createElement("text"), _el$1 = _$createTextNode(` \u2193`);
            _$insertNode(_el$0, _el$1);
            _$setProp(_el$0, "fg", arrowFg);
            _$insert(_el$0, () => ab.behind, null);
            return _el$0;
          })();
        })(), null);
        _$effect((_$p) => _$setProp(_el$6, "fg", theme.success, _$p));
        return _el$5;
      })();
    }
    const allSegments = [];
    const sessionSegments = [];
    allSegments.push({
      label: "git",
      fg: theme.textMuted
    });
    for (const cat of CATEGORIES) {
      const n = s.counts[cat.key];
      if (n > 0)
        allSegments.push({
          label: `${cat.label}${n}`,
          fg: resolveToken(cat.token, theme)
        });
    }
    if (hasAheadBehind) {
      const arrowFg = ab.ahead > 0 && ab.behind > 0 ? theme.error : ab.ahead > 0 ? theme.success : theme.warning;
      if (ab.ahead > 0)
        allSegments.push({
          label: `\u2191${ab.ahead}`,
          fg: arrowFg
        });
      if (ab.behind > 0)
        allSegments.push({
          label: `\u2193${ab.behind}`,
          fg: arrowFg
        });
    }
    if (categorySome(s.sessionCounts)) {
      for (const cat of CATEGORIES) {
        const n = s.sessionCounts[cat.key];
        if (n > 0)
          sessionSegments.push({
            label: `${cat.label}${n}`,
            fg: resolveToken(cat.token, theme)
          });
      }
    }
    return (() => {
      var _el$10 = _$createElement("box");
      _$setProp(_el$10, "paddingLeft", 0);
      _$setProp(_el$10, "flexDirection", "row");
      _$insert(_el$10, () => allSegments.map((p, i) => (() => {
        var _el$11 = _$createElement("text");
        _$insert(_el$11, i > 0 ? " " : "", null);
        _$insert(_el$11, () => p.label, null);
        _$effect((_$p) => _$setProp(_el$11, "fg", p.fg, _$p));
        return _el$11;
      })()), null);
      _$insert(_el$10, (() => {
        var _c$3 = _$memo(() => sessionSegments.length > 0);
        return () => _c$3() && [(() => {
          var _el$12 = _$createElement("text");
          _$insertNode(_el$12, _$createTextNode(` (`));
          _$effect((_$p) => _$setProp(_el$12, "fg", theme.textMuted, _$p));
          return _el$12;
        })(), _$memo(() => sessionSegments.map((p, i) => (() => {
          var _el$16 = _$createElement("text");
          _$insert(_el$16, i > 0 ? " " : "", null);
          _$insert(_el$16, () => p.label, null);
          _$effect((_$p) => _$setProp(_el$16, "fg", p.fg, _$p));
          return _el$16;
        })())), (() => {
          var _el$14 = _$createElement("text");
          _$insertNode(_el$14, _$createTextNode(`)`));
          _$effect((_$p) => _$setProp(_el$14, "fg", theme.textMuted, _$p));
          return _el$14;
        })()];
      })(), null);
      return _el$10;
    })();
  }
  api.slots.register({
    order: 10,
    slots: {
      session_prompt_right(_ctx, _value) {
        if (_value.session_id !== storedSessionId) {
          storedSessionId = _value.session_id;
          sessionFiles = new Set;
        }
        if (storedSessionId && lastCleanTimestamp === 0) {
          const sid = storedSessionId;
          setTimeout(() => seedSessionFiles(sid), 0);
        }
        const dir = api.state?.path?.directory ?? null;
        const inSolution = isInSolution(dir);
        if (inSolution && serviceInterval === null) {
          pollServiceStatus();
          serviceInterval = setInterval(pollServiceStatus, SERVICE_POLL_MS);
        } else if (!inSolution && serviceInterval !== null) {
          clearInterval(serviceInterval);
          serviceInterval = null;
          setServiceDot("grey");
        }
        const dot = serviceDot();
        const dotFg = dot === "green" ? api.theme.current.success : dot === "yellow" ? api.theme.current.warning : dot === "red" ? api.theme.current.error : api.theme.current.textMuted;
        let stashFg = api.theme.current.textMuted;
        let stashText = null;
        const s = gitState();
        if (s.stash && s.stash.count > 0) {
          const ageSeconds = s.stash.oldestTimestamp ? Date.now() / 1000 - s.stash.oldestTimestamp : 0;
          const ageDays = ageSeconds / 86400;
          stashFg = ageDays >= 2 ? api.theme.current.error : ageDays >= 1 ? api.theme.current.warning : api.theme.current.success;
          stashText = `\u203B${s.stash.count}[${s.stash.latestFileCount}f]`;
        }
        return (() => {
          var _el$17 = _$createElement("box");
          _$setProp(_el$17, "paddingLeft", 0);
          _$setProp(_el$17, "flexDirection", "row");
          _$insert(_el$17, () => renderBadge(gitState(), api.theme.current), null);
          _$insert(_el$17, stashText && (() => {
            var _el$18 = _$createElement("text"), _el$19 = _$createTextNode(` `);
            _$insertNode(_el$18, _el$19);
            _$setProp(_el$18, "fg", stashFg);
            _$insert(_el$18, stashText, null);
            return _el$18;
          })(), null);
          _$insert(_el$17, inSolution && (() => {
            var _el$20 = _$createElement("text");
            _$insertNode(_el$20, _$createTextNode(` \u25CF`));
            _$setProp(_el$20, "fg", dotFg);
            return _el$20;
          })(), null);
          return _el$17;
        })();
      }
    }
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
var plugin = {
  id,
  tui
};
var tui_default = plugin;
export {
  tui_default as default
};
