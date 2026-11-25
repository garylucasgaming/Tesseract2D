---
name: Tesseract2D doc sync
description: Use after any Tesseract2D code change to update affected docs with exact path:line citations, honest status badges, and cross-links
---

# Tesseract2D doc sync

Use this after a code change, not before. The goal is documentation that matches the source exactly.

## Workflow

1. Identify which of `docs/00`–`docs/14` and `README.md` are affected by the change.
2. Re-open the changed source and record exact `path:line` anchors. Search by symbol if lines moved.
3. Update status badges using `Working` / `Partial` / `Stub` / `Experimental` / `Unused/WIP`. Split a row if one part works and another does not — for example, scene activation works while serialized loading does not.
4. Replace aspirational wording with the actual behaviour. Comments that claim a hook is "called automatically" are not evidence it is called.
5. Add or fix cross-links instead of duplicating explanations. Active Alpha work goes in `docs/14-alpha-todo.md`; features and scope go in `docs/12-future-roadmap.md`; the closed hardening pass is archived at `docs/11-health-todo.md`.
6. Validate all Markdown links resolve before reporting done.

## Style rules

- Repo-root-relative paths with line numbers, e.g. `Engine/ECS/GameScene.cs:222-225`.
- Real snippets from source, not paraphrased intent.
- Explicitly call out gaps: todo methods, missing call sites, stale configs, stub hosts.
- Never present a design or manifest as implemented.

## Related

Load `t2d-engine-context` when the status claim concerns engine code, and `t2d-serialization-verify` when it concerns file formats.
