# Runtime

This document describes the runtime as it executes today. `GameScene` is the de facto scene container, `GameHost` owns the shared frame loop, and both the WinForms editor viewport and the DesktopGL front end delegate common update/tick/render work to it. The `SceneManager` activation API is implemented but has no host call site; serialized `SceneManager.LoadScene`/`PreLoadScene` are still TODOs, while `SceneSerializer.LoadScene` loads scenes directly. DesktopGL boot is partial: the window creates a context, session, renderer, and host, but is not yet a manifest/content-driven game launch. See [01-architecture.md](01-architecture.md) for layering intent, [02-ecs.md](02-ecs.md) for systems and manager internals, and [12-future-roadmap.md §3.7](12-future-roadmap.md) for scene-switching scope.

**Status:** `Partial` — core, context, session state, and shared host are `Working`; Desktop boot is `Partial`; serialized scene switching is `Stub`.

## Runtime services at a glance

| Service | Role | Status | Evidence |
| --- | --- | --- | --- |
| `GameScene` | Scene container: entities, systems, managers, physics, databases, maps, update/render entry points | Partial | `Engine/ECS/GameScene.cs:62-90`, `:105-134`, `:269-282` |
| `IEngineContext` / `EngineContext` | Injectable per-host project context with static compatibility bridge | Working | `Engine/Runtime/EngineContext.cs:13-41`, `:46-64` |
| `SessionGlobals` | Per-session input source, `InputManager`, editor camera, viewport | Working | `Engine/Runtime/SessionGlobals.cs:11-41` |
| `GameHost` | Shared frame loop, manager lifecycle, scene load/reload, render dispatch | Working | `Engine/Runtime/GameHost.cs:33-57`, `:69-99`, `:141-177`, `:207-230` |
| `IFrameRenderer` / `IInputSource` | Platform presentation and input boundaries | Working | `Engine/Runtime/IFrameRenderer.cs:5-16`; `Engine/Runtime/IInputSource.cs` |
| `GameManager` | Per-scene service with `Initialize`/`Update`/`Shutdown` and `IsPersistent` | Partial | `Engine/Runtime/GameManager.cs:11-37`; `Engine/Runtime/GameHost.cs:141-155`, `:207-230` |
| `ManagersManager` | Duplicate-guarded registry and `GetManager<T>()` lookup | Working | `Engine/ECS/Managers/ManagersManager.cs:23-54` |
| `SceneManager` | Static active/next scene slots and scene switching | Partial — activation API implemented but not host-wired | `Engine/Runtime/SceneManager.cs:15-131`; TODOs at `:104-121` |
| `Globals` | Static compatibility shim over the current `SessionGlobals` | Partial | `Engine/Runtime/Globals.cs:10-43`, `Engine/Runtime/SessionGlobals.cs:17` |
| `InputManager` | Mouse/keyboard polling with snapshot-based events | Working (host-driven) | `Engine/Runtime/InputManager.cs:14-89`, `Engine/Runtime/SessionGlobals.cs:39-41` |
| `Camera2D` | Position/zoom/rotation, view matrix, screen↔world transforms | Working | `Engine/Utilities/Camera2D.cs:14-48` |
| `AssetManager` | Asset-type → subfolder mapping, content keys, physical paths | Partial | `Engine/Utilities/AssetManager.cs:57-123` |
| `ProjectPaths` | Canonical gameplay script source and compiler output paths | Working | `Engine/Runtime/ProjectPaths.cs:10-43` |
| `Log` | Severity-tagged diagnostic event bus | Working | `Engine/Utilities/Log.cs:9-65` |
| `GameWorldManager` | Global `TileSize` consumed by `Map` | Partial | `Engine/Runtime/GameWorldManager.cs:10-18`, `Engine/Gameplay/Map.cs:195-200` |
| `MockGame` / `MockGraphicsDeviceService` | Pure placeholder `Game` instance required by an experimental MonoGame hot content loader | Experimental | `Engine/Utilities/MockGame.cs:11-45`; sentinel comment at `:26-29` |

## Context and session state

