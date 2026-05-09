#!/usr/bin/env pwsh
# cleanup-sessions.ps1 — Delete OpenCode sessions older than N days
# Invoke with: pwsh -NoProfile .config/opencode/scripts/cleanup-sessions.ps1 [-Days N] [-WhatIf]
param(
    [int]$Days = 5,
    [switch]$WhatIf
)

function Invoke-SessionCleanup {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [int]$Days = 5
    )

    # --- Dependency check ---
    if (-not (Get-Command opencode -ErrorAction SilentlyContinue)) {
        throw "opencode not found on PATH. Install OpenCode first: https://github.com/sst/open-code"
    }

    # --- Parameter validation ---
    if ($Days -le 0) {
        throw "Days must be positive (got: $Days)"
    }

    # --- Query old sessions ---
    $cutoffMs = (Get-CurrentEpoch) - ($Days * 86400 * 1000)
    $query = "SELECT id, title, time_updated, parent_id FROM session WHERE time_updated < $cutoffMs ORDER BY time_updated DESC"
    $sessions = Invoke-OpenCodeDb -Query $query

    # --- Current session detection ---
    $currentSessionId = Get-CurrentSessionId -CandidateSessions $sessions
    if ($currentSessionId) {
        $sessions = @($sessions | Where-Object { $_.id -ne $currentSessionId })
        Write-Host "Excluded current session: $currentSessionId"
    }

    if (-not $sessions -or $sessions.Count -eq 0) {
        Write-Host "No sessions older than $Days days to delete."
        return
    }

    # --- Forked-tree safety: skip sessions with recent children ---
    $treesToSkip = Find-SessionTreeWithRecentChild -CandidateSessions $sessions -CutoffMs $cutoffMs
    if ($treesToSkip.Count -gt 0) {
        $skipIds = $treesToSkip | ForEach-Object { $_.id }
        $sessions = @($sessions | Where-Object { $_.id -notin $skipIds })
    }

    if (-not $sessions -or $sessions.Count -eq 0) {
        Write-Host "No sessions to delete (all trees preserved due to recent children)."
        return
    }

    # --- WhatIf preview ---
    if ($WhatIfPreference) {
        Write-Host "Would delete $($sessions.Count) session(s) older than $Days days:"
        foreach ($s in $sessions) {
            $ageDays = [math]::Round(((Get-CurrentEpoch) - $s.time_updated) / 86400000.0, 1)
            Write-Host "  $($s.id)  $($s.title)  (last touched ${ageDays}d ago)"
        }
        return
    }

    # --- Deletion loop ---
    $deleted = 0
    $failed  = 0
    $failureDetails = @()

    foreach ($s in $sessions) {
        if ($PSCmdlet.ShouldProcess($s.id, "Delete session: $($s.title)")) {
            try {
                Invoke-OpenCodeDelete -SessionId $s.id
                Write-Host "Deleted: $($s.id)  $($s.title)"
                $deleted++
            }
            catch {
                Write-Host "Failed: $($s.id)  $($s.title)  — $($_.Exception.Message)"
                $failed++
                $failureDetails += "$($s.id): $($_.Exception.Message)"
            }
        }
    }

    # --- Summary ---
    Write-Host "$deleted deleted, $failed failed"
    if ($failed -gt 0) {
        $script:ExitCode = 1
    }
}

# --- Helper: current Unix millisecond timestamp ---
function Get-CurrentEpoch {
    return [int64]((Get-Date).ToUniversalTime() - (Get-Date "1970-01-01Z")).TotalMilliseconds
}

