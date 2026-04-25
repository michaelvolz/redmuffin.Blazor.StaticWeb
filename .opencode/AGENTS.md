# Global Rules for OpenCode

## Communication Protocol (CRITICAL)

**Questions = Discussion Phase Only**

- When I ask a question, I ONLY want ANSWERS - NOT actions
- Question means discussion, not action
- It is NEVER okay to do/install/create/change something when asked a question
- I will explicitly say "proceed" or "go" when action can start
- If unsure whether something is a question or a request, ask for clarification first

## File System

- File and directory names: Use PascalCase
- When uncertain about naming, ask first

## DHH/Omarchy Package Management Principles

### When started from WSL

This system runs on WSL with Arch Linux (user's remix of Omarchy principles). Follow these principles:

#### Language Runtimes & Package Managers

- **Preferred**: Pacman/AUR for system packages (aligned with Omarchy)
- **node/npm**: Use Arch packages (`nodejs`, `npm`) - NOT mise, fnm, nvm
- **Python**: Use Arch packages (`python`, `python-pip`) - NOT pyenv, uv, pipx
- **Rust**: Use Arch packages (`rust`) - NOT rustup
- **Ruby**: Use Arch packages

#### npm/JavaScript

- **DO NOT use**: mise, fnm, nvm, or any version managers for node
- **Use**: Pacman nodejs/npm
- For npm packages:
  - Use `npx --yes <package>` for one-off commands
  - Use `omarchy-npx-install <package> <command>` for persistent tools (DHH's method)
- Global npm packages installed via pacman node are discouraged; prefer npx/omarchy-npx-install

#### When Researching

- If you don't know who someone is (e.g., "DHH"), ALWAYS search the web
- DHH = David Heinemeier Hansson, creator of Ruby on Rails, Omakub (Ubuntu), Omarchy (Arch)
