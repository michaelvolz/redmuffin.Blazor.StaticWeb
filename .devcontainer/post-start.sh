#!/bin/bash
set -e

echo "=== DevContainer Post-Start Check ==="

# Verify workspace
WORKSPACE_DIR="$PWD"
echo "Workspace: $WORKSPACE_DIR"

# Check SSH keys
echo ""
echo "SSH keys check:"
if [ -f "$HOME/.ssh/id_ed25519" ] || [ -f "$HOME/.ssh/id_rsa" ]; then
    echo "  SSH keys available: YES"
else
    echo "  SSH keys available: NO"
fi

# Check git remote
echo ""
echo "Git remote:"
cd "$WORKSPACE_DIR"
if git rev-parse --git-dir > /dev/null 2>&1; then
    git remote -v 2>/dev/null || echo "  No remote configured"
else
    echo "  Not a git repository"
fi

# Check Docker
echo ""
echo "Docker:"
if command -v docker &> /dev/null; then
    echo "  Docker available: YES"
    if docker ps &> /dev/null; then
        echo "  Docker accessible: YES"
    else
        echo "  Docker accessible: NO (check configuration)"
    fi
else
    echo "  Docker available: NO"
fi

echo ""
echo "Ready!"
