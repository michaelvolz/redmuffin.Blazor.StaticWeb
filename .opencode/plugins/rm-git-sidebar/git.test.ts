import { describe, expect, test } from "bun:test";
import {
  CATEGORIES,
  EMPTY_COUNTS,
  LABEL_TO_KEY,
  STATUS_LABEL,
  categorySome,
  categoryTotal,
  computeSessionCounts,
  parseGitCounts,
  parseAheadBehind,
} from "./git";

describe("parseGitCounts", () => {
  test("empty output returns all zeros", () => {
    const result = parseGitCounts("");
    expect(result).toEqual(EMPTY_COUNTS);
  });

  test("single unstaged modified file", () => {
    const result = parseGitCounts(" M src/foo.ts");
    expect(result.modified).toBe(1);
    expect(result.added).toBe(0);
    expect(result.deleted).toBe(0);
  });

  test("one file per category", () => {
    const output = [
      " M modified.ts",
      "A  added.ts",
      " D deleted.ts",
      "R  old.ts -> new.ts",
      "?? untracked.ts",
      "UU conflict.ts",
      "C  copied.ts",
      "T  typechanged.ts",
      "!! ignored.ts",
    ].join("\n");
    const result = parseGitCounts(output);
    expect(result.modified).toBe(1);
    expect(result.added).toBe(1);
    expect(result.deleted).toBe(1);
    expect(result.renamed).toBe(1);
    expect(result.untracked).toBe(1);
    expect(result.conflicting).toBe(1);
    expect(result.copied).toBe(1);
    expect(result.typechanged).toBe(1);
    expect(result.ignored).toBe(1);
  });

  test("staged and unstaged same file (MM)", () => {
    const result = parseGitCounts("MM src/foo.ts");
    expect(result.modified).toBe(1);
  });

  test("expanded conflict codes", () => {
    const output = [
      "DD both-deleted.ts",
      "AU added-by-us.ts",
      "UD unmerged-deleted-by-us.ts",
      "UA unmerged-added-by-them.ts",
      "DU deleted-by-us.ts",
      "AA both-added.ts",
      "UU both-modified.ts",
    ].join("\n");
    const result = parseGitCounts(output);
    expect(result.conflicting).toBe(7);
  });

  test("expanded rename codes", () => {
    const output = [
      "R  renamed.ts -> newname.ts",
      "RM renamed-modified.ts -> newmod.ts",
      "RD renamed-deleted.ts -> newdel.ts",
    ].join("\n");
    const result = parseGitCounts(output);
    expect(result.renamed).toBe(3);
  });

  test("unknown status code is ignored", () => {
    const result = parseGitCounts("XX src/foo.ts");
    expect(result).toEqual(EMPTY_COUNTS);
  });

  test("multiple lines of same category", () => {
    const result = parseGitCounts(" M a.ts\n M b.ts\n M c.ts");
    expect(result.modified).toBe(3);
  });

  test("skips branch header line from --porcelain=v1 --branch", () => {
    const output = "## main...origin/main [ahead 1]\n M dirty.ts";
    const result = parseGitCounts(output);
    expect(result.modified).toBe(1);
  });
});

describe("parseAheadBehind", () => {
  test("no branch line returns null", () => {
    const result = parseAheadBehind(" M file.ts");
    expect(result).toBeNull();
  });

  test("empty output returns null", () => {
    const result = parseAheadBehind("");
    expect(result).toBeNull();
  });

  test("no bracket (no upstream) returns null", () => {
    const result = parseAheadBehind("## main");
    expect(result).toBeNull();
  });

  test("in sync returns null", () => {
    const result = parseAheadBehind("## main...origin/main");
    expect(result).toBeNull();
  });

  test("ahead only", () => {
    const result = parseAheadBehind("## main...origin/main [ahead 3]");
    expect(result).toEqual({ ahead: 3, behind: 0 });
  });

  test("behind only", () => {
    const result = parseAheadBehind("## main...origin/main [behind 2]");
    expect(result).toEqual({ ahead: 0, behind: 2 });
  });

  test("diverged (ahead and behind)", () => {
    const result = parseAheadBehind("## main...origin/main [ahead 1, behind 2]");
    expect(result).toEqual({ ahead: 1, behind: 2 });
  });

  test("single-digit counts", () => {
    const result = parseAheadBehind("## feat/branch...origin/feat/branch [ahead 5, behind 0]");
    expect(result).toEqual({ ahead: 5, behind: 0 });
  });

  test("multi-digit counts", () => {
    const result = parseAheadBehind("## main...origin/main [ahead 42, behind 137]");
    expect(result).toEqual({ ahead: 42, behind: 137 });
  });
});

