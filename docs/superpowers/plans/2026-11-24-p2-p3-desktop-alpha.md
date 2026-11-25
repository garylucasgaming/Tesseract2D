# P2/P3 Desktop Alpha Implementation Plan

> **Status:** Historical implementation plan. The P2/P3 health pass is closed; current work is tracked in [14-alpha-todo.md](../../14-alpha-todo.md).

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Close the remaining P2/P3 health items needed for a DesktopGL Alpha v1 runtime, without building a CLI or Photino editor host.

**Architecture:** Introduce an injectable engine context and per-session globals so `Engine.Core` no longer depends on editor state. Add a shared host-owned frame loop consumed by `Engine.Desktop` (real MonoGame window) and headless tests (null renderer). Align script paths and add CI, then document the hot-reload policy. P2-6/P2-7 are deferred to roadmap Phase 5/6; P3-5/P3-6 are out of scope.

**Tech Stack:** .NET 8, MonoGame DesktopGL 3.8.4.1, Aether.Physics2D, YamlDotNet, xUnit, GitHub Actions.

**Spec:** `docs/archive/11-health-todo.md` (historical checklist), `docs/12-future-roadmap.md`, `AGENTS.md`, and `docs/14-alpha-todo.md` (active work)

## Global Constraints

- 2D only; DesktopGL only for Alpha.
- No new engine→editor coupling. `Engine.Core` must not reference any `Editor*` assembly/type.
- WinForms is frozen: keep it working through adapters/shims; add no WinForms features.
- Do not touch `GISM/`, `Engine/Utilities/MockGame.cs`, `.mgcb`, `BuildContent.targets`, or content output paths.
- Build individual projects; `dotnet build Tesseract2D.sln` fails without .NET 9 mobile workloads.
- Verification baseline per task: `dotnet build Engine/Engine.Core.csproj`, `dotnet test Engine.Tests/Engine.Tests.csproj`, `dotnet build Editor/Engine.Editor.csproj`, `dotnet build Engine.Desktop/Engine.Desktop.csproj`, plus manual load of the author's `Fonts` saved project for integration-sensitive changes.
- No commits unless explicitly requested by the human partner.
- Execute in the current working tree: it contains the uncommitted P0/P1 baseline, so a fresh worktree would omit the starting point.

## Review Focus

1. Editor build/saved-project load after context extraction — pinned by Task 1 editor build + manual `Fonts` load.
2. Scene load with no editor project mounted — pinned by Task 2 `DatabaseInjectionTests`.
3. Two sessions do not share cameras/input — pinned by Task 4 `SessionGlobalsTests`.
4. Standalone frames do not secretly require WinForms — pinned by Task 5 `HeadlessHostTests`.
5. Script path changes do not break dev-branch content assumptions — pinned by Task 6 saved-project check and `ProjectPathTests`.

---

### Task 1: P2-1 extract engine context

**Files:**
- Create: `Engine/Runtime/IEngineContext.cs`, `Engine/Runtime/EngineContext.cs`
- Move: `Engine/Serialization/EditorContextManager.cs` → `Editor/Services/EditorContextManager.cs`
- Modify: `Engine/ECS/GameScene.cs:108,148`, `Engine/Gameplay/Map.cs:330`, `Engine/Utilities/AssetManager.cs:61,65,116`, `Engine/ECS/Managers/SystemsManager.cs:211`, `Engine/ECS/Systems/UIInputSystem.cs:55`, `Engine/ECS/Components/UI/UICanvasComponent.cs:157`, `Engine/Serialization/SceneSerializer.cs:143-335`
- Test: `Engine.Tests/ArchitectureTests.cs`, `Engine.Tests/EngineContextTests.cs`

**Interfaces:**
- Produces: `IEngineContext` with project paths, `IsProjectLoaded`, `ActiveScene`, `PlayState`, `SelectedTileIndex`.

