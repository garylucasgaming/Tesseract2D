# 13 — Refactor Backlog

This is an inventory of changes worth considering in the broader engine pass before Alpha v1. It is **not** a schedule: nothing is implemented merely by being listed here. The hardening checklist is archived in [11-health-todo.md](11-health-todo.md), active Alpha work is queued in [14-alpha-todo.md](14-alpha-todo.md), and feature and scope decisions remain in [12-future-roadmap.md](12-future-roadmap.md).

**Status:** Backlog — no item below is claimed as `Working`.

Baseline: P0-1 through P0-10, P1-1 through P1-10, P2-1 through P2-5, and P3-1 through P3-3 are complete (`docs/archive/11-health-todo.md:7`), and `Engine.Tests` contains 71 passing headless tests (`scripts/build.ps1:23-50`). The broader refactor/optimization pass is deliberately deferred until before Alpha v1.

WinForms is frozen and will be replaced by the Photino/React editor host before Alpha v1. Items tied to WinForms internals are marked `Likely moot (new editor)`: carry the *requirement* forward if it still matters, not the WinForms implementation.

## Status labels

| Label | Meaning |
|---|---|
| `Likely moot (new editor)` | WinForms-bound; expect the new host to supersede it |
| `Needs decision before Alpha v1` | Engine/runtime behavior independent of the editor UI |
| `Keep as-is` | Reviewed; no change recommended unless new evidence appears |
| `Optimization (measure first)` | Perf candidate from the ECS lifecycle scan; profile before restructuring |
| `Decided` | Direction agreed; implementation may still be deferred |
| `Cross-cutting` | Process/build/test concern rather than one code change |

## 1. Editor-bound script compilation and hot reload

These items describe the Unity-like goal: create a C# script/component, add it to the project, re-enter the editor, have it auto-compile, then see and use it immediately in inspectors and hierarchies. The goal is valid; the current implementation lives almost entirely in the frozen WinForms editor and overlaps health items P3-2/P3-3.

| Item | Detail | Source | Disposition |
|---|---|---|---|
| Split gameplay assembly loading | `LoadGameplayAssembly` reads DLL/PDB bytes into a collectible `AssemblyLoadContext`; `ReloadProjectAssemblies` separately loads every DLL under the canonical gameplay output directory with `Assembly.Load(byte[])` into the default ALC. Duplicate assemblies still pin old `Type` objects; `TypeResolver.ClearCache()` cannot unload those default-ALC copies. | `Editor/Utilities/ScriptAssenblyManager.cs:22-46`, `:48-89`, `:92-104`; `Engine/Runtime/ProjectPaths.cs:29-43` | `Likely moot (new editor)`. Redesign belongs in the new host. Do not patch WinForms beyond cache clearing. See P3-3. |
| Script path disagreement | Resolved by P3-2: `ProjectPaths` now defines `Content/Assets` as the single script root and `Source/<name>.Gameplay/bin/Debug/net8.0` as the single output root; generator, watcher, compiler, reload scanner, and launcher consume it. | `Engine/Runtime/ProjectPaths.cs:12-43`; `Editor/Form1.cs:173-199`; `Editor/Utilities/SolutionGenerator.cs:10-41`; `Editor/Utilities/ScriptCompiler.cs:21-77`; `Editor/Utilities/ScriptAssenblyManager.cs:48-89` | `Resolved (P3-2)`. Carry the canonical-path requirement into the new editor; do not reintroduce per-consumer path composition. |
| Split type discovery | Component/manager/system dropdowns reflect over `_scriptManager.CurrentAssembly` (`Editor/Form1.cs:1376-1388`, `:1478-1490`, `:2077-2089`), while `DatabaseViewer` force-loads assemblies and scans `AppDomain.CurrentDomain.GetAssemblies()` (`Editor/DatabaseViewer.cs:329-345`). | Same as detail column | `Likely moot (new editor)`. The new host should expose one gameplay type provider rather than two discovery paths. |
| No state-preserving hot swap | WinForms `CompileAndReloadScriptsAsync` still loads the new DLL and calls `RefreshEditor()`; live instances keep old types. The shared host now reports `ReloadRequired` and requires `scene.reload` instead of pretending to rehydrate. | `Editor/Form1.cs:221-233`; `Engine/Runtime/GameHost.cs:101-134` | `Resolved as policy (P3-3)`. Carry forward only if Unity-like hot reload becomes a product requirement. |
| Engine → editor coupling | Resolved by P2-1: `IEngineContext`/`SessionGlobals` live in core; the concrete `EditorContextManager` bridge lives in the editor assembly. Static shims remain for frozen WinForms callers. | `Engine/Runtime/EngineContext.cs:13-20`; `Editor/Services/EditorContextManager.cs:10-17` | `Resolved (P2-1)`. The new editor should inject its own context/session rather than restore static process state. |

