export const BlockPushPlugin = async () => {
  return {
    "tool.execute.before": async (input, output) => {
      if (input.tool !== "bash") return;

      const cmd = output.args.command || "";
      if (detectGitPush(cmd)) {
        throw new Error(
          "BLOCKED by Policy: git push is restricted to the repository owner only.\n\n" +
          "Command: " + sanitize(cmd) + "\n\n" +
          "Reason: Only humans may push to remote repositories. " +
          "Ask the user to push manually when ready."
        );
      }
    },
  };
};

function detectGitPush(cmd) {
  const segments = parseCommand(cmd);
  for (const segment of segments) {
    if (isGitPush(segment)) return true;
  }
  return false;
}

function isGitPush(segment) {
  const tokens = tokenize(segment);
  if (tokens.length === 0) return false;

  const base = basename(tokens[0]);
  if (base === "git") {
    const sub = tokens.find(t => !t.startsWith("-") && t !== "git");
    if (sub === "push") return true;
  }

  if (["bash", "sh", "zsh", "cmd", "powershell", "pwsh"].includes(base)) {
    const execArg = extractExecArg(tokens);
    if (execArg && detectGitPush(execArg)) return true;
  }

  if (["python", "python3", "node", "ruby", "perl", "php"].includes(base)) {
    const execArg = extractExecArg(tokens);
    if (execArg && detectGitPush(execArg)) return true;
  }

  return false;
}

function parseCommand(cmd) {
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
