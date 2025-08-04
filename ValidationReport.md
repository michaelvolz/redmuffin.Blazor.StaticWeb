# Test Migration Validation Report

## Summary

- **Total files validated**: 60
- **Files with all expected methods**: 49
- **Files with missing methods**: 11
- **Total errors**: 49
- **Total warnings**: 48

## Critical Issues Found

### 1. Missing Files

- `tests/redmuffin.Blazor.StaticWeb.Tests/CodeQuality/BlazorCodeBehindEnforcementTests.Infrastructure.cs`
- `tests/redmuffin.Blazor.StaticWeb.Tests/Features/Pages/ApiExamplePage/CallApiExampleTests.Behavior.cs`

### 2. Empty Files (Expected methods but found 0)

- `tests/redmuffin.Blazor.StaticWeb.Tests/Features/Home/HomeTests.Behavior.cs` (Expected: 5 methods)
- `tests/redmuffin.Blazor.StaticWeb.Api.Tests/Api/TestDeserialization.Infrastructure.cs` (Expected: 2 methods)

### 3. Methods in Wrong Files

#### HomeTests Issues

- **HomeTests.Behavior.cs**: Missing 5 methods that should be moved from main file
- **HomeTests.cs**: Has extra methods that should be in Behavior/Infrastructure files

#### VideosTests Issues

- **VideosTests.Infrastructure.cs**: Missing `Videos_Should_Display_Videos_When_Available`
- **VideosTests.cs**: Has extra `Videos_Should_Display_Videos_When_Available` (should be in Infrastructure)

#### CallApiExampleTests Issues

- **CallApiExampleTests.cs**: Has extra `CallApiExample_Should_Clear_Previous_Response_On_New_Call` (should be in Behavior)
- **CallApiExampleTests.Behavior.cs**: File doesn't exist

#### BlazorCodeBehindEnforcementTests Issues

- **BlazorCodeBehindEnforcementTests.cs**: Has extra `Should_Use_Code_Behind_Files_When_Components_Have_Complex_Logic` (should be in Infrastructure)
- **BlazorCodeBehindEnforcementTests.Infrastructure.cs**: File doesn't exist

#### StringExtensionsTests Issues

- **StringExtensionsTests.cs**: Has extra `Should_Reverse_String_Correctly_When_Valid_Input_Provided`

## Recommendations

1. **Create missing partial class files**
2. **Move methods to correct files according to migration mapping**
3. **Verify all test methods are properly categorized**
4. **Re-run validation after fixes**

## Next Steps

1. Fix the most critical issues first (missing files and empty files)
2. Move misplaced methods to their correct locations
3. Validate that all test methods maintain their `[Test]` attributes
4. Ensure all files compile and tests pass