## 2. Engine and runtime candidates that survive editor replacement

| Item | Detail | Source | Disposition |
|---|---|---|---|
| `TypeResolver` assembly scope | `FindType` scans `AppDomain.CurrentDomain.GetAssemblies()` and `Type.GetType` can bind an assembly-qualified name to an already loaded older copy. Ambiguous full/short fallback matches are warned and rejected, but that is a symptom guard rather than an assembly-scope fix. | `Engine/Utilities/TypeResolver.cs:26`, `:36`, `:57-72`, `:79-85` | `Needs decision before Alpha v1`. Consider an injected assembly provider / load-context scope when integrating the new editor. Keep the ambiguity rejection. |
| `GameComponent.isEnabled` default | Resolved by P1-4 for add paths: both `AddComponent<T>()` and `AddComponent(GameComponent)` register the component before setting `isEnabled = true`, duplicate adds are rejected before hooks, and unchanged assignments are ignored. `entity.isActive` remains the system cache gate. Non-generic removal still does not disable the component (`GameObject.cs:326-337`) and is tracked in §2A. | `Engine/ECS/GameObject.cs:213-250`; `Engine/ECS/GameComponent.cs:46-64`; `Engine/ECS/Managers/SystemsManager.cs:291` | `Resolved (P1-4, add paths)`. |
| Two enable hooks | `GameComponent.OnEnabled/OnDisabled` versus `ScriptComponent.OnEnable/OnDisable` remain separate contracts. | `Engine/ECS/GameComponent.cs:70-76`; `Engine/ECS/Components/ScriptComponent.cs:19-33`; `Engine/ECS/Systems/ScriptComponentSystem.cs:28-66` | `Keep as-is (by design)`: `ScriptComponent` is a MonoBehaviour-like subsystem; do not merge it into `GameComponent.isEnabled`. |
| Query does not use component buckets | `EntityManager.GetQuery` rescans all entities and allocates a list; its in-repo callers are `SystemsManager.AddSystem()` warm-up and `SystemsManager.RebuildCaches()`. `_componentTypeBuckets` is maintained but write-only. | `Engine/ECS/Managers/EntityManager.cs:20`, `:186-193`, `:214-228`; `Engine/ECS/Managers/SystemsManager.cs:112-119`, `:135-150` | `Needs decision before Alpha v1`: consider a query planner (exact buckets for simple `Has<T>`, assignability map for base types, fallback scan for `Or`/`Not`). `Optimization (measure first)`. |
| `Has<T>` versus `HasComponent<T>` semantics | `Query.Has<T>` is polymorphic (`IsAssignableFrom`) for systems; `GameObject.HasComponent<T>` and `EntityManager` buckets are exact-type. | `Engine/ECS/Query.cs:46-55`; `Engine/ECS/GameObject.cs:272-281`; `Engine/ECS/Managers/EntityManager.cs:184-195`; `Engine.Tests/QueryTests.cs:9` | `Keep both (by design)`. The polymorphism is intentional for base-type systems; the exact helper is the cheap Unity-style lookup. |
| Hierarchy semantics | Resolved by P1-7 for `AddChild`/`RemoveChild`: `AddChild` delegates to `SetParent`, registers unregistered children, rejects cycles, and `RemoveChild` clears `ParentId` and fires the child's `ParentChangedEvent`. The cycle guard lives in `AddChild`; direct `SetParent` callers remain responsible for avoiding cycles. `GameComponent.Parent` still has a no-op setter. | `Engine/ECS/GameObject.cs:78-124`, `:170-206`; `Engine/ECS/GameComponent.cs:25-41`; `Engine.Tests/HierarchyRegistrationTests.cs:9-97` | `Resolved (P1-7)` for `AddChild`/`RemoveChild`. Do not use the inert `GameComponent.Parent` setter; remove it only as part of a deliberate API cleanup. |
| `GameManager` lifecycle hooks had no call sites | `GameHost` initializes/updates active registered managers but shuts down all registered managers; `ManagersManager` remains a registry rather than a tick owner. `IsPersistent` and additive scene switching remain open. | `Engine/Runtime/GameManager.cs:24-37`; `Engine/Runtime/GameHost.cs:141-155`, `:207-230` | `Resolved as host contract (P2-5)`. Revisit persistence and multi-scene lifecycle before Alpha v1. |
| `SceneManager` serialized load is a TODO | `SceneManager.LoadScene`/`PreLoadScene` are TODOs, while `SceneSerializer.LoadScene` works and is used by the editor. | `Engine/Runtime/SceneManager.cs:104-121`; `Engine/Serialization/SceneSerializer.cs:139-295`; `Editor/Form1.cs:338-357` | `Keep as-is` until the new host defines scene switching; clarify which API owns loading. |
| Scene-switch state used to be only partly reset | `resetContextSceneInManagers()` is now an alias for `FinalizeInitialization()`, which rebinds pointers, rebuilds caches, and keeps one event subscription. | `Engine/ECS/GameScene.cs:126-137`, `:286-289`; `Engine/ECS/Managers/SystemsManager.cs:122-150` | `Resolved (P1-9)`. Revisit only if host-level scene switching needs more than per-scene readiness. |
| `EntityUpdate` systems get zero delta | `OnEntityChanged` defaults `deltaTime = 0` and `playModeActive = true`. | `Engine/ECS/Managers/SystemsManager.cs:283` | `Needs decision before Alpha v1`. |
| `FixedUpdate` catch-up cap | Catch-up is capped at 8 ticks while preserving the sub-interval remainder. | `Engine/ECS/Managers/SystemsManager.cs:171-193` | `Keep as-is` unless profiling or gameplay reports a problem. |
| `TransformComponent` cannot be removed | Both generic and non-generic removal paths guard it. | `Engine/ECS/GameObject.cs:307-308`, `:328-329` | `Keep as-is`. |
| Built-in systems are hardcoded out of YAML | Seven concrete system types are excluded by an explicit list on save. | `Engine/Serialization/SceneSerializer.cs:112-121` | `Needs decision` (hardcoded list versus data-driven exclusion). |
| Serialization has version validation but no migration | `Version` is declared on scene/entity/map/database DTOs and checked by `SerializationVersion.Validate` (future throws, stale warns, missing defaults to current); unknown YAML fields are ignored, unsupported values become `null`, and `$ref` still supports only `GameObject`, `Component`, and `GameEvent`. No migration hook exists. | `Engine/Serialization/SerializationVersion.cs:12-22`; `Engine/Serialization/ComponentSerializer.cs:138`, `:241-286`; `Engine/Serialization/SceneSerializer.cs:69-72` | `Needs decision before Alpha v1` for a real migration hook. Version/strict failure work itself is `Resolved (P1-6)`. |
| Project manifest fields lack editor consumers | `AssetRegistry`, `LastUsedScene`, and `EngineVersion` serialize with defaults and are read by `FixtureManifestTests`, but the editor has no meaningful writer beyond manifest creation. | `Engine/Serialization/ProjectManifest.cs:12-38`; `Engine.Tests/FixtureManifestTests.cs:12-55` | `Needs decision before Alpha v1`. |
| Content builder skips `*.database` | Wildcards copy `.yml`, `.cs`, `.scene`, `.prefab`, and `.data` but not `.database`; the author’s dev-branch pipeline is authoritative. | `Engine.Content/Builder/DynamicBuilder.cs:43-55` | `Needs decision`, but only after validating the author’s dev-branch workflow. |
| Stale checked-in scene fixture | `HierarchyTest.scene` is old JSON, not current YAML DTO format. A portable current-format fixture now exists under `Engine.Tests/TestContent/Fonts/`. | `SavedGameProjectData/Content/Scenes/HierarchyTest.scene:1-35`; `Engine.Tests/TestContent/Fonts/README.md:7-18` | `Keep as-is`. Never use the stale scene as a format reference. |

