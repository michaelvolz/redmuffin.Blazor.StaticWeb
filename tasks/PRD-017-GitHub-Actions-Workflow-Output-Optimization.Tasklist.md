# Task List: GitHub Actions Workflow Output Optimization

## Relevant Files

- `.github/workflows/azure-static-web-apps-lively-cliff-0945be603.yml` - Main workflow file to be optimized (contains all decorative emojis and verbose output)
- `.github/workflows/azure-static-web-apps-lively-cliff-0945be603.yml.backup` - Backup of current workflow (created in task 1.2)
- `tasks/PRD-017-GitHub-Actions-Workflow-Output-Optimization.md` - PRD document with full requirements and specifications

### Notes

- This is a workflow optimization task - no code changes, only output formatting
- No unit tests needed (workflow changes are validated through actual execution)
- Testing is done by running the workflow on a feature branch and comparing outputs
- Backup file should be kept for 1 week after successful production deployment

---

## Instructions for Completing Tasks

**IMPORTANT:** As you complete each task, you must check it off in this markdown file by changing `- [ ]` to `- [x]`. This helps track progress and ensures you don't skip any steps.

Example:

- `- [ ] 1.1 Read file` → `- [x] 1.1 Read file` (after completing)

Update the file after completing each sub-task, not just after completing an entire parent task.

**PUSH POLICY**: This task list does NOT include `git push` commands. Push manually to GitHub only when you are ready to test in CI. All local work (branching, committing, editing) can be done without pushing.

---

## Tasks

- [ ] **0.0** Create feature branch
  - [ ] **0.1** Create and checkout new branch: `git checkout -b feature/PRD-017-workflow-output-optimization`
  - [ ] **0.2** Verify clean working directory: `git status`
  - [ ] **0.3** Confirm branch created successfully: `git branch --show-current`

- [ ] **1.0** Phase 1: Preparation - Backup and Baseline
  - [ ] **1.1** Read current workflow file completely: `.github/workflows/azure-static-web-apps-lively-cliff-0945be603.yml`
  - [ ] **1.2** Create backup copy: `cp .github/workflows/azure-static-web-apps-lively-cliff-0945be603.yml .github/workflows/azure-static-web-apps-lively-cliff-0945be603.yml.backup`
  - [ ] **1.3** Verify backup created: `ls -la .github/workflows/*.backup`
  - [ ] **1.4** Document baseline - note current emoji count and output format patterns
  - [ ] **1.5** Create test checklist document for validation

- [ ] **2.0** Phase 2: Implementation - Add Context Headers and Resource Monitoring
  - [ ] **2.1** Add job context header at start of `test_and_build_job`
    - Insert after checkout step, before setup
    - Include: Job name, Trigger, Ref, Commit, Actor, Runner
    - Use format: `echo "=== Job Context ==="`
  - [ ] **2.2** Add system resources monitoring section
    - Insert after context header
    - Include: CPU cores, Memory total, Disk available
    - Use commands: `nproc`, `free -h`, `df -h`
    - Format: `echo "=== System Resources ==="`
  - [ ] **2.3** Verify YAML syntax: No indentation errors, proper quoting
  - [ ] **2.4** Commit changes: `git add . && git commit -m "feat(ci): add context headers and resource monitoring"`

- [ ] **3.0** Phase 2: Implementation - Remove Decorative Emojis
  - [ ] **3.1** Remove 🧪 (testing emoji) - Line ~141
    - Change: `echo "🧪 Running TUnit Tests..."` → `echo "Running tests..."`
  - [ ] **3.2** Remove ℹ️ (information emojis) - Lines ~142-143
    - Remove decorative info lines or convert to plain text
  - [ ] **3.3** Remove ✅ (premature success) - Line ~149
    - Remove: `echo "✅ All tests passed! Proceeding..."`
  - [ ] **3.4** Remove 📦 (package emoji) - Line ~173
    - Change: `echo "📦 Installing..."` → `echo "Installing Azure Static Web Apps CLI..."`
  - [ ] **3.5** Remove 🚀 (deployment indicator) - Line ~181
    - Remove from comment or deployment section header
  - [ ] **3.6** Remove all emojis from environment info section - Lines ~191-203
    - Clean up: Working Directory, Node Version, NPM Version, etc.
    - Use plain text: `echo "Working Directory: $(pwd)"`
  - [ ] **3.7** Remove 🏗️ (building emoji) - Line ~205
    - Change: `echo "🏗️ Building Blazor..."` → `echo "Building Blazor WebAssembly..."`
  - [ ] **3.8** Remove ⚡ (API building emoji) - Line ~216
    - Change: `echo "⚡ Building Azure Functions API..."` → `echo "Building Azure Functions API..."`
  - [ ] **3.9** Remove 📦 (deploy emoji) - Line ~226
    - Change: `echo "📦 Deploying..."` → `echo "Deploying to Azure Static Web Apps..."`
  - [ ] **3.10** Remove ✅ (completion emoji) - Line ~236
    - Move to final summary only
  - [ ] **3.11** Remove 🔍 (verification emoji) - Line ~244
    - Change: `echo "🔍 Verifying deployment health..."` → `echo "Verifying deployment health..."`
  - [ ] **3.12** Remove ✅ (health check emoji) - Line ~250
    - Keep text only: `echo "Deployment health check passed"`
  - [ ] **3.13** Remove decorative emojis from docs-only job - Lines ~267-274
    - Remove: 📝, ⏭️, 💡
  - [ ] **3.14** Final emoji audit - Search for any remaining decorative emojis
    - Command: `grep -n "emoji" .github/workflows/azure-static-web-apps-lively-cliff-0945be603.yml || echo "No emojis found"`
  - [ ] **3.15** Verify only ❌, ⚠️, ✅ remain (and ✅ only at end)
  - [ ] **3.16** Commit changes: `git add . && git commit -m "style(ci): remove decorative emojis from workflow output"`

