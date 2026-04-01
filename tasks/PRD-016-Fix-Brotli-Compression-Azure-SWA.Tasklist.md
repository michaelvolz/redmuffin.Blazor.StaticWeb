# Task List: Fix Brotli Compression for Blazor WebAssembly on Azure Static Web Apps

**Corresponding PRD:** `PRD-016-Fix-Brotli-Compression-Azure-SWA.md`

**Estimated Effort:** 2-4 hours

**Priority:** High

---

## Relevant Files

- `.github/workflows/azure-static-web-apps-lively-cliff-0945be603.yml` - Main deployment workflow that needs modification
- `src/redmuffin.Blazor.StaticWeb/wwwroot/staticwebapp.config.json` - SWA configuration file (needs to be present in publish output)
- `src/redmuffin.Blazor.StaticWeb/redmuffin.Blazor.StaticWeb.csproj` - Blazor project file (to verify compression settings)

### Notes

- The workflow already has `dotnet publish` in place - we're only changing the deployment mechanism from SWA CLI to GitHub Action
- Backup the current workflow before making changes to ensure quick rollback if needed
- Test deployment on a staging environment first if possible
- Verify Brotli files (.br extension) are present in the publish output before deployment

---

## Instructions for Completing Tasks

**IMPORTANT:** As you complete each task, you must check it off in this markdown file by changing `- [ ]` to `- [x]`. This helps track progress and ensures you don't skip any steps.

Example:

- `- [ ] 1.1 Read file` → `- [x] 1.1 Read file` (after completing)

Update the file after completing each sub-task, not just after completing an entire parent task.

---

## Tasks

- [x] 0.0 Create feature branch
  - [x] 0.1 Create and checkout a new branch: `git checkout -b feature/016-fix-brotli-compression`
  - [x] 0.2 Verify you're on the new branch with `git branch`

- [x] 1.0 Backup and analyze current workflow
  - [x] 1.1 Create a backup copy: `cp .github/workflows/azure-static-web-apps-lively-cliff-0945be603.yml .github/workflows/azure-static-web-apps-lively-cliff-0945be603.yml.backup`
  - [x] 1.2 Review the current workflow to understand the existing structure
  - [x] 1.3 Identify the exact lines where `swa deploy` is called (around lines 243-250)
  - [x] 1.4 Verify `dotnet publish` is present and outputs to expected location
  - [x] 1.5 Check if `staticwebapp.config.json` exists in the project wwwroot folder

- [x] 2.0 Update deployment workflow configuration
  - [x] 2.1 Open `.github/workflows/azure-static-web-apps-lively-cliff-0945be603.yml` for editing
  - [x] 2.2 Comment out or remove the SWA CLI deployment step (lines ~167-177 and ~239-250)
  - [x] 2.3 Replace with Azure/static-web-apps-deploy GitHub Action:
    ```yaml
    - name: Deploy to Azure Static Web Apps
      uses: Azure/static-web-apps-deploy@v1
      with:
        azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN_LIVELY_CLIFF_0945BE603 }}
        repo_token: ${{ secrets.GITHUB_TOKEN }}
        action: "upload"
        app_location: "src/redmuffin.Blazor.StaticWeb/bin/Release/publish/wwwroot"
        api_location: "src/redmuffin.Blazor.StaticWeb.Api/bin/Release/publish"
        output_location: "" # Empty because app is pre-built
        skip_app_build: true # Critical: prevents Oryx rebuild
    ```
  - [x] 2.4 Ensure the workflow environment variables (APP_LOCATION, API_LOCATION, APP_ARTIFACT_LOCATION) are still defined or update references
  - [x] 2.5 Verify the workflow syntax is valid (check indentation, quotes, etc.)
  - [x] 2.6 Save the changes

- [x] 3.0 Add Brotli compression verification step
  - [x] 3.1 Add a post-build verification step before deployment to check for .br files:
    ```yaml
    - name: Verify Brotli compression files exist
      run: |
        echo "🔍 Verifying Brotli compressed files..."
        if find ${{ env.APP_LOCATION }}/bin/Release/publish/wwwroot -name "*.br" | grep -q .; then
          echo "✅ Brotli files found"
          find ${{ env.APP_LOCATION }}/bin/Release/publish/wwwroot -name "*.br" | head -10
        else
          echo "❌ No Brotli files found - build may not have compression enabled"
          exit 1
        fi
    ```
  - [x] 3.2 Add a post-deployment verification step that uses curl to check headers (lines ~258-270)
  - [x] 3.3 Update the existing health check to verify content-encoding headers:
    ```bash
    # Add to existing health check step
    echo "Checking Brotli compression..."
    curl -s -I -H "Accept-Encoding: br" "https://redmuffin.net/_framework/dotnet.wasm" | grep -i content-encoding || echo "⚠️ Brotli encoding not detected"
    ```
  - [ ] 2.4 Ensure the workflow environment variables (APP_LOCATION, API_LOCATION, APP_ARTIFACT_LOCATION) are still defined or update references
  - [ ] 2.5 Verify the workflow syntax is valid (check indentation, quotes, etc.)
  - [ ] 2.6 Save the changes

