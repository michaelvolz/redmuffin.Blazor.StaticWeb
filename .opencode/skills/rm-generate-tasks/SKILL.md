---
name: rm-generate-tasks
description: "Shortcut: rm:tasks. Task list generation from PRD documents. Use when asked to create tasks, todo lists, task breakdowns, or implementation plans from a PRD."
---

# Rule: Generating a Task List from User Requirements

## Goal

To guide an AI assistant in creating a detailed, step-by-step task list in Markdown format based on user requirements, feature requests, or existing documentation. The task list should guide a developer through implementation.

## Output

- **Format:** Markdown (`.md`)
- **Location:** `/tasks/`
- **Filename:** `PRD-[number]-[Feature-Name].Tasklist.md` (e.g., `PRD-001-User-Profile-Editing.Tasklist.md`)

### Determining the Task List Number

**CRITICAL:** You MUST determine the next available task list number dynamically:

1. **List existing PRD files:** Run `ls tasks/ | grep -E '^PRD-[0-9]+'` to find all PRD files
2. **Extract numbers:** Extract the numeric portion from each filename (e.g., PRD-001 → 001, PRD-013 → 013)
3. **Find highest:** Identify the maximum number currently in use
4. **Match PRD number:** Use the SAME number as the corresponding PRD file (e.g., if PRD is 014, use tasks-014)

**Example command to find the PRD number:**

```bash
ls tasks/ | grep -E '^PRD-[0-9]+' | sed 's/PRD-\([0-9]*\).*/\1/' | sort -n | tail -1
```

The task list number must match the PRD number it corresponds to. Always use 3-digit format with leading zeros (001, 013, 042, etc.).

## Process

1.  **Receive Requirements:** The user provides a feature request, task description, or points to existing documentation
2.  **Analyze Requirements:** The AI analyzes the functional requirements, user needs, and implementation scope from the provided information
3.  **Phase 1: Generate Parent Tasks:** Based on the requirements analysis, create the file and generate the main, high-level tasks required to implement the feature. **Default to trunk-based development.** Only include branch-related tasks if the user explicitly requests feature branches. Use your judgement on how many additional high-level tasks to use. It's likely to be about 5. Present these tasks to the user in the specified format (without sub-tasks yet). Inform the user: "I have generated the high-level tasks based on your requirements. Ready to generate the sub-tasks? Respond with 'Go' to proceed."
4.  **Wait for Confirmation:** Pause and wait for the user to respond with "Go".
5.  **Phase 2: Generate Sub-Tasks:** Once the user confirms, break down each parent task into smaller, actionable sub-tasks necessary to complete the parent task. Ensure sub-tasks logically follow from the parent task and cover the implementation details implied by the requirements.
6.  **Identify Relevant Files:** Based on the tasks and requirements, identify potential files that will need to be created or modified. List these under the `Relevant Files` section, including corresponding test files if applicable.
7.  **Generate Final Output:** Combine the parent tasks, sub-tasks, relevant files, and notes into the final Markdown structure.
8.  **Save Task List:** Save the generated document in the `/tasks/` directory with the filename `PRD-[number]-[Feature-Name].Tasklist.md`, where `[number]` is the PRD number and `[Feature-Name]` describes the main feature in Title-Case (e.g., if the request was about user profile editing, the output is `PRD-001-User-Profile-Editing.Tasklist.md`).

    **IMPORTANT:** Before saving, determine the correct number by finding the highest existing PRD number in `/tasks/` and using that same number (since task lists correspond to their PRDs).

## Output Format

The generated task list _must_ follow this structure:

```markdown
## Relevant Files

- `path/to/potential/file1.cs` - Brief description of why this file is relevant (e.g., Contains the main component for this feature).
- `path/to/file1.Tests.cs` - Unit tests for `file1.cs`.
- `path/to/another/File.razor` - Brief description (e.g., UI component for data submission).
- `path/to/another/File.Tests.cs` - Unit tests for `File.razor`.
- `path/to/utils/Helper.cs` - Brief description (e.g., Utility functions needed for calculations).
- `path/to/utils/Helper.Tests.cs` - Unit tests for `Helper.cs`.

### Notes

- Unit tests should typically be placed alongside the code files they are testing (e.g., `MyComponent.razor` and `MyComponent.Tests.cs` in the same directory).
- Use `dotnet test [optional/path/to/test/file]` to run tests. Running without a path executes all tests found by the test framework.

## Instructions for Completing Tasks

**IMPORTANT:** As you complete each task, you must check it off in this markdown file by changing `- [ ]` to `- [x]`. This helps track progress and ensures you don't skip any steps.

Example:

- `- [ ] 1.1 Read file` → `- [x] 1.1 Read file` (after completing)

Update the file after completing each sub-task, not just after completing an entire parent task.

## Tasks

- [ ] 0.0 Parent Task Title
  - [ ] 0.1 [Sub-task description 0.1]
  - [ ] 0.2 [Sub-task description 0.2]
- [ ] 1.0 Parent Task Title
  - [ ] 1.1 [Sub-task description 1.1]
- [ ] 2.0 Parent Task Title (may not require sub-tasks if purely structural or configuration)
```

## Interaction Model

The process explicitly requires a pause after generating parent tasks to get user confirmation ("Go") before proceeding to generate the detailed sub-tasks. This ensures the high-level plan aligns with user expectations before diving into details.

## Target Audience

Assume the primary reader of the task list is a **junior developer** who will implement the feature.
