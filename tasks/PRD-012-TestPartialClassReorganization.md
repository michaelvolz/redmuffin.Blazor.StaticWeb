---
title: PRD-012 - Test Partial Class Reorganization
date: 2025-08-03
project: redmuffin.Blazor.StaticWeb
author: AI Assistant
version: 1.0
description: Reorganize all existing tests into appropriate partial class files based on categorization rules
tags: [testing, partial-classes, organization, tunit, refactoring]
---

# PRD-012: Test Partial Class Reorganization

## 📋 Project Overview

Reorganize all existing TUnit tests in the project into appropriate partial class files according to the categorization rules defined in `docs/TestCategorizationRules.md`. This will improve test maintainability, readability, and organization without changing any test logic or functionality.

## 🎯 Objectives

### Primary Goals
- Move 100% of existing tests into the correct partial class files based on categorization rules
- Create new partial class files only when tests exist that need to be moved there (no empty files)
- Maintain all existing test functionality and logic unchanged
- Ensure all tests continue to pass after reorganization

### Success Criteria
- [ ] All test files analyzed against categorization rules
- [ ] Tests moved to appropriate partial class files:
  - `[TestClass].EdgeCases.cs` for error/exception scenarios
  - `[TestClass].Infrastructure.cs` for framework/system concerns
  - `[TestClass].Behavior.cs` for user interactions
  - `[TestClass].cs` for basic functionality (main file)
- [ ] Only create partial class files that will contain tests (no empty files)
- [ ] All tests pass after reorganization
- [ ] No build warnings introduced
- [ ] Existing `[TestClass].Helpers.cs` files remain unchanged

## 🔍 Current State Analysis

### Test Files to Process
Based on project structure, analyze and reorganize tests in:
- Component tests (e.g., `HomeTests.cs`, `ArticlesTests.cs`)
- Service tests (e.g., `UserServiceTests.cs`, `ArticleServiceTests.cs`)
- Integration tests
- Any other existing test files

### Categorization Rules (Reference)
1. **EdgeCases**: Error, exception, null, invalid input, timeout scenarios
2. **Infrastructure**: Lifecycle, logging, DI, authentication, caching, disposal
3. **Behavior**: User interactions, clicks, form submissions, workflows
4. **Main**: Basic rendering, property validation, happy path scenarios

## 📝 Technical Requirements

### File Organization Standards
- Use partial class structure: `public sealed partial class [TestClass]`
- Maintain same namespace across all partial files
- Follow naming convention: `[TestClass].[Category].cs`
- Preserve all existing using statements and dependencies
- Keep IDisposable implementation in main file only

### Quality Assurance
- All tests must continue to pass (`dotnet test`)
- No build warnings introduced (`dotnet build`)
- Maintain existing test naming conventions
- Preserve all test attributes and configurations
- Keep existing TestScope and mock implementations in Helpers files

## 🚀 Implementation Plan

### Phase 1: Analysis and Planning
1. **Inventory Existing Tests**
   - Scan all test files in the project
   - Catalog each test method with its current location
   - Apply categorization rules to determine target partial class

2. **Create Migration Plan**
   - Map each test to its target partial class file
   - Identify which partial class files need to be created
   - Plan the move sequence to avoid conflicts

### Phase 2: Partial Class File Creation
1. **Create Required Partial Files**
   - Generate `[TestClass].EdgeCases.cs` files where needed
   - Generate `[TestClass].Infrastructure.cs` files where needed
   - Generate `[TestClass].Behavior.cs` files where needed
   - Ensure proper namespace and class declarations

### Phase 3: Test Migration
1. **Move Tests by Category**
   - Move EdgeCases tests first (highest priority)
   - Move Infrastructure tests second
   - Move Behavior tests third
   - Leave remaining tests in main files

2. **Validation After Each Move**
   - Run `dotnet build` to check for build errors
   - Run `dotnet test` to verify test functionality
   - Fix any issues before proceeding

### Phase 4: Final Verification
1. **Comprehensive Testing**
   - Run full test suite to ensure 100% pass rate
   - Verify no build warnings
   - Check that all tests are properly categorized

2. **Documentation Update**
   - Update any references to moved tests
   - Ensure categorization aligns with rules

## 🎯 Acceptance Criteria

### Functional Requirements
- [ ] All existing tests maintain their original functionality
- [ ] Test methods are moved to appropriate partial class files
- [ ] No duplicate test methods exist
- [ ] All partial class files use correct namespace and class declaration

### Technical Requirements
- [ ] `dotnet build` produces no new warnings
- [ ] `dotnet test` shows 100% pass rate
- [ ] All partial class files follow established naming convention
- [ ] Only partial class files with actual tests are created

### Quality Requirements
- [ ] Code maintains existing quality standards
- [ ] No test logic is modified during the move
- [ ] All using statements are preserved correctly
- [ ] TestScope and helper methods remain in `.Helpers.cs` files

## 🔄 Testing Strategy

### Validation Steps
1. **Pre-Migration Baseline**
   - Record current test count and pass rate
   - Document any existing build warnings
   - Create backup of current state

2. **Progressive Validation**
   - Build and test after each partial class file creation
   - Verify tests after each batch of moves
   - Address issues immediately when found

3. **Final Verification**
   - Complete test suite execution
   - Build verification with warning check
   - Manual spot-check of moved tests

## 📊 Success Metrics

### Quantitative Metrics
- **Test Coverage**: 100% of tests moved to appropriate files
- **Test Success Rate**: 100% tests continue to pass
- **Build Health**: Zero new build warnings
- **File Organization**: Only non-empty partial class files created

### Qualitative Metrics
- **Maintainability**: Improved test organization and findability
- **Consistency**: All tests follow consistent categorization rules
- **Clarity**: Clear separation of concerns in test organization

## 🚨 Risk Mitigation

### Potential Risks
1. **Test Failures**: Tests might fail after moving due to dependencies
   - **Mitigation**: Move tests incrementally and validate after each step

2. **Build Errors**: Missing using statements or namespace issues
   - **Mitigation**: Preserve all existing imports and validate builds frequently

3. **Merge Conflicts**: Concurrent development might conflict with file moves
   - **Mitigation**: Complete reorganization in focused sprint, communicate with team

## 📅 Timeline

### Estimated Duration: 1-2 days

### Phase Breakdown
- **Analysis**: 2-4 hours (catalog all tests, create migration plan)
- **Implementation**: 4-6 hours (create files, move tests, validate)
- **Verification**: 1-2 hours (final testing, documentation)

## 🎉 Definition of Done

- [ ] All tests analyzed and moved according to categorization rules
- [ ] Only partial class files with tests created (no empty files)
- [ ] All tests pass without modification to test logic
- [ ] No new build warnings introduced
- [ ] Migration follows established partial class patterns
- [ ] Documentation reflects current organization
- [ ] Code ready for production deployment

## 📋 Next Steps

After completion of this PRD:
1. All future tests will be written directly in appropriate partial class files
2. Categorization rules can be refined based on practical experience
3. Similar reorganization can be applied to other code areas if beneficial
4. Test organization becomes a standard part of development workflow
