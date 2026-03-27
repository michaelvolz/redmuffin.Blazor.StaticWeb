---
aliases: [design-patterns]
description: C#/.NET design pattern review checklist
---
C#/.NET Design Pattern Review Checklist

## Required Patterns
- **Command Pattern**: Generic base classes, ICommandHandler<TOptions> interface, CommandHandlerOptions inheritance
- **Factory Pattern**: Complex object creation, service provider integration
- **Dependency Injection**: Primary constructors, ArgumentNullException null checks, interface abstractions
- **Repository Pattern**: Async data access, provider abstractions
- **Provider Pattern**: External service abstractions, clear contracts, configuration handling

## Review Checklist
- Design Patterns: Command Handler, Factory, Provider, Repository correctly implemented?
- Architecture: Namespace conventions? Proper separation of concerns?
- .NET Best Practices: Primary constructors, async/await, ResourceManager, structured logging?
- GoF Patterns: Command, Factory, Template Method, Strategy patterns?
- SOLID Principles: Any violations?
- Performance: Async/await, resource disposal, ConfigureAwait(false)?
- Testability: Mockable components, async testability, AAA pattern?
- Security: Input validation, secure credential handling, parameterized queries?
- Documentation: XML docs for public APIs?

## Key Focus Areas
- Command Handlers: Validation in base class, consistent error handling
- Factories: Dependency configuration, service provider integration
- Providers: Connection management, async patterns, exception handling
- Configuration: Data annotations, validation attributes