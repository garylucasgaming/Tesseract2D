# 00 — Status & Scope

This engine is a work in progress. This document records what actually exists in the current tree so that readers and agents do not mistake class definitions for working features. Everything here is derived from source; claims are anchored with repo-root-relative paths and line numbers.

**Status:** Working (core, shared host, build script) · Partial (editor/databases/content/DesktopGL boot) · Stub (iOS/Android/Engine.Gameplay hosts) · Experimental (Multiplatform/web) · Unused/WIP (GISM, several services)

**Scope boundaries:** this is a **2D-only** engine by decision; 3D is explicitly deferred until after full release. Alpha v1.0 targets DesktopGL only and plans to replace the frozen WinForms editor with a Photino/React host. See [12-future-roadmap.md](12-future-roadmap.md) for the full set of decisions.

## 1. Status legend

| Badge | Meaning |
|---|---|
| `Working` | Functional in the current code path used by the editor |
| `Partial` | Works, but with significant caveats or missing edges |
| `Stub` | Template / `TODO` lifecycle methods; does not bootstrap the engine |
| `Experimental` | Isolated prototype, separate solution, not consumed by the main stack |
| `Unused/WIP` | Present in source but has no call sites or is effectively inert |

## 2. Subsystem status matrix

| Area | Status | Evidence / notes |
|---|---|---|
| ECS object model (`GameObject`, `GameComponent`, `GameScene`) | `Working` | `Engine/ECS/GameObject.cs`, `Engine/ECS/GameScene.cs`; actively used by editor. Component enable/hierarchy registration were normalized by P1-4/P1-7 (`GameObject.cs:99-124`, `:213-250`, `:170-206`), and scene readiness now has one idempotent engine-only path (`GameScene.cs:80-124`, `:126-137`, `:286-289`). Remaining quirks are in [10-known-issues.md](10-known-issues.md). |
| Fluent query API (`Query`, `IComponentQuery`) | `Working` | `Engine/ECS/Query.cs:9-44`; used by every built-in system. |
| Systems & managers | `Partial` | `Engine/ECS/Managers/SystemsManager.cs:43-89` builds seven built-ins. Per-system warm caches reconcile on component add/remove and entity activation changes (`SystemsManager.cs:283-317`, `Engine/ECS/GameScene.cs:85-90`); P1-9 adds explicit context rebinding and a full cache rebuild (`SystemsManager.cs:122-150`, `GameScene.cs:126-137`). Lifecycle edge cases and several update policies remain partial. |
| Frame flow | `Working` | `GameHost` owns input, manager, scene update, fixed tick, and render sequence (`Engine/Runtime/GameHost.cs:141-177`). Both the editor (`Editor/MGWindowControl.cs:64`, `:100-129`) and DesktopGL host (`Engine.Desktop/Game1.cs:44-90`) delegate to it. |
| Injectable context and per-session state | `Working` | `IEngineContext`/`EngineContext` with a static compatibility bridge lives in core (`Engine/Runtime/EngineContext.cs:13-20`, `:46-63`); the editor bridge is in `Editor/Services/EditorContextManager.cs:10-17` and delegates to `EngineContext.Current`. `SessionGlobals` owns per-session input, camera, and viewport (`Engine/Runtime/SessionGlobals.cs:11-41`). |
| Physics (Aether wrapper) | `Partial` | `PhysicsManager.PhysicsSpace` + body/collider components. Late spawns, PPM-based transform↔body sync, and late-added colliders work (`Engine/ECS/Systems/PhysicsSystem.cs:75-109`, `:119-173`); collision callbacks still expose only the owner — see [02-ecs.md](02-ecs.md). |
| Serialization (scene/component/database/map YAML) | `Partial` | `Engine/Serialization/` uses YamlDotNet; versioned DTOs reject future formats, while opt-in strict mode checks malformed IDs/types and unresolvable tile rows in the individual loaders (`SerializationVersion.cs:12-22`, `GameObjectSerializer.cs:28-59`, `TileMapSerializer.cs:106-204`, `DatabaseSerializer.cs:61-96`). Default mode keeps legacy recovery. Unknown fields are still ignored and there is no migration hook. |
| Databases / `DataComponent` | `Partial` | `Engine/Collections/Database.cs`, `Editor/DatabaseViewer.cs`; authoring works, generic component→database references do not serialize reliably. See [04-databases.md](04-databases.md). |
| Custom UI widgets | `Partial` | `Engine/ECS/Components/UI/*`, `Engine/ECS/Systems/UI*.cs`; recently added and still evolving. |
| Tilemaps | `Partial` | `Engine/Gameplay/Map.cs`, `TileMapRenderSystem.cs`; editor tile painting works, and P1-8 round-trips `TileIndexDataDictionary` and rebuilds the derived per-tile grid after load (`Engine/Serialization/TileMapSerializer.cs:57-86`, `:145-200`). Other save/load gaps remain. |
| Editor (WinForms) | `Partial` | `Editor/` is the most complete consumer of the engine; heavy WinForms coupling and fragile `PropertyGrid` internals. |
| Content pipeline | `Partial` | `Engine.Content/Builder/*`; editor-driven build works, but paths and target files contain known errors. See [09-build-and-content.md](09-build-and-content.md). |
| `GameManager` lifecycle hooks | `Partial` | `GameHost` initializes and updates active registered managers, but shuts down all registered managers (`Engine/Runtime/GameHost.cs:141-155`, `:207-230`). `IsPersistent` and serialized scene switching are still unimplemented, so full lifecycle semantics remain partial. |
| Scene switching | `Partial` | Activation API is implemented but not host-wired: `AddScene`, `LoadFirstScene`, `SetNextScene`, `LoadNextScene`, `Update`, and `TickUpdate` exist (`Engine/Runtime/SceneManager.cs:34-102`, `:123-131`), while `GameHost` loads/attaches one scene directly (`Engine/Runtime/GameHost.cs:69-99`). Ordered load-order keys drive activation and next-scene selection (`Engine/Runtime/SceneManager.cs:55-102`). `LoadScene` and `PreLoadScene` are still empty TODOs (`:104-121`). |
| `GameSpace` | `Unused/WIP` | Project metadata DTO; `ActiveScene` (`Engine/ECS/GameSpace.cs:23-27`) is never assigned. |
| `Engine.Gameplay` | `Stub` | Only `testSystem.cs` (empty) and a project file. |
| Desktop host | `Partial` | `Game1` now creates an `EngineContext`, `SessionGlobals`, `GameHost`, and `MonoGameFrameRenderer`, loads `TESSERACT2D_SCENE`/`TESSERACT2D_PROJECT_ROOT` when supplied, and drives update/render (`Engine.Desktop/Game1.cs:34-92`). Content/manifest boot and manual GUI verification remain open. |
| iOS / Android hosts | `Stub` | Template `Game1.cs` classes with `TODO` lifecycle methods. |
| Web host (`Engine.Web`) | `Experimental` | KNI/nkast BlazorGL, no `Engine.Core` reference, template game class. |
| Multiplatform tree | `Experimental` | Separate solution, KNI template, shared game assembly named `QueryTestGame`, no engine reference. |
| GISM | `Unused/WIP` | Abandoned markup-language experiment, not in main solution. **Ignore it.** |
| CI / automated tests | `Partial` | `scripts/build.ps1:23-50` builds four maintained targets and runs 71 xUnit facts with TRX output; `.github/workflows/ci.yml:7-29` runs it on `windows-latest`. `Engine.Tests` is an xUnit project (`Engine.Tests/Engine.Tests.csproj:8,12-15`) with portable fixtures (`Engine.Tests/TestContent/Fonts/README.md:1-27`). Manual integration testing via saved editor projects remains the primary end-to-end check (see [archived verification assets](archive/11-health-todo.md)). |

