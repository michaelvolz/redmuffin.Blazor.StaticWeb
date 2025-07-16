# PRD: Modify Generate-Changelog.ps1 to Use Git Log Directly

## Introduction/Overview

The Generate-Changelog.ps1 script currently requires a pre-generated git-commits.txt file as input. This creates an unnecessary manual step in the changelog generation process. This feature will modify the script to directly execute `git log` commands and process the output in-memory, eliminating the need for the intermediate git-commits.txt file.

## Goals

1. Eliminate the manual step of creating git-commits.txt before running the changelog generator
2. Integrate git log command execution directly into the Generate-Changelog.ps1 script
3. Maintain all existing functionality including filtering, categorization, and output format
4. Improve error handling for git-related operations
5. Ensure the script works in non-git directories with appropriate error messages

## User Stories

1. **As a developer**, I want to generate a changelog without manually creating git-commits.txt, so that I can streamline my release process.

2. **As a developer**, I want the script to automatically fetch git commits from the repository, so that I always get the most up-to-date commit history.

3. **As a developer**, I want clear error messages when running the script outside a git repository, so that I understand what went wrong.

4. **As a maintainer**, I want the script to handle git command failures gracefully, so that the build process doesn't break unexpectedly.

## Functional Requirements

1. The script must execute `git --no-pager log --oneline` to retrieve commit history
2. The script must process the git log output directly in memory without creating temporary files
3. The script must maintain backward compatibility with the -InputFile parameter, but make it optional
4. The script must detect if git is installed and available in the system PATH
5. The script must detect if the current directory is a git repository
6. The script must provide clear error messages for:
   - Git not installed
   - Not in a git repository
   - Git command failures
7. The script must preserve all existing filtering and categorization logic
8. The script must maintain the same output format for CHANGELOG.md
9. The script must handle empty git repositories gracefully
10. The script must support all existing parameters (-OutputFile, -Update, -Preview)

## Non-Goals (Out of Scope)

1. This feature will NOT change the changelog output format
2. This feature will NOT modify the filtering or categorization logic
3. This feature will NOT add new git log formatting options (beyond --oneline)
4. This feature will NOT support multiple git repositories in a single run
5. This feature will NOT change the existing module structure
6. This feature will NOT add support for git log parameters like date ranges or branch filtering (existing filtering logic remains unchanged)

## Design Considerations

1. **Backward Compatibility**: The -InputFile parameter should remain functional for users who still want to use pre-generated commit files
2. **Module Structure**: The changes should be primarily in the main script and CommitParser module, minimizing changes to other modules
3. **Error Handling**: Git-related errors should be caught and converted to user-friendly messages
4. **Performance**: Git log output should be processed efficiently without creating large memory footprints

## Technical Considerations

1. **Git Command**: Use `git --no-pager log --oneline` to ensure non-paginated output
2. **PowerShell Integration**: Use appropriate PowerShell cmdlets for executing external commands and capturing output
3. **Module Updates**: The CommitParser module may need minor updates to handle direct git output
4. **Testing**: New Pester tests should follow the existing pattern in the scripts/modules folder
5. **Cross-platform**: Ensure the git command works on both Windows PowerShell and PowerShell Core

## Success Metrics

1. The script successfully generates a changelog without requiring git-commits.txt
2. All existing tests continue to pass
3. New tests verify git command execution and error handling
4. The script provides clear, actionable error messages for common failure scenarios
5. The generated CHANGELOG.md is identical whether using git log directly or a pre-generated file with the same commits

## Open Questions

1. Should the script support additional git log parameters in the future (e.g., --since, --until for date filtering)?
2. Should we cache the git log output for performance in Update mode?
3. Should we add a parameter to specify the number of commits to process (e.g., -CommitLimit)?
4. Should the script automatically detect and use the correct git executable if multiple are installed?
