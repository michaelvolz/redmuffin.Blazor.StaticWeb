export const BlockPushPlugin = async () => {
  return {
    "tool.execute.before": async (input, output) => {
      if (input.tool === "bash") {
        const cmd = (output.args.command || "").toLowerCase();
        const isGitPush = cmd.includes("git") && cmd.includes("push");

        if (isGitPush) {
          throw new Error(
            "CRITICAL POLICY VIOLATION: This command should NEVER execute. " +
            "The explicit rule in AGENTS.md states: 'NEVER commit or push without explicit user permission'. " +
            "You MUST NOT push to remote repositories under any circumstances. " +
            "This is a grave mistake. Ask the user for permission before any push operation."
          );
        }
      }
    },
  };
};