`IEngineContext` owns project root/name/manifest, `PlayState`, `SelectedTileIndex`, `ActiveScene`, and path helpers (`Engine/Runtime/EngineContext.cs:13-41`). `EngineContext` is the mutable implementation and exposes a static `Current` compatibility bridge for code not yet injected (`:46-64`). Hosts should create one context per session; the frozen WinForms editor keeps its existing static API by delegating to that bridge (`Editor/Services/EditorContextManager.cs:10-17`).

`SessionGlobals` owns `InputManager`, `EditorCamera`, `Viewport`, and the current `IInputSource`; its static `Current` is another compatibility shim (`Engine/Runtime/SessionGlobals.cs:11-41`). Built-in UI input is constructed from these injected session services rather than static globals (`Engine/ECS/Managers/SystemsManager.cs:88`). `Globals.Current` can point at a session for older WinForms callers (`Engine/Runtime/Globals.cs:10-43`).

- **Input:** `SessionGlobals.UpdateInput()` calls `InputManager.Update()` (`Engine/Runtime/SessionGlobals.cs:39-41`); `InputManager.Update` refreshes mouse then keyboard state and raises left down/up, mouse moved with delta, and key down/up/held events (`Engine/Runtime/InputManager.cs:14-89`). Desktop uses `MonoGameInputSource` (`Engine.Desktop/Game1.cs:44`); headless hosts can use `NullInputSource`.
- **Camera:** `Camera2D` builds a translate/rotate/scale/center view matrix and inverts it for `ScreenToWorld` (`Engine/Utilities/Camera2D.cs:24-48`). A renderer sets the session viewport at initialization (`Engine.Desktop/MonoGameFrameRenderer.cs:26-30`).
- **Assets:** `GetContentRelativePath("foo", AssetType.Texture)` returns `Textures/foo` with no `Content/Assets` prefix (`Engine/Utilities/AssetManager.cs:92-103`), while `GetAbsolutePhysicalPath` builds `<BinPath>/Content/Assets/Textures/foo.xnb` from the injected/default context (`:108-123`). Key discovery also accepts a context (`:57-84`). Path conventions are covered in [09-build-and-content.md](09-build-and-content.md).
- **Logging:** `Log.Info/Warning/Error/Print` format a timestamped message, write to debug output, and raise `OnLogMessage` for UI listeners (`Engine/Utilities/Log.cs:19-65`).
- **World metrics:** `GameWorldManager.TileSize` defaults to `32` and is read by the `Map` constructor (`Engine/Runtime/GameWorldManager.cs:12-18`, `Engine/Gameplay/Map.cs:195-200`).

`MockGame` is a deliberately empty `Game` subclass (`Engine/Utilities/MockGame.cs:26-45`) whose XML comment warns that it is an external dev-branch sentinel with no behaviour of its own (`:26-29`). According to the project author, this exists to support an **experimental dev branch of MonoGame with a hot content loader**: the content builder requires a `Game` instance reference, even though that instance's own state — and the folder it is associated with — has nothing to do with the assets being reloaded. `MockGame` is a **pure sentinel**: it does nothing beyond satisfying that requirement and must not be deleted or wired to gameplay. Its consumer is outside this repository, which is why no in-repo call sites exist. See [09-build-and-content.md §4.4](09-build-and-content.md) for the full pipeline picture.

## GameHost: the shared frame loop

**Status:** `Working` for input, manager lifecycle, scene update, fixed tick, and render dispatch.

`GameHost` accepts optional context, session, and renderer; defaults are the static bridge, a fresh session, and `NullFrameRenderer` (`Engine/Runtime/GameHost.cs:41-53`). `Initialize()` calls `Renderer.Initialize(Session)` once (`:59-67`). `LoadScene(path, database)` and `AttachScene(scene)` shut down previous managers, assign `Context.ActiveScene`, and clear the hot-reload flag (`:69-99`).

