---
description: 'Optimized C# coding standards and best practices for AI coding assistants'
applyTo: '**/*.cs'
---

**FOR AI CODE ASSISTANTS ONLY** - .NET 9 C# 12/13 development guidelines

## Code Style and Structure

- **Target:** .NET 9, C# 12/13 features
- **var Usage:** Only when type is clearly apparent (e.g., `var items = new List<string>()`)
- **Line Length:** 160 characters maximum
- **Braces:** Always use braces, even for single-line statements
- **Indentation:** Use consistent indentation throughout
- **Async:** Always use `async`/`await` for non-blocking operations
- **Avoid:** `async void`; use `async Task` instead
- **Formatting:** Apply `.editorconfig` style, file-scoped namespaces, single-line usings

## Naming Conventions

- **PascalCase:** Classes, methods, properties, namespaces, interfaces (prefix with "I")
- **camelCase:** Variables, method parameters, local variables
- **Private Fields:** Prefix with `_` and use camelCase (e.g., `_userService`)
- **Descriptive Names:** Avoid abbreviations; prefer meaningful names
- **Components:** File names must match class names

## Modern C# Features (12/13)

| Feature | Example |
|---------|---------|
| Primary Constructors | `public class Person(string name, int age) { ... }` |
| Collection Expressions | `int[] nums = [1,2,3];` |
| Default Lambda Params | `Func<int,int,int> add = (x, y=5) => x+y;` |
| ref readonly Parameters | `void M(ref readonly int x) { ... }` |
| Alias Any Type | `using IntPair = (int, int);` |
| Inline Arrays | `[InlineArray(10)] struct Buffer { ... }` |
| params Collections | `void M(params ReadOnlySpan<T> items) { ... }` |
| New Lock Object | `var l = new Lock(); using(l.EnterScope()) { ... }` |
| New Escape Sequence | `char esc = '\e';` |
| Method Group Natural Type | `var act = (string s) => ...;` |
| Implicit Index Access | `buffer = { [^1]=0 }` |
| ref/unsafe in Iterators | `async Task M() { ref int x = ...; }` |
| Partial Properties | `public partial string Name { get; set; }` |
| Overload Priority | `[OverloadResolutionPriority(1)] void M(int a) {}` |
| Pattern Matching | Use pattern matching and switch expressions |
| Null-coalescing | Use `??`, `??=` operators |
| Records | Prefer records for immutable data structures |
| Expression-bodied | Use expression-bodied members when appropriate |

## Nullable Reference Types

- Declare variables non-nullable, check for `null` at entry points
- Always use `is null` or `is not null` instead of `== null` or `!= null`
- Trust C# null annotations; don't add unnecessary null checks
- Use `nameof` instead of string literals for member names

## Architecture and Design Patterns

- **SOLID Principles:** Follow all SOLID principles
- **Composition:** Prefer composition over inheritance
- **Dependency Injection:** Use DI for decoupling and testability
- **Clean Architecture:** Structure large applications appropriately
- **Design Patterns:** Apply Repository, Strategy, Factory, Adapter patterns where applicable
- **Separation of Concerns:** Models, services, and data access layers
- **Feature Organization:** Use feature-based folders for organization

## Performance and Memory Efficiency

- **Allocations:** Avoid unnecessary allocations (e.g., avoid `ToList()` unless required)
- **Memory Types:** Use `Span<T>` and `Memory<T>` for data slices
- **Async Performance:** Prefer `ValueTask` over `Task` in performance-critical methods
- **Hot Paths:** Avoid boxing/unboxing and excessive object creation in loops
- **Caching:** Implement appropriate caching strategies (in-memory, distributed, response caching)
- **Pagination:** Use pagination, filtering, and sorting for large data sets
- **Compression:** Implement compression and other optimizations

## Error Handling and Logging

- **Exceptions:** Don't silently catch or suppress exceptions
- **Specific Catching:** Catch specific exception types when possible
- **Exception Filters:** Use `when` clauses for improved readability
- **Logging:** Use structured logging (Serilog, Microsoft.Extensions.Logging)
- **Global Handling:** Implement global exception handling with middleware
- **Consistent Responses:** Create consistent error responses across APIs
- **Problem Details:** Use RFC 7807 for standardized error responses

## Security Best Practices

