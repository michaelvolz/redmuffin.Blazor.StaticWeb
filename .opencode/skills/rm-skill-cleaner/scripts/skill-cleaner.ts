#!/usr/bin/env bun
import { readFileSync, readdirSync, statSync, realpathSync, accessSync, unlinkSync } from "node:fs";
import { Dirent } from "node:fs";
import { homedir, tmpdir } from "node:os";
import path from "node:path";
import { execSync } from "node:child_process";

// ────────────────────────────────── Types ──────────────────────────────────

type Skill = {
  name: string;
  baseName: string;
  description: string;
  path: string;
  realPath: string;
  dir: string;
  root: string;
  realRoot: string;
  scope: string;
  enabled: boolean;
  descChars: number;
  lineChars: number;
  lineBytes: number;
  bodyHash: string;
  bodyKey: string;
  descKey: string;
};

type Usage = { dollar: number; fileRead: number; text: number };

type Budget = {
  model: string;
  contextTokens: number;
  contextSource: string;
  effectivePercent: number | null;
  effectiveContextTokens: number | null;
  budgetPercent: number;
  budgetTokens: number;
  effectiveBudgetTokens: number | null;
  renderedLineChars: number;
  unbudgetedFullTokens: number;
  minimumTokens: number;
  budgetedTokens: number;
  charsPerToken: number;
  unbudgetedBudgetUsedRatio: number;
  budgetedBudgetUsedRatio: number;
  effectiveBudgetUsedRatio: number | null;
  unbudgetedContextUsedRatio: number;
  budgetedContextUsedRatio: number;
  effectiveContextUsedRatio: number | null;
  remainingBudgetTokens: number;
  remainingEffectiveBudgetTokens: number | null;
  includedSkills: number;
  omittedSkills: number;
  truncatedDescriptionChars: number;
  truncatedDescriptionCount: number;
};

// ──────────────────────────── CLI Args ─────────────────────────────────────

const home = homedir();
const args = new Set(process.argv.slice(2));

function argValue(name: string, fallback: string): string {
  const raw = process.argv.slice(2);
  const idx = raw.indexOf(name);
  return idx >= 0 && raw[idx + 1] ? raw[idx + 1] : fallback;
}

const months = Number(argValue("--months", "3"));
const noLogs = args.has("--no-logs");
const deepLogs = args.has("--deep-logs");
const json = args.has("--json");
const includeAll = args.has("--all");
const model = argValue("--model", "opencode");
const budgetPercent = Number(argValue("--budget-percent", "2"));
const contextTokensOverride = argValue("--context-tokens", "");
const charsPerToken = Number(argValue("--chars-per-token", "4"));
const maxLogBytes = Number(argValue("--max-log-mb", "300")) * 1024 * 1024;
const cutoffMs = Date.now() - Math.max(0, months) * 31 * 24 * 60 * 60 * 1000;
const extraRoots = process.argv
  .slice(2)
  .flatMap((arg, i, all) => (arg === "--root" && all[i + 1] ? [all[i + 1]] : []));

function expandHome(input: string): string {
  return input.replace(/^~(?=$|\/)/, home);
}

function exists(input: string): boolean {
  try { accessSync(input); return true; } catch { return false; }
}

function numberArg(value: string, fallback: number): number {
  const n = Number(value);
  return Number.isFinite(n) && n > 0 ? n : fallback;
}

// ───────────────────── OpenCode Config Parsing ─────────────────────────────

function openCodeConfigPath(): string {
  const local = ".opencode/opencode.jsonc";
  if (exists(local)) return local;
  return path.join(home, ".config/opencode/opencode.jsonc");
}

function openCodeModelContext(_modelName: string): {
  tokens: number;
  source: string;
  effectivePercent: number | null;
} {
  const override = numberArg(contextTokensOverride, 0);
  if (override > 0) return { tokens: override, source: "--context-tokens", effectivePercent: null };

  const configPath = openCodeConfigPath();
  if (!exists(configPath)) return { tokens: 393_216, source: "fallback:deepseek-v4-pro", effectivePercent: null };

  // Regex-extract provider model context limits from JSONC without full parse.
  // Matches: "limit": { ... "context": NNN, ... }
  const raw = readFileSync(configPath, "utf8");
  const contexts: number[] = [];
  // Strip line comments, then match limit.context blocks
  const cleaned = raw.replace(/\/\/.*$/gm, "");
  const limitBlockRe = /"limit"\s*:\s*\{[^}]*"context"\s*:\s*(\d+)[^}]*\}/g;
  // Also try: "context": NNN directly
  const ctxDirectRe = /"context"\s*:\s*(\d+)/g;
  let match: RegExpExecArray | null;
  while ((match = limitBlockRe.exec(cleaned)) !== null) {
    contexts.push(Number(match[1]));
  }
  // Fallback: any context value in provider section
  if (contexts.length === 0) {
    while ((match = ctxDirectRe.exec(cleaned)) !== null) {
      contexts.push(Number(match[1]));
    }
  }
  if (contexts.length > 0) {
    // Use the minimum context (most conservative for budget)
    const tokens = Math.min(...contexts);
    return { tokens, source: configPath, effectivePercent: null };
  }

  return { tokens: 393_216, source: "fallback:deepseek-v4-pro", effectivePercent: null };
}

