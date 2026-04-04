---
description: Switch to master and fast-forward merge the current branch
---

Switch to master and fast-forward merge the current branch into it. Local only — no remote interaction.

```powershell
$branch = git branch --show-current; if ($branch -eq 'master') { Write-Output 'Already on master.'; return }; git checkout master; git merge --ff-only $branch
```

Captures the current branch name before switching, so it always merges the right branch regardless of reflog state.

Report the result. If fast-forward fails (master has diverged commits), report the conflict and stop — do not force or create a merge commit.
