---
name: markdown
description: Markdown content creation standards, MarkdownLint rules (MD001-MD036), formatting guidelines, and VS Code integration.
invocable: false
---

# Markdown Standards

## Content Rules

1. **Headings**: Use appropriate heading levels (H1, H2, H3, etc.)
2. **Lists**: Use bullet points or numbered lists with proper indentation
3. **Code Blocks**: Use fenced code blocks with language specified
4. **Links**: Use proper `[text](url)` syntax
5. **Images**: Use `![alt text](image URL)`
6. **Tables**: Use markdown tables with proper alignment
7. **Line Length**: Limit to 400 characters for readability
8. **Whitespace**: Use appropriate whitespace to separate sections

## MarkdownLint Rules (MD001-MD059)

### Heading Rules
- MD001: Heading levels increment by one only
- MD003: Consistent heading style (atx: `##`)
- MD018-021: Space requirements around `#`
- MD022: Blank lines before/after headings
- MD024: No duplicate headings
- MD025: One H1 per document
- MD026: No `.!?` at heading end

### List Rules
- MD004: Consistent unordered list markers (`-`)
- MD007: Unordered list indentation (default: 2 spaces)
- MD029: Ordered list style
- MD030: Spaces after list markers
- MD032: Blank lines around lists

### Code Rules
- MD031: Blank lines around fenced code blocks
- MD038: No spaces inside `` `code` ``
- MD040: Language specified for fenced blocks
- MD046: Consistent code block style
- MD048: Consistent fence style

### Spacing Rules
- MD009: No trailing whitespace
- MD010: No hard tabs (use spaces)
- MD012: Single blank lines only
- MD013: Line length limit

### Link Rules
- MD034: No bare URLs (use descriptive link text)

## Formatting

- Use `##` for H2 and `###` for H3
- Use `-` for bullet points and `1.` for numbered lists
- Break lines at 100 characters for readability
- End file with single newline

## Link Syntax

Use descriptive text instead of bare URLs:

```markdown
BAD:  Visit https://example.com for more info
GOOD: Visit [the documentation](https://example.com) for more info
```

## VS Code Integration

```json
"[markdown]": {
  "editor.formatOnSave": true,
  "editor.formatOnPaste": true
},
"editor.codeActionsOnSave": {
  "source.fixAll.markdownlint": true
}
```

## Auto-Fixable Rules

Most rules can be fixed via `Ctrl+.` or Format Document:
- MD004, MD005, MD007, MD009, MD010, MD011, MD012
- MD018, MD019, MD020, MD021, MD022, MD023, MD026
- MD031, MD032, MD034, MD037, MD038, MD039, MD047
