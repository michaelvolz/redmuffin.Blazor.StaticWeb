# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/devcontainers/dotnet:9.0-ubuntu

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

RUN curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor -o /usr/share/keyrings/microsoft-prod.gpg \
    && echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/microsoft-prod.gpg] https://packages.microsoft.com/ubuntu/$(. /etc/os-release && echo $VERSION_CODENAME)/prod $(. /etc/os-release && echo $VERSION_CODENAME) main" > /etc/apt/sources.list.d/dotnet.list \
    && apt-get update \
    && apt-get -y install azure-functions-core-tools-4 \
    && rm -rf /var/lib/apt/lists/*

RUN npm install -g \
    @azure/static-web-apps-cli \
    prettier \
    @commitlint/cli \
    @commitlint/config-conventional \
    chrome-devtools-mcp \
    --silent \
    --no-audit \
    --no-fund

USER vscode

RUN dotnet tool install --global dotnet-reportgenerator-globaltool --quiet 2>/dev/null || true

WORKDIR /workspaces/redmuffin.Blazor.StaticWeb