- [ ] **4.0** Phase 2: Implementation - Restructure and Standardize Output
  - [ ] **4.1** Update test execution section (FR-005)
    - Clean up test running message
    - Remove parallel execution info (move to config comment if needed)
    - Keep: `echo "Running tests..."` and `echo "Configuration: Release mode"`
  - [ ] **4.2** Update test failure error format (FR-004)
    - Change from: `echo "❌ Tests failed! Stopping deployment."`
    - Change to:
      ```bash
      echo ""
      echo "❌ STEP FAILED: Test Execution"
      echo "Exit code: $?"
      echo ""
      ```
  - [ ] **4.3** Update build section headers
    - Standardize: `echo "Building Blazor WebAssembly..."`
    - Standardize: `echo "Building Azure Functions API..."`
  - [ ] **4.4** Update deployment section
    - Clean header: `echo "Deploying to Azure Static Web Apps..."`
    - Remove inline comments with emojis
  - [ ] **4.5** Update warning format (FR-007)
    - Change health check warning to: `echo "⚠️ Deployment health check: Site may not be fully ready (normal for new deployments)"`
    - Remove decorative ℹ️ symbols
  - [ ] **4.6** Add final summary section (FR-006)
    - Insert at end of successful deployment
    - Format:
      ```bash
      echo ""
      echo "=== Pipeline Summary ==="
      echo "Duration: [calculate if possible, or omit]"
      echo "Tests: completed"
      echo "Build: Success"
      echo "Deploy: Success"
      echo ""
      echo "✅ All checks passed"
      ```
  - [ ] **4.7** Review all echo statements for professional tone
  - [ ] **4.8** Verify YAML syntax is valid
  - [ ] **4.9** Commit changes: `git add . && git commit -m "feat(ci): standardize output format and add final summary"`

- [ ] **5.0** Phase 3: Testing - Full Regression Test Suite
  - [ ] **5.1** Feature branch ready for GitHub
    - **NOTE**: Push manually when ready to test: `git push origin feature/PRD-017-workflow-output-optimization`
  - [ ] **5.2** Test Case TC-001: Normal Execution
    - Create test PR with code changes
    - Verify workflow runs without errors
    - Check output format matches requirements:
      - [ ] Context header displays
      - [ ] Resource monitoring shows CPU, memory, disk
      - [ ] No decorative emojis in logs
      - [ ] Final summary appears with ✅
  - [ ] **5.3** Test Case TC-002: Test Failure Scenario
    - Intentionally break a test in feature branch
    - Push and verify workflow fails
    - Check error format:
      - [ ] ❌ STEP FAILED appears
      - [ ] Exit code displayed
      - [ ] Clear failure message
    - Revert test break after verification
  - [ ] **5.4** Test Case TC-003: Cache Miss Warning
    - If possible, clear cache or wait for expiration
    - Verify ⚠️ cache miss warning appears
    - Check warning format is clean
  - [ ] **5.5** Test Case TC-004: Documentation-Only Change
    - Create test PR with only README.md changes
    - Verify workflow skips correctly
    - Check skip message has no decorative emojis
  - [ ] **5.6** Test Case TC-005: Resource Monitoring
    - Check any workflow run
    - Verify System Resources section displays
    - Confirm all three metrics present
  - [ ] **5.7** Regression Checklist
    - [ ] NuGet package caching still works
    - [ ] npm/SWA CLI caching still works
    - [ ] Tests run and pass
    - [ ] Blazor WebAssembly builds
    - [ ] Azure Functions API builds
    - [ ] Brotli compression preserved (check file sizes)
    - [ ] Deployment succeeds
    - [ ] Health check executes
    - [ ] Documentation-only detection works
  - [ ] **5.8** Compare output - Before vs After
    - Open backup file and new file side by side
    - Verify all changes align with PRD
    - Document any discrepancies
  - [ ] **5.9** Create test evidence document
    - Screenshot or copy key output sections
    - Note any issues found
  - [ ] **5.10** Fix any issues discovered during testing
  - [ ] **5.11** Final commit: `git add . && git commit -m "test(ci): complete regression testing"`