- [x] **Step 1: Write failing architecture and context tests** — assert no `Editor*` referenced assembly and a scene load using an in-memory `IEngineContext`.
- [x] **Step 2: Run them and verify they fail** — `dotnet test Engine.Tests/Engine.Tests.csproj --filter "FullyQualifiedName~ArchitectureTests|FullyQualifiedName~EngineContextTests"`
- [x] **Step 3: Implement `IEngineContext`/`EngineContext`, move the editor class, inject consumers** — preserve the editor's static API via `Editor/Services/EditorContextManager.cs`.
- [x] **Step 4: Run the focused tests, then the full suite and four builds** — all green.
- [ ] **Step 5: Manual editor load** — load the author's `Fonts` project; no new console errors.

### Task 2: P2-2 explicit database loading

**Files:**
- Modify: `Engine/ECS/GameScene.cs:106-110`, `Engine/Gameplay/Map.cs:328-344`, `Engine/Serialization/SceneSerializer.cs`, `Editor/Form1.cs`
- Test: `Engine.Tests/DatabaseInjectionTests.cs`

**Interfaces:**
- Consumes: `IEngineContext` from Task 1.
- Produces: `SceneSerializer.LoadScene(path, IEngineContext context, DatabaseManager database, bool strict = false)` and `GameScene.Database` as the only database source.

- [x] **Step 1: Write failing test** — load `TestContent/Fonts` database with no project mounted.
- [x] **Step 2: Verify RED.**
- [x] **Step 3: Inject `DatabaseManager` into `GameScene` and `SceneSerializer`; make `Map.ResolveDatabase` use `scene.Database`.**
- [x] **Step 4: Verify GREEN plus full suite/builds.**

### Task 3: P2-3 explicit runtime-created flag

**Files:**
- Modify: `Engine/ECS/GameScene.cs:142-149`, `:154-212`, `Editor/MGWindowControl.cs:171-216`
- Test: `Engine.Tests/RuntimeEntityFlagTests.cs`

**Interfaces:**
- Produces: `GameScene.CreateEntityInstance(..., bool isRuntimeCreated = false)`.

- [x] **Step 1: Write failing test** — cleanup removes only flagged entities.
- [x] **Step 2: Verify RED.**
- [x] **Step 3: Replace the `EditorContextManager.PlayState` read with the explicit parameter and update call sites.**
- [x] **Step 4: Verify GREEN plus full suite/builds.**

### Task 4: P2-4 session-scoped globals

**Files:**
- Create: `Engine/Runtime/SessionGlobals.cs`, `Engine/Runtime/IInputSource.cs`, `Engine/Runtime/MonoGameInputSource.cs`, `Engine/Runtime/NullInputSource.cs`
- Modify: `Engine/Runtime/Globals.cs:10-43`, `Engine/ECS/Managers/SystemsManager.cs:84`, `Editor/MGWindowControl.cs:50-59`
- Test: `Engine.Tests/SessionGlobalsTests.cs`

**Interfaces:**
- Consumes: `IEngineContext` from Task 1.
- Produces: `SessionGlobals` instance with `InputManager`, `EditorCamera`, viewport, and `IInputSource`.

- [x] **Step 1: Write failing test** — two sessions hold different cameras/input and do not interfere.
- [x] **Step 2: Verify RED.**
- [x] **Step 3: Add `SessionGlobals`, make `Globals` a shim, inject session into systems.**
- [x] **Step 4: Verify GREEN plus full suite/builds.**

### Task 5: P2-5 shared host loop and DesktopGL front end

**Files:**
- Create: `Engine/Runtime/GameHost.cs`, `Engine/Runtime/IFrameRenderer.cs`, `Engine/Runtime/NullFrameRenderer.cs`, `Engine.Desktop/MonoGameFrameRenderer.cs`
- Modify: `Engine.Desktop/Game1.cs:13-58`, `Engine/ECS/Managers/SystemsManager.cs`, `Editor/MGWindowControl.cs:74-169`
- Test: `Engine.Tests/HeadlessHostTests.cs`

**Interfaces:**
- Consumes: Tasks 1–4.
- Produces: `GameHost.Initialize/LoadScene/Update/TickUpdate/Render/RunFrames/Shutdown`, `IFrameRenderer`.

