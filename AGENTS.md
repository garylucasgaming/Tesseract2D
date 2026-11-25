# Agent instructions — Tesseract2D

Persistent guidance for AI agents working in this repository. Human-readable documentation lives in [docs/](docs/); start with [README.md](README.md).

## Golden rules

1. **The engine is unfinished.** Never assume a build target or manager feature works just because a class exists. Scene *activation* works (`SceneManager.AddScene`/`LoadFirstScene`/`LoadNextScene`/`Update`), but serialized `LoadScene`/`PreLoadScene` are TODOs. `GameHost` calls custom `GameManager` `Initialize`/`Update` for active registered managers and `Shutdown` for all registered managers, but `IsPersistent` and additive scene switching are unused. Check the status matrix in [docs/00-status-and-scope.md](docs/00-status-and-scope.md).
2. **`Engine/` (the `Engine.Core` project) is the engine.** Gameplay code, ECS, serializers, physics, and utilities all live under `Engine/`. The `Engine.*` siblings are wrapper targets; most are template stubs. `Engine.Gameplay` is a placeholder, not the game.
3. **Do not make engine→editor coupling worse.** The stated philosophy (`Engine/Engine_Manifest.txt:4`, "the engine knows nothing about the editor and never will") is now honored for engine code: `IEngineContext`/`SessionGlobals` are injectable in `Engine.Core`, and the concrete `EditorContextManager` bridge lives in `Editor/Services/`. Static bridges remain for frozen WinForms callers; new engine code should not add dependencies on them. See [docs/01-architecture.md](docs/01-architecture.md).
4. **Verify against source, not comments or manifests.** `Engine_Manifest.txt` and `Editor/Templates/.cs/*.tmpl` describe intent and sometimes drift from the code. Cite exact paths and line numbers when making claims.
5. **Do not touch `GISM/`.** It is an abandoned experiment and is not in the main solution.
6. **Do not trust the old checked-in `.scene`/YAML samples.** `HierarchyTest.scene` is stale JSON — not a format reference. Portable current-format fixtures now live under `Engine.Tests/TestContent/Fonts/`; for real end-to-end examples, use the author's saved projects under `D:\GameDevelopment\TestFolderEngine\TestProject\` (`AnotherUITest`, `Fonts`, `UITest`); see [archived verification assets](docs/archive/11-health-todo.md). Never modify those saved projects — copy them to a scratch location first.

7. **Scope is 2D only.** 3D, networking, Lua, Vulkan/other backends, and localization-as-a-new-feature are out of scope per [docs/12-future-roadmap.md](docs/12-future-roadmap.md). Do not add them "helpfully" mid-task.
8. **WinForms is frozen.** Stabilize only; do not add features there. The Photino/React console host is the successor, and there is deliberately no cross-process edit sync with WinForms.
9. **`MockGame` is a sentinel.** It exists for an external experimental dev-branch MonoGame content builder. Do not delete it or wire behaviour into it.
10. **The author's dev-branch content pipeline is authoritative.** It auto-registers new files into `.mgcb` and hot-reloads assets. Do not "repair" stale checked-in content config without validating against that workflow. See [docs/09-build-and-content.md](docs/09-build-and-content.md).

## Read order for a new task

| Task | Read first |
|---|---|
| Any engine change | `docs/00`, `docs/01`, `docs/02` |
| ECS / systems / queries | `docs/02-ecs.md`, `Engine/ECS/Query.cs`, `Engine/ECS/Managers/SystemsManager.cs` |
| Serialization / file formats | `docs/03-projects-and-serialization.md`, `Engine/Serialization/` |
| Databases / data assets | `docs/04-databases.md`, `Engine/Collections/Database.cs` |
| Editor or inspector work | `docs/07-editor-internals.md`, `Editor/Form1.cs`, `Editor/PropertyWrappers/` |
| React / new editor planning | `docs/08-react-editor-boundary.md` and `docs/12-future-roadmap.md` §4 Phase 5 |
| Alpha v1 work | `docs/14-alpha-todo.md`, then `docs/12-future-roadmap.md` |
| Feature or scope question | `docs/12-future-roadmap.md` |
| Build failures | `docs/09-build-and-content.md` |
| "Why does this behave oddly?" | `docs/10-known-issues.md` |