function openCodeConfig(): Record<string, unknown> {
  const configPath = openCodeConfigPath();
  if (!exists(configPath)) return {};
  try {
    const raw = readFileSync(configPath, "utf8");
    // JSONC: strip comments, then use eval-style parse via Function (handles trailing commas)
    const cleaned = raw.replace(/\/\/.*$/gm, "").replace(/\/\*[\s\S]*?\*\//g, "");
    return JSON.parse(cleaned);
  } catch {
    return {};
  }
}

function permissionSkillDenies(): Set<string> {
  const denies = new Set<string>();
  const config = openCodeConfig();
  const permission = config.permission as Record<string, unknown> | undefined;
  if (!permission) return denies;
  const skill = permission.skill as Record<string, string> | undefined;
  if (!skill) return denies;
  for (const [pattern, rule] of Object.entries(skill)) {
    if (rule === "deny") denies.add(pattern);
  }
  return denies;
}

// ─────────────────── Skill Discovery ───────────────────────────────────────

function walkFiles(root: string, predicate: (file: string) => boolean, maxDepth = 8): string[] {
  const out: string[] = [];
  const seen = new Set<string>();
  function walk(dir: string, depth: number) {
    if (depth > maxDepth) return;
    let real = dir;
    try { real = realpathSync(dir); } catch { return; }
    if (seen.has(real)) return;
    seen.add(real);
    let entries: Dirent[];
    try { entries = readdirSync(dir, { withFileTypes: true }); } catch { return; }
    for (const entry of entries) {
      if (entry.name === "node_modules" || entry.name === ".git") continue;
      const file = path.join(dir, entry.name);
      if (entry.isDirectory() || entry.isSymbolicLink()) {
        let stat;
        try { stat = statSync(file); } catch { continue; }
        if (stat.isDirectory()) walk(file, depth + 1);
      } else if (entry.isFile() && predicate(file)) {
        out.push(file);
      }
    }
  }
  if (exists(root)) walk(root, 0);
  return out;
}

function discoverRoots(): string[] {
  const rootsByRealPath = new Map<string, string>();
  const roots: string[] = [
    path.join(home, ".config/opencode/skills"),
    path.join(home, ".cache/opencode/packages"),
    ...extraRoots.map(expandHome),
  ];

  // Project-local skills
  const cwd = process.cwd();
  const projectSkills = path.join(cwd, ".opencode/skills");
  if (exists(projectSkills)) roots.push(projectSkills);

  // Also check common project roots
  const projectsDir = path.join(home, "Projects");
  if (exists(projectsDir)) {
    for (const entry of readdirSync(projectsDir, { withFileTypes: true })) {
      if (!entry.isDirectory()) continue;
      const skillRoot = path.join(projectsDir, entry.name, ".opencode/skills");
      if (exists(skillRoot)) roots.push(skillRoot);
    }
  }

  for (const root of roots) {
    if (!exists(root)) continue;
    const real = realpathSync(root);
    const current = rootsByRealPath.get(real);
    if (!current || root.length < current.length) rootsByRealPath.set(real, root);
  }
  return [...rootsByRealPath.values()].sort();
}

function skillRootScope(root: string): string {
  const norm = root.split(path.sep).join("/");
  if (norm.includes("/.cache/opencode/packages")) return "plugin";
  if (norm.includes("/.config/opencode/skills")) return "user";
  if (norm.includes("/.opencode/skills")) return "project";
  return "extra";
}

function deletePriority(skill: Skill): number {
  // Keep: built-in (lowest number = keep first)
  if (skill.scope === "plugin") return 3; // delete plugin dupes first
  if (skill.scope === "project") return 2; // then project-local
  if (skill.scope === "user") return 1; // user is canonical
  return 4; // extra roots — delete first
}

function preferredKeepSkill(list: Skill[]): Skill {
  return [...list].sort((a, b) => {
    const byPri = deletePriority(a) - deletePriority(b);
    if (byPri !== 0) return byPri;
    return a.realPath.length - b.realPath.length || a.realPath.localeCompare(b.realPath);
  })[0]!;
}

function displayPathPriority(skill: Skill): number {
  if (skill.path === skill.realPath) return 0;
  return 1;
}

function preferredDisplaySkill(a: Skill, b: Skill): Skill {
  const byDisp = displayPathPriority(a) - displayPathPriority(b);
  if (byDisp < 0) return a;
  if (byDisp > 0) return b;
  return a.path.length <= b.path.length ? a : b;
}

function pluginPrefixFor(file: string): string | null {
  const parts = file.split(path.sep);
  const cache = parts.indexOf("packages");
  if (cache < 0) return null;
  // ~/.cache/opencode/packages/<name>@<version>/...
  const pkg = parts[cache + 1];
  if (!pkg) return null;
  const atIdx = pkg.lastIndexOf("@");
  return atIdx > 0 ? pkg.slice(0, atIdx) : pkg;
}

// ─────────────────── Frontmatter Parsing ────────────────────────────────────

function sanitizeSingleLine(value: string): string {
  return value.replace(/[\r\n\t]+/g, " ").replace(/\s+/g, " ").trim();
}

function parseYamlScalar(raw: string): string {
  const value = raw.trim();
  if ((value.startsWith('"') && value.endsWith('"')) || (value.startsWith("'") && value.endsWith("'")))
    return value.slice(1, -1);
  return value;
}

function parseFrontmatter(file: string): { name?: string; description?: string; body: string } | null {
  const text = readFileSync(file, "utf8");
  const lines = text.split(/\r?\n/);
  if (lines[0]?.trim() !== "---") return null;
  const fm: string[] = [];
  let end = -1;
  for (let i = 1; i < lines.length; i++) {
    if (lines[i]?.trim() === "---") { end = i; break; }
    fm.push(lines[i] ?? "");
  }
  if (end < 0) return null;
  let name: string | undefined;
  let description: string | undefined;
  for (let i = 0; i < fm.length; i++) {
    const line = fm[i] ?? "";
    const match = /^([A-Za-z0-9_-]+):\s*(.*)$/.exec(line);
    if (!match) continue;
    const key = match[1];
    const raw = match[2] ?? "";
    if (key === "name") name = sanitizeSingleLine(parseYamlScalar(raw));
    if (key === "description") {
      if (raw.trim() === "|" || raw.trim() === ">") {
        const block: string[] = [];
        for (let j = i + 1; j < fm.length; j++) {
          if (/^[A-Za-z0-9_-]+:\s*/.test(fm[j] ?? "")) break;
          block.push((fm[j] ?? "").replace(/^\s{2}/, ""));
        }
        description = sanitizeSingleLine(block.join(" "));
      } else {
        description = sanitizeSingleLine(parseYamlScalar(raw));
      }
    }
  }
  return { name, description, body: lines.slice(end + 1).join("\n") };
}

function discoverSkills(): Skill[] {
  const denials = permissionSkillDenies();
  const skillsByRealPath = new Map<string, Skill>();
  for (const root of discoverRoots()) {
    for (const file of walkFiles(root, (f) => path.basename(f) === "SKILL.md", 12)) {
      const parsed = parseFrontmatter(file);
      if (!parsed) continue;
      const baseName = parsed.name || path.basename(path.dirname(file));
      const pluginPrefix = pluginPrefixFor(file);
      const name = pluginPrefix ? `${pluginPrefix}:${baseName}` : baseName;

      // Check permission.skill denies
      const enabled = ![...denials].some((pattern) => {
        if (pattern.includes("*")) {
          const re = new RegExp("^" + pattern.replace(/\*/g, ".*") + "$");
          return re.test(baseName);
        }
        return pattern === baseName;
      });

      const description = parsed.description ?? "";
      const rendered = description
        ? `- ${name}: ${description} (file: ${file})`
        : `- ${name}: (file: ${file})`;
      const skill: Skill = {
        name,
        baseName,
        description,
        path: file,
        realPath: realpathSync(file),
        dir: path.dirname(file),
        root,
        realRoot: realpathSync(root),
        scope: skillRootScope(root),
        enabled,
        descChars: [...description].length,
        lineChars: [...`${rendered}\n`].length,
        lineBytes: Buffer.byteLength(`${rendered}\n`, "utf8"),
        bodyHash: fnv1a(normalizeWords(parsed.body)),
        bodyKey: normalizeWords(parsed.body),
        descKey: normalizeWords(description),
      };
      const existing = skillsByRealPath.get(skill.realPath);
      skillsByRealPath.set(skill.realPath, existing ? preferredDisplaySkill(existing, skill) : skill);
    }
  }
  return [...skillsByRealPath.values()];
}

// ─────────────────── Hashing & Similarity ────────────────────────────────────

function fnv1a(input: string): string {
  let hash = 0x811c9dc5;
  for (let i = 0; i < input.length; i++) {
    hash ^= input.charCodeAt(i);
    hash = Math.imul(hash, 0x01000193);
  }
  return (hash >>> 0).toString(16).padStart(8, "0");
}

function normalizeWords(input: string): string {
  return input
    .toLowerCase()
    .replace(/[`"'’().,;:!?/\\[\]{}_-]+/g, " ")
    .replace(/\s+/g, " ")
    .trim();
}

function wordSet(input: string): Set<string> {
  return new Set(normalizeWords(input).split(" ").filter((w) => w.length >= 2));
}

function jaccard(a: Set<string>, b: Set<string>): number {
  if (a.size === 0 && b.size === 0) return 1;
  let inter = 0;
  for (const item of a) if (b.has(item)) inter++;
  return inter / (a.size + b.size - inter);
}

function similarity(a: Skill, b: Skill): { description: number; body: number; overall: number } {
  const desc = jaccard(wordSet(a.description), wordSet(b.description));
  const body = a.bodyHash === b.bodyHash ? 1 : jaccard(wordSet(a.bodyKey), wordSet(b.bodyKey));
  return { description: desc, body, overall: body * 0.8 + desc * 0.2 };
}

function isLikelyCopy(score: { description: number; body: number }): boolean {
  return score.body >= 0.95 || (score.body >= 0.85 && score.description >= 0.85);
}

// ─────────────────── OpenCode Session Scanning ───────────────────────────────

function openCodeDBPath(): string {
  return argValue("--db-path", path.join(home, ".local/share/opencode/opencode.db"));
}

function recentSessionParts(): string {
  if (noLogs) return "";
  const db = openCodeDBPath();
  if (!exists(db)) return "";

  const tempFile = path.join(tmpdir(), `oc-sc-${process.pid}-${Date.now()}.txt`);
  try {
    // Write SQL output to temp file to avoid ENOBUFS
    execSync(
      `sqlite3 -separator '\t' "${db}" "SELECT m.time_created, p.data FROM part p JOIN message m ON p.message_id = m.id WHERE m.time_created > ${cutoffMs} AND (p.data LIKE '%\"type\":\"text\"%' OR p.data LIKE '%\"type\":\"tool\"%') ORDER BY m.time_created" > "${tempFile}"`,
      { timeout: 60000, maxBuffer: 1024 * 1024 }
    );
    const stat = statSync(tempFile);
    if (stat.size > maxLogBytes) {
      // Read only up to maxLogBytes
      const buf = Buffer.alloc(maxLogBytes);
      const fd = require("node:fs").openSync(tempFile, "r");
      require("node:fs").readSync(fd, buf, 0, maxLogBytes, 0);
      require("node:fs").closeSync(fd);
      return buf.toString("utf8");
    }
    return readFileSync(tempFile, "utf8");
  } catch {
    return "";
  } finally {
    try { unlinkSync(tempFile); } catch {}
  }
}

function scanUsage(skills: Skill[], sessionText: string): Map<string, Usage> {
  const aliases = new Map<string, string[]>();
  for (const skill of skills) {
    const values = new Set([skill.name, skill.baseName]);
    aliases.set(skill.name, [...values].map((v) => v.toLowerCase()));
  }
  const usage = new Map<string, Usage>();
  for (const skill of skills) usage.set(skill.name, { dollar: 0, fileRead: 0, text: 0 });
  if (!sessionText) return usage;

  // OpenCode invocation: skill({ name: "skill-name" })
  const invokeCounts = countTokens(
    [...sessionText.matchAll(/skill\(\s*\{\s*name:\s*"([^"]+)"/g)].map((m) => (m[1] ?? "").toLowerCase())
  );
  // File path references: skills/<name>/SKILL.md
  const pathCounts = countTokens(
    [...sessionText.matchAll(/(?:^|[/"'`\\])skills\/([^/"'`\\\s]+)\/SKILL\.md/g)].map((m) => (m[1] ?? "").toLowerCase())
  );
  // Text mentions: use/using/load $?skill-name
  const textCounts = countTokens(
    [...sessionText.matchAll(/\b(?:use|using|load|invoke|call)\s+`?\$?([A-Za-z][A-Za-z0-9_.:-]{1,80})`?/gi)].map((m) =>
      (m[1] ?? "").toLowerCase()
    )
  );
  // Also match @skill-name mentions
  const atMentions = countTokens(
    [...sessionText.matchAll(/@([a-z][a-z0-9-]{1,60})/gi)].map((m) => (m[1] ?? "").toLowerCase())
  );

  for (const [name, names] of aliases) {
    const item = usage.get(name);
    if (!item) continue;
    for (const candidate of names) {
      item.dollar += invokeCounts.get(candidate) ?? 0;
      item.fileRead += pathCounts.get(candidate) ?? 0;
      item.text += textCounts.get(candidate) ?? 0;
      item.text += atMentions.get(candidate) ?? 0;
    }
  }
  return usage;
}

function countTokens(values: string[]): Map<string, number> {
  const map = new Map<string, number>();
  for (const v of values) map.set(v, (map.get(v) ?? 0) + 1);
  return map;
}

// ─────────────────── Description Suggestions ────────────────────────────────

function suggestDescription(skill: Skill): string {
  const source = normalizeWords(`${skill.baseName} ${skill.description}`);
  const cues: string[] = [];
  const add = (label: string, pattern: RegExp) => { if (pattern.test(source) && !cues.includes(label)) cues.push(label); };
  add("OpenCode", /\bopencode|tui|plugin|session|agent\b/);
  add("Git", /\bgit\b|commit|push|pull|branch/);
  add("dotnet", /\bdotnet|csharp|blazor|nuget|roslyn\b/);
  add("config", /\bconfig|opencode\.json|toml|yaml|setup\b/);
  add("docs", /\bdoc|docs|markdown|write|review\b/);
  add("debug", /\bdebug|trace|inspect|profile|diagnos/);
  add("cleanup", /\bclean|prune|remove|delete|unused\b/);
  add("security", /\bsecurity|token|auth|supply.chain|permission\b/);
  const verbs = cues.length ? cues.slice(0, 5).join(", ") : skill.baseName.replace(/-/g, " ");
  return `${verbs}: ${shortAction(source)}.`;
}

function shortAction(source: string): string {
  if (/\bdebug|diagnos|inspect\b/.test(source)) return "debug, inspect, fix";
  if (/\bclean|prune|remove\b/.test(source)) return "scan, clean, remove";
  if (/\bconfig|setup\b/.test(source)) return "configure, validate, apply";
  if (/\bdoc|write|review\b/.test(source)) return "draft, review, publish";
  if (/\bgit|commit\b/.test(source)) return "stage, commit, verify";
  return "audit, inspect, report";
}

// ─────────────────── Budget Calculation ──────────────────────────────────────

function groupBy<T>(items: T[], key: (item: T) => string): Map<string, T[]> {
  const map = new Map<string, T[]>();
  for (const item of items) {
    const k = key(item);
    map.set(k, [...(map.get(k) ?? []), item]);
  }
  return map;
}

function tokenCost(text: string): number {
  return Math.ceil(Buffer.byteLength(text, "utf8") / 4);
}

function skillOrderRank(skill: Skill): number {
  if (skill.scope === "plugin") return 1;
  if (skill.scope === "project") return 2;
  return 3; // user
}

function orderedSkillsForBudget(skills: Skill[]): Skill[] {
  return [...skills].sort((a, b) => {
    const byScope = skillOrderRank(a) - skillOrderRank(b);
    if (byScope !== 0) return byScope;
    return a.name.localeCompare(b.name) || a.path.localeCompare(b.path);
  });
}

function renderSkillLine(skill: Skill, description: string): string {
  return description
    ? `- ${skill.name}: ${description} (file: ${skill.path})`
    : `- ${skill.name}: (file: ${skill.path})`;
}

function renderSkillDescriptionPrefix(skill: Skill, chars: number): string {
  if (chars <= 0) return "";
  return [...skill.description].slice(0, chars).join("");
}

function lineTokenCost(line: string): number { return tokenCost(`${line}\n`); }
function minimumLineTokenCost(skill: Skill): number { return lineTokenCost(renderSkillLine(skill, "")); }
function fullLineTokenCost(skill: Skill): number { return lineTokenCost(renderSkillLine(skill, skill.description)); }

function extraDescriptionCosts(skill: Skill): number[] {
  const minLine = renderSkillLine(skill, "");
  const minBytes = Buffer.byteLength(`${minLine}\n`, "utf8");
  const minCost = Math.ceil(minBytes / 4);
  const costs = [0];
  let prefixBytes = 0;
  for (const char of skill.description) {
    prefixBytes += Buffer.byteLength(char, "utf8");
    costs.push(Math.ceil(minBytes + prefixBytes + 1) / 4 - minCost);
  }
  return costs;
}

function budgetSkillCost(
  skills: Skill[], budgetTokens: number
): { fullTokens: number; minimumTokens: number; budgetedTokens: number;
     includedSkills: number; omittedSkills: number;
     truncatedDescriptionChars: number; truncatedDescriptionCount: number } {
  const ordered = orderedSkillsForBudget(skills);
  const fullTokens = ordered.reduce((sum, s) => sum + fullLineTokenCost(s), 0);
  const minTokens = ordered.reduce((sum, s) => sum + minimumLineTokenCost(s), 0);

  // Budget fits everything — no truncation
  if (fullTokens <= budgetTokens) {
    return { fullTokens, minimumTokens: minTokens, budgetedTokens: fullTokens,
             includedSkills: ordered.length, omittedSkills: 0,
             truncatedDescriptionChars: 0, truncatedDescriptionCount: 0 };
  }

  // Budget fits minimum lines but not full descriptions — equal-description truncation
  if (minTokens <= budgetTokens) {
    const remainingByIndex = ordered.map((s) => [...s.description].length);
    const allocatedByIndex = ordered.map(() => 0);
    const currentExtraCosts = ordered.map(() => 0);
    const costsByIndex = ordered.map(extraDescriptionCosts);
    let remaining = budgetTokens - minTokens;
    let changed = true;
    while (changed) {
      changed = false;
      for (let i = 0; i < ordered.length; i++) {
        if (allocatedByIndex[i] >= remainingByIndex[i]) continue;
        const nextChars = allocatedByIndex[i] + 1;
        const nextCost = costsByIndex[i]?.[nextChars] ?? currentExtraCosts[i];
        const delta = nextCost - currentExtraCosts[i];
        if (delta <= remaining) {
          allocatedByIndex[i] = nextChars;
          currentExtraCosts[i] = nextCost;
          remaining -= delta;
          changed = true;
        }
      }
    }
    const rendered = ordered.map((s, i) => renderSkillLine(s, renderSkillDescriptionPrefix(s, allocatedByIndex[i] ?? 0)));
    const truncatedChars = ordered.reduce((sum, s, i) =>
      sum + Math.max(0, [...s.description].length - (allocatedByIndex[i] ?? 0)), 0);
    const truncatedCount = ordered.filter((s, i) => (allocatedByIndex[i] ?? 0) < [...s.description].length).length;
    return {
      fullTokens, minimumTokens: minTokens,
      budgetedTokens: rendered.reduce((sum, line) => sum + lineTokenCost(line), 0),
      includedSkills: ordered.length, omittedSkills: 0,
      truncatedDescriptionChars: truncatedChars, truncatedDescriptionCount: truncatedCount,
    };
  }

  // Budget can't even fit minimum lines — omit skills
  let budgetedTokens = 0;
  let includedSkills = 0;
  let omittedSkills = 0;
  let truncatedChars = 0;
  let truncatedCount = 0;
  for (const skill of ordered) {
    const cost = minimumLineTokenCost(skill);
    if (budgetedTokens + cost <= budgetTokens) {
      budgetedTokens += cost;
      includedSkills++;
    } else {
      omittedSkills++;
    }
    const descChars = [...skill.description].length;
    truncatedChars += descChars;
    if (descChars > 0) truncatedCount++;
  }
  return {
    fullTokens, minimumTokens: minTokens, budgetedTokens,
    includedSkills, omittedSkills,
    truncatedDescriptionChars: truncatedChars, truncatedDescriptionCount: truncatedCount,
  };
}

function skillBudget(skills: Skill[]): Budget {
  const context = openCodeModelContext(model);
  const ratio = numberArg(String(charsPerToken), 4);
  const pct = numberArg(String(budgetPercent), 2);
  const renderedLineChars = skills.reduce((sum, s) => sum + s.lineChars, 0);
  const effectiveCtx = context.effectivePercent
    ? Math.floor(context.tokens * (context.effectivePercent / 100)) : null;
  const budgetToks = Math.floor(context.tokens * (pct / 100));
  const effBudgetToks = effectiveCtx ? Math.floor(effectiveCtx * (pct / 100)) : null;
  const cost = budgetSkillCost(skills, budgetToks);
  return {
    model,
    contextTokens: context.tokens, contextSource: context.source,
    effectivePercent: context.effectivePercent, effectiveContextTokens: effectiveCtx,
    budgetPercent: pct, budgetTokens: budgetToks,
    effectiveBudgetTokens: effBudgetToks,
    renderedLineChars,
    unbudgetedFullTokens: cost.fullTokens, minimumTokens: cost.minimumTokens,
    budgetedTokens: cost.budgetedTokens,
    charsPerToken: ratio,
    unbudgetedBudgetUsedRatio: cost.fullTokens / budgetToks,
    budgetedBudgetUsedRatio: cost.budgetedTokens / budgetToks,
    effectiveBudgetUsedRatio: effBudgetToks ? cost.budgetedTokens / effBudgetToks : null,
    unbudgetedContextUsedRatio: cost.fullTokens / context.tokens,
    budgetedContextUsedRatio: cost.budgetedTokens / context.tokens,
    effectiveContextUsedRatio: effectiveCtx ? cost.budgetedTokens / effectiveCtx : null,
    remainingBudgetTokens: budgetToks - cost.budgetedTokens,
    remainingEffectiveBudgetTokens: effBudgetToks ? effBudgetToks - cost.budgetedTokens : null,
    includedSkills: cost.includedSkills, omittedSkills: cost.omittedSkills,
    truncatedDescriptionChars: cost.truncatedDescriptionChars,
    truncatedDescriptionCount: cost.truncatedDescriptionCount,
  };
}

// ─────────────────── Formatting ──────────────────────────────────────────────

function formatPct(value: number): string { return `${Math.round(value * 100)}%`; }
function formatOnePct(value: number): string { return `${(value * 100).toFixed(1)}%`; }
function formatNumber(value: number): string { return Math.round(value).toLocaleString("en-US"); }

// ─────────────────── Duplicate Detection ─────────────────────────────────────

function duplicateDeleteSuggestions(groups: [string, Skill[][]][]): string[] {
  const lines: string[] = [];
  for (const [name, list] of groups.slice(0, 80)) {
    const keep = preferredKeepSkill(list);
    const candidates = list
      .filter((s) => s.realPath !== keep.realPath)
      .map((s) => ({ skill: s, score: similarity(keep, s) }))
      .filter(({ score }) => isLikelyCopy(score))
      .sort((a, b) => b.score.body - a.score.body || b.score.description - a.score.description);
    if (!candidates.length) continue;
    lines.push(`- ${name}`);
    lines.push(`  keep: ${keep.scope}: ${keep.path}`);
    for (const { skill, score } of candidates) {
      lines.push(`  delete: ${skill.scope}: ${skill.path} (similarity body=${formatPct(score.body)}, desc=${formatPct(score.description)})`);
    }
  }
  return lines.length ? lines : ["- none"];
}

// ─────────────────── Report Rendering ────────────────────────────────────────

function render(skills: Skill[], usage: Map<string, Usage>, logFileCount: number): string {
  const enabled = skills.filter((s) => s.enabled || includeAll);
  const roots = groupBy(skills, (s) => s.root);
  const byBase = [...groupBy(enabled, (s) => s.baseName.toLowerCase()).entries()]
    .filter(([, list]) => list.length > 1);
  const byBody = [...groupBy(enabled, (s) => s.bodyHash).entries()]
    .filter(([h, list]) => h !== "811c9dc5" && list.length > 1);
  const longDescs = enabled
    .filter((s) => s.descChars >= 110 || s.lineChars >= 180)
    .sort((a, b) => b.descChars - a.descChars)
    .slice(0, 30);
  const unused = enabled
    .filter((s) => {
      const item = usage.get(s.name);
      return !item || item.dollar + item.fileRead + item.text === 0;
    })
    .filter((s) => !["plugin"].includes(s.scope))
    .sort((a, b) => a.scope.localeCompare(b.scope) || a.name.localeCompare(b.name))
    .slice(0, 80);

  const totalLineChars = enabled.reduce((sum, s) => sum + s.lineChars, 0);
  const totalDescChars = enabled.reduce((sum, s) => sum + s.descChars, 0);
  const budget = skillBudget(enabled);
  const lines: string[] = [];

  lines.push("# Skill Cleaner Report", "");
  lines.push(`generated: ${new Date().toISOString()}`);
  lines.push(`months: ${months}`);
  lines.push(`skills: ${skills.length} discovered, ${enabled.length} enabled`);
  lines.push(`description_chars: ${totalDescChars}`);
  lines.push(`rendered_line_chars: ${totalLineChars}`);
  lines.push(`log_source: ${openCodeDBPath()} (sqlite sessions)`);
  lines.push(`log_files_equivalent: ${logFileCount}`, "");

  // Budget
  lines.push("## Skill Budget", "");
  lines.push(`model: ${budget.model}`);
  lines.push(`context_tokens: ${formatNumber(budget.contextTokens)}`);
  lines.push(`context_source: ${budget.contextSource}`);
  lines.push(`${budget.budgetPercent}%_budget_tokens: ${formatNumber(budget.budgetTokens)}`);
  lines.push(`cost_rule: ceil(utf8_bytes / ${budget.charsPerToken})`);
  lines.push(`unbudgeted_full_tokens: ${formatNumber(budget.unbudgetedFullTokens)}`);
  lines.push(`minimum_no_description_tokens: ${formatNumber(budget.minimumTokens)}`);
  lines.push(`budgeted_tokens_used: ${formatNumber(budget.budgetedTokens)}`);
  lines.push(`used_of_2%_budget: ${formatOnePct(budget.budgetedBudgetUsedRatio)}`);
  lines.push(`unbudgeted_used_of_2%_budget: ${formatOnePct(budget.unbudgetedBudgetUsedRatio)}`);
  lines.push(`used_of_context: ${formatOnePct(budget.budgetedContextUsedRatio)}`);
  lines.push(`remaining_2%_budget_tokens: ${formatNumber(budget.remainingBudgetTokens)}`);
  lines.push(`included_skills_after_budget: ${budget.includedSkills}`);
  lines.push(`omitted_skills_after_budget: ${budget.omittedSkills}`);
  lines.push(`truncated_description_chars: ${formatNumber(budget.truncatedDescriptionChars)}`);
  if (budget.effectiveContextTokens && budget.remainingEffectiveBudgetTokens != null) {
    lines.push(`effective_context_tokens: ${formatNumber(budget.effectiveContextTokens)} (${budget.effectivePercent}%)`);
    lines.push(`effective_2%_budget_tokens: ${formatNumber(budget.effectiveBudgetTokens!)}`);
    lines.push(`used_of_effective_2%_budget: ${formatOnePct(budget.effectiveBudgetUsedRatio ?? 0)}`);
    lines.push(`remaining_effective_2%_budget_tokens: ${formatNumber(budget.remainingEffectiveBudgetTokens)}`);
  }
  lines.push("");

  // Description candidates
  lines.push("## Description Candidates", "");
  for (const skill of longDescs) {
    lines.push(`- ${skill.name}`);
    lines.push(`  path: ${skill.path}`);
    lines.push(`  chars: desc=${skill.descChars}, line=${skill.lineChars}`);
    lines.push(`  current: ${skill.description}`);
    lines.push(`  suggested: ${suggestDescription(skill)}`);
  }
  if (!longDescs.length) lines.push("- none");
  lines.push("");

  // Duplicates by name
  lines.push("## Duplicates By Name", "");
  for (const [name, list] of byBase.slice(0, 40)) {
    lines.push(`- ${name}`);
    const keep = preferredKeepSkill(list);
    lines.push(`  keep-default: ${keep.scope}: ${keep.path}`);
    for (const skill of list) {
      const score = skill.realPath === keep.realPath ? { body: 1, description: 1 } : similarity(keep, skill);
      lines.push(`  - ${skill.scope}: ${skill.path} (body=${formatPct(score.body)}, desc=${formatPct(score.description)})`);
    }
  }
  if (!byBase.length) lines.push("- none");
  lines.push("");

  // Delete suggestions
  lines.push("## Duplicate Delete Suggestions", "");
  lines.push(...duplicateDeleteSuggestions(
    [...groupBy(enabled, (s) => s.baseName.toLowerCase()).entries()]
      .filter(([, list]) => list.length > 1)
      .map(([name, list]) => [name, list] as [string, Skill[]])
  ));
  lines.push("");

  // Body hash duplicates
  lines.push("## Duplicates By Body Hash", "");
  for (const [, list] of byBody.slice(0, 30)) {
    lines.push(`- ${list.map((s) => s.name).join(", ")}`);
    for (const skill of list) lines.push(`  - ${skill.scope}: ${skill.path}`);
  }
  if (!byBody.length) lines.push("- none");
  lines.push("");

  // Unused
  lines.push("## Unused Candidates", "");
  for (const skill of unused) {
    const item = usage.get(skill.name) ?? { dollar: 0, fileRead: 0, text: 0 };
    lines.push(`- ${skill.name}: ${skill.scope}; $=${item.dollar}, reads=${item.fileRead}, text=${item.text}; ${skill.path}`);
  }
  if (!unused.length) lines.push("- none");
  lines.push("");

  // Root summary
  lines.push("## Root Summary", "");
  for (const [root, list] of [...roots.entries()].sort((a, b) => b[1].length - a[1].length)) {
    const disabled = list.filter((s) => !s.enabled).length;
    lines.push(`- ${root}: ${list.length} skills${disabled ? `, ${disabled} disabled` : ""}`);
  }
  return lines.join("\n");
}

// ─────────────────── Main ────────────────────────────────────────────────────

const skills = discoverSkills();
const sessionText = recentSessionParts();
const usage = scanUsage(skills, sessionText);
const logFileCount = sessionText ? 1 : 0; // SQL output is one logical source
const considered = skills.filter((s) => s.enabled || includeAll);
const budget = skillBudget(considered);
const output = json
  ? JSON.stringify({ skills, usage: Object.fromEntries(usage), logFiles: [openCodeDBPath()], budget }, null, 2)
  : render(skills, usage, logFileCount);
console.log(output);
