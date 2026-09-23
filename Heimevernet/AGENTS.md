# AGENTS.md

## Project Overview

This repository contains our group project for UiA.

The project is developed as a team, so changes should be made carefully and consistently with the existing codebase.

The main goal is to build a working, maintainable application while keeping the code understandable for all members of the group.

---

## General Rules

Before making changes:

1. Read the existing code before modifying it.
2. Follow the existing project structure and conventions.
3. Do not rewrite working code unnecessarily.
4. Keep changes focused on the task.
5. Avoid introducing new dependencies unless they are actually needed.
6. Do not delete or rename files without a clear reason.
7. Do not modify configuration files unless the task requires it.
8. Preserve existing functionality when adding new features.
9. If something is unclear, inspect related code before making assumptions.
1. Don't ever commit code that doesn't compile or run. and before committing, make sure to run the application and tests to ensure everything works as expected.
1. Ask before making any changes and before deleting or renaming files. If you are unsure, ask a team member for guidance.

---

## Team Development

This is a group project.

Code should be written so that another team member can understand and continue working on it.

Prefer:

- Clear variable and method names
- Small, focused methods
- Simple solutions over clever solutions
- Consistent formatting
- Comments only where they add useful context
- Existing project patterns over introducing new patterns

Avoid:

- Over-engineering
- Unnecessary abstractions
- Huge methods
- Duplicate code
- Hard-coded values when configuration or existing constants should be used
- Changing architecture just to solve a small problem

---

## Git Rules

Use small, focused commits.

Commit messages should describe what changed.

Examples:

```text
Add responsive navigation
Fix validation for user registration
Update database seed data
Refactor resource controller