## 2A. ECS lifecycle performance findings (read-only scan, 2026-11-24)

This section records a read-only lifecycle scan, **not implemented work**. P1-4, P1-7, P1-6, P1-8, P1-9, and P1-10 were applied afterwards and are marked as resolved below; everything else remains backlog. Severity: `[HOT]` = per frame/tick; `[MUTATION]` = per spawn/component/activation event; `[STARTUP/LOAD]` = scene construction or serialize/deserialize; `[ONE-TIME]` = registration only. Items listed as already sound are explicitly **not** optimization targets.

### Scene construction / manager init

| Severity | Finding | Source | Recommended action |
|---|---|---|---|
| `[STARTUP]` | A new `GameScene` still loads databases from disk, with LINQ duplicate and case-insensitive lookups. P2-2 changed the source to an injected `DatabaseManager`, not the loading cost. | `Engine/ECS/GameScene.cs:62-83`, `:105-123`; `Engine/Runtime/DatabaseManager.cs:48-58`, `:34`, `:62-70` | Load once per project/session; cache parsed instances; use dictionaries keyed by ID/name/type. |
| `[STARTUP, resolved by P1-9]` | Sandbox scene used to initialize managers twice. `InitializeManagers()` is now idempotent and `FinalizeInitialization()` rebuilds contexts/caches without duplicate event subscriptions. | `Engine/ECS/GameScene.cs:80-124`, `:126-137`, `:286-289`; `Engine/ECS/Managers/SystemsManager.cs:122-150`; `Editor/Form1.SceneHierarchy.cs:420-424` | `Resolved (P1-9)`; no WinForms change required. |
| `[ONE-TIME]` | System registration uses LINQ duplicate detection and a full `GetQuery()` warm-up. | `Engine/ECS/Managers/SystemsManager.cs:99`, `:112-119`; `Engine/ECS/Managers/EntityManager.cs:214-228` | Keep a `HashSet<Type>` of registered system types; seed caches from component buckets. |
| `[STARTUP/LOW]` | Manager registry is list-based. | `Engine/ECS/Managers/ManagersManager.cs:28-36`, `:42-46` | Use `Dictionary<Type, GameManager>` if manager counts grow. |
| `[STARTUP, partly optimized]` | `TypeResolver` caches hits, but misses scan all assemblies and call `GetTypes()` with LINQ. | `Engine/Utilities/TypeResolver.cs:9-30`, `:36-55`, `:94` | Low priority; build a short-name → `Type` index per assembly load if reloads become frequent. |