- [ ] **6.0** Phase 4: Deployment
  - [ ] **6.1** Create pull request
    - Title: "PRD-017: Optimize GitHub Actions workflow output format"
    - Description: Reference PRD-017, summarize changes
    - Link to PRD: `tasks/PRD-017-GitHub-Actions-Workflow-Output-Optimization.md`
  - [ ] **6.2** Request code review
    - Tag appropriate reviewer
    - Provide testing evidence
  - [ ] **6.3** Address review feedback (if any)
  - [ ] **6.4** Merge to master after approval
    - Merge the PR through GitHub UI
    - Verify output format in production
    - **NOTE**: Changes will be applied after merge; no manual push needed
  - [ ] **6.6** Monitor for 1 week
    - Check daily workflow runs
    - Verify no issues introduced
  - [ ] **6.7** Cleanup (after 1 week stable)
    - Remove backup file: `.github/workflows/azure-static-web-apps-lively-cliff-0945be603.yml.backup`
  - [ ] **6.8** Update this task list
    - Mark all tasks complete: `- [x]`
    - Add completion date

---

## Validation Checklist

Before considering this complete, verify:

### Output Format

- [ ] Context headers present and formatted correctly
- [ ] Resource monitoring displays CPU, memory, disk
- [ ] No decorative emojis remain (except ❌, ⚠️, ✅)
- [ ] Error messages use standardized format
- [ ] Final summary section present
- [ ] Single ✅ at end only

### Functionality

- [ ] All tests pass
- [ ] Builds succeed
- [ ] Deployments work
- [ ] Caching still functions
- [ ] Documentation-only skip works

### Quality

- [ ] YAML syntax valid
- [ ] No shell errors
- [ ] Professional tone throughout
- [ ] AI-debuggable format

---

## Notes for Implementer

### Emoji Reference

**Remove These**:

- 🧪 testing
- 🏗️ building
- ⚡ API building
- 📦 deploying/packaging
- 🔍 verifying
- 🚀 deployment indicators
- 📝 documentation
- ℹ️ information (use text)
- 💡 tips/hints (use text)
- ⏭️ skip indicators (use text)

**Keep These**:

- ❌ FAILURE (always use)
- ⚠️ WARNING (always use)
- ✅ SUCCESS (final summary only)

### Key Line Numbers (Approximate)

Use these as starting points, but verify actual line numbers in current file:

- Line ~141: Test execution section
- Line ~173: CLI installation
- Line ~191: Environment info section
- Line ~205: Blazor build
- Line ~216: API build
- Line ~226: Deployment
- Line ~236: Completion
- Line ~244: Health check
- Line ~267: Docs-only job

### Testing Tips

1. **Use draft PRs** for testing - they don't notify everyone
2. **Break a test intentionally** by adding `Assert.Fail()` temporarily
3. **Check the Actions tab** - you can see live logs
4. **Compare outputs** - open two browser tabs side by side
5. **Test one change at a time** if debugging issues

### Rollback Plan

If something goes wrong:

```bash
git checkout master
cp .github/workflows/azure-static-web-apps-lively-cliff-0945be603.yml.backup \
   .github/workflows/azure-static-web-apps-lively-cliff-0945be603.yml
git add . && git commit -m "Revert: Restore workflow from backup"
# Push manually when ready: git push origin master
```

---

**Task List Information:**

- **PRD**: PRD-017-GitHub-Actions-Workflow-Output-Optimization
- **Status**: Ready for implementation
- **Estimated Duration**: 6-7 hours
- **Priority**: NOW (immediate)
- **Created**: 2025-03-31
- **Last Updated**: 2025-03-31

**Progress Tracking:**

- Total Tasks: 6 major phases
- Total Sub-tasks: 70+
- Current Phase: Not started
