# PRD-016: Fix Brotli Compression for Blazor WebAssembly on Azure Static Web Apps

## 1. Introduction/Overview

### Problem Statement

Our Blazor WebAssembly application generates Brotli-compressed files (`.br` files) during the build process, which significantly reduce file sizes and improve initial load times. However, these optimizations are not reaching production. The current deployment workflow uses the SWA CLI (`swa deploy`) which triggers Oryx (Azure Static Web Apps' build engine) to rebuild the application during deployment, potentially overwriting our pre-compressed assets.

### Goal

Ensure that Brotli-compressed files generated during `dotnet publish` are preserved and properly served by Azure Static Web Apps, reducing the application's initial load size and improving user experience.

### Background

Blazor WebAssembly's `dotnet publish` command generates three versions of each static asset:

1. Original uncompressed file (e.g., `app.wasm`)
2. Gzip-compressed file (e.g., `app.wasm.gz`)
3. Brotli-compressed file (e.g., `app.wasm.br`)

Azure Static Web Apps automatically serves these pre-compressed files when clients support them, but only if they survive the deployment process without being regenerated.

## 2. Goals

1. **Preserve Pre-Compressed Files**: Ensure Brotli (`.br`) and Gzip (`.gz`) files generated during `dotnet publish` are deployed to production
2. **Eliminate Double Build**: Prevent Oryx from rebuilding the application during deployment
3. **Reduce Payload Size**: Achieve the expected file size reductions (typically 60-70% smaller than uncompressed)
4. **Maintain Deployment Speed**: Keep the deployment process efficient by avoiding redundant builds
5. **Verify Compression Works**: Confirm that browsers receive compressed files in production

## 3. User Stories

- As a **website visitor**, I want the Blazor app to load quickly so I don't wait long for the initial page render
- As a **developer**, I want my Brotli compression optimizations to reach production without being overwritten
- As a **DevOps engineer**, I want a reliable deployment process that doesn't rebuild already-built artifacts
- As a **product owner**, I want improved Core Web Vitals scores from smaller asset sizes

## 4. Functional Requirements

### FR-001: Modify Deployment Workflow

The GitHub Actions workflow must be updated to use the `Azure/static-web-apps-deploy` GitHub Action with `skip_app_build: true` instead of the SWA CLI.

**Acceptance Criteria:**

- Workflow uses `Azure/static-web-apps-deploy@v1` action
- `skip_app_build` is set to `true`
- `output_location` is set to empty string or omitted
- `app_location` points to the pre-built publish output directory

### FR-002: Separate Build and Deploy Phases

The workflow must clearly separate the build phase (`dotnet publish`) from the deployment phase.

**Acceptance Criteria:**

- Build phase runs `dotnet publish` with Release configuration
- Build outputs to a known directory (e.g., `bin/Release/publish/wwwroot`)
- Deployment phase uses the pre-built artifacts without modification

### FR-003: Preserve Brotli Files

All `.br` and `.gz` files generated during publish must be present in the deployed application.

**Acceptance Criteria:**

- Files with `.br` extension are present in deployment
- Files with `.gz` extension are present in deployment
- Files are served with correct `Content-Encoding: br` header

### FR-004: Verify Compression in Production

A verification step must confirm that compression is working in the production environment.

**Acceptance Criteria:**

- Health check verifies compressed files are accessible
- Response headers show `content-encoding: br` for `.wasm` and `.dll` files
- File sizes match the compressed versions, not uncompressed

## 5. Non-Goals (Out of Scope)

- **Dynamic Compression**: We will not implement runtime/dynamic compression; we rely on pre-compressed files
- **CDN Configuration**: This PRD does not cover Azure CDN or Front Door configuration
- **Other Compression Algorithms**: Only Brotli and Gzip are in scope
- **Build Optimization**: Changes to the actual build process (e.g., tree shaking) are not included
- **API Compression**: This focuses on static web assets, not API responses
- **SWA CLI Migration**: While we move away from SWA CLI for deployment, we may still use it for local development

## 6. Design Considerations

### Workflow Architecture

```yaml
# Current Approach (Problematic)
- Build with dotnet publish
- Deploy with swa deploy
  └─► Oryx detects build artifacts and rebuilds
      └─► Pre-compressed files are overwritten

# New Approach (Solution)
- Build with dotnet publish
  └─► Brotli/Gzip files generated
- Deploy with Azure/static-web-apps-deploy
  └─► skip_app_build: true
      └─► Pre-compressed files preserved
```

### Key Configuration Parameters

| Parameter         | Old Value                          | New Value                                                      | Reason                    |
| ----------------- | ---------------------------------- | -------------------------------------------------------------- | ------------------------- |
| `skip_app_build`  | Not set / `false`                  | `true`                                                         | Prevents Oryx rebuild     |
| `output_location` | `"wwwroot"`                        | `""` (empty)                                                   | Indicates pre-built app   |
| `app_location`    | `"src/redmuffin.Blazor.StaticWeb"` | `"src/redmuffin.Blazor.StaticWeb/bin/Release/publish/wwwroot"` | Points to built artifacts |

### File Structure

After `dotnet publish`, the output should contain:

```
wwwroot/
├── _framework/
│   ├── blazor.boot.json
│   ├── blazor.boot.json.br      # ← Brotli compressed
│   ├── blazor.boot.json.gz      # ← Gzip compressed
│   ├── dotnet.js
│   ├── dotnet.js.br             # ← Brotli compressed
│   ├── dotnet.js.gz             # ← Gzip compressed
│   ├── dotnet.wasm
│   ├── dotnet.wasm.br           # ← Brotli compressed
│   ├── dotnet.wasm.gz           # ← Gzip compressed
│   └── *.dll
│       ├── *.dll.br             # ← Brotli compressed
│       └── *.dll.gz             # ← Gzip compressed
```

## 7. Technical Considerations

### GitHub Action vs SWA CLI

The Microsoft documentation explicitly recommends using `skip_app_build: true` with the GitHub Action when you want complete control over the build process. The SWA CLI's `deploy` command may not respect this flag in the same way.

### Oryx Build Detection

Oryx (the build engine behind Azure Static Web Apps) automatically detects project types and runs builds. When `skip_app_build: true` is set, Oryx skips the build phase and deploys files exactly as they exist in `app_location`.

### Content Negotiation

Azure Static Web Apps handles content negotiation automatically:

- If a request includes `Accept-Encoding: br` and `file.br` exists → serve `.br` file with `Content-Encoding: br`
- If a request includes `Accept-Encoding: gzip` and `file.gz` exists → serve `.gz` file with `Content-Encoding: gzip`
- Otherwise → serve uncompressed file

### Potential Risks

1. **StaticWebAppConfig.json**: Must be present in the `app_location` directory for configuration to work
2. **API Location**: The API build may still need to run; use `skip_api_build` if pre-building API
3. **File Permissions**: Ensure the GitHub Actions runner has read access to the publish output

## 8. Implementation Steps

### Step 1: Backup Current Workflow

Create a backup of the current workflow file before making changes.

### Step 2: Update Workflow File

Modify `.github/workflows/azure-static-web-apps-lively-cliff-0945be603.yml`:

1. Replace the SWA CLI deployment step with the Azure/static-web-apps-deploy action
2. Set `skip_app_build: true`
3. Set `output_location: ""`
4. Update `app_location` to point to the publish output

### Step 3: Add Verification Step

Add a post-deployment verification step that checks:

- Presence of `.br` files in deployment
- Response headers include `content-encoding: br`
- File sizes match compressed versions

### Step 4: Test Deployment

Deploy to a staging environment first to verify:

- Build completes successfully
- Deployment succeeds
- Compressed files are accessible
- Application functions correctly

### Step 5: Monitor Production

After production deployment:

- Monitor application load times
- Check Core Web Vitals scores
- Verify no 404 errors for compressed files

## 9. Success Metrics

### Performance Metrics

- [ ] Blazor app initial load size reduced by 60-70%
- [ ] Largest Contentful Paint (LCP) improved
- [ ] Time to Interactive (TTI) reduced

### Verification Metrics

- [ ] All `.wasm.br` files present in deployment
- [ ] All `.dll.br` files present in deployment
- [ ] Response headers show `content-encoding: br`
- [ ] No build errors in deployment logs
- [ ] No "Oryx build" messages in deployment logs (indicating skip worked)

### Quality Metrics

- [ ] Zero downtime during deployment
- [ ] No broken functionality
- [ ] All existing tests pass

## 10. References

### Official Documentation

1. [Azure Static Web Apps - Build Configuration](https://learn.microsoft.com/en-us/azure/static-web-apps/build-configuration)
   - _"Ensure that you set `skip_app_build` to `true`"_

2. [Azure Static Web Apps - FAQ](https://learn.microsoft.com/en-us/azure/static-web-apps/faq)
   - _"For other file types, Static Web Apps allows you to include a Brotli-compressed version of your file with a `.br` extension"_
   - _"If you want complete control over how to build your app... set `skip_app_build` to `true`"_

3. [Host and deploy ASP.NET Core Blazor WebAssembly](https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/webassembly/)
   - _"Blazor build does for you is produce pre-compressed files for Brotli and Gzip compression"_

### GitHub Resources

4. [Azure/static-web-apps-deploy Action](https://github.com/Azure/static-web-apps-deploy)

### Blog Posts

5. [Hosting Blazor WebAssembly in Azure Static Web Apps](https://timheuer.com/blog/hosting-blazor-in-azure-static-web-apps)
   - _"When you host Blazor Wasm using ASP.NET Core, we deliver these files to the client automatically"_

## 11. Open Questions

1. **API Build**: Does the API also need `skip_api_build: true`, or should we let Oryx build the Azure Functions?
2. **StaticWebAppConfig.json**: Is this file currently being copied to the publish output directory?
3. **Staging Environment**: Should we implement a staging deployment for testing before production?
4. **Rollback Plan**: What is the rollback procedure if the new deployment approach causes issues?

## 12. Appendix

### Example Workflow Configuration

```yaml
- name: Deploy to Azure Static Web Apps
  uses: Azure/static-web-apps-deploy@v1
  with:
    azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
    repo_token: ${{ secrets.GITHUB_TOKEN }}
    action: "upload"
    app_location: "src/redmuffin.Blazor.StaticWeb/bin/Release/publish/wwwroot"
    api_location: "src/redmuffin.Blazor.StaticWeb.Api/bin/Release/publish"
    output_location: "" # Empty because app is pre-built
    skip_app_build: true # Critical: prevents Oryx rebuild
```

### Verification Script

```bash
#!/bin/bash
# Verify Brotli compression is working

URL="https://redmuffin.net"

# Check if .br files exist in deployment
echo "Checking for Brotli files..."
curl -s "$URL/_framework/dotnet.wasm.br" -o /dev/null -w "%{http_code}\n"

# Check response headers for content-encoding
echo "Checking response headers..."
curl -s -I -H "Accept-Encoding: br" "$URL/_framework/dotnet.wasm" | grep -i content-encoding
```

---

**Document Information:**

- **Author**: Research Agent
- **Created**: 2026-03-30
- **Status**: Draft
- **Priority**: High
- **Estimated Effort**: 2-4 hours