### Entity creation / registration

| Severity | Finding | Source | Recommended action |
|---|---|---|---|
| `[MUTATION/STARTUP]` | `AddEntity` reconciles the same entity twice and registers the auto-added `TransformComponent` twice. | `Engine/ECS/Managers/EntityManager.cs:43-45`, `:49-53`, `:57-60`, `:61`; `Engine/ECS/GameScene.cs:80-91`; `Engine/ECS/Managers/SystemsManager.cs:283-317` | Use one component-registration path and coalesce component-added + entity-created into one reconciliation per spawn. |
| `[MUTATION]` | Component buckets are `List<GameObject>` with O(n) `Contains`/`Remove`; destruction scans every bucket. | `Engine/ECS/Managers/EntityManager.cs:195-206`, `:142-145`, `:98-102` | Use `HashSet<GameObject>` per component type plus a per-entity `HashSet<Type>` reverse index. |
| `[MUTATION]` | Buckets are write-only: `GetEntitiesWithComponent` has no caller and `GetQuery` ignores them. | `Engine/ECS/Managers/EntityManager.cs:186-193`, `:214-228` | Use buckets as query candidate sets, or remove their registration cost. Buckets key on concrete `component.GetType()` (`:59`, `:135`), so base-type queries need an assignability map. |
| `[MUTATION]` | Three event subscriptions and unsubscriptions per entity. | `Engine/ECS/Managers/EntityManager.cs:43-45`, `:93-95` | Low priority; consider one scene-level multicast if spawn bursts are measured. |

