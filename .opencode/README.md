# OpenCode project configuration

This directory is **agent tooling**, not engine code. Nothing here is compiled into Tesseract2D.

> After changing `AGENTS.md` or adding agents/skills, start a new OpenCode session so discovery picks up the changes.

## Layout

```text
.opencode/
├── agents/                 # named subagents with isolated context and permissions
├── skills/                 # on-demand workflows (SKILL.md + supporting files)
└── commands/               # slash commands
```

## Local agents

| ID | Mode | Purpose |
|---|---|---|
| `t2d-reviewer` | `subagent` | Read-only review of changes: golden-rule violations, engine→editor coupling, source/doc drift. |
| `t2d-doc-auditor` | `subagent` | Read-only audit of docs against source: stale badges, drifted citations, aspirational claims. |

Both deny `edit` and `shell`. They are independent checks, not inline role-play.

## Local skills

| ID | Use when |
|---|---|
| `t2d-engine-context` | Editing `Engine/` code, ECS, components, or runtime |
| `t2d-content-build` | Building projects, adding content, touching `.mgcb`, `MockGame`, platform targets |
| `t2d-serialization-verify` | Changing YAML serialization, DTOs, scenes, databases, manifests |
| `t2d-doc-sync` | Updating docs after a code change |
| `t2d-health-task` | Historical workflow for the archived hardening checklist; active work is `docs/14-alpha-todo.md` |

## Vendored third-party skills

These were fetched from GitHub on 2026-11-23. They are **reference material**: the golden rules in `../AGENTS.md` always win, and the 2D-only / DesktopGL-only scope is not negotiable.

| IDs | Source | License | Notes |
|---|---|---|---|
| `brainstorming`, `systematic-debugging`, `test-driven-development`, `writing-plans`, `executing-plans`, `writing-skills`, `requesting-code-review`, `receiving-code-review`, `verification-before-completion`, `using-git-worktrees`, `finishing-a-development-branch`, `subagent-driven-development`, `dispatching-parallel-agents`, `using-superpowers`, `diagnosing-superpowers` | [obra/superpowers](https://github.com/obra/superpowers) | MIT | Skills vendored only. The official OpenCode V2 **plugin** was deliberately not installed, so no `opencode.json` plugin entry and no mandatory workflow injection. Some skill descriptions (notably `using-superpowers`) still ask for automatic use; treat that as optional. `using-git-worktrees` assumes git is callable — it is not on PATH here (the binary lives under GitHub Desktop). |
| `skill-miner`, `skill-personalizer`, `skill-generalizer` | [hqhq1025/skill-optimizer](https://github.com/hqhq1025/skill-optimizer) v2.0.0 | MIT | v2 replaced one `skill-optimizer` skill with these three lifecycle skills: mine workflows from session history, adapt skills to your environment, prepare them for sharing. |
| `impeccable` | [pbakaus/impeccable](https://github.com/pbakaus/impeccable) v4.10.0 | Apache 2.0 | Design vocabulary for the Photino/React host (24 commands). Slash command at `commands/impeccable.md`. Skills and command copied manually; no design hook, detector binary, or MCP installed. |
| `ui-ux-pro-max`, `ui-styling`, `design`, `design-system`, `brand`, `banner-design`, `slides` | [nextlevelbuilder/ui-ux-pro-max-skill](https://github.com/nextlevelbuilder/ui-ux-pro-max-skill) | MIT | Installed with `npx ui-ux-pro-max-cli init --ai opencode --offline`. Local catalogs and Python search scripts are included. Python 3 is available on this machine. |

## Deliberately skipped

| Tool | Reason |
|---|---|
| `gitnexus` | Its installer writes `AGENTS.md` (clobbering project instructions), needs git on PATH, and adds an MCP server. Revisit only if a git executable is put on PATH and the config changes are reviewed. |
| `claude-mem` | Needs a Bun worker, local HTTP service, MCP tools, and session hooks. Bun is not installed here, so a partial install would be inert. |
| Superpowers plugin mode | Requires a git-backed dependency spec resolved at runtime; git is not on PATH. Vendoring the skills gives the useful content without changing global workflow. |

## Trust boundary

Third-party skills are instructions from external repositories. They may suggest tools, workflows, or installs that are wrong for this project. Never let them:

- add engine→editor coupling,
- touch `GISM/`,
- introduce 3D, networking, Lua, Vulkan, or a separate localization system,
- modify the saved test projects under `D:\GameDevelopment\TestFolderEngine\TestProject\`,
- change WinForms beyond stabilization or modify `MockGame`.

## Updating

Each vendored set is a plain copy from `main` at the date above. To update, fetch the upstream again and replace that skill directory, then start a new session. There is no lockfile or automatic updater for the vendored copies.