## Conventions observed in the code

- Unity-flavoured names: `GameObject`, `GameComponent`, `gameObject` (lowercase back-reference), `AddComponent<T>`, `GetComponent<T>`, `Start`/`Update`/`OnEnable`, `UsedInEditor` (= ExecuteInEditMode).
- Lifecycle spellings differ: `GameComponent.OnEnabled/OnDisabled` vs `ScriptComponent.OnEnable/OnDisable` (no `d`). Both exist.
- "Space" means three different things: `GameSpace` (project metadata), `PhysicsManager.PhysicsSpace` (the Aether `World`), `UISpace` (Screen/World enum). See the glossary in `docs/01`.
- Serialization uses YamlDotNet with `NullNamingConvention` (DTO member names written verbatim). `[Ignore]` is detected by attribute *name string*; `[Browsable(false)]` and `[DatabaseIgnore]` only affect editor UI. Scene/entity/map/database DTOs carry a `Version`; future versions throw, stale versions warn, missing versions default to current, and strict failure is opt-in (`Engine/Serialization/SerializationVersion.cs:12-22`).
- Components need public parameterless constructors for reflection-based creation and deserialization. Only **public instance fields and read/write properties** are serialized.
- System execution order is registration order; there is no priority field. Built-in systems are created in `SystemsManager`'s constructor. `GameScene.FinalizeInitialization()` is the canonical engine-only scene-ready step; `resetContextSceneInManagers()` is just an alias (`Engine/ECS/GameScene.cs:126-137`, `:286-289`).
- `GameComponent.isEnabled` defaults to `false`; both `AddComponent<T>()` and `AddComponent(GameComponent)` set it to `true` after registration and duplicate checks (`Engine/ECS/GameObject.cs:213-250`).
- `TransformComponent` and Aether physics components stay separate: sync positions through `PhysicsManager.PixelsPerMeter` with epsilon guards; do not merge their coordinate spaces (`Engine/ECS/Systems/PhysicsSystem.cs:69-164`).

## Verification guidance

- .NET 8 SDK is required for the main projects; editor requires Windows/WinForms; iOS/Android targets need .NET 9 workloads. A plain `dotnet build Tesseract2D.sln` on Windows will fail on mobile targets without those workloads — build individual projects instead.
- CI runs `scripts/build.ps1:23-50` via `.github/workflows/ci.yml:19-21`, and `Engine.Tests` is an xUnit suite with 71 `[Fact]` tests; it supplements the author's manual saved-project integration workflow rather than replacing it. The author's primary integration test is manual: open the editor and load the saved projects under `D:\GameDevelopment\TestFolderEngine\TestProject\` (three valid current-format projects; see the archived verification assets in `docs/archive/11-health-todo.md`). Verify changes by building the affected project and, where relevant, running the editor (`dotnet run --project Editor/Engine.Editor.csproj`) and checking the console log.
- Content is built by `Engine.Content` via `Editor/Form1.cs:388-420` at editor startup; paths in that area are inconsistent, so asset-loading bugs are often path bugs. See `docs/09`.
- After changing nested `AGENTS.md` files (if added later), start a new session so the new instructions apply.

## Project agent tooling

- Read-only reviewers live in `.opencode/agents/`: `t2d-reviewer` reviews changes, `t2d-doc-auditor` audits docs against source. Use them for independent checks instead of duplicating that role inline.
- Reusable workflows live in `.opencode/skills/`: `t2d-engine-context`, `t2d-content-build`, `t2d-serialization-verify`, `t2d-doc-sync`, `t2d-health-task` (historical hardening workflow only). Load the relevant one before engine, content, serialization, docs, or archived hardening work.
- Third-party skills vendored alongside them are reference material. They never override the golden rules in this file. Sources, versions, and deliberately skipped tools are recorded in `.opencode/README.md`.
- After changing this file or adding agents/skills, start a new session.

## When writing documentation

- Reference style, exact `path:line` citations, real snippets from source, explicit status badges (`Working`/`Partial`/`Stub`/`Experimental`/`Unused/WIP`).
- Prefer linking between docs over duplicating explanations.
- Never present an aspirational design as implemented. Call out the gap.
