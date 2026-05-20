import { resolve as pathResolve, sep } from "node:path";

// --- Theme token type ---

export type ThemeToken = "success" | "warning" | "error" | "textMuted";

// --- Category definitions — single source of truth ---

export const CATEGORIES = [
  { key: "modified" as const,    statuses: ["M ", " M", "MM"],                        label: "M", token: "warning" as const },
  { key: "added" as const,       statuses: ["A ", "AM"],                              label: "A", token: "success" as const },
  { key: "deleted" as const,     statuses: ["D ", " D"],                              label: "D", token: "error" as const },
  { key: "renamed" as const,     statuses: ["R ", "RM", "RD"],                        label: "R", token: "success" as const },
  { key: "untracked" as const,   statuses: ["??"],                                    label: "U", token: "success" as const },
  { key: "conflicting" as const, statuses: ["DD", "AU", "UD", "UA", "DU", "AA", "UU"], label: "!", token: "error" as const },
  { key: "copied" as const,      statuses: ["C "],                                    label: "C", token: "success" as const },
  { key: "typechanged" as const, statuses: ["T "],                                    label: "T", token: "warning" as const },
  { key: "ignored" as const,     statuses: ["!!"],                                    label: "I", token: "textMuted" as const },
] as const;

export type CategoryKey = (typeof CATEGORIES)[number]["key"];
export type GitCounts = Record<CategoryKey, number>;

// Build STATUS_LABEL, LABEL_TO_KEY, and EMPTY_COUNTS from CATEGORIES
export const STATUS_LABEL: Record<string, string> = {};
export const LABEL_TO_KEY: Record<string, CategoryKey> = {};
for (const cat of CATEGORIES) {
  for (const s of cat.statuses) {
    STATUS_LABEL[s] = cat.label;
  }
  LABEL_TO_KEY[cat.label] = cat.key;
}

export const EMPTY_COUNTS: GitCounts = Object.fromEntries(
  CATEGORIES.map((c) => [c.key, 0]),
) as GitCounts;

export function categoryTotal(c: GitCounts): number {
  return CATEGORIES.reduce((sum, cat) => sum + c[cat.key], 0);
}

export function categorySome(c: GitCounts): boolean {
  return CATEGORIES.some((cat) => c[cat.key] > 0);
}

// --- Status parsing ---

export function parseGitCounts(output: string): GitCounts {
  const counts: GitCounts = { ...EMPTY_COUNTS };
  if (!output) return counts;
  const lines = output.trimEnd().split("\n").filter(Boolean);
  for (const line of lines) {
    // Skip branch header line from --porcelain=v1 --branch output
    if (line.startsWith("## ")) continue;
    const code = line.substring(0, 2);
    const label = STATUS_LABEL[code] || code.trim() || "?";
    const key = LABEL_TO_KEY[label];
    if (key) counts[key]++;
  }
  return counts;
}

export function computeSessionCounts(
  output: string,
  dir: string,
  sessionFiles: ReadonlySet<string>,
): GitCounts {
  const counts: GitCounts = { ...EMPTY_COUNTS };
  if (!output || sessionFiles.size === 0) return counts;

  const lines = output.trimEnd().split("\n").filter(Boolean);
  for (const line of lines) {
    // Skip branch header line
    if (line.startsWith("## ")) continue;
    if (line.length < 4) continue;

    const code = line.substring(0, 2);
    const label = STATUS_LABEL[code] || code.trim() || "?";

    let relPath = line.substring(3).trim();
    if (relPath.includes(" -> ")) {
      relPath = relPath.split(" -> ")[1];
    }

    const absPath = pathResolve(dir, relPath);
    // Match exactly, or for directory entries (trailing / from git status)
    // check if any session file is inside that directory
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
      if (key) counts[key]++;
    }
  }
  return counts;
}

// --- Stash info ---

export interface StashInfo {
  count: number;
  latestFileCount: number;
  /** Unix timestamp of the oldest stash (for age-based coloring). null when no stashes. */
  oldestTimestamp: number | null;
}

/**
 * Parse stash list output (lines of Unix timestamps from `git stash list --format="%ct"`)
 * and latest file count. Returns null when zero stashes exist.
 */
export function buildStashInfo(
  timestampsOutput: string,
  latestShowOutput: string,
): StashInfo | null {
  const timestamps = timestampsOutput.trim().split("\n").filter(Boolean).map(Number).filter((n) => !isNaN(n));
  if (timestamps.length === 0) return null;
  const latestFileCount = latestShowOutput.trim()
    ? latestShowOutput.trim().split("\n").length
    : 0;
  return {
    count: timestamps.length,
    latestFileCount,
    oldestTimestamp: Math.min(...timestamps),
  };
}

// --- Ahead/behind parsing ---

export interface AheadBehind {
  ahead: number;
  behind: number;
}

/**
 * Parse ahead/behind counts from git status --porcelain=v1 --branch output.
 * The first line is a branch header: "## main...origin/main [ahead 1, behind 2]".
 * Returns null when no upstream is configured or no ahead/behind info is present.
 */
export function parseAheadBehind(output: string): AheadBehind | null {
  if (!output) return null;
  const firstLine = output.split("\n")[0];
  if (!firstLine || !firstLine.startsWith("## ")) return null;
  // Only parse if there's bracket content (upstream info exists)
  if (!firstLine.includes("[")) return null;

  const aheadMatch = firstLine.match(/ahead (\d+)/);
  const behindMatch = firstLine.match(/behind (\d+)/);

  const ahead = aheadMatch ? parseInt(aheadMatch[1], 10) : 0;
  const behind = behindMatch ? parseInt(behindMatch[1], 10) : 0;

  if (ahead === 0 && behind === 0) return null;

  return { ahead, behind };
}
