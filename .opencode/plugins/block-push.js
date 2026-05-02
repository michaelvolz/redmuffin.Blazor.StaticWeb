export const BlockPushPlugin = async () => {
  return {
    "tool.execute.before": async (input, output) => {
      if (input.tool !== "bash") return;

      try {
        const cmd = output?.args?.command || "";
        const segments = parseCommand(cmd);

        for (const segment of segments) {
          const block = checkSegment(segment);
          if (block) {
            throw new Error(
              "BLOCKED by Safety (block-push)\n\n" +
              "Reason: " + block.reason + "\n\n" +
              "Command: " + sanitize(cmd) + "\n\n" +
              "If this operation is truly needed, ask the user for explicit " +
              "permission and have them run the command manually."
            );
          }
        }

        // Non-git blocks
        if (cmd.trim().startsWith("eval")) {
          throw new Error(
            "BLOCKED by Policy: eval is restricted to the repository owner only.\n\n" +
            "Command: " + sanitize(cmd) + "\n\n" +
            "Reason: eval can execute arbitrary code from strings and is a common attack vector. " +
            "Ask the user to run this command manually when ready."
          );
        }

        if (cmd.trim().startsWith("source") || cmd.trim().startsWith(".")) {
          throw new Error(
            "BLOCKED by Policy: source is restricted to the repository owner only.\n\n" +
            "Command: " + sanitize(cmd) + "\n\n" +
            "Reason: source on untrusted files can execute malicious code. " +
            "Ask the user to run this command manually when ready."
          );
        }
      } catch (err) {
        if (err.message.startsWith("BLOCKED by")) {
          throw err;
        }
        console.error("BlockPushPlugin error:", err.message);
      }
    },
  };
};

// ─── Rule Engine ────────────────────────────────────────────────────────────

function checkSegment(segment) {
  const tokens = tokenize(segment);
  if (tokens.length === 0) return null;

  const base = basename(tokens[0]);

  // Direct git command
  if (base === "git") return checkGit(tokens);

  // RTK wrapper: rtk git <cmd> → strip rtk, check inner command
  // This catches RTK-wrapped variants that cc-safety-net custom rules can't
  // handle precisely (e.g. rtk git branch -D, rtk git stash drop)
  if (base === "rtk") {
    const rest = tokens.slice(1).join(" ");
    if (rest) return checkSegment(rest);
    return null;
  }

  // Non-git gaps: cc-safety-net only catches RTK-wrapped, not raw
  if (base === "xargs" && tokens.some(t => t === "rm" || t === "rmdir")) {
    return { reason: "xargs rm is dangerous — dynamic input makes targets unpredictable." };
  }
  if (base === "parallel" && tokens.some(t => t === "rm" || t === "rmdir")) {
    return { reason: "parallel rm is dangerous — dynamic input makes targets unpredictable." };
  }

  // Shell wrappers: bash -c "git push", sh -c "...", pwsh -Command "..."
  if (["bash", "sh", "zsh", "cmd", "powershell", "pwsh"].includes(base)) {
    const execArg = extractExecArg(tokens);
    if (execArg) return checkSegment(execArg);
  }

  // Script interpreters
  if (["python", "python3", "node", "ruby", "perl", "php"].includes(base)) {
    const execArg = extractExecArg(tokens);
    if (execArg) return checkSegment(execArg);
  }

  return null;
}

function checkGit(tokens) {
  const args = getGitArgs(tokens);
  const flags = getFlags(tokens);
  const sub = args[0];
  const subsub = args[1];

  // ── git push (ALL variants, NO exceptions) ──────────────────────────────
  if (sub === "push") {
    return { reason: "git push is restricted to the repository owner only. Ask the user to push manually when ready." };
  }

  // ── git revert ──────────────────────────────────────────────────────────
  if (sub === "revert") {
    return { reason: "git revert is restricted to the repository owner only. Ask the user to perform the revert manually when ready." };
  }

  // ── git update-ref ──────────────────────────────────────────────────────
  if (sub === "update-ref") {
    return { reason: "git update-ref is restricted to the repository owner only. Ask the user to perform the update-ref manually when ready." };
  }

  // ── git branch -D ───────────────────────────────────────────────────────
  if (sub === "branch" && (flags.has("-D") || tokens.includes("-D"))) {
    return { reason: "git branch -D force-deletes a branch without merge check. Use -d instead." };
  }

  // ── git stash drop ──────────────────────────────────────────────────────
  if (sub === "stash" && subsub === "drop") {
    return { reason: "git stash drop permanently deletes stashed changes. Use 'git stash pop' to recover." };
  }

  // ── git stash clear ─────────────────────────────────────────────────────
  if (sub === "stash" && subsub === "clear") {
    return { reason: "git stash clear deletes ALL stashed changes permanently." };
  }

  // ── git worktree remove ─────────────────────────────────────────────────
  if (sub === "worktree" && subsub === "remove") {
    return { reason: "git worktree remove deletes worktrees. Verify no unsaved changes exist before proceeding." };
  }

  // ── git reset --hard ────────────────────────────────────────────────────
  if (sub === "reset" && (flags.has("--hard") || tokens.includes("--hard"))) {
    return { reason: "git reset --hard destroys all uncommitted changes. Use 'git stash' first or reset without --hard." };
  }

  // ── git clean (block unless dry-run) ────────────────────────────────────
  if (sub === "clean" && !flags.has("-n") && !flags.has("--dry-run")) {
    return { reason: "git clean removes untracked files permanently. Use -n or --dry-run first to preview what would be deleted." };
  }

  // ── git checkout -- (pathspec discard) ──────────────────────────────────
  if (sub === "checkout" && tokens.includes("--")) {
    return { reason: "git checkout -- discards uncommitted changes permanently. Use 'git stash' first." };
  }

  // ── git restore (block unless --staged) ─────────────────────────────────
  if (sub === "restore" && !flags.has("--staged") && !flags.has("-S")) {
    return { reason: "git restore discards uncommitted working-tree changes. Use --staged to only unstage files." };
  }

  // ── git reflog expire --expire=now --all (RTK gap: cc-safety-net only catches raw) ──
  if (sub === "reflog" && subsub === "expire" &&
      tokens.some(t => t === "--expire=now" || t === "--all" || t === "expire")) {
    return { reason: "git reflog expire --expire=now --all permanently deletes reflog history and can hide malicious activity." };
  }

  return null;
}