Each frame, `Update(deltaTime, beforeSceneUpdate)` calls `Session.UpdateInput()`, initializes active managers once, calls `manager.Update(deltaTime)` on active managers, invokes the host-specific callback, then calls `scene.Update(deltaTime, Context.PlayState)` (`Engine/Runtime/GameHost.cs:141-156`). `TickUpdate`, `Render`, and `RunFrames` provide the remaining loop entry points (`:158-178`). `Shutdown()` shuts down all registered managers and the renderer (`:180-189`, `:207-218`). The editor uses `beforeSceneUpdate` for gizmo input and its fixed-tick accumulator; DesktopGL uses it for physics stepping (`Editor/MGWindowControl.cs:100-115`; `Engine.Desktop/Game1.cs:74-83`).

```mermaid
sequenceDiagram
    participant Host as GameHost
    participant Session as SessionGlobals / IInputSource
    participant Managers as ManagersManager
    participant Scene as GameScene
    participant Renderer as IFrameRenderer

    Host->>Renderer: Initialize(session)
    loop per frame
        Host->>Session: UpdateInput()
        Host->>Managers: Initialize once; Update(deltaTime) when active
        Host->>Host: beforeSceneUpdate callback (host-specific)
        Host->>Scene: Update(deltaTime, PlayState)
        Host->>Scene: TickUpdate(fixedDelta, PlayState)
        Host->>Renderer: Render(scene, session)
    end
    Host->>Managers: Shutdown()
    Host->>Renderer: Shutdown()
```

## SceneManager: slots, not scene loading

**Status:** Partial — activation API implemented but not host-wired; serialized loading is still a stub.

`SceneManager` keeps a static `ActiveScene`, `NextScene`, and `Dictionary<int, GameScene> TotalScenes` (`Engine/Runtime/SceneManager.cs:20-31`). `AddScene` recursively increments a colliding load order (`Engine/Runtime/SceneManager.cs:34-52`), `LoadFirstScene` selects the lowest load-order key and calls `SetNextScene` (`:55-67`), and `SetNextScene` walks sorted load-order keys so sparse orders work (`:69-89`). `LoadNextScene` assigns `NextScene` and advances the index; it no-ops when there is no successor (`:92-102`). `GameHost` loads or attaches one scene directly instead of calling this API (`Engine/Runtime/GameHost.cs:69-99`).

The serialization-backed operations do not exist yet:

```csharp
public static void LoadScene(GameScene newScene)
{
    //todo

    //ActiveScene = SceneSerializer.LoadScene(newScene);
}
```

Source: `Engine/Runtime/SceneManager.cs:104-113`; `PreLoadScene` is likewise empty with its load call commented out (`:118-121`). The only implementation around this area is pass-through frame forwarding (`:123-131`).

## GameManager: hooks with a host caller

**Status:** Partial — `GameHost` drives the hooks, but persistence and additive scene switching remain unused.

`GameManager` declares scene context, `IsActive`, `IsPersistent`, and three abstract lifecycle methods (`Engine/Runtime/GameManager.cs:11-37`):

```csharp
// Lifecycle Initialization Hook. Called automatically by the GameScene once the entire scene has loaded into memory,
// ensuring all GameObjects and Systems exist.
public abstract void Initialize();

// The high-level execution tick. Unlike systems, this doesn't automatically loop through 
// GameObjects; it runs once per frame to update overarching state logic.
// <param name="deltaTime">Time elapsed in seconds since the last frame tick.</param>
public abstract void Update(float deltaTime);

// Lifecycle Shutdown Hook. Called automatically when the scene is being torn down 
// by the SceneDirector. Perfect for cleanly unsubscribing from GameEvents to prevent memory leaks.
public abstract void Shutdown();
```

`GameHost.InitializeSceneManagers` assigns `ContextScene`, calls `Initialize()` once for each active manager, and `Update()` calls `manager.Update(deltaTime)` each frame for active managers (`Engine/Runtime/GameHost.cs:141-155`, `:220-230`). `ShutdownSceneManagers` calls `Shutdown()` on scene swap and host shutdown for **all** registered managers (`:207-218`). `SceneManager` is not wired to manager lifetimes, and `IsPersistent` still has no consumer (`Engine/Runtime/GameManager.cs:21`).