- **Input Validation:** Always validate/sanitize user input
- **XSS/CSRF:** Use built-in protections and best practices
- **Secrets:** Never expose secrets in client code; use secrets management
- **Output Encoding:** Sanitize and encode output to prevent attacks
- **Secure Algorithms:** Use SHA-256, AES-GCM for hashing and encryption
- **Authentication:** Use ASP.NET Core Identity or JWT tokens
- **Authorization:** Implement role-based and policy-based authorization
- **HTTPS:** Use HTTPS for all communication
- **CORS:** Implement proper CORS policies

## API Design and Integration

- **HTTP Client:** Use `IHttpClientFactory` for HTTP calls
- **Minimal APIs:** Prefer minimal APIs for backend services
- **Versioning:** Implement API versioning strategies
- **Documentation:** Use Swagger/OpenAPI with proper documentation
- **Endpoints:** Document endpoints, parameters, responses, authentication
- **Error Handling:** Implement proper error handling for API calls
- **User Feedback:** Provide clear user feedback for API operations

## Testing and Quality

- **Unit Tests:** Use TUnit (NOT NUnit/xUnit/MSTest)
- **Test Attributes:** `[Test]` for methods, `[Tests]` with `[Arguments]` for data-driven
- **Test Structure:** Follow Arrange-Act-Assert pattern
- **Mocking:** Use NSubstitute for mocking dependencies
- **Interfaces:** Ensure services are injected through interfaces
- **Test Methods:** Keep methods small and focused; one method = one responsibility
- **Critical Paths:** Always include tests for critical application paths
- **Integration Tests:** Test API endpoints and authentication/authorization
- **TDD:** Apply test-driven development principles

## Data Access and Entity Framework

- **Entity Framework Core:** Use for data access layer
- **Database Options:** SQL Server, SQLite, In-Memory for different environments
- **Repository Pattern:** Implement when beneficial
- **Migrations:** Handle database migrations and data seeding
- **Query Patterns:** Use efficient patterns to avoid performance issues
- **Async Operations:** Use async methods for database operations

## File Organization and Structure

- **File-scoped Namespaces:** Use namespaces that match folder structure
- **One Type Per File:** Define one type per file
- **Immutability:** Prefer immutable types unless mutability requested
- **Records:** Prefer records over classes for immutable types
- **Record Design:** Define properties on same line as record declaration
- **Factory Pattern:** Accompany records with static factory classes
- **Static Create:** Expose static `Create` method for instantiation
- **Validation:** Place argument validation in factory `Create` method
- **Collections:** Use `ImmutableList<T>` in records when possible
- **Extensions:** Define record behavior in extension methods

## Discriminated Unions

- **Records:** Use records for discriminated unions
- **Inheritance:** Derive specific types from base abstract record
- **Single File:** Define entire union in one file
- **Factories:** One static factory class per union
- **Factory Methods:** One static factory method per variant
- **Consistency:** Follow record design rules for unions

## Comments and Documentation

- **XML Documentation:** Create for all public APIs
- **Examples:** Include `<example>` and `<code>` documentation
- **Design Decisions:** Comment on why certain decisions were made
- **Libraries:** Mention usage and purpose of external dependencies
- **Clear Comments:** Write clear, concise comments for functions
- **Edge Cases:** Document edge case handling

## Constants and Magic Values

- **Magic Numbers:** Avoid magic numbers and strings; use constants or enums
- **Configuration:** Use configuration system for environment-specific settings
- **Environment Settings:** Explain Program.cs and ASP.NET Core 9 configuration

## Deployment and DevOps

- **Containerization:** Use .NET's built-in container support
- **Publishing:** `dotnet publish --os linux --arch x64 -p:PublishProfile=DefaultContainer`
- **CI/CD:** Implement pipelines for .NET applications
- **Health Checks:** Implement health checks and readiness probes
- **Monitoring:** Use Application Insights for telemetry and monitoring
- **Performance:** Implement correlation IDs and request tracking

## Best Practices Summary

- Write maintainable, testable code with clear separation of concerns
- Use dependency injection and favor interfaces over concrete types
- Avoid static classes when testability is important
- Handle edge cases with clear exception handling
- Make high-confidence suggestions when reviewing code
- Keep builds and tests passing before merging
- Reference code with filename and line numbers
- Use structured logging and monitoring for production applications
