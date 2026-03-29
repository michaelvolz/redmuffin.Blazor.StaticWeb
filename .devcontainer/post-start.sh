#!/bin/bash

cd /workspaces/redmuffin.Blazor.StaticWeb

echo "=== DevContainer Post-Start ==="

if command -v docker &> /dev/null; then
    echo "Docker version:"
    docker --version
    
    echo "Testing Docker access..."
    if docker ps &> /dev/null; then
        echo "Docker is accessible"
    else
        echo "Warning: Docker may not be properly configured"
    fi
fi

echo "=== Post-Start Complete ==="