The registry is functional and is used: the editor registers user-defined manager subclasses with `scene.Managers.AddManager(newManager)` (`Editor/Form1.cs:1381-1431`, `AddManager` call at `:1426`), `AddManager` rejects duplicate concrete types, `GetManager<T>()` uses `OfType<T>().FirstOrDefault()`, and `GetRegisteredManagers()` exposes the list used by serialization (`Engine/ECS/Managers/ManagersManager.cs:23-54`, `Engine/Serialization/SceneSerializer.cs:133-139`). `RemoveManager` is available (`Engine/ECS/Managers/ManagersManager.cs:56-62`).

## GameScene: the real container and frame API

A `GameScene` constructor accepts optional `IEngineContext`, `DatabaseManager`, and `SessionGlobals`; externally supplied databases are not reloaded from disk, and locally created ones load from `Context.DatabasePath` (`Engine/ECS/GameScene.cs:62-90`, `:105-123`). It always ensures at least one `25 x 19` map exists (`:85-89`). Spawning helpers and `AddGameObject` create/register entities, with runtime-created ownership passed explicitly rather than read from editor play state (`:153-212`). `FinalizeInitialization()` re-points scene references and rebuilds caches (`Engine/ECS/GameScene.cs:126-137`), with `resetContextSceneInManagers()` as an alias (`:286-289`).

Its "main loop execution" region is three forwarding methods — the host owns timing, manager iteration, and rendering (`Engine/ECS/GameScene.cs:267-284`):

```csharp
public void Update(float deltaTime, bool playModeActive = false)
{
    Systems.Update(deltaTime, playModeActive);
}
```

Scene files are loaded into this container by `SceneSerializer.LoadScene`, which constructs a `GameScene` with the supplied context, database manager, and session, restores maps/managers/systems, rebuilds entities and references, then calls `FinalizeInitialization()` (`Engine/Serialization/SceneSerializer.cs:147-169`, `:210-245`, `:337`). The editor assigns the result to `EditorContextManager.ActiveLoadedScene` (`Editor/Form1.cs:338-357`); `GameHost` assigns it to `Context.ActiveScene` (`Engine/Runtime/GameHost.cs:82`).

## Editor delegation

**Status:** `Working` — the editor viewport delegates to `GameHost`; editor-only gizmos and simulation controls remain in `Editor/`.

`MGWindowControl` constructs a `GameHost` with the editor context, `SessionGlobals`, and an editor-only `EditorFrameRenderer` (`Editor/MGWindowControl.cs:64`). Its `Update` calls `_host.Update(clampedDelta, ...)` with a callback that runs the 60 Hz physics accumulator only in play mode, and its `Draw` calls `_host.Render()` (`:100-129`). The `EditorFrameRenderer` implements `IFrameRenderer` and draws editor overlays around the shared scene/system render calls (`:269-289`). Simulation controls are editor-side: `StartSimulation`, `pauseSimulation`, and `StopSimulation` toggle flags, reset physics transforms/states, and clean up runtime-created entities (`:171-216`). `EditorContextManager.PlayState` bridges to `EngineContext.Current.PlayState`, which the host passes into scene updates (`Editor/Services/EditorContextManager.cs:23-27`; `Engine/Runtime/GameHost.cs:155`).

## DesktopGL front end

**Status:** `Partial`.

`Engine.Desktop.Game1` is no longer a stock template: `Initialize` creates an `EngineContext` from optional `TESSERACT2D_PROJECT_ROOT`, a `SessionGlobals` with `MonoGameInputSource`, a `MonoGameFrameRenderer`, and a `GameHost` (`Engine.Desktop/Game1.cs:34-49`). It loads `TESSERACT2D_SCENE` when supplied or attaches an empty `GameScene`, enables play state, and shuts the host down on exit (`:51-64`). `Update` delegates to `_host.Update` with a fixed-step physics callback; `Draw` delegates to `_host.Render` (`:66-92`). `MonoGameFrameRenderer` clears to cornflower blue, renders the scene and systems, then draws world and screen UI (`Engine.Desktop/MonoGameFrameRenderer.cs:26-48`).

