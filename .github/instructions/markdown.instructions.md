---
description: 'Documentation and content creation standards'
applyTo: '**/*.md'
---

## Markdown Content Rules

The following markdown content rules are enforced in the validators:

1. **Headings**: Use appropriate heading levels (H2, H3, etc.) to structure your content. Do not use an H1 heading, as this will be generated based on the title.
2. **Lists**: Use bullet points or numbered lists for lists. Ensure proper indentation and spacing.
3. **Code Blocks**: Use fenced code blocks for code snippets. Specify the language for syntax highlighting.
4. **Links**: Use proper markdown syntax for links. Ensure that links are valid and accessible.
5. **Images**: Use proper markdown syntax for images. Include alt text for accessibility.
6. **Tables**: Use markdown tables for tabular data. Ensure proper formatting and alignment.
7. **Line Length**: Limit line length to 400 characters for readability.
8. **Whitespace**: Use appropriate whitespace to separate sections and improve readability.

## MarkdownLint Rules (MD001-MD059)

### Heading Rules
- **MD001** *heading-increment*: Heading levels increment by one only (H2→H3, not H2→H4)
- **MD003** *heading-style*: Consistent heading style (atx: `##`, setext: underlines, atx_closed: `## Title ##`)
- **MD018** *no-missing-space-atx*: Space required after `#` in headings
- **MD019** *no-multiple-space-atx*: Single space after `#` in headings
- **MD020** *no-missing-space-closed-atx*: Space inside `## Title ##` style
- **MD021** *no-multiple-space-closed-atx*: Single space inside closed atx headings
- **MD022** *blanks-around-headings*: Blank lines before/after headings
- **MD023** *heading-start-left*: Headings start at line beginning
- **MD024** *no-duplicate-heading*: Unique heading content (unless siblings_only)
- **MD025** *single-title/single-h1*: One H1 per document
- **MD026** *no-trailing-punctuation*: No `.!?` at heading end
- **MD036** *no-emphasis-as-heading*: Use headings, not **bold** for titles
- **MD041** *first-line-heading/first-line-h1*: Document starts with H1
- **MD043** *required-headings*: Enforce specific heading structure

### List Rules
- **MD004** *ul-style*: Consistent unordered list markers (`-`, `*`, `+`)
- **MD005** *list-indent*: Consistent indentation at same level
- **MD007** *ul-indent*: Unordered list indentation (default: 2 spaces)
- **MD029** *ol-prefix*: Ordered list style (ordered: 1,2,3 vs one_or_ordered: all 1s)
- **MD030** *list-marker-space*: Spaces after list markers
- **MD032** *blanks-around-lists*: Blank lines around lists

