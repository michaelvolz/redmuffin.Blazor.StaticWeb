---
name: grill-me
description: Interview the user relentlessly about a plan or design until reaching shared understanding, resolving each branch of the decision tree. Use when user wants to stress-test a plan, get grilled on their design, or mentions "grill me".
---

Interview me relentlessly about every aspect of this plan until we reach a shared understanding. Walk down each branch of the design tree, resolving dependencies between decisions one-by-one. For each question, provide your recommended answer.

Ask the questions one at a time, waiting for feedback on each question before continuing.

If a question can be answered by exploring the codebase, explore the codebase instead.

## Asking questions

Prefer the `question` tool over text output. It gives the user clickable options instead of making them type.

The `question` tool works for any question you can frame as multiple choice. The auto-added "Type your own answer" option covers cases where none of the listed choices fit. Use it for:

- Decision branches (2–6 clear options)
- "Pick one" or "pick many" questions
- Any question where you can enumerate reasonable answers

Fall back to text output only when the question truly can't be framed as options — open-ended exploration ("Describe what you mean"), creative prompts, or follow-ups to a custom answer where you need the user's exact words.

When using the `question` tool:

- `header`: max 12 characters, a very short label
- `label`: max 30 characters per option, concise display text
- `description`: longer explanation of what the option means
- Put the recommended option first and append " (Recommended)"
