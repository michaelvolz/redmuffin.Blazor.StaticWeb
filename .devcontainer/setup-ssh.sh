#!/bin/bash
# SSH Setup Script for DevContainer
# Mounts SSH keys from host and configures ssh-agent

set -e

MOUNTED_SSH_DIR="/mnt/host-ssh"
LOCAL_SSH_DIR="$HOME/.ssh"

echo "=== SSH Setup ==="

# Check if SSH directory is mounted
if [ ! -d "$MOUNTED_SSH_DIR" ]; then
    echo "WARNING: Host SSH directory not mounted at $MOUNTED_SSH_DIR"
    echo "Git SSH authentication will not work."
    echo ""
    echo "To fix: Ensure your SSH keys are at ~/.ssh on the host."
    exit 0
fi

# Create local .ssh directory
mkdir -p "$LOCAL_SSH_DIR"
chmod 700 "$LOCAL_SSH_DIR"

# Add GitHub to known hosts (required for SSH connections)
echo "Adding GitHub to known hosts..."
ssh-keyscan github.com >> "$LOCAL_SSH_DIR/known_hosts" 2>/dev/null
chmod 644 "$LOCAL_SSH_DIR/known_hosts"

# Copy private keys from mounted directory
key_count=0
for key_file in "$MOUNTED_SSH_DIR"/id_*; do
    if [ -f "$key_file" ] && [[ ! "$key_file" == *.pub ]]; then
        key_name=$(basename "$key_file")
        echo "Found SSH key: $key_name"
        
        # Copy with proper permissions
        cp "$key_file" "$LOCAL_SSH_DIR/$key_name"
        chmod 600 "$LOCAL_SSH_DIR/$key_name"
        key_count=$((key_count + 1))
    fi
done

# Copy public keys
for pub_file in "$MOUNTED_SSH_DIR"/*.pub; do
    if [ -f "$pub_file" ]; then
        pub_name=$(basename "$pub_file")
        cp "$pub_file" "$LOCAL_SSH_DIR/$pub_name"
        chmod 644 "$LOCAL_SSH_DIR/$pub_name"
    fi
done

# Copy config if it exists
if [ -f "$MOUNTED_SSH_DIR/config" ]; then
    cp "$MOUNTED_SSH_DIR/config" "$LOCAL_SSH_DIR/config"
    chmod 644 "$LOCAL_SSH_DIR/config"
    echo "Copied SSH config"
fi

if [ $key_count -eq 0 ]; then
    echo "WARNING: No SSH keys found in mounted directory."
    echo "Git SSH authentication will not work."
    exit 0
fi

echo ""
echo "Copied $key_count SSH key(s) to container."
echo "Starting ssh-agent..."

# Start ssh-agent
eval "$(ssh-agent -s)" > /dev/null

# Add keys to agent
echo "Adding keys to ssh-agent..."
ssh-add "$LOCAL_SSH_DIR"/id_* 2>/dev/null || echo "Note: Some keys may require a passphrase"

# Persist SSH agent environment
echo "export SSH_AUTH_SOCK=$SSH_AUTH_SOCK" >> "$HOME/.bashrc"
echo "export SSH_AGENT_PID=$SSH_AGENT_PID" >> "$HOME/.bashrc"

echo ""
echo "SSH agent is ready."
echo ""
echo "Loaded keys:"
ssh-add -l 2>/dev/null || echo "  (no keys loaded - may need passphrase)"
echo ""
echo "Test with: ssh -T git@github.com"