- [ ] 3.0 Add Brotli compression verification step
  - [ ] 3.1 Add a post-build verification step before deployment to check for .br files:
    ```yaml
    - name: Verify Brotli compression files exist
      run: |
        echo "🔍 Verifying Brotli compressed files..."
        if find ${{ env.APP_LOCATION }}/bin/Release/publish/wwwroot -name "*.br" | grep -q .; then
          echo "✅ Brotli files found"
          find ${{ env.APP_LOCATION }}/bin/Release/publish/wwwroot -name "*.br" | head -10
        else
          echo "❌ No Brotli files found - build may not have compression enabled"
          exit 1
        fi
    ```
  - [ ] 3.2 Add a post-deployment verification step that uses curl to check headers (lines ~258-270)
  - [ ] 3.3 Update the existing health check to verify content-encoding headers:
    ```bash
    # Add to existing health check step
    echo "Checking Brotli compression..."
    curl -s -I -H "Accept-Encoding: br" "https://redmuffin.net/_framework/dotnet.wasm" | grep -i content-encoding || echo "⚠️ Brotli encoding not detected"
    ```

- [ ] 4.0 Test deployment and verify results
  - [ ] 4.1 Commit changes: `git add -A && git commit -m "fix(deploy): preserve Brotli compression by using skip_app_build"`
  - [ ] 4.2 Push branch: `git push origin feature/016-fix-brotli-compression`
  - [ ] 4.3 Monitor the GitHub Actions workflow run
  - [ ] 4.4 Verify the deployment completes successfully
  - [ ] 4.5 Check deployment logs for:
    - No "Oryx build" messages (indicating skip_app_build worked)
    - Confirmation that compressed files are uploaded
  - [ ] 4.6 Verify in browser:
    - Open site in Chrome/Edge DevTools
    - Go to Network tab
    - Check that `.wasm` and `.dll` files show "br" in Content-Encoding column
    - Verify file sizes match compressed versions (significantly smaller than uncompressed)
  - [ ] 4.7 If tests pass, create a pull request

- [ ] 5.0 Update documentation and closeout
  - [ ] 5.1 Update workflow comments to explain the `skip_app_build` configuration
  - [ ] 5.2 Document the change in CHANGELOG.md under "Fixed" section
  - [ ] 5.3 Remove backup file: `rm .github/workflows/azure-static-web-apps-lively-cliff-0945be603.yml.backup`
  - [ ] 5.4 Mark this task list as completed: Update all checkboxes to `[x]`
  - [ ] 5.5 Delete this task file after successful merge: `rm tasks/tasks-016-fix-brotli-compression.md`

---

## Rollback Procedure

If the deployment fails or causes issues:

1. **Immediate rollback**:

   ```bash
   cp .github/workflows/azure-static-web-apps-lively-cliff-0945be603.yml.backup .github/workflows/azure-static-web-apps-lively-cliff-0945be603.yml
   git add .github/workflows/azure-static-web-apps-lively-cliff-0945be603.yml
   git commit -m "rollback: restore previous workflow"
   git push
   ```

2. **Verify restoration**: Check that the previous workflow is running and deployment succeeds

3. **Investigate**: Review logs to understand what went wrong before attempting fix again

---

## Success Criteria Checklist

Before closing this task list, verify:

- [ ] Workflow uses `Azure/static-web-apps-deploy@v1` action
- [ ] `skip_app_build` is set to `true`
- [ ] `output_location` is set to `""`
- [ ] Brotli files (`.br`) are present in the deployment
- [ ] Response headers show `content-encoding: br` for static assets
- [ ] Application loads and functions correctly
- [ ] Initial load size is reduced by 60-70%
- [ ] All tests pass
- [ ] CHANGELOG.md is updated
- [ ] Pull request is created and ready for review

---

**Created:** 2026-03-30  
**Last Updated:** 2026-03-30
