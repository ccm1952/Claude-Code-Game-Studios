# Claude Code Game Studios -- Game Studio Agent Architecture

Indie game development managed through 48 coordinated Claude Code subagents.
Each agent owns a specific domain, enforcing separation of concerns and quality.

## Technology Stack

- **Engine**: Unity 2022.3.62f2 (LTS)
- **Language**: C# (with HybridCLR hot-reload)
- **Version Control**: Git with trunk-based development
- **Build System**: Unity Build Pipeline + HybridCLR Build
- **Asset Pipeline**: YooAsset 2.3.17 (hot-update capable asset management)

> **Note**: This project uses Unity engine specialists with TEngine framework
> sub-specialists. See `@.claude/docs/technical-preferences.md` for routing.

## Project Structure

@.claude/docs/directory-structure.md

## Engine Version Reference

@docs/engine-reference/unity/VERSION.md

## Technical Preferences

@.claude/docs/technical-preferences.md

## Coordination Rules

@.claude/docs/coordination-rules.md

## Collaboration Protocol

**User-driven collaboration, not autonomous execution.**
Every task follows: **Question -> Options -> Decision -> Draft -> Approval**

- Agents MUST ask "May I write this to [filepath]?" before using Write/Edit tools
- Agents MUST show drafts or summaries before requesting approval
- Multi-file changes require explicit approval for the full changeset
- No commits without user instruction

See `docs/COLLABORATIVE-DESIGN-PRINCIPLE.md` for full protocol and examples.

> **First session?** If the project has no engine configured and no game concept,
> run `/start` to begin the guided onboarding flow.

## Coding Standards

@.claude/docs/coding-standards.md

## Context Management

@.claude/docs/context-management.md
