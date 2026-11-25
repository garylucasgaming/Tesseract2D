---
name: Tesseract2D engine context
description: Use before editing Engine/ code, ECS systems, components, or runtime — repo-specific conventions and the Working/Partial/Stub status traps
---

# Tesseract2D engine context

Read `AGENTS.md`, `docs/00-status-and-scope.md`, and `docs/02-ecs.md` before changing engine code.

## The engine is `Engine/`

`Engine.Core` contains the engine: gameplay object model, ECS, serializers, physics, databases, and utilities. The `Engine.*` siblings are wrapper targets and mostly template stubs. `Engine.Gameplay` is a placeholder, not the game. Verify status in `docs/00`; never infer it from a class existing.

## Naming and lifecycle quirks

- Unity-flavoured names: `GameObject`, `GameComponent`, `gameObject` (lowercase back-reference), `AddComponent<T>`, `GetComponent<T>`.
- `GameComponent.OnEnabled/OnDisabled` and `ScriptComponent.OnEnable/OnDisable` both exist. Do not "fix" one spelling.
- "Space" has three meanings: `GameSpace` (project metadata), `PhysicsManager.PhysicsSpace` (the Aether `World`), `UISpace` (Screen/World enum).
- `GameComponent.isEnabled` defaults to `false` on the class, but both `AddComponent<T>()` and `AddComponent(GameComponent)` set it to `true` after registration and duplicate checks (`Engine/ECS/GameObject.cs:215-225`, `:238-249`).
- Systems run in registration order; there is no priority field. Built-ins are created in `SystemsManager`'s constructor.

## Runtime status — precise version

`GameScene.Update` drives only `Systems.Update`. Scene activation works (`SceneManager.AddScene`/`LoadFirstScene`/`LoadNextScene`/`Update`/`TickUpdate`), but serialized `LoadScene`/`PreLoadScene` are empty TODOs. `GameHost` owns the frame loop: it calls `GameManager.Initialize`/`Update` for active registered managers and `Shutdown` for all registered managers. `ManagersManager` remains a duplicate-guarded registry with no tick. `IsPersistent` has no consumer. See `docs/05-runtime.md` and `docs/10-known-issues.md`.

## Play mode

The editor is a live MonoGame game. `GameSystem.UsedInEditor` defaults to `false`: Transform opts in to running outside play mode, physics runs only in play mode. Any new host must preserve this gating. See `docs/12-future-roadmap.md` §3.6.

## Hard constraints

- No new engine→editor dependency. `IEngineContext`/`SessionGlobals` are injectable in `Engine.Core`; the concrete `EditorContextManager` bridge lives in `Editor/Services/`. Static bridges are compatibility shims only.
- 2D only. 3D, networking, Lua, and Vulkan are deferred per `docs/12-future-roadmap.md`.
- Components need public parameterless constructors. Only public instance fields and read-write properties serialize.
- `[Ignore]` is detected by attribute *name string*; `[Browsable(false)]` and `[DatabaseIgnore]` affect editor UI only.

## Verification

Build the affected project individually; `dotnet build Tesseract2D.sln` fails on mobile targets without .NET 9 workloads. Then use the archived manual saved-project workflow in `docs/archive/11-health-todo.md`.
