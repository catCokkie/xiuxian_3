# Repo Working Rules

## Single Source Of Truth

- Task status, completion notes, blockers, and phase progress must be maintained only in `docs/design/10_todo.md`.
- Do not duplicate per-task progress in agent prompt files, scratch notes, or temporary planning docs unless the user explicitly asks for a separate plan artifact.
- If an agent instruction needs to mention project progress, it should reference `docs/design/10_todo.md` instead of restating task state.

## Update Rule

- After finishing a task, update `docs/design/10_todo.md` in the same work session.
- Keep agent-facing instruction files focused on workflow rules, not implementation status.

## Priority

- If `AGENTS.md` and other notes disagree on task state, treat `docs/design/10_todo.md` as canonical.
