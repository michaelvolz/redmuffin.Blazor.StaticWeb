#!/bin/bash
set -e

echo "=== DevContainer Post-Create Setup ==="

cd /workspaces/redmuffin.Blazor.StaticWeb

echo "[1/5] Restoring .NET tools..."
dotnet tool restore --quiet || echo "Warning: Some tools may not have restored"

echo "[2/5] Restoring NuGet packages..."
dotnet restore --verbosity quiet || dotnet restore

echo "[3/5] Verifying .NET SDK..."
dotnet --list-sdks
dotnet --version

echo "[4/5] Configuring git hooks..."
if [ -d ".githooks" ]; then
    git config core.hooksPath .githooks
    echo "Git hooks configured from .githooks"
fi

echo "[5/5] Verifying npm global tools..."
which swa || echo "Warning: SWA CLI not found in PATH"
which prettier || echo "Warning: Prettier not found in PATH"

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