// ─── Token Analysis Helpers ─────────────────────────────────────────────────

function getGitArgs(tokens) {
  const args = [];
  const optsWithValue = new Set([
    "-c", "-C", "--git-dir", "--work-tree", "--namespace", "--config-env",
  ]);
  let skipNext = false;

  for (let i = 1; i < tokens.length; i++) {
    const t = tokens[i];
    if (skipNext) { skipNext = false; continue; }
    if (optsWithValue.has(t)) { skipNext = true; continue; }
    if (t.startsWith("--") && t.includes("=")) continue;
    if (t.startsWith("-")) continue;
    args.push(t);
  }
  return args;
}

function getFlags(tokens) {
  const flags = new Set();
  for (const t of tokens) {
    if (t.startsWith("--")) {
      const eq = t.indexOf("=");
      flags.add(eq === -1 ? t : t.substring(0, eq));
    } else if (t.startsWith("-") && t.length > 1 && t !== "--") {
      for (let j = 1; j < t.length; j++) {
        flags.add("-" + t[j]);
      }
    }
  }
  return flags;
}

// ─── Shell Parsing ──────────────────────────────────────────────────────────

function parseCommand(cmd) {
  if (!cmd) return [];
  const trimmed = cmd.trim();
  if (!trimmed) return [];

  const segments = [];
  segments.push(stripEnvPrefix(trimmed));

  // Split on && and ;
  const andChain = trimmed.split(/\s*(?:&&|;)\s*/);
  for (const part of andChain) {
    // Split each part on pipes (respecting quotes)
    const pipeParts = splitPipes(part.trim());
    for (const sub of pipeParts) {
      segments.push(stripEnvPrefix(sub.trim()));
    }
  }

  return segments.filter(Boolean);
}

function splitPipes(cmd) {
  const parts = [];
  let current = "";
  let inSQ = false;
  let inDQ = false;
  let esc = false;

  for (let i = 0; i < cmd.length; i++) {
    const c = cmd[i];
    if (esc) { current += c; esc = false; continue; }
    if (c === "\\") { esc = true; current += c; continue; }
    if (c === "'" && !inDQ) { inSQ = !inSQ; current += c; continue; }
    if (c === '"' && !inSQ) { inDQ = !inDQ; current += c; continue; }
    if (c === "|" && !inSQ && !inDQ) {
      if (current.trim()) parts.push(current.trim());
      current = "";
      continue;
    }
    current += c;
  }
  if (current.trim()) parts.push(current.trim());
  return parts;
}

function stripEnvPrefix(cmd) {
  return cmd.replace(/^(?:[A-Z_]+=\S+\s+)+/, "").trim();
}

function tokenize(cmd) {
  if (!cmd) return [];

  const tokens = [];
  let current = "";
  let inSingleQuote = false;
  let inDoubleQuote = false;
  let escape = false;

  for (let i = 0; i < cmd.length; i++) {
    const c = cmd[i];

    if (escape) {
      current += c;
      escape = false;
      continue;
    }

    if (c === "\\" && !inSingleQuote) {
      escape = true;
      current += c;
      continue;
    }

    if (c === "'" && !inDoubleQuote) {
      inSingleQuote = !inSingleQuote;
      continue;
    }

    if (c === '"' && !inSingleQuote) {
      inDoubleQuote = !inDoubleQuote;
      continue;
    }

    if (c === " " && !inSingleQuote && !inDoubleQuote) {
      if (current) tokens.push(current);
      current = "";
      continue;
    }

    current += c;
  }

  if (current) tokens.push(current);
  return tokens;
}

function extractExecArg(tokens) {
  const execFlags = ["-c", "/c", "-Command"];
  for (let i = 0; i < tokens.length - 1; i++) {
    if (execFlags.includes(tokens[i])) {
      return tokens.slice(i + 1).join(" ");
    }
  }
  return null;
}

function basename(path) {
  if (!path) return "";
  return path.replace(/^.*[\\\/]/, "").toLowerCase();
}

// ─── Output Sanitization ────────────────────────────────────────────────────

function sanitize(cmd) {
  return cmd
    .replace(/(Bearer\s+)\S+/gi, "$1[REDACTED]")
    .replace(/(api[_-]?key[=:]\s*)\S+/gi, "$1[REDACTED]")
    .replace(/(password[=:]\s*)\S+/gi, "$1[REDACTED]")
    .replace(/(token[=:]\s*)\S+/gi, "$1[REDACTED]")
    .trim();
}