## 3. Two framework stacks

| Stack | Framework | Projects |
|---|---|---|
| Main | MonoGame 3.8.4.1 (DesktopGL/iOS/Android) + Aether.Physics2D 2.2.0 | `Engine.Core`, `Editor`, `Engine.Content`, `Engine.Desktop`, `Engine.Ios`, `Engine.Android` |
| Experimental | KNI/nkast 4.2.9001 | `Engine.Web`, all of `Engine.Multiplatform` |

**There is no FNA in this repository.** A word-boundary search finds no FNA package or reference. The web/platform experiments use KNI/nkast, which is an XNA-compatible framework, not FNA.

Version drift is real: core/desktop use `3.8.4.1`, the content tool uses `3.8.5-preview.*` (`Engine.Content/Engine.Content.csproj:10`), and KNI projects use `4.2.9001`. See [09-build-and-content.md](09-build-and-content.md).

## 4. Design intent vs. implementation

`Engine/Engine_Manifest.txt` is the closest thing to an architecture manifest. It states the intended design. Several statements are aspirational rather than true today:

| Manifest claim | Reality |
|---|---|
| "The engine is built strictly as a standalone library... knows nothing about the editor and never will" (`:4`) | The concrete `EditorContextManager` has moved to `Editor/Services/EditorContextManager.cs:10-17` and now bridges to `EngineContext.Current`; core uses injected `IEngineContext`/`SessionGlobals` (`Engine/Runtime/EngineContext.cs:13-20`, `Engine/Runtime/SessionGlobals.cs:11-41`). Static bridges remain for frozen WinForms callers. See [01-architecture.md](01-architecture.md). |
| "Platform targets consume the engine library" (`:5`) | `Engine.Web` and all Multiplatform projects have no `Engine.Core` reference. |
| "High-speed component-type buckets" in the query system (`:17`) | `Query` evaluation re-scans entities; buckets exist in `EntityManager` but are used by a different lookup API. System warm caches are separate and now reconcile on entity/component changes. See [02-ecs.md](02-ecs.md). |
| "YAML is used exclusively" (`:26`) | Serializers do use YamlDotNet, but the project manifest is JSON (`ProjectManifest.cs:27-38`), the checked-in `HierarchyTest.scene` sample is stale JSON (`SavedGameProjectData/Content/Scenes/HierarchyTest.scene:1-35`), and a current-format scene fixture exists at `Engine.Tests/TestContent/Fonts/Content/Assets/Scenes/Main.scene:1-8` (`Engine.Tests/TestContent/Fonts/README.md:7-18`). |
| "MonoGame 3.8.5 Preview" (`:21`) | Core uses `3.8.4.1`; only the content tool uses a preview version. |
| "Future global layer: GameWorldManager, SceneManager, InputManager" (`:18`) | These classes exist. `SceneManager` activation methods are implemented (`Engine/Runtime/SceneManager.cs:34-102`, `:123-131`), but serialized `LoadScene`/`PreLoadScene` are still TODOs (`:104-121`) and no host invokes the activation API. Custom `GameManager` hooks are now called by `GameHost` (`Engine/Runtime/GameHost.cs:141-155`, `:207-230`); `IsPersistent` remains unused. See [12-future-roadmap.md §3.7](12-future-roadmap.md). |

