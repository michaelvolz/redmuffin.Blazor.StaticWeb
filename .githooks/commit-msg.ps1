param([string]$CommitMsgFile)

$commitMsg = Get-Content $CommitMsgFile -Raw

$lines = $commitMsg -split "`n"
$title = $lines[0]

if ($title -notmatch '^(feat|fix|docs|style|refactor|perf|test|chore|security|ci|config|revert)(\([a-z0-9-]+\))?!?: .+') {
    Write-Error "ERROR: Commit title must follow format: <type>(<scope>): <description>"
    Write-Error "Example: feat(blazor): add new navigation component"
    Write-Error ""
    Write-Error "Valid types: feat, fix, docs, style, refactor, perf, test, chore, security, ci, config, revert"
    exit 1
}

if ($title.Length -gt 112) {
    Write-Error "ERROR: Commit title exceeds 112 characters (current: $($title.Length))"
    exit 1
}

if ($lines.Count -le 1) {
    if ($title -match '^chore\(deps\):') { exit 0 }
    if ($title -match '^Merge') { exit 0 }
    if ($title -match '^Revert') { exit 0 }

    Write-Error "ERROR: Commit message must have a body explaining the change."
    Write-Error "Add a blank line after the title and bullet points describing:"
    Write-Error "  - What was changed"
    Write-Error "  - Why it was changed"
    Write-Error "  - Any breaking changes"
    exit 1
}

if ($lines.Count -gt 2) {
    $bodyLines = $lines[2..($lines.Count - 1)]
    $body = $bodyLines -join "`n"
    if ($body -notmatch '^\s*[-*]\s+') {
        Write-Error "ERROR: Commit body must use bullet points, not full sentences."
        Write-Error "Use '-' or '*' at the start of each line for bullet points."
        exit 1
    }
}

exit 0
