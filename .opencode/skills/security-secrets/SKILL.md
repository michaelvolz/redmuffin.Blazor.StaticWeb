---
name: security-secrets
description: Secret management patterns, MCP environment variable configuration, zero-tolerance security rules, and devcontainer security reference. Use when handling API keys, tokens, passwords, MCP secrets, environment variables, or any security-sensitive configuration.
invocable: false
---

# Security and Secret Management

## Zero Tolerance

- NEVER commit secrets to git
- NEVER hardcode secrets (`"api_key": "value"`)
- NEVER suggest file-based secrets (.env, appsettings.json real values)
- Detected secrets → stop, alert, rotate, cleanup

## Secret Management Methods

| Method                | Use Case     | Syntax                                |
| --------------------- | ------------ | ------------------------------------- |
| Environment Variables | MCP, dev     | `{env:VAR}` or `${env:VAR}`           |
| VS Code DevContainer  | Devcontainer | `devcontainer.json` secrets block     |
| VS Code Copilot MCP   | Copilot      | `${input:secret_id}` `password: true` |
| GitHub Secrets        | CI/CD        | `${{ secrets.NAME }}`                 |
| Azure Key Vault       | Production   | `az keyvault secret show`             |
| User Secrets          | Local .NET   | `dotnet user-secrets`                 |

## MCP Environment Variables

```json
"env": { "API_KEY": "${env:API_KEY}" }  // CORRECT
"env": { "API_KEY": "actual_secret" }   // WRONG
```

## References

- **Devcontainer Security**: `.devcontainer/SECURITY.md`
