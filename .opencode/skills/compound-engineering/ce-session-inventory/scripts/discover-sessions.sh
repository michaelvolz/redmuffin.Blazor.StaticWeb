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
discover_opencode() {
    local db="$HOME/.local/share/opencode/opencode.db"
    [ -f "$db" ] || return 0

    local now_sec cutoff_ms
    now_sec=$(date +%s)
    cutoff_ms=$(( (now_sec - DAYS * 86400) * 1000 ))

    # Temp directory for exported JSONL — clean stale files first
    local tmpdir="/tmp/opencode-sessions"
    rm -rf "$tmpdir"
    mkdir -p "$tmpdir"

    # Escape LIKE wildcards in repo name before embedding in pattern
    local escaped_repo
    escaped_repo=$(printf '%s' "$REPO_NAME" | sed 's/[%_]/\\&/g')

    # Query matching sessions via JOIN with project table for worktree
    # matching.  Uses sqlite3 parameter binding to prevent SQL injection
    # from repo names containing single quotes.
    sqlite3 "$db" \
        ".param set :repo \"%${escaped_repo}%\"" \
        ".param set :cutoff \"$cutoff_ms\"" \
        "SELECT s.id FROM session s JOIN project p ON s.project_id = p.id
         WHERE (p.worktree LIKE :repo OR s.directory LIKE :repo OR s.path LIKE :repo)
           AND s.time_created >= :cutoff
         ORDER BY s.time_created DESC" \
    | while IFS= read -r session_id; do
        [ -z "$session_id" ] && continue
        local tmpfile="${tmpdir}/${session_id}.jsonl"

        # Export session as Claude Code-compatible JSONL.
        # Quoted heredoc (<<'PYEOF') disables bash expansion — all
        # $ signs in the Python code are literal.
        OP_SESSION_ID="$session_id" OP_OUTPUT="$tmpfile" OP_DB="$db" python3 <<'PYEOF'
import sqlite3, json, os, sys
from datetime import datetime, timezone

session_id = os.environ['OP_SESSION_ID']
tmpfile = os.environ['OP_OUTPUT']
db_path = os.environ['OP_DB']

conn = sqlite3.connect(db_path)
conn.row_factory = sqlite3.Row

# Get session metadata
sess = conn.execute(
    'SELECT * FROM session WHERE id = ?', (session_id,)
).fetchone()
if not sess:
    conn.close()
    sys.exit(0)

# Get messages with parts, ordered by message time then part time
rows = conn.execute('''
    SELECT m.id as msg_id, m.data as msg_data, m.time_created as msg_time,
           p.id as part_id, p.data as part_data, p.time_created as part_time
    FROM message m
    LEFT JOIN part p ON p.message_id = m.id
    WHERE m.session_id = ?
    ORDER BY m.time_created ASC, p.time_created ASC
''', (session_id,)).fetchall()

# Group parts by message, skipping non-content part types
SKIP_TYPES = {'reasoning', 'step-start', 'step-finish',
              'compaction', 'patch', 'file'}

msgs = {}
for row in rows:
    mid = row['msg_id']
    if mid not in msgs:
        try:
            mdata = json.loads(row['msg_data'])
        except (json.JSONDecodeError, TypeError):
            continue
        msgs[mid] = {
            'role': mdata.get('role', ''),
            'ts': row['msg_time'],
            'agent': mdata.get('agent', ''),
            'model': mdata.get('model', {}),
            'parts': []
        }
    if row['part_id']:
        try:
            pdata = json.loads(row['part_data'])
        except (json.JSONDecodeError, TypeError):
            continue
        if pdata.get('type', '') not in SKIP_TYPES:
            msgs[mid]['parts'].append(pdata)

if not msgs:
    conn.close()
    sys.exit(0)

# Emit Claude Code-compatible JSONL
wrote_file = False
with open(tmpfile, 'w') as out:
    first_msg = True
    msg_ids_sorted = sorted(msgs.keys(), key=lambda m: msgs[m]['ts'])

    for mid in msg_ids_sorted:
        m = msgs[mid]
        ts_iso = datetime.fromtimestamp(m['ts'] / 1000,
                                        tz=timezone.utc).isoformat()
        role = m['role']

        content = []
        tool_results = []

        for part in m['parts']:
            ptype = part.get('type', '')
            if ptype == 'text':
                content.append({
                    'type': 'text',
                    'text': part.get('text', '')
                })
            elif ptype == 'tool':
                call_id = part.get('callID', '')
                if not call_id:
                    # Skip tool parts without callID — cannot pair
                    # tool_use/tool_result without a matching ID
                    continue
                tool_name = part.get('tool', 'unknown')
                state = part.get('state') or {}
                tool_input = state.get('input', {})
                tool_status = state.get('status', 'completed')
                tool_output = state.get('output', '')

                # tool_use block goes in assistant message content
                content.append({
                    'type': 'tool_use',
                    'name': tool_name,
                    'id': call_id,
                    'input': tool_input
                })
                # tool_result emitted as follow-up user message
                tool_results.append({
                    'type': 'tool_result',
                    'tool_use_id': call_id,
                    'is_error': tool_status != 'completed',
                    'content': str(tool_output) if tool_output else ''
                })

        # Emit the main message (user or assistant with text/tool_use)
        if content or first_msg:
            obj = {
                'type': role,
                'timestamp': ts_iso,
                'message': {'content': content}
            }

            if first_msg:
                # Flatten model object to providerID/modelID string
                model_obj = m['model']
                if model_obj and model_obj.get('providerID'):
                    model_str = (f"{model_obj['providerID']}/"
                                 f"{model_obj.get('modelID', '')}")
                    if model_obj.get('variant'):
                        model_str += f"/{model_obj['variant']}"
                else:
                    model_str = ''

                obj['sessionId'] = session_id
                obj['_opencode'] = {
                    'session': session_id,
                    'directory': sess['directory'] or '',
                    'model': model_str,
                    'agent': m.get('agent', '')
                }
                first_msg = False

            out.write(json.dumps(obj) + '\n')
            wrote_file = True

        # Emit tool results as a follow-up user message
        if tool_results:
            result_obj = {
                'type': 'user',
                'timestamp': ts_iso,
                'sessionId': session_id,
                'message': {'content': tool_results}
            }
            out.write(json.dumps(result_obj) + '\n')
            wrote_file = True

conn.close()
if wrote_file:
    print(tmpfile)
PYEOF
    done
}

# --- Dispatch ---
case "$PLATFORM" in
    claude)   discover_claude ;;
    codex)    discover_codex ;;
    cursor)   discover_cursor ;;
    opencode) discover_opencode ;;
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
