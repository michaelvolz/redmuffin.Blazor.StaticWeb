---
description: 'Security rules for input validation, XSS/CSRF, secrets, API usage, CSP, Azure Functions, and authentication'
applyTo: '**/*.cs, **/*.razor'
---

# Security and API Guidelines

## Input Validation

- Always validate and sanitize user inputs
- Use model validation attributes on DTOs
- Never trust client-side validation alone

## XSS and CSRF Protection

- Blazor provides built-in XSS protection - rely on Blazor's escaping
- For traditional web forms, use anti-forgery tokens
- Avoid `dangerouslySetInnerHTML` patterns unless content is strictly sanitized

## Secrets Management

- Never expose secrets in client-side code (Blazor WebAssembly)
- Use Azure Key Vault for API keys and connection strings
- Environment variables for development (never committed to repo)
- `local.settings.json` should be in `.gitignore`

## HTTP Client Usage

- Use `IHttpClientFactory` for all HTTP needs in backend services
- Configure appropriate timeouts
- Handle failures gracefully with retry policies

## Content Security Policy

- Configure CSP in `staticwebapp.config.json`
- Allow `'unsafe-inline'` for styles only
- Restrict scripts to specific sources
- Configure appropriate cache headers

## Azure Functions

- Use isolated worker model with dependency injection
- Validate function keys and authentication tokens
- Use Azure AD authentication where possible
- Implement proper logging and monitoring

## Authentication and Authorization

- Use ASP.NET Core Identity for user management
- Implement Role-Based Access Control (RBAC)
- Protect API endpoints with `[Authorize]` attributes
- Use JWT tokens for API authentication

## API Design

- Follow RESTful conventions
- Use appropriate HTTP status codes
- Implement rate limiting for public APIs
- Document APIs with OpenAPI/Swagger