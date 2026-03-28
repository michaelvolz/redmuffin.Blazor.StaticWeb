## Relevant Files

### Scripts

- `scripts/DisplayWarnings.ps1` - PowerShell script to run `dotnet clean` and `dotnet build`, capturing and displaying warnings in a formatted summary.

### Notes

- Before changing any code, please read the `.github/copilot-instructions.md` file in the root of the solution/repository to ensure compliance with existing guidelines.
- Script follows PowerShell cmdlet development guidelines with proper error handling and user-friendly output.
- Uses PowerShell's native text processing capabilities to parse build output.
- Implements color-coded output with emojis for enhanced readability.
- IL\* warnings (specifically IL2111) are displayed separately with softened colors as they are expected in Blazor WebAssembly projects.

## Tasks

- [x] 1.0 Setup and Environment Review
  - [x] 1.1 Review `.github/copilot-instructions.md` for project guidelines and coding standards
  - [x] 1.2 Verify PowerShell version compatibility and ensure script placement in `/scripts/` directory
  - [x] 1.3 Understand project structure and build process requirements

- [x] 2.0 Core Script Development
  - [x] 2.1 Implement `dotnet clean` and `dotnet build` execution with output capture
  - [x] 2.2 Create warning parsing logic to extract warnings from build output
  - [x] 2.3 Implement warning categorization and frequency counting
  - [x] 2.4 Develop formatted output display with emojis and color coding

- [x] 3.0 Advanced Features Implementation
  - [x] 3.1 Implement IL\* warning separation and softened color display
  - [x] 3.2 Add warning sorting by frequency/count
  - [x] 3.3 Create compact and precise output formatting
  - [x] 3.4 Add progress indicators and build status reporting

- [x] 4.0 Testing and Validation
  - [x] 4.1 Test script execution in repository root directory
  - [x] 4.2 Verify warning capture accuracy and completeness
  - [x] 4.3 Validate output formatting and color display
  - [x] 4.4 Test edge cases (no warnings, build errors, etc.)

- [x] 5.0 Documentation and Finalization
  - [x] 5.1 Add comment-based help documentation to script
  - [x] 5.2 Include usage examples and parameter descriptions
  - [x] 5.3 Verify script follows PowerShell best practices and naming conventions
  - [x] 5.4 Final testing and validation of all requirements
