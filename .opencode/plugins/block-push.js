export const BlockPushPlugin = async () => {
  return {
    "tool.execute.before": async (input, output) => {
      if (input.tool !== "bash") return;

      try {
        const cmd = output?.args?.command || "";

        if (detectGitCommand(cmd, "push")) {
          throw new Error(
            "BLOCKED by Policy: git push is restricted to the repository owner only.\n\n" +
            "Command: " + sanitize(cmd) + "\n\n" +
            "Reason: Only humans may push to remote repositories. " +
            "Ask the user to push manually when ready."
          );
        }

        if (detectGitCommand(cmd, "revert")) {
          throw new Error(
            "BLOCKED by Policy: git revert is restricted to the repository owner only.\n\n" +
            "Command: " + sanitize(cmd) + "\n\n" +
            "Reason: Only humans may rewrite history with git revert. " +
            "Ask the user to perform the revert manually when ready."
          );
        }

        if (detectGitCommand(cmd, "update-ref")) {
          throw new Error(
            "BLOCKED by Policy: git update-ref is restricted to the repository owner only.\n\n" +
            "Command: " + sanitize(cmd) + "\n\n" +
            "Reason: Only humans may forcefully rewrite git references. " +
            "Ask the user to perform the update-ref manually when ready."
          );
        }

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
        // Re-throw blocked commands so they're enforced
        if (err.message.startsWith("BLOCKED by Policy:")) {
          throw err;
        }
        // Log other errors but don't block all commands - fail open for safety
        console.error("BlockPushPlugin error:", err.message);
      }
    },
  };
};

/**
 * Detect if a command contains a specific git subcommand
 * @param {string} cmd - The command to check
 * @param {string} gitCommand - The git subcommand to detect (e.g., "push", "revert")
 * @returns {boolean}
 */
function detectGitCommand(cmd, gitCommand) {
  const segments = parseCommand(cmd);
  for (const segment of segments) {
    if (isGitCommand(segment, gitCommand)) return true;
  }
  return false;
}

/**
 * Check if a command segment contains a specific git subcommand
 * @param {string} segment - The command segment to check
 * @param {string} gitCommand - The git subcommand to detect
 * @returns {boolean}
 */
function isGitCommand(segment, gitCommand) {
  const tokens = tokenize(segment);
  if (tokens.length === 0) return false;

  const base = basename(tokens[0]);
  if (base === "git") {
    const sub = tokens.find(t => !t.startsWith("-") && t !== "git");
    if (sub === gitCommand) return true;
  }

  // Check shell wrappers (bash, sh, zsh, cmd, powershell, pwsh)
  if (["bash", "sh", "zsh", "cmd", "powershell", "pwsh"].includes(base)) {
    const execArg = extractExecArg(tokens);
    if (execArg && detectGitCommand(execArg, gitCommand)) return true;
  }

  // Check script interpreters (python, node, ruby, perl, php)
  if (["python", "python3", "node", "ruby", "perl", "php"].includes(base)) {
    const execArg = extractExecArg(tokens);
    if (execArg && detectGitCommand(execArg, gitCommand)) return true;
  }

  return false;
}

// Keep legacy functions for backward compatibility
function detectGitPush(cmd) {
  return detectGitCommand(cmd, "push");
}

function detectGitRevert(cmd) {
  return detectGitCommand(cmd, "revert");
}

function isGitPush(segment) {
  return isGitCommand(segment, "push");
}

function isGitRevert(segment) {
  return isGitCommand(segment, "revert");
}

function parseCommand(cmd) {
  if (!cmd) return [];
  const trimmed = cmd.trim();
  if (!trimmed) return [];

  const segments = [];
  segments.push(stripEnvPrefix(trimmed));

  const andChain = trimmed.split(/\s*(?:&&|;)\s*/);
  for (const part of andChain) {
    segments.push(stripEnvPrefix(part.trim()));
  }

  return segments.filter(Boolean);
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

function sanitize(cmd) {
  return cmd
    .replace(/(Bearer\s+)\S+/gi, "$1[REDACTED]")
    .replace(/(api[_-]?key[=:]\s*)\S+/gi, "$1[REDACTED]")
    .replace(/(password[=:]\s*)\S+/gi, "$1[REDACTED]")
    .replace(/(token[=:]\s*)\S+/gi, "$1[REDACTED]")
    .trim();
}