# --- Helper: find session trees that have a recent child ---
function Find-SessionTreeWithRecentChild {
    param([array]$CandidateSessions, [int64]$CutoffMs)

    $parentIds = @($CandidateSessions | Where-Object { -not $_.parent_id } | ForEach-Object { $_.id })
    if ($parentIds.Count -eq 0) { return @() }

    $idList = ($parentIds | ForEach-Object { "'$_'" }) -join ","
    $childQuery = "SELECT id, title, time_updated, parent_id FROM session WHERE parent_id IN ($idList) AND time_updated >= $CutoffMs"
    $recentChildren = Invoke-OpenCodeDb -Query $childQuery

    if (-not $recentChildren -or $recentChildren.Count -eq 0) { return @() }

    $affectedParents = $recentChildren | ForEach-Object { $_.parent_id } | Select-Object -Unique
    $treesToSkip = @($CandidateSessions | Where-Object { $_.id -in $affectedParents -or $_.parent_id -in $affectedParents })

    foreach ($tree in $treesToSkip) {
        Write-Host "Skipped: $($tree.id)  $($tree.title)  (tree has recent child)"
    }

    return $treesToSkip
}

# --- Helper: detect the current OpenCode session ---
function Get-CurrentSessionId {
    param([array]$CandidateSessions)

    if (-not $CandidateSessions -or $CandidateSessions.Count -eq 0) { return $null }

    # Query ALL sessions (not just old ones) to find the truly most-recent
    $allQuery = "SELECT id, time_updated FROM session ORDER BY time_updated DESC LIMIT 1"
    $latest = Invoke-OpenCodeDb -Query $allQuery
    if (-not $latest -or $latest.Count -eq 0) { return $null }
    $latestId = $latest[0].id
    $latestTime = $latest[0].time_updated

    # Safeguard 1: PID check
    $openCodePid = Get-OpenCodePid
    if (-not $openCodePid) {
        Write-Verbose "OpenCode process not running — cannot verify current session. Skipping auto-exclusion."
        return $null
    }

    # Safeguard 2: Recency guard (must be touched within last 10 min)
    $tenMinMs = 10 * 60 * 1000
    if ((Get-CurrentEpoch) - $latestTime -gt $tenMinMs) {
        Write-Verbose "Most recent session ($latestId) not touched in 10+ minutes. Skipping auto-exclusion."
        return $null
    }

    Write-Verbose "Detected current session: $latestId"
    return $latestId
}

# --- Helper: get the OpenCode process PID ---
function Get-OpenCodePid {
    if ($env:OPENCODE_PID) {
        return [int]$env:OPENCODE_PID
    }
    # Fallback: check running processes. On Linux, Get-Process may fail
    # if the binary name doesn't match exactly. Prefer env:OPENCODE_PID.
    try {
        $proc = Get-Process -Name opencode -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($proc) { return $proc.Id }
    } catch {
        Write-Verbose "Get-Process fallback failed: $_"
    }
    return $null
}

# --- Helper: delete a single session ---
function Invoke-OpenCodeDelete {
    param([string]$SessionId)
    $result = & opencode session delete $SessionId 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to delete $SessionId : $result"
    }
}

# --- Helper: query the session database ---
function Invoke-OpenCodeDb {
    param([string]$Query)
    $output = & opencode db $Query --format tsv 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "opencode db failed: $output"
    }
    $lines = $output -split "`n" | Where-Object { $_ -ne '' }
    if ($lines.Count -eq 0) { return @() }
    $start = 0
    if ($lines[0] -match '^id\s') { $start = 1 }
    $results = @()
    for ($i = $start; $i -lt $lines.Count; $i++) {
        $cols = $lines[$i] -split "`t"
        $results += [PSCustomObject]@{
            id           = $cols[0]
            title        = $cols[1]
            time_updated = [int64]$cols[2]
            parent_id    = if ($cols.Count -ge 4 -and $cols[3]) { $cols[3] } else { $null }
        }
    }
    return $results
}

# Entry point: only run when invoked as script (not dot-sourced for tests)
$script:ExitCode = 0
if ($MyInvocation.InvocationName -ne '.') {
    Invoke-SessionCleanup -Days $Days -WhatIf:$WhatIf
    exit $script:ExitCode
}
