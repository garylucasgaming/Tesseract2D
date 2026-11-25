# Tesseract2D

A custom 2D game engine built on **MonoGame 3.8.x**, with **Aether Physics2D** for physics, a Unity-flavoured ECS, YAML-based serialization, and a data/database system inspired by Unity ScriptableObjects. The editor is currently a separate WinForms application that hosts a MonoGame viewport; the long-term goal is a framework-agnostic editor (e.g. React) talking to the engine through a real API boundary.

> **Important:** This engine is **unfinished**. Core ECS, serialization, databases, physics, the shared host loop, and the editor work to varying degrees; DesktopGL host boot is partial, the other platform hosts are template stubs, and the Multiplatform tree is an isolated experiment. Read [docs/00-status-and-scope.md](docs/00-status-and-scope.md) before assuming any given subsystem or build target is production-ready.

## Documentation

| Document | Purpose |
|---|---|
| [00 — Status & Scope](docs/00-status-and-scope.md) | What works, what is stubbed, and what is aspirational |
| [01 — Architecture](docs/01-architecture.md) | Design philosophy, repository map, naming conventions and glossary |
| [02 — ECS Core](docs/02-ecs.md) | GameObjects, components, scenes, systems, managers, queries, frame flow, physics |
| [03 — Projects & Serialization](docs/03-projects-and-serialization.md) | On-disk project layout, YAML schemas, references, type resolution |
| [04 — Databases & DataComponents](docs/04-databases.md) | The ScriptableObject-analogue data system |
| [05 — Runtime](docs/05-runtime.md) | Boot flow, runtime services, and what is not yet wired up |
| [06 — Authoring Guide](docs/06-authoring-guide.md) | Writing components, systems, scripts, managers, and data in C# and YAML |
| [07 — Editor Internals](docs/07-editor-internals.md) | How the WinForms editor is built and how it touches the engine |
| [08 — React Editor Boundary](docs/08-react-editor-boundary.md) | What a non-WinForms editor would need from the engine |
| [09 — Build & Content](docs/09-build-and-content.md) | Solutions, build targets, prerequisites, content pipeline |
| [10 — Known Issues](docs/10-known-issues.md) | Consolidated gotchas with source citations |
| [11 — Engine Health TODO](docs/11-health-todo.md) | Archived hardening pass; active work moved to the Alpha TODO |
| [12 — Future Roadmap](docs/12-future-roadmap.md) | Alpha v1.0 phases, exit criteria, feature tiers, and tools |
| [13 — Refactor Backlog](docs/13-refactor-todo.md) | Candidate engine/runtime refactors to reconsider before Alpha v1 |
| [14 — Alpha v1 TODO](docs/14-alpha-todo.md) | Active Alpha work queue, derived from roadmap phases and exit criteria |

[AGENTS.md](AGENTS.md) contains concise rules for AI agents working in this repository.

## Repository at a glance

```text
Engine/                  Engine.Core — the actual engine (class library, net8.0)
Engine.Content/          MonoGame content builder tool
Editor/                  WinForms editor (net8.0-windows, MonoGame.Forms.DX)
Engine.Gameplay/         Placeholder game project (nearly empty)
Engine.Desktop/          DesktopGL host booting the shared GameHost (partial)
Engine.Web/              Web host experiment (KNI/nkast BlazorGL)
Engine.Ios/              iOS host stub (MonoGame)
Engine.Android/          Android host stub (MonoGame)
Engine.Multiplatform/    Isolated KNI/nkast multi-platform template experiment
GISM/                    Abandoned custom markup-language experiment — ignore
SavedGameProjectData/    Hardcoded development sandbox used by the editor
docs/                    This documentation
```

The real engine code lives in `Engine/`. The platform folders are mostly wrapper targets, several of which are still empty templates. See [01-architecture.md](docs/01-architecture.md) for the dependency picture and [09-build-and-content.md](docs/09-build-and-content.md) for build commands.

## Two framework stacks

- **MonoGame 3.8.x** — `Engine.Core`, editor, Desktop, iOS, Android, content pipeline.
- **KNI/nkast 4.2.9001** — `Engine.Web` and `Engine.Multiplatform` only. **There is no FNA in this repository.**

## Quick orientation for newcomers

1. Read [00-status-and-scope.md](docs/00-status-and-scope.md) — know what is real.
2. Read [01-architecture.md](docs/01-architecture.md) — especially the glossary of overloaded names (`GameSpace`, `PhysicsSpace`, `UISpace`).
3. Read [02-ecs.md](docs/02-ecs.md) and [04-databases.md](docs/04-databases.md) — the two central systems.
4. Use [06-authoring-guide.md](docs/06-authoring-guide.md) when writing game code.
5. Consult [10-known-issues.md](docs/10-known-issues.md) when something behaves surprisingly.
6. Read [12-future-roadmap.md](docs/12-future-roadmap.md) for the Alpha v1.0 plan and the 2D-only / DesktopGL-only scope boundaries.
7. Use [14-alpha-todo.md](docs/14-alpha-todo.md) as the active work queue; the old health checklist is archived.