### Code Rules
- **MD031** *blanks-around-fences*: Blank lines around fenced code blocks
- **MD038** *no-space-in-code*: No spaces inside `` `code` ``
- **MD040** *fenced-code-language*: Language specified for fenced blocks
- **MD046** *code-block-style*: Consistent code block style (indented vs fenced)
- **MD048** *code-fence-style*: Consistent fence style (``` vs ~~~)

### Link Rules
- **MD011** *no-reversed-links*: Correct link syntax `[text](url)` not `(url)[text]`
- **MD034** *no-bare-urls*: Wrap URLs in `<>` or use `[text](url)`
- **MD039** *no-space-in-links*: No spaces in `[link text](url)`
- **MD042** *no-empty-links*: Links must have destinations
- **MD051** *link-fragments*: Valid link fragments/anchors
- **MD052** *reference-links-images*: Reference labels must be defined
- **MD053** *link-image-reference-definitions*: Remove unused references
- **MD054** *link-image-style*: Consistent link style (inline vs reference)
- **MD059** *descriptive-link-text*: Meaningful link text (not "click here")

### Spacing & Formatting Rules
- **MD009** *no-trailing-spaces*: No trailing whitespace
- **MD010** *no-hard-tabs*: Use spaces, not tabs
- **MD012** *no-multiple-blanks*: Single blank lines only
- **MD013** *line-length*: Line length limit (default: 80, often disabled)
- **MD027** *no-multiple-space-blockquote*: Single space after `>`
- **MD028** *no-blanks-blockquote*: No blank lines in blockquotes
- **MD037** *no-space-in-emphasis*: No spaces in `*emphasis*` or `**strong**`
- **MD047** *single-trailing-newline*: File ends with single newline
- **MD049** *emphasis-style*: Consistent emphasis (`*` vs `_`)
- **MD050** *strong-style*: Consistent strong (`**` vs `__`)

### Table Rules
- **MD055** *table-pipe-style*: Consistent table pipe style
- **MD056** *table-column-count*: Consistent column count in tables
- **MD058** *blanks-around-tables*: Blank lines around tables

### Content Rules
- **MD014** *commands-show-output*: Show output for `$ command` examples
- **MD033** *no-inline-html*: Avoid inline HTML (configurable)
- **MD035** *hr-style*: Consistent horizontal rule style
- **MD044** *proper-names*: Correct capitalization for proper names
- **MD045** *no-alt-text*: Images require alt text

## Formatting and Structure

Follow these guidelines for formatting and structuring your markdown content:

- **Headings**: Use `##` for H2 and `###` for H3. Ensure that headings are used in a hierarchical manner. Recommend restructuring if content includes H4, and more strongly recommend for H5.
- **Lists**: Use `-` for bullet points and `1.` for numbered lists. Indent nested lists with two spaces.
- **Code Blocks**: Use triple backticks (`) to create fenced code blocks. Specify the language after the opening backticks for syntax highlighting (e.g., `csharp).
- **Links**: Use `[link text](URL)` for links. Ensure that the link text is descriptive and the URL is valid.
- **Images**: Use `![alt text](image URL)` for images. Include a brief description of the image in the alt text.
- **Tables**: Use `|` to create tables. Ensure that columns are properly aligned and headers are included.
- **Line Length**: Break lines at 80 characters to improve readability. Use soft line breaks for long paragraphs.
- **Whitespace**: Use blank lines to separate sections and improve readability. Avoid excessive whitespace.

## MarkdownLint Configuration

### Configuration Files
Create configuration files in project root:
- `.markdownlint.json` - Basic JSON configuration
- `.markdownlint.jsonc` - JSON with comments
- `.markdownlint.yaml/.yml` - YAML configuration
- `.markdownlint-cli2.jsonc/.yaml/.cjs` - CLI2 configuration with additional options

### Configuration Precedence (highest to lowest)
1. `.markdownlint-cli2.{jsonc,yaml,cjs}` in same/parent directory
2. `.markdownlint.{jsonc,json,yaml,yml,cjs}` in same/parent directory
3. VS Code user/workspace settings
4. Default configuration

### Example Configuration
```json
{
  "MD013": false,
  "MD003": { "style": "atx" },
  "MD007": { "indent": 4 },
  "MD024": { "siblings_only": true },
  "MD033": { "allowed_elements": ["br", "sub", "sup"] },
  "MD044": { "names": ["JavaScript", "TypeScript", "GitHub"] }
}
```

### Extending Configuration
```json
{
  "extends": "../.markdownlint.json",
  "MD013": { "line_length": 120 }
}
```

## Auto-Fixable Rules

The following rules can be automatically fixed via `Ctrl+.` or Format Document:
- **MD004** *ul-style*, **MD005** *list-indent*, **MD007** *ul-indent*
- **MD009** *no-trailing-spaces*, **MD010** *no-hard-tabs*
- **MD011** *no-reversed-links*, **MD012** *no-multiple-blanks*
- **MD014** *commands-show-output*, **MD018** *no-missing-space-atx*
- **MD019** *no-multiple-space-atx*, **MD020** *no-missing-space-closed-atx*
- **MD021** *no-multiple-space-closed-atx*, **MD022** *blanks-around-headings*
- **MD023** *heading-start-left*, **MD026** *no-trailing-punctuation*
- **MD027** *no-multiple-space-blockquote*, **MD030** *list-marker-space*
- **MD031** *blanks-around-fences*, **MD032** *blanks-around-lists*
- **MD034** *no-bare-urls*, **MD037** *no-space-in-emphasis*
- **MD038** *no-space-in-code*, **MD039** *no-space-in-links*
- **MD044** *proper-names*, **MD047** *single-trailing-newline*
- **MD049** *emphasis-style*, **MD050** *strong-style*
- **MD051** *link-fragments*, **MD053** *link-image-reference-definitions*
- **MD054** *link-image-style*, **MD058** *blanks-around-tables*

## VS Code Integration

### Auto-Format Settings
```json
"[markdown]": {
  "editor.formatOnSave": true,
  "editor.formatOnPaste": true
},
"editor.codeActionsOnSave": {
  "source.fixAll.markdownlint": true
}
```

### Commands
- `markdownlint.fixAll` - Fix all auto-fixable violations
- `markdownlint.lintWorkspace` - Lint entire workspace
- `markdownlint.toggleLinting` - Enable/disable linting
- `markdownlint.openConfigFile` - Open/create config file

## Validation Requirements

Ensure compliance with the following validation requirements:

- **Content Rules**: Ensure that the content follows the markdown content rules specified above.
- **MarkdownLint Rules**: All enabled MD rules must pass (see MD001-MD059 above).
- **Formatting**: Ensure that the content is properly formatted and structured according to the guidelines.
- **Auto-Fix**: Use auto-fixable rules to maintain consistency.
- **Configuration**: Use project-specific `.markdownlint.json` for custom rule settings.
- **Validation**: Run the validation tools to check for compliance with the rules and guidelines.
