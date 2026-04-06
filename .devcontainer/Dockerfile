# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/devcontainers/dotnet:10.0

ENV DEBIAN_FRONTEND=noninteractive
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV DOTNET_NOLOGO=1
ENV NPM_CONFIG_UPDATE_NOTIFIER=false

RUN apt-get update && apt-get -y install --no-install-recommends \
    curl \
    ca-certificates \
    git \
    unzip \
    && rm -rf /var/lib/apt/lists/*

# Install dotnet workloads as ROOT (base image has .NET 9 SDK)
RUN dotnet workload install wasm-tools

# Configure dotnet to skip file timestamp checks (fixes bind mount permission issues)
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
ENV DOTNET_NOLOGO=1

# Switch to vscode user for the rest
USER vscode

RUN dotnet tool install --global dotnet-reportgenerator-globaltool 2>/dev/null || true

WORKDIR /workspaces/redmuffin.Blazor.StaticWeb