Treat the manifest as a statement of direction, and the source as the contract.

## 5. What works today

The one complete, end-to-end path is the **editor-driven development loop**:

```text
Launcher creates/opens project
  → EditorContextManager bridges to EngineContext.Current
  → GameScene + managers created with injected context/session
  → SceneSerializer loads .scene YAML from Content/Assets/Scenes
  → Gameplay assembly compiled and loaded (hot reload on file change)
  → MGWindowControl delegates Update/TickUpdate/Render to GameHost
  → Inspector edits live objects; serializers write YAML back to disk
```

Opening an existing project reads `Content/ProjectManifest.db` through `Editor/Tesseract2DLauncher.cs:347-361` (P0-1), and newly created projects get a populated manifest via `Engine/Serialization/ProjectDirectoryFactory.cs:72-74` (P0-2). The author's older saved projects still contain literal `{}` manifests. See the [archived P0 hardening notes](archive/11-health-todo.md).

See [07-editor-internals.md](07-editor-internals.md) for the editor side and [05-runtime.md](05-runtime.md) for the runtime services involved.

A DesktopGL host now boots a `GameScene` through `GameHost` and can load a scene path from `TESSERACT2D_SCENE` (`Engine.Desktop/Game1.cs:34-92`), but it is not yet a full manifest/content-driven game launch. iOS, Android, Web, and `Engine.Gameplay` remain template-level.

## 6. Practical consequences

- If you are **writing game code**, target `Engine.Core` and the editor workflow; DesktopGL can host a scene directly but is not yet a full content/manifest-driven launch, and the other platform hosts are template-level.
- If you are **fixing a bug**, assume the bug is in core or the editor, not in the wrappers.
- If you are **planning the React editor**, read [08-react-editor-boundary.md](08-react-editor-boundary.md): there is no editor API today, so a backend service must be built. The chosen host is a .NET console app with a Photino webview — see [12-future-roadmap.md](12-future-roadmap.md).
- If you are **building locally**, run `./scripts/build.ps1` for the four maintained targets and tests, or use the repo-root `global.json:1-7` directly (`dotnet --version` resolves to .NET 8.0.428). Mobile directories have nested .NET 9 pins; see [09-build-and-content.md §3](09-build-and-content.md).
- If you are **evaluating the project**, judge it as a prototype with a solid core, not a finished engine.

## See also

- [01-architecture.md](01-architecture.md) — repository map and conventions
- [02-ecs.md](02-ecs.md) — the core object model
- [10-known-issues.md](10-known-issues.md) — detailed gotchas
- [12-future-roadmap.md](12-future-roadmap.md) — Alpha phases and scope decisions
- [14-alpha-todo.md](14-alpha-todo.md) — active Alpha v1 work queue
- [11-health-todo.md](11-health-todo.md) — archived hardening pass
