{
"reviewer": "scope-guardian",
"findings": [],
"residual_risks": [
"Mutation manifest embeds metadata as source-code comment blocks (matching clj-mutate), so every mutation run modifies git-tracked files. Despite in-memory restore, manifest append is persistent — accidental commits of manifested files are a workflow risk the plan acknowledges but delegates to implementer discipline.",
"MutationDiscoverer and MutationApplicator must traverse the syntax tree identically for site indices to match. The plan flags this as SYNC WARNING in both U1 and U3 and suggests extracting a shared base walker, but leaves the resolution choice to implementation. This is the highest-maintenance coupling in the design.",
"In-place mutation with in-memory restore (U4) handles normal execution but not process crashes mid-write. System-Wide Impact says a file-system backup/restore pattern will be implemented, but no implementation unit explicitly allocates saving a .bak file or checking for leftover backups on startup. The U4 description only covers in-memory restore.",
"--since-last-run differential mode (U5) relies on Roslyn NormalizeWhitespace for hash-stable member comparison. Whitespace normalization can produce different results across Roslyn versions — the plan accepts a small false-positive rate (matching clj-mutate), but version-locked CI vs. developer SDK mismatch could surface surprising differential results."
],
"deferred_questions": [
"Scope Boundaries list --update-manifest as deferred to follow-up, but U6 step 11 writes the manifest inline after every mutation run. Is the deferred item only the standalone --update-manifest CLI flag (rewrite manifest without testing), or should inline manifest writing also be deferred? Current plan does both without flagging the inconsistency."
]
}
