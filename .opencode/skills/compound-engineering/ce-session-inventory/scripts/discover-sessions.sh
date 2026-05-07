#!/usr/bin/env bash
# Discover session files across Claude Code, Codex, Cursor, and OpenCode.
#
# Usage: discover-sessions.sh <repo-name> <days> [--platform claude|codex|cursor|opencode]
#
# Outputs one file path per line. Safe in both bash and zsh (all globs guarded).
# Pass output to extract-metadata.py:
#   python3 extract-metadata.py --cwd-filter <repo-name> $(bash discover-sessions.sh <repo-name> 7)
#
# Arguments:
#   repo-name  Folder name of the repo (e.g., "my-repo"). Used for directory matching.
#   days       Scan window in days (e.g., 7). Files older than this are skipped.
#   --platform Restrict to a single platform. Omit to search all.

set -euo pipefail

REPO_NAME="${1:?Usage: discover-sessions.sh <repo-name> <days> [--platform claude|codex|cursor|opencode]}"
DAYS="${2:?Usage: discover-sessions.sh <repo-name> <days> [--platform claude|codex|cursor|opencode]}"
PLATFORM="${4:-all}"

# Parse optional --platform flag
shift 2
while [ $# -gt 0 ]; do
    case "$1" in
        --platform) PLATFORM="$2"; shift 2 ;;
        *) shift ;;
    esac
done

# --- Claude Code ---
discover_claude() {
    local base="$HOME/.claude/projects"
    [ -d "$base" ] || return 0

    # Find all project dirs matching repo name
    for dir in "$base"/*"$REPO_NAME"*/; do
        [ -d "$dir" ] || continue
        find "$dir" -maxdepth 1 -name "*.jsonl" -mtime "-${DAYS}" 2>/dev/null
    done
}

# --- Codex ---
discover_codex() {
    for base in "$HOME/.codex/sessions" "$HOME/.agents/sessions"; do
        [ -d "$base" ] || continue

        # Use mtime-based discovery (consistent with Claude/Cursor) so that
        # sessions started before the scan window but still active within it
        # are not missed.
        find "$base" -name "*.jsonl" -mtime "-${DAYS}" 2>/dev/null
    done
}

# --- Cursor ---
discover_cursor() {
    local base="$HOME/.cursor/projects"
    [ -d "$base" ] || return 0

    for dir in "$base"/*"$REPO_NAME"*/; do
        [ -d "$dir" ] || continue
        local transcripts="$dir/agent-transcripts"
        [ -d "$transcripts" ] || continue
        find "$transcripts" -name "*.jsonl" -mtime "-${DAYS}" 2>/dev/null
    done
}

# --- OpenCode ---
# OpenCode stores sessions in SQLite (~/.local/share/opencode/opencode.db).
# There are no JSONL files on disk, so we use a temp-export bridge:
# export each matching session as a temporary JSONL file and output its path.
discover_opencode() {
    local db="$HOME/.local/share/opencode/opencode.db"
    [ -f "$db" ] || return 0

    command -v sqlite3 >/dev/null 2>&1 || return 0

    # Calculate cutoff in milliseconds (DAYS ago from now)
    local cutoff_ms
    cutoff_ms=$(($(date +%s) * 1000 - DAYS * 86400 * 1000))

    # Query matching sessions
    local session_ids
    session_ids=$(sqlite3 "$db" \
        "SELECT id FROM session
         WHERE directory LIKE '%${REPO_NAME}%'
         AND time_created >= ${cutoff_ms}
         ORDER BY time_created DESC" 2>/dev/null) || return 0

    for sid in $session_ids; do
        local tmpfile
        tmpfile=$(mktemp -t "oc-session-${sid}-XXXXXX.jsonl")

        # Get session metadata for header line
        local sdir stitle sver sts
        sdir=$(sqlite3 "$db" "SELECT directory FROM session WHERE id = '${sid}'" 2>/dev/null) || sdir=""
        stitle=$(sqlite3 "$db" "SELECT title FROM session WHERE id = '${sid}'" 2>/dev/null) || stitle=""
        sver=$(sqlite3 "$db" "SELECT version FROM session WHERE id = '${sid}'" 2>/dev/null) || sver=""
        sts=$(sqlite3 "$db" "SELECT time_created FROM session WHERE id = '${sid}'" 2>/dev/null) || sts="0"
        # Convert ms timestamp to ISO 8601
        local iso_ts
        if [ -n "$sts" ] && [ "$sts" != "0" ]; then
            iso_ts=$(date -d "@$(( sts / 1000 ))" -Iseconds 2>/dev/null || echo "")
        fi

        # Write session meta header line for platform detection (Section 9.2: role + agent/model keys)
        printf '{"role":"user","agent":"opencode","model":"%s","sessionId":"%s","timestamp":"%s","directory":"%s","title":"%s","version":"%s"}\n' \
            "opencode" "$sid" "$iso_ts" "$sdir" "$stitle" "$sver" >> "$tmpfile"

        # Export messages and parts as JSONL
        sqlite3 "$db" \
            "SELECT json_object(
                'type', CASE WHEN m.data->>'role' = 'user' THEN 'user' ELSE 'assistant' END,
                'message', json_object(
                    'role', m.data->>'role',
                    'content', (
                        SELECT json_group_array(
                            json_object('type', p.data->>'type', 'text', p.data->>'text')
                        )
                        FROM part p
                        WHERE p.message_id = m.id
                        AND p.data->>'type' IN ('text', 'tool')
                        ORDER BY p.time_created
                    )
                ),
                'timestamp', datetime(m.time_created / 1000, 'unixepoch'),
                'sessionId', m.session_id
            )
            FROM message m
            WHERE m.session_id = '${sid}'
            ORDER BY m.time_created" 2>/dev/null >> "$tmpfile" || {
                rm -f "$tmpfile"
                continue
            }
        # Only output if the file has content
        if [ -s "$tmpfile" ]; then
            echo "$tmpfile"
        else
            rm -f "$tmpfile"
        fi
    done
}

# --- Dispatch ---
case "$PLATFORM" in
    claude)    discover_claude ;;
    codex)     discover_codex ;;
    cursor)    discover_cursor ;;
    opencode)  discover_opencode ;;
    all)
        discover_claude
        discover_codex
        discover_cursor
        discover_opencode
        ;;
    *)
        echo "Unknown platform: $PLATFORM" >&2
        exit 1
        ;;
esac
