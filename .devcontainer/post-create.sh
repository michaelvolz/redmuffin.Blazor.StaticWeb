#!/bin/bash
set -e

echo "=== DevContainer Post-Create Setup ==="

# Get workspace folder (devcontainer sets this correctly)
WORKSPACE_DIR="$PWD"
cd "$WORKSPACE_DIR"

# Get Git clone URL from environment (set in devcontainer.json)
GIT_CLONE_URL="${GIT_CLONE_URL:-git@github.com:michaelvolz/redmuffin.Blazor.StaticWeb.git}"

echo "[1/8] Cloning repository..."
if [ ! -d ".git" ]; then
    echo "Cloning from: $GIT_CLONE_URL"
    git clone "$GIT_CLONE_URL" .
fi

echo "[2/8] Restoring .NET tools..."
dotnet tool restore || echo "Warning: Some tools may not have restored"

echo "[3/8] Verifying .NET workloads..."
dotnet workload list | grep wasm-tools || echo "Note: wasm-tools workload installed during image build"

echo "[4/8] Restoring NuGet packages..."
dotnet restore --verbosity minimal || echo "Warning: Restore may require build first"

echo "[5/8] Verifying .NET SDK..."
dotnet --list-sdks
dotnet --version

echo "[6/8] Installing npm global tools..."
npm install -g \
    azure-functions-core-tools@4 \
    @azure/static-web-apps-cli \
    prettier \
    @commitlint/cli \
    @commitlint/config-conventional \
    chrome-devtools-mcp \
    opencode-ai \
    --silent \
    --no-audit \
    --no-fund

echo "[7/8] Setting up SSH access..."
bash .devcontainer/setup-ssh.sh

echo "[8/8] Configuring git hooks..."
if [ -d ".githooks" ] && git rev-parse --git-dir > /dev/null 2>&1; then
    git config core.hooksPath .githooks
    echo "Git hooks configured from .githooks"
else
    echo "Note: Not in a git repository or .githooks not found, skipping hooks"
fi

echo ""
echo "=== Setup Complete ==="
echo ""
echo "Development environment is ready!"
echo ""
echo "Quick start commands:"
echo "  dotnet build       - Build the solution"
echo "  dotnet test        - Run tests"
echo "  npm run dev        - Start development (if npm scripts configured)"
echo ""
echo "Ports available:"
echo "  - Blazor WebApp: http://localhost:5233"
echo "  - Full Stack (SWA): http://localhost:4280"
echo "  - Azure Functions: http://localhost:7071"
echo ""