- [x] **Step 1: Write failing headless test** — fixture scene runs 5 frames with null renderer/input; manager hooks count 1/5/1.
- [x] **Step 2: Verify RED.**
- [x] **Step 3: Implement `GameHost`; wire `Engine.Desktop` with MonoGame renderer and editor with delegated common loop.**
- [x] **Step 4: Verify GREEN plus full suite/builds.**
- [ ] **Step 5: Manual DesktopGL run** — launch `Engine.Desktop` outside editor and confirm a scene renders.

### Task 6: P3-2 canonical script paths

**Files:**
- Create: `Engine/Runtime/ProjectPaths.cs`
- Modify: `Editor/Utilities/SolutionGenerator.cs:16,24,37-40`, `Editor/Form1.cs:167-178`, `Editor/Utilities/ScriptCompiler.cs:64-74`, `Editor/Utilities/ScriptAssenblyManager.cs:48-89`, `Editor/Tesseract2DLauncher.cs:49-53`
- Test: `Engine.Tests/ProjectPathTests.cs`

**Interfaces:**
- Produces: one canonical `<project>/Content/Assets/**/*.cs` source root and one canonical compiler output path.

- [x] **Step 1: Verify dev-branch/saved-project assumptions before editing paths.**
- [x] **Step 2: Write failing path-provider test; verify RED.**
- [x] **Step 3: Add `ProjectPaths` and align all four consumers.**
- [ ] **Step 4: Verify GREEN, builds, and scratch-project watcher smoke.**

### Task 7: P3-1 CI and build script

**Files:**
- Create: `scripts/build.ps1`, `.github/workflows/ci.yml`
- Modify: `docs/09-build-and-content.md`

- [x] **Step 1: Write script that runs the four builds plus `dotnet test` with TRX.**
- [x] **Step 2: Run it locally and verify output matches the baseline.**
- [x] **Step 3: Add `windows-latest` workflow using root `global.json`; exclude mobile/Web/Multiplatform/GISM.**
- [x] **Step 4: Verify workflow YAML parses and local script exits 0.**

### Task 8: P3-3 hot reload policy

**Files:**
- Modify: `Engine/Runtime/GameHost.cs`, `docs/11-health-todo.md`
- Test: `Engine.Tests/HotReloadPolicyTests.cs`

- [x] **Step 1: Write failing test** — policy reports `ReloadRequired` after an assembly swap and scene reload clears it.
- [x] **Step 2: Verify RED.**
- [x] **Step 3: Implement the host flag/event and `scene.reload` path; leave WinForms unchanged.**
- [x] **Step 4: Verify GREEN plus full suite/builds.**

### Task 9: Docs sync

**Files:** `AGENTS.md`, `README.md`, `docs/00-status-and-scope.md`, `docs/01-architecture.md`, `docs/02-ecs.md`, `docs/03-projects-and-serialization.md`, `docs/04-databases.md`, `docs/05-runtime.md`, `docs/06-authoring-guide.md`, `docs/07-editor-internals.md`, `docs/08-react-editor-boundary.md`, `docs/09-build-and-content.md`, `docs/10-known-issues.md`, `docs/11-health-todo.md`, `docs/12-future-roadmap.md`, `docs/13-refactor-todo.md`

- [x] **Step 1: Re-open changed source and record exact `path:line` anchors.**
- [x] **Step 2: Update status badges and mark P2-1…P2-5 / P3-1…P3-3 closed with evidence.**
- [x] **Step 3: Record P2-6/P2-7 deferred to roadmap Phase 5/6; P3-5/P3-6 out.**
- [x] **Step 4: Validate all Markdown links resolve.**

**Ruling:** Three GUI-only steps remain intentionally open: Task 1 Step 5 (WinForms `Fonts` load), Task 5 Step 5 (DesktopGL window launch), and Task 6 Step 4's watcher smoke. Automated builds, 71 xUnit facts, canonical path tests, and headless host frames are green; GUI-only console/visual behaviour was not verifiable in this headless session.
