---
name: Tesseract2D health task (historical)
description: Historical workflow for the archived hardening checklist; active Alpha work uses docs/14-alpha-todo.md in Tesseract2D
---

# Tesseract2D health task (historical)

`docs/11-health-todo.md` is an archive index. The full historical checklist is in `docs/archive/11-health-todo.md`. Active Alpha v1 work belongs in `docs/14-alpha-todo.md`; feature and scope decisions belong in `docs/12-future-roadmap.md`. Do not open new hardening items from the archive.

## Workflow

1. For a historical question, read the item in `docs/archive/11-health-todo.md`, its issue in `docs/10-known-issues.md`, and its evidence in `docs/00-status-and-scope.md`.
2. Re-verify the claim against current source before answering. Line numbers drift; search by symbol. If it is already fixed, say so and update the affected docs instead of writing code.
3. If the request is active Alpha work, route it to `docs/14-alpha-todo.md` and use the normal planning/implementation workflow instead of this skill.
4. Build the affected project individually. Use `t2d-content-build` for build and content constraints.
5. Verify with the manual saved-project workflow in the archive under **Existing verification assets**. Unit tests under `Engine.Tests` supplement this; they do not replace it.
6. Update `docs/00`, `docs/10`, and the archive only when correcting a historical claim. Active Alpha status goes in `docs/14-alpha-todo.md` via `t2d-doc-sync`.

## Execution constraints

- WinForms is frozen after roadmap Phase 0. Command/dirty and inspector-metadata work lands in the new console host, not as deep WinForms refactors.
- P3-4 (WinForms dirty tracking) was dropped; do not resurrect it.
- P3-3 landed as an engine host policy: assembly swaps report `ReloadRequired`, and `scene.reload` reloads from disk. Frozen WinForms is not wired to that event.
- Do not add 3D, networking, Lua, Vulkan, or a separate localization system as a "bonus" fix.
