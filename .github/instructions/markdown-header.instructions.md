---
description: 'Documentation and content creation standards'
applyTo: '**/*.md'
---

# Markdown Header Instructions

When creating Markdown files, include a standardized, Markdown-compliant header containing essential metadata to provide context and clarity for readers. Follow these best practices for form and content:

1. **Use a YAML Front Matter Block**: Enclose metadata in a `---` delimited YAML block at the top of the file for compatibility with static site generators and Markdown parsers.
2. **Include Key Metadata**:
   - **Title**: A clear, concise title of the document.
   - **Date**: Creation or last updated date (format: `YYYY-MM-DD`).
   - **Project**: The associated project name or identifier.
   - **Author**: The creator or primary contributor.
   - **Version**: Document version (e.g., `1.0` or `draft`).
   - **Description**: A brief summary of the document’s purpose.
   - **Tags**: Relevant keywords for categorization (optional).
3. **Follow Markdown Best Practices**:
   - Use `#` for the main document title below the front matter.
   - Keep the header concise yet informative.
   - Ensure metadata is machine-readable and human-friendly.
4. **Example Header**:

```yaml
---
title: Project Documentation
date: 2025-08-03
project: Example Project
author: John Doe
version: 1.0
description: Overview of Example Project's architecture and setup.
tags: [documentation, architecture, csharp]
---
# Project Documentation
```

This ensures vital information is accessible, organized, and consistent across Markdown files.