### Component add / remove / activation

| Severity | Finding | Source | Recommended action |
|---|---|---|---|
| `[MUTATION]` | Every query `Has<T>` node runs LINQ `Any` + `IsAssignableFrom` per component, evaluated for every system on every mutation. | `Engine/ECS/Query.cs:50-54`; `Engine/ECS/Managers/SystemsManager.cs:291` | Add exact `Components.TryGetValue(_type, ...)` fast path; cache `(queriedType, componentType)` assignability; combine with bucket candidates. |
| `[MUTATION]` | `HasComponent<T>()` duplicates `ContainsKey`; `GetComponent<T>()` falls back to a value scan for base types. | `Engine/ECS/GameObject.cs:253-281` | Collapse `HasComponent<T>()` to one lookup; document/optimize the polymorphic fallback separately. |
| `[MUTATION]` | `RemoveComponent<T>()` resolves the stored key once, but the non-generic overload still leaves `isEnabled` true. | `Engine/ECS/GameObject.cs:305-336` | Normalize disable handling between overloads. |
| `[MUTATION, resolved by P1-4]` | The `isEnabled` setter fired hooks on unchanged assignments, and only the non-generic add path enabled components. | `Engine/ECS/GameComponent.cs:16`, `:46-64`; `Engine/ECS/GameObject.cs:213-250`; `Engine/ECS/Components/UI/ButtonComponent.cs:76`, `ListBoxComponent.cs:56-80`, `PanelComponent.cs:22` | `Done`: unchanged assignments early-out; both add paths register the component before calling `OnEnabled()` once, and rejected non-generic duplicates do not call it. |
| `[MUTATION]` | `GameObject.isActive` and `ScriptComponent.IsActive` fire hooks/events even when unchanged. | `Engine/ECS/GameObject.cs:20-35`; `Engine/ECS/Components/ScriptComponent.cs:19-33` | Add equality guards; cache script components per entity instead of scanning all components on toggle. |
| `[MUTATION, partly resolved by P1-7]` | Hierarchy uses `List` membership checks. `AddChild` now guards cycles and registers unregistered children; `SetParent` dedupes `Children`. | `Engine/ECS/GameObject.cs:99-124`, `:126-153`, `:170-206` | Use `HashSet<GameObject>` for child membership if hierarchies grow. |

### Query evaluation / system cache

| Severity | Finding | Source | Recommended action |
|---|---|---|---|
| `[STARTUP, not hot]` | `GetQuery()` full scan allocates a list but is used for `AddSystem()` warm-up and `RebuildCaches()`. | `Engine/ECS/Managers/EntityManager.cs:214-228`; `Engine/ECS/Managers/SystemsManager.cs:112-119`, `:135-150` | Keep startup-only; if reused, intersect buckets and reuse output buffers. |
| `[MUTATION]` | Reconciliation walks every system cache per event; `EntityUpdate` allocates a single-element `HashSet`; zero delta by default. | `Engine/ECS/Managers/SystemsManager.cs:283-317`, `:314-315` | Maintain a query → systems index; reuse a single-element buffer. |
| `[MUTATION]` | Destruction reconciles caches twice: direct call plus `OnEntityRemoved` event. | `Engine/ECS/GameObject.cs:376`; `Engine/ECS/Managers/EntityManager.cs:103`; `Engine/ECS/GameScene.cs:82`; `Engine/ECS/Managers/SystemsManager.cs:322-328` | Choose one owner, preferably the event. |
| `[ONE-TIME/LOW]` | `GetSystem<T>()` uses LINQ `OfType<T>().FirstOrDefault()`. | `Engine/ECS/Managers/SystemsManager.cs:332-335`, `:252-260` | Fine unless manual systems are triggered every frame; then cache by `Type`. |

### Frame / tick / fixed / manual policies

| Severity | Finding | Source | Recommended action |
|---|---|---|---|
| `[HOT]` | `Render()` calls `SpriteRenderSystem.LoadSprites()` every frame while a project is loaded. | `Engine/ECS/Managers/SystemsManager.cs:211-215`; `Engine/ECS/Systems/SpriteRenderSystem.cs:140-149` | Hook loading to sprite creation or texture-path change. |
| `[HOT]` | `RenderUI()` re-initializes UI every frame and rebuilds canvas lists. | `Engine/ECS/Managers/SystemsManager.cs:237-242`; `Engine/ECS/Systems/UIRenderSystem.cs:34-52`, `:57-65`, `:93-113` | Remove the per-frame `Initialize()`; rebuild only on canvas/state change. |
| `[HOT]` | Editor overlay allocates an entity snapshot every draw. | `Editor/MGWindow/Services/EditorRenderService.cs:26-31`; `Engine/ECS/Managers/EntityManager.cs:209-212`; `Editor/MGWindowControl.cs:134-155` | Expose a read-only view or cache the snapshot with invalidation. |
| `[MUTATION/INFREQUENT]` | Stop-simulation cleanup allocates via LINQ and logs per entity. | `Engine/ECS/GameScene.cs:128-139`; `Engine/ECS/Managers/SystemsManager.cs:222-236`; `Editor/MGWindowControl.cs:198-215` | Low priority; use one-pass removal if stop latency matters. |
| `[ONE-TIME, sound]` | FixedUpdate catch-up is bounded at 8 ticks and preserves the sub-interval remainder. | `Engine/ECS/Managers/SystemsManager.cs:24`, `:171-193` | `Keep as-is`. |

### Built-in systems, hot loops

| Severity | Finding | Source | Recommended action |
|---|---|---|---|
| `[HOT]` | Sprite hooking is O(n²) per frame via `List.Contains`; hooked sprites are never unhooked. | `Engine/ECS/Systems/SpriteRenderSystem.cs:28`, `:47-59` | Use `HashSet<SpriteComponent>` and hook/unhook from component events. |
| `[HOT]` | Sprite rendering bypasses the warm cache and allocates colors/vectors/recomputes origin per sprite. | `Engine/ECS/Systems/SpriteRenderSystem.cs:73-76`, `:86-138`, `:90`, `:100-122`; `Engine/ECS/Components/TransformComponent.cs:307`, `:313-316`; `Engine/ECS/Components/SpriteComponent.cs:69-83` | Drive drawing from cached entities; cache draw data and invalidate on change. |
| `[HOT]` | `TransformSystem` walks every root hierarchy every frame and allocates a child list per node; physics writes amplify it. | `Engine/ECS/Systems/TransformSystem.cs:28-60`, `:63-105`, `:108-153`; `Engine/ECS/GameObject.cs:126-153`; `Engine/ECS/Systems/PhysicsSystem.cs:133-137` | Add a transform dirty version, cache child lists, early-out for clean subtrees. |
| `[HOT]` | `ScriptComponentSystem` re-resolves its component every frame, rechecks inactive state already gated by the cache, and has no null guard. | `Engine/ECS/Systems/ScriptComponentSystem.cs:31-56`; `Engine/ECS/GameObject.cs:253-270`; `Engine/ECS/Managers/SystemsManager.cs:291` | Keep a typed `GameObject → ScriptComponent` cache, drop the redundant branch, null-guard. Multiple scripts per entity are out of scope by user preference. |
| `[HOT, partly resolved by P1-10]` | Physics logs per contact via `Log.Info`; body/transform lookups and caches still deserve a typed pair cache. P1-10 removed the dead loop and made transform writes conditional with PPM + epsilon guards. | `Engine/ECS/Systems/PhysicsSystem.cs:119-173`; `Engine/ECS/ColliderComponent.cs:143-147`; `Engine/Utilities/Log.cs:51-65` | Gate or remove per-contact logging; cache body/transform pairs. Do not merge `TransformComponent` with the Aether components — they use different scalars. |
| `[HOT]` | UI canvases are rebuilt during render with descendant-list allocation, LINQ, per-label logging, and repeated component lookups. | `Engine/ECS/Systems/UIRenderSystem.cs:57-65`, `:93-113`, `:154-247`; `Engine/ECS/Components/UI/UICanvasComponent.cs:122-136`, `:66-86`, `:97`, `:128`; `Engine/ECS/GameObject.cs:126-153` | Use a hierarchy version counter, cache sprite/label refs, remove or gate the per-label log. |
| `[HOT/MODERATE]` | `UIInputSystem` resolves transforms per element per frame; `GameEvent` delegate caching is already sound. | `Engine/ECS/Systems/UIInputSystem.cs:48-126`, `:75`, `:80`; `Engine/Runtime/GameEvent.cs:28-44` | Cache transforms alongside `ChildElements`; skip the pass when mouse/button/hierarchy state is unchanged. |

### Destruction / subscription lifetime

| Severity | Finding | Source | Recommended action |
|---|---|---|---|
| `[MUTATION]` | Component-level event subscriptions are never detached: transform hooks, sprite hooks, Aether fixture events; destroy only resets script flags. | `Engine/ECS/Systems/TransformSystem.cs:36-39`; `Engine/ECS/Systems/SpriteRenderSystem.cs:53-58`; `Engine/ECS/ColliderComponent.cs:108-135`; `Engine/ECS/GameObject.cs:370-401` | Add component-removed/entity-destroyed unhooks; detach Aether bodies/fixtures (`Engine/ECS/Components/PhysicsBodyComponent.cs:81-93`). |
| `[MUTATION, resolved by P1-10 for late spawns]` | Late bodies are now initialized on the next tick via `InitializeEntity`; destroyed bodies are still removed only through the general system cache eviction. | `Engine/ECS/Systems/PhysicsSystem.cs:27-28`, `:75-109`, `:133-137`; `Engine/ECS/Managers/SystemsManager.cs:322-328` | Late-spawn half is `Resolved (P1-10)`. Consider explicit Aether body teardown if stale fixtures become observable. |

### Deferred API changes from P1-10

| Item | Finding | Source | Disposition |
|---|---|---|---|
| Collision callback signature | `OnCollisionEnterEvent`, `OnCollisionExitEvent`, `BeforeCollisionEvent`, and `AfterCollisionEvent` are `Action<GameObject>` and receive the owning object only. They do not expose the other collider/body or contact data. | `Engine/ECS/ColliderComponent.cs:98-101`, `:138-159` | `Needs decision before Alpha v1`. Changing the public delegate shape affects subscribers, so it was deliberately deferred from P1-10; do not fold it into an unrelated physics fix. |

### Serialization save / load

| Severity | Finding | Source | Recommended action |
|---|---|---|---|
| `[STARTUP/LOAD]` | Component state is reflected per member with no metadata cache. | `Engine/Serialization/ComponentSerializer.cs:17-27`, `:40-67`, `:152-199`, `:213-239` | Cache per-`Type` field/property metadata and ignore flags; reuse for export/import/reference resolution. |
| `[STARTUP/LOAD]` | Reference resolution has a nested component-name loop. | `Engine/Serialization/SceneSerializer.cs:312-330`, `:318-328` | Build a per-entity `Dictionary<string, GameComponent>` keyed by normalized type name. |
| `[STARTUP/LOAD]` | Imported components go through `AddComponent` and fire enable hooks during load. | `Engine/Serialization/GameObjectSerializer.cs:50-75`; `Engine/ECS/GameObject.cs:213-250` | Assign directly during deserialization; run enable hooks once after the scene is fully assembled. |
| `[SAVE/LOW]` | Save enumerates the system cache with seven `typeof` checks and copies entity lists. | `Engine/Serialization/SceneSerializer.cs:107-130`; `Engine/Serialization/GameObjectSerializer.cs:15`; `Engine/ECS/Managers/EntityManager.cs:209-212` | Low priority: track a custom-system flag in the registry and expose read-only entity views. |

### Highest-value hot-path shortlist