describe("computeSessionCounts", () => {
  const dir = "/home/user/project";
  const gitOutput = [
    " M src/foo.ts",
    "A  src/bar.ts",
    "?? src/baz.ts",
  ].join("\n");

  test("empty sessionFiles returns all zeros", () => {
    const result = computeSessionCounts(gitOutput, dir, new Set());
    expect(result).toEqual(EMPTY_COUNTS);
  });

  test("intersection: one match among many dirties", () => {
    const sessionFiles = new Set(["/home/user/project/src/foo.ts"]);
    const result = computeSessionCounts(gitOutput, dir, sessionFiles);
    expect(result.modified).toBe(1);
    expect(result.added).toBe(0);
    expect(result.untracked).toBe(0);
  });

  test("intersection: minimum case", () => {
    const sessionFiles = new Set(["/tmp/foo.ts"]);
    const result = computeSessionCounts(" M foo.ts\n", "/tmp", sessionFiles);
    expect(result.modified).toBe(1);
  });

  test("renamed file tracks new name", () => {
    const output = "R  old.ts -> new.ts";
    const sessionFiles = new Set(["/home/user/project/new.ts"]);
    const result = computeSessionCounts(output, dir, sessionFiles);
    expect(result.renamed).toBe(1);
  });

  test("renamed file: old name is not matched", () => {
    const output = "R  old.ts -> new.ts";
    const sessionFiles = new Set(["/home/user/project/old.ts"]);
    const result = computeSessionCounts(output, dir, sessionFiles);
    expect(result.renamed).toBe(0);
  });

  test("short line (<4 chars) is skipped", () => {
    const result = computeSessionCounts("M\n", dir, new Set(["/home/user/project/M"]));
    expect(result).toEqual(EMPTY_COUNTS);
  });

  test("empty git output returns zeros", () => {
    const result = computeSessionCounts("", dir, new Set(["/home/user/project/src/foo.ts"]));
    expect(result).toEqual(EMPTY_COUNTS);
  });

  test("skips branch header line", () => {
    const output = "## main...origin/main [ahead 1]\n M foo.ts";
    const sessionFiles = new Set(["/home/user/project/foo.ts"]);
    const result = computeSessionCounts(output, dir, sessionFiles);
    expect(result.modified).toBe(1);
  });
});

describe("constants", () => {
  test("EMPTY_COUNTS has all category keys set to 0", () => {
    for (const cat of CATEGORIES) {
      expect(EMPTY_COUNTS[cat.key]).toBe(0);
    }
  });

  test("STATUS_LABEL covers all statuses from CATEGORIES", () => {
    for (const cat of CATEGORIES) {
      for (const s of cat.statuses) {
        expect(STATUS_LABEL[s]).toBe(cat.label);
      }
    }
  });

  test("LABEL_TO_KEY maps all labels", () => {
    for (const cat of CATEGORIES) {
      expect(LABEL_TO_KEY[cat.label]).toBe(cat.key);
    }
  });

  test("CATEGORIES has exactly 9 entries", () => {
    expect(CATEGORIES.length).toBe(9);
  });

  test("untracked label is U (matching VS Code)", () => {
    const untracked = CATEGORIES.find((c) => c.key === "untracked")!;
    expect(untracked.label).toBe("U");
  });

  test("conflicting label is ! (matching VS Code)", () => {
    const conflicting = CATEGORIES.find((c) => c.key === "conflicting")!;
    expect(conflicting.label).toBe("!");
  });

  test("all tokens are valid theme tokens", () => {
    const validTokens = new Set(["success", "warning", "error", "textMuted"]);
    for (const cat of CATEGORIES) {
      expect(validTokens.has(cat.token)).toBe(true);
    }
  });
});

describe("helpers", () => {
  test("categoryTotal sums all non-zero categories", () => {
    const c = { ...EMPTY_COUNTS, modified: 3, added: 2, deleted: 1 };
    expect(categoryTotal(c)).toBe(6);
  });

  test("categoryTotal returns 0 for empty counts", () => {
    expect(categoryTotal(EMPTY_COUNTS)).toBe(0);
  });

  test("categorySome returns true when any > 0", () => {
    const c = { ...EMPTY_COUNTS, untracked: 1 };
    expect(categorySome(c)).toBe(true);
  });

  test("categorySome returns false when all zero", () => {
    expect(categorySome(EMPTY_COUNTS)).toBe(false);
  });
});