This is a real MonoGame DesktopGL window, but it is not yet a full content/manifest-driven game launch: saved projects, generated gameplay assemblies, and asset loading are still editor-led workflows. Manual GUI launch/visual confirmation was not possible in the headless verification session; headless frames and all maintained builds are green.

## Hot reload policy

**Status:** `Working` as an explicit host policy; live-instance rehydration is deliberately not attempted.

`GameHost.NotifyAssemblyReloaded()` sets `HotReloadState.ReloadRequired` and raises `HotReloadStateChanged`; existing component instances keep their old assembly types (`Engine/Runtime/GameHost.cs:101-108`, `:198-205`). `TryExecuteCommand("scene.reload")` re-loads the scene file and clears the flag; attached scenes without a file path cannot be reloaded (`:88-99`, `:113-134`). WinForms script rebuilds still refresh the editor UI only and are not wired to this host event (`Editor/Form1.cs:216-260`).

## Gotchas / Known Limitations

- `GameManager.IsActive` is consulted by `GameHost`; `IsPersistent` is not (`Engine/Runtime/GameManager.cs:17-21`, `Engine/Runtime/GameHost.cs:222-230`).
- `SceneManager.LoadScene` and `PreLoadScene` are empty; the serialized-load lines are commented out (`Engine/Runtime/SceneManager.cs:104-121`). `GameHost` loads one scene directly and does not call `LoadFirstScene`.
- `LoadFirstScene`, `GetOrderedLoadOrders`, and `SetNextScene` use sorted load-order keys, so non-contiguous/custom load orders work (`Engine/Runtime/SceneManager.cs:55-89`); `NextScene` is cleared when the last scene is active (`:92-102`).
- `AddManager` throws on a duplicate concrete type rather than replacing it (`Engine/ECS/Managers/ManagersManager.cs:30-34`).
- `GameScene` alone does not drive a frame; the host owns timing, physics stepping, and rendering (`Engine/Runtime/GameHost.cs:141-177`; `Engine/ECS/GameScene.cs:267-284`).
- `SceneManager.Update`/`TickUpdate` are not used by either host; `GameHost` calls `GameScene` directly (`Engine/Runtime/SceneManager.cs:123-131`, `Engine/Runtime/GameHost.cs:155-166`).
- Static `EngineContext.Current`, `SessionGlobals.Current`, and `Globals.Current` remain compatibility bridges; new engine code should use injection (`Engine/Runtime/EngineContext.cs:48-52`, `Engine/Runtime/SessionGlobals.cs:17`).
- Asset key helpers still disagree about whether paths include `Content/Assets` and whether extensions are stripped, so load-path bugs remain common (`Engine/Utilities/AssetManager.cs:92-123`); see [09-build-and-content.md](09-build-and-content.md).
- Hot reload reports `ReloadRequired` but does not rehydrate instances; `scene.reload` is a scene-file reload, not an in-place type swap (`Engine/Runtime/GameHost.cs:101-134`).
- `MockGame` intentionally does not implement a boot sequence — it is a stand-in `Game` for an external experimental hot content loader, not a host. It has no in-repo call sites by design (`Engine/Utilities/MockGame.cs:26-45`).
- iOS, Android, Web, and Multiplatform hosts remain template-level; DesktopGL is the only maintained standalone host (`Engine.Ios/Game1.cs:23`, `Engine.Web/Engine.WebGame.cs:37`).

## See also

- [00-status-and-scope.md](00-status-and-scope.md)
- [01-architecture.md](01-architecture.md)
- [02-ecs.md](02-ecs.md)
- [03-projects-and-serialization.md](03-projects-and-serialization.md)
- [07-editor-internals.md](07-editor-internals.md)
- [09-build-and-content.md](09-build-and-content.md)
- [10-known-issues.md](10-known-issues.md)
- [14-alpha-todo.md](14-alpha-todo.md) — active Alpha v1 work queue, including additive scene runtime
- [11-health-todo.md](11-health-todo.md) — archived hardening pass for runtime, `SceneManager`, and manager lifecycle