1. `Engine/ECS/Systems/SpriteRenderSystem.cs:28`, `:47-59` — O(n²) `List.Contains` hooking per frame; no unhook.
2. `Engine/ECS/Systems/TransformSystem.cs:28-60`, `:63-105` + `Engine/ECS/GameObject.cs:126-153` — full hierarchy walk and per-node list allocations every frame, amplified by physics writes.
3. `Engine/ECS/Query.cs:50-54` + `Engine/ECS/Managers/SystemsManager.cs:291` — LINQ/`IsAssignableFrom` on every mutation while exact-type buckets remain unused (`Engine/ECS/Managers/EntityManager.cs:186-193`, `:214-228`).
4. `Engine/ECS/Managers/SystemsManager.cs:211-215`, `:237-242` + `Engine/ECS/Components/UI/UICanvasComponent.cs:122-136`, `:66-86` — per-frame UI initialization/rebuild and log churn.
5. `Engine/ECS/Systems/PhysicsSystem.cs:119-173` + `Engine/ECS/ColliderComponent.cs:143-147` — typed pair cache and per-contact logging remain; P1-10 already removed the dead loop and gated transform writes by PPM + epsilon.

### Already sound — do not optimize first

- Incremental warm caches and explicit rebuild: `Engine/ECS/Managers/SystemsManager.cs:122-150`, `:283-328`.
- Capped fixed-step catch-up: `Engine/ECS/Managers/SystemsManager.cs:24`, `:171-193`.
- `TypeResolver` hit caching: `Engine/Utilities/TypeResolver.cs:9-30`.
- `GameEvent` delegate caching: `Engine/Runtime/GameEvent.cs:26-44`.

## 3. Cross-cutting and process

| Item | Detail | Disposition |
|---|---|---|
| CI covers only maintained DesktopGL targets | `scripts/build.ps1` and `.github/workflows/ci.yml` verify four builds plus 71 xUnit facts; `Engine.Tests` supplements but does not replace manual editor integration testing. Mobile/Web/Multiplatform/GISM are excluded. | `Cross-cutting` / `Resolved (P3-1)`. Extend only when a new editor smoke path exists. |
| Saved-project verification | Author projects under `D:\GameDevelopment\TestFolderEngine\TestProject\` are the end-to-end check but must never be modified. Scratch copies and the saved-scene harness are the safe substitute. | `Cross-cutting`. Keep using copies. |
| Full solution build needs mobile workloads | `dotnet build Tesseract2D.sln` fails without .NET 9 iOS/Android workloads; build individual projects. | `Cross-cutting`. No change needed. |
| Serialization change verification | After any DTO/serializer change, run `Engine.Tests`, both project builds, Markdown link validation, and the scratch saved-scene harness on copies of all three projects. | `Cross-cutting`. |

## 4. Explicitly out of scope / do not do

- No 3D, networking, Lua, Vulkan/other backends, or localization-as-a-new-feature ([12-future-roadmap.md](12-future-roadmap.md)).
- Do not touch `GISM/`.
- Do not delete or wire behavior into `Engine/Utilities/MockGame.cs`.
- Do not “repair” stale checked-in `.mgcb` content config without validating the author’s dev-branch content pipeline ([09-build-and-content.md](09-build-and-content.md)).
- Do not add features to WinForms; stabilization only.
- Do not pursue the WinForms gameplay-assembly reload redesign; revisit the requirement in the new editor host.

## 5. When to revisit

1. **When the new Photino/React editor reaches gameplay-script loading:** revisit section 1 as requirements, not as code to port.
2. **Before Alpha v1:** record a decision for every `Needs decision before Alpha v1` row in section 2.
3. **After any serialization/DTO change:** run the full verification checklist in section 3 before marking anything done.
4. **Before acting on section 2A:** capture a baseline with a representative scene; implement only shortlist items whose impact is measured.

## See also

- [00-status-and-scope.md](00-status-and-scope.md) — what exists today
- [08-react-editor-boundary.md](08-react-editor-boundary.md) — the intended editor API boundary
- [14-alpha-todo.md](14-alpha-todo.md) — active Alpha v1 work queue
- [11-health-todo.md](11-health-todo.md) — archived hardening pass
- [12-future-roadmap.md](12-future-roadmap.md) — Alpha v1 phases and scope decisions
