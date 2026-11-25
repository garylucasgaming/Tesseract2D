# 10 — Known Issues

This is the consolidated catalogue of gotchas, inconsistencies, and half-implemented behaviours found across the codebase. Each entry has a source citation so you can verify the current state. Subsystem docs contain deeper explanation: this file is the quick lookup and the "why does this behave oddly?" index.

**Status:** `Partial` — the engine works, but many of these are load-bearing quirks rather than cosmetic bugs.

> Line numbers reflect the snapshot this documentation was written against. If a citation looks off by a few lines, search by member name.

## 1. ECS and queries

| Issue | Detail | Source |
|---|---|---|
| **Resolved (P1-4, add paths):** Component `isEnabled` default | Both `AddComponent<T>()` and `AddComponent(GameComponent)` register the component before setting `isEnabled = true`; duplicate non-generic adds are rejected before hooks and unchanged assignments are ignored. `entity.isActive` remains the separate system cache gate. Non-generic removal still does not disable the component. | `Engine/ECS/GameObject.cs:213-250`, `:326-337`; `Engine/ECS/GameComponent.cs:46-64`; `Engine/ECS/Managers/SystemsManager.cs:291` |
| Two different enable hooks (by design) | `GameComponent.OnEnabled/OnDisabled` and `ScriptComponent.OnEnable/OnDisable` are separate contracts; `ScriptComponent` is a MonoBehaviour-like subsystem driven by `ScriptComponentSystem`. Do not merge them. | `Engine/ECS/GameComponent.cs:70-76`; `Engine/ECS/Components/ScriptComponent.cs:19-33`; `Engine/ECS/Systems/ScriptComponentSystem.cs:28-66` |
| `Has<T>` vs `HasComponent<T>` semantics (by design) | `Query.Has<T>` is polymorphic (`IsAssignableFrom`) for systems; `GameObject.HasComponent<T>` and `EntityManager` buckets are exact-type. `QueryTests` pins the distinction. | `Engine/ECS/Query.cs:46-55`; `Engine/ECS/GameObject.cs:272-281`; `Engine/ECS/Managers/EntityManager.cs:184-195`; `Engine.Tests/QueryTests.cs:9` |
| Query evaluation does not use component buckets | `EntityManager.GetQuery` re-scans all entities; the type buckets service a different lookup API. The manifest's "high-speed buckets" claim does not describe `Query`. | `Engine/ECS/Managers/EntityManager.cs:214-228`, `Engine/ECS/Query.cs`, `Engine/Engine_Manifest.txt:17` |
| **Resolved (P1-2):** System caches are add-only | Warm `HashSet<GameObject>` caches now add or evict entities from `entity.isActive && RequiredComponents.IsMatched(entity)`. Activation changes are republished by `EntityManager` and routed through `GameScene`. `GameComponent.isEnabled` is still intentionally outside the cache gate. | `Engine/ECS/Managers/SystemsManager.cs:283-317`, `Engine/ECS/GameScene.cs:85-90`, `Engine/ECS/Managers/EntityManager.cs:27`, `:45`, `:95`, `:149-155` |
| `EntityUpdate` systems get zero delta | `OnEntityChanged` passes `deltaTime = 0` and `playModeActive = true` by default. | `Engine/ECS/Managers/SystemsManager.cs:283` |
| **Resolved (P0-6):** `FixedUpdate` accumulator | The timer subtracts `UpdateInterval`, passes the fixed interval to the system, caps catch-up at 8 ticks while keeping the sub-interval remainder, and guards non-positive intervals. | `Engine/ECS/Managers/SystemsManager.cs:24`, `:171-193` |
| **Resolved (P1-7):** `AddChild` registration | `AddChild` delegates to `SetParent`, registers unregistered children with the scene, rejects cycles, and `RemoveChild` clears `ParentId` and fires the child's `ParentChangedEvent`. | `Engine/ECS/GameObject.cs:78-124`, `:170-206`; `Engine.Tests/HierarchyRegistrationTests.cs:9-97` |
| `GameComponent.Parent` setter is a no-op | Declared but does nothing. | `Engine/ECS/GameComponent.cs:39-41` |
| `TransformComponent` cannot be removed | Both generic and non-generic removal paths guard it. | `GameObject.cs:307-308`, `:328-329` |
| No `OnDestroy` hook | `GameObject.Destroy()` unregisters/unlinks and resets script `hasStarted`, but does not call a destruction callback. | `GameObject.cs:370-401` |
| **Resolved (P2-5):** Lifecycle manager hooks had no call sites | `GameHost` initializes and updates active registered managers, but calls `Shutdown` on all registered managers at scene swap/host shutdown (`Engine/Runtime/GameHost.cs:141-155`, `:207-230`). `SceneManager` activation remains separately unwired and `IsPersistent` is still unused. See [12-future-roadmap.md §3.7](12-future-roadmap.md). | `Engine/Runtime/GameManager.cs:11-37`, `Engine/Runtime/GameHost.cs:141-155`, `:207-230` |
| **Resolved (P1-9):** Scene-switch state was not reset | `resetContextSceneInManagers()` is now an alias for the canonical `FinalizeInitialization()`, which rebinds contexts and rebuilds warm caches. `InitializeManagers()` is idempotent for managers, events, and database loading, and `SceneSerializer.LoadScene` finalizes before returning. | `Engine/ECS/GameScene.cs:80-124`, `:126-137`, `:286-289`; `Engine/ECS/Managers/SystemsManager.cs:122-150`; `Engine/Serialization/SceneSerializer.cs:337` |
| `GameSpace.ActiveScene` is never assigned | `GameSpace` is metadata only; the real active scene is editor state. | `Engine/ECS/GameSpace.cs:23-27` |

See [02-ecs.md](02-ecs.md) for the mechanics behind these.

## 2. Physics (Aether)

| Issue | Detail | Source |
|---|---|---|
| **Resolved (P1-10):** One-shot body initialization | `InitializeEntity` runs for each matching entity on every tick and is guarded per body and per collider, so late spawns and late-added colliders join the next simulation step. | `Engine/ECS/Systems/PhysicsSystem.cs:75-109`, `:133-137` |
| **Resolved (P1-10):** Transform → body sync was missing | Before `Step`, a changed transform is pushed to the Aether body using `WorldPosition / PixelsPerMeter` with position/rotation epsilons; after `Step`, dynamics/kinematics write pose back to the transform. | `Engine/ECS/Systems/PhysicsSystem.cs:139-172` |
| **Resolved (P1-10):** One collider per body assumed | Initialization now loops over every `ColliderComponent` on the entity and attaches each fixture. | `Engine/ECS/Systems/PhysicsSystem.cs:95-98` |
| `ColliderComponent.Offset` is unused | Declared but not applied. | `Engine/ECS/ColliderComponent.cs` |
| Collision callbacks lose the other party (deferred sub-item) | Callback type is `Action<GameObject>` and receives the owner, not the collider/contact that was hit. No contact payload. Signature change tracked in [13-refactor-todo.md](13-refactor-todo.md). | `Engine/ECS/ColliderComponent.cs:98-101`, `:138-159` |
| Physics does not run in editor | `PhysicsSystem` has `UsedInEditor = false`; the editor drives a fixed-step accumulator only in play mode. | `Engine/ECS/Systems/PhysicsSystem.cs:44-45`, `Editor/MGWindowControl.cs:92-111` |

## 3. Runtime and boot

| Issue | Detail | Source |
|---|---|---|
| Scene switching via serialized `SceneManager` methods is a TODO — activation methods exist but are not host-wired | `SceneManager.LoadScene` and `PreLoadScene` are empty and serialized load lines are commented out, but `AddScene`/`LoadFirstScene`/`SetNextScene`/`LoadNextScene`/`Update`/`TickUpdate` are implemented. `GameHost` loads/attaches one scene directly instead of calling the activation API (`Engine/Runtime/GameHost.cs:69-99`). | `Engine/Runtime/SceneManager.cs:34-52`, `:55-89`, `:92-102`, `:104-121`, `:123-131`; `Engine/Runtime/GameHost.cs:69-99` |
| **Resolved (P0-7):** `SetNextScene` key/Count mismatch | Ordered load-order keys are now used for both `LoadFirstScene` and `SetNextScene`; `NextScene` is cleared when there is no successor and `LoadNextScene` no-ops at the end. | `Engine/Runtime/SceneManager.cs:55-102` |
| `GameManager.IsPersistent` is unused | Declared but not implemented by scene switching. | `Engine/Runtime/GameManager.cs:21` |
| **Resolved (P2-5):** No standalone boot path | DesktopGL `Game1` constructs `EngineContext`, `SessionGlobals`, and `GameHost`, loads a scene path from `TESSERACT2D_SCENE` or attaches an empty scene, and delegates update/tick/render (`Engine.Desktop/Game1.cs:34-92`). Manifest/content-driven boot and a real GUI run remain unverified. | `Engine.Desktop/Game1.cs:34-92`, `Engine/Runtime/GameHost.cs:69-177` |
| Platform hosts are mixed maturity | Desktop now boots the shared host; iOS/Android/Web and `Engine.Gameplay` remain template stubs. | `Engine.Desktop/Game1.cs:34-92`; `Engine.Ios/Game1.cs:23`; `Engine.Web/Engine.WebGame.cs:37` |
| `MockGame` has no in-repo call sites — this is intentional | It is a pure sentinel `Game` for an experimental MonoGame dev-branch content builder that requires a `Game` reference but does not use that instance's state or folder. The class performs no work of its own; do not try to wire behaviour into it. The in-code XML comment now states this too. The consumer is external, so it is **not** dead code despite having no local callers. The dev branch is ~5 months old as of this doc and may be obsolete upstream. | `Engine/Utilities/MockGame.cs:26-45`, comment at `:26-29` |
| **Resolved (P2-4):** `Globals` was static process-wide | `SessionGlobals` owns per-session `InputManager`, `EditorCamera`, and `Viewport` (`Engine/Runtime/SessionGlobals.cs:11-41`); `Globals.Current` remains a compatibility shim for not-yet-injected callers. | `Engine/Runtime/SessionGlobals.cs:11-41`, `Engine/Runtime/Globals.cs` |

See [05-runtime.md](05-runtime.md).

## 4. Projects, manifest, and content

> **Scope note:** the content-related rows below describe the **checked-in** MonoGame configuration. The author's experimental dev-branch pipeline auto-registers new files in the `.mgcb` list and hot-reloads assets, so some of these (stale `BuildContent.targets`, the `ContentOutPut` typo, empty `.mgcb` manifests) may not apply in practice. See [09-build-and-content.md §4.4](09-build-and-content.md).

> **Resolved (P0-1):** `LoadManifest` used to append `Content/ProjectManifest.db` to an already-complete path. It now takes the full manifest path directly (`Editor/Tesseract2DLauncher.cs:347-361`), so the launcher's existing call sites at `:113` and `:331` read real manifest values instead of falling back to `null`. The launcher also prefers the manifest's `ProjectName` for history (`:115`). See the [archived P0 hardening notes](archive/11-health-todo.md).

> **Resolved (P0-2):** `ProjectDirectoryFactory.CreateNewProject` now calls `ProjectManifest.CreateInitialManifest` (`Engine/Serialization/ProjectDirectoryFactory.cs:22`, `:72-74`) instead of writing `"{}"`; the launcher passes its selected platforms (`Editor/Tesseract2DLauncher.cs:45`). A newly created project gets a populated manifest. Note that the author's three saved test projects still have literal `{}` manifests — this fix applies only to projects created after it. See the [archived P0 hardening notes](archive/11-health-todo.md).

| Issue | Detail | Source |
|---|---|---|
| `CurrentProjectManifest` is effectively inert | Declared on `IEngineContext`/`EngineContext` but not meaningfully assigned or used; `EditorContextManager` only forwards it. | `Engine/Runtime/EngineContext.cs:17`, `:56`; `Editor/Services/EditorContextManager.cs:29-32` |
| Unused local in project creation | `globalSystemsGo` is created but never added to the scene, so it is not serialized. | `ProjectDirectoryFactory.cs:103-106` |
| Stale checked-in scene | `HierarchyTest.scene` is old JSON with `id`/`Components`/`Children` keys, not the current `SceneDataDto` YAML schema. Valid current-format samples now also exist in `Engine.Tests/TestContent/Fonts/`; see the [archived verification assets](archive/11-health-todo.md). | `SavedGameProjectData/Content/Scenes/HierarchyTest.scene:1-35`, `Engine.Tests/TestContent/Fonts/README.md:7-18` |
| **Resolved (P1-1b):** No current-format content samples | Sanitized current-format `.scene`, map YAML, populated `.database`, short-name script, and populated manifest are checked in under `Engine.Tests/TestContent/Fonts/`. | `Engine.Tests/TestContent/Fonts/README.md:7-18` |
| Content builder skips `*.database` | Wildcards copy `*.yml`, `*.cs`, `*.scene`, `*.prefab`, `*.data` — but not `*.database`. | `Engine.Content/Builder/DynamicBuilder.cs:43-55` |
| Content path conventions conflict | Editor content root `Content/Bin/Content`, builder output `Content/Bin`, `AssetManager` looks under `BinPath/Content/Assets`. | `Editor/Form1.cs:335-336`, `:403`, `Engine/Utilities/AssetManager.cs:108-123` |
| `BuildContent.targets` has unreplaced placeholders | Contains `___SafeGameName___` and is not imported. | `Engine.Content/BuildContent.targets:6-10` |
| Desktop project has path typos | `ContentOutPut` vs `$(ContentOutput)`, and `-s Assets` with no such folder. | `Engine.Desktop/Engine.Desktop.csproj:33-40` |
| Hardcoded developer sandbox | `AssetPathProvider` ascends four directories from the binary and pins `SavedGameProjectData`. | `Engine/Utilities/AssetPathProvider.cs:20-38` |
| **Resolved (P0-9):** Editor solution metadata | `Editor/Editor.sln:6` now references `Engine.Editor.csproj` with the main solution's editor project GUID. | `Editor/Editor.sln` |
| **Resolved (P0-9):** Web solution GUID mismatch | `Engine.Web/Engine.Web.sln:6` now uses the same Web project GUID as `Tesseract2D.sln:14`. | `Engine.Web/Engine.Web.sln` |
| **Resolved (P3-1):** CI now runs 71 headless xUnit tests | `scripts/build.ps1:23-50` builds `Engine.Core`, `Engine.Content`, `Editor`, and `Engine.Desktop`, then runs `Engine.Tests` with TRX output; `.github/workflows/ci.yml:7-29` runs it on `windows-latest` using root `global.json`. `Engine.Tests/Engine.Tests.csproj:8,12-15` defines the xUnit project and portable fixtures live in `Engine.Tests/TestContent/Fonts/README.md:1-27`. Manual integration testing via the author's saved editor projects remains the end-to-end check — see the [archived verification assets](archive/11-health-todo.md). | `scripts/build.ps1:23-50`, `.github/workflows/ci.yml:7-29`, `Engine.Tests/Engine.Tests.csproj`, `global.json` |
| Full solution build needs mobile workloads | `dotnet build Tesseract2D.sln` fails on Windows without .NET 9 iOS/Android workloads. Build individual projects. | `Engine.Ios/`, `Engine.Android/` project files |
| Gameplay reload can duplicate assemblies in the default load context | `ReloadProjectAssemblies()` drops only the manager's own list and calls `Assembly.Load(byte[])` again for every DLL; previous copies stay loaded. `TypeResolver.ClearCache()` releases cached `Type` references but cannot unload those default-ALC assemblies, so an assembly-qualified name can still bind to an already loaded older copy through `Type.GetType`. Duplicate short/full-name fallback matches are warned and rejected instead of resolving. The collectible `LoadGameplayAssembly` path does unload correctly. Redesigning the reload source of truth is deferred with the broader refactor. | `Editor/Utilities/ScriptAssenblyManager.cs:48-89`, `:22-46`, `:92-105`, `Engine/Utilities/TypeResolver.cs:26`, `:57-72`, `:79-85` |

See [03-projects-and-serialization.md](03-projects-and-serialization.md) and [09-build-and-content.md](09-build-and-content.md).

## 5. Serialization

| Issue | Detail | Source |
|---|---|---|
| **Resolved (P1-6):** No versioning or strict failure | Scene/entity/map/database DTOs now declare `Version` (default `1`); future versions throw a path-bearing `SerializationVersionException`, stale versions warn, and opt-in strict mode fails on malformed IDs/types and unresolvable map tile rows. Default mode still recovers. Unknown fields remain ignored and there is no migration hook. | `Engine/Serialization/SerializationVersion.cs:12-22`, `Engine/Serialization/SerializationVersionException.cs:9`, `Engine/Serialization/SceneSerializer.cs:143`, `:151-162`; `Engine/Serialization/GameObjectSerializer.cs:28-59`; `Engine/Serialization/TileMapSerializer.cs:106-204`; `Engine/Serialization/DatabaseSerializer.cs:61-96` |
| Unsupported values are silently dropped | `ExportValue` returns null for anything outside primitives/enums/Vector2/known `$ref`s/lists; component import swallows errors. | `ComponentSerializer.cs:72-139`, `:194-198` |
| **Resolved (P1-3):** Two type resolvers with different rules | `TypeResolver.FindType` is now the only serialized type resolver; scene manager/system names use it with `GameManager`/`GameSystem` constraints. Unknown, wrong-base, and ambiguous full/short names are warned and skipped by default; strict mode throws instead. | `Engine/Utilities/TypeResolver.cs:12-77`, `Engine/Serialization/SceneSerializer.cs:215-279` |
| **Resolved (P1-3):** `TypeResolver` cache can go stale | `TypeResolver.ClearCache()` drops cached `Type` references; the editor calls it after gameplay assembly load, reload, and unload. Default-ALC assembly duplication on reload remains a separate editor issue in §4. | `Engine/Utilities/TypeResolver.cs:79-85`, `Editor/Utilities/ScriptAssenblyManager.cs:43`, `:88`, `:97` |
| `[Ignore]` detection is by attribute name string | `ShouldIgnore` checks `attr.GetType().Name == "IgnoreAttribute"`; `[Browsable(false)]` and `[DatabaseIgnore]` do not affect serialization. | `ComponentSerializer.cs:17-27` |
| Private backing fields are not serialized | Serializer uses `BindingFlags.Public \| BindingFlags.Instance`, despite template comments saying otherwise. | `ComponentSerializer.cs:15`, `Editor/Templates/.cs/DataComponent.tmpl:36-38` |
| Component references match by short name only | Case-insensitive short `Name`, no namespace — collisions possible. | `ComponentSerializer.cs:266` |
| **Resolved (P0-5):** Unguarded GUID parsing | Scene, entity, parent, and database IDs now use `Guid.TryParse` with warnings and recovery values by default. Invalid scene/entity/database IDs get generated GUIDs; invalid parent IDs become `Guid.Empty`. Strict mode throws instead. Component references are paired to imported entities by index so recovered entity IDs still resolve. | `Engine/Serialization/SceneSerializer.cs:151-162`, `:312-330`; `Engine/Serialization/GameObjectSerializer.cs:32-54`; `Engine/Serialization/DatabaseSerializer.cs:100-110` |
| Active map index clamp asymmetry | Save clamps `ActiveMapIndex`; load assigns directly. | `Engine/Serialization/SceneSerializer.cs:101`, `:207` |
| Built-in systems are hardcoded out of YAML | Seven concrete types are excluded from serialized custom systems. | `SceneSerializer.cs:112-121` |
| **Resolved (P1-8):** Map tile-data rebind was incomplete | `SaveMap` now emits `TileIndexDataDictionary` with explicit `AssetID`s; `LoadMap` rebinds by `AssetID`/`DisplayName` and rebuilds the derived per-tile grid after the final grid assignment. | `Engine/Serialization/TileMapSerializer.cs:57-86`, `:145-200`; `Engine.Tests/MapRoundTripTests.cs:11-152` |
| `TileMapSerializer` calls `IgnoreFields()` | Serialization ignores fields, which can surprise field-based DTOs. | `TileMapSerializer.cs:47-50` |

## 6. Databases

| Issue | Detail | Source |
|---|---|---|
| Generic database references do not round-trip | `DatabaseReference` (type `Database`) is omitted; `DataReference` (type `DataComponent`) is written as a GameComponent `$ref` with empty owner ID and cannot resolve to a row. | `ComponentSerializer.cs:72-139`, `:255-272` |
| **Resolved (P2-2):** Runtime loading depended on editor path | `GameScene.Database` is injected or created locally; `GameScene.InitializeManagers` loads from `Context.DatabasePath` through an injected `DatabaseManager`, and `Map.ResolveDatabase` reads only `activeScene.Database`. | `Engine/ECS/GameScene.cs:62-83`, `:105-123`; `Engine/Gameplay/Map.cs:328-344`; `Engine/Serialization/SceneSerializer.cs:147`, `:169` |
| `DatabaseType` stored as short name | No namespace; combo population and selection use inconsistent values. | `Editor/DatabaseViewer.cs:92-95`, `:373`, `:381` |
| Duplicate display names survive suffixing | YAML keys strip spaces and gain `_1`, `_2`, but `DisplayName` stays the same after load. | `DatabaseSerializer.cs:62-74` |
| `GetComponent(string)` is case-sensitive | Editor converters are generally case-insensitive — inconsistent. | `Engine/Collections/Database.cs:32-42` |
| `Clone()` is shallow | `MemberwiseClone()` on `DataComponent`. | `Engine/ECS/Components/DataComponent.cs:50-53` |
| Unresolved `DataType` drops the whole database | Logged and returns null. | `DatabaseSerializer.cs:125-134` |
| `[DatabaseIgnore]` is grid-only | It does not hide fields from serialization — `[Ignore]` does. | `Editor/DatabaseViewer.cs:236-241`, `ComponentSerializer.cs:17-27` |

See [04-databases.md](04-databases.md).

## 7. GameEvent

| Issue | Detail | Source |
|---|---|---|
| Editor uses short names, converter uses full names | Runtime accepts both, but metadata/DTO contracts should standardize. | `Editor/PropertyWrappers/GameEventPropertyDescriptor.cs:135`, `GameEventTypeConverter.cs:39`, `Engine/Runtime/GameEvent.cs:52-54` |
| Method filtering differs | Editor dropdowns use `DeclaredOnly`; runtime search does not. Inherited methods can invoke but not be picked. | `GameEventPropertyDescriptor.cs:157-160`, `GameEvent.cs:58-64` |
| Cached delegates can go stale | Cleared only on error, not when target references change. | `GameEvent.cs:33-45`, `:71-81` |
| Limited signature support | Only public instance zero-argument `void` methods; no args, return values, async, or event args. | `GameEvent.cs:28-76` |
| Errors are logged and swallowed | No exception surfaces to the caller. | `GameEvent.cs:39-43`, `:71-75` |

## 8. Editor and the engine/editor boundary

| Issue | Detail | Source |
|---|---|---|
| **Resolved (P2-1):** `EditorContextManager` lived in `Engine.Core` | The concrete bridge now lives in the editor assembly and forwards to `EngineContext.Current`; engine code uses injected `IEngineContext` and `SessionGlobals`. | `Editor/Services/EditorContextManager.cs:10-17`; `Engine/Runtime/EngineContext.cs:13-20`; `Engine/Runtime/SessionGlobals.cs:11-41`; `Engine/Engine_Manifest.txt:4` |
| **Resolved (P2-1/P2-2/P2-3):** Core read editor state | Database loading uses injected `DatabaseManager`, runtime-created ownership is explicit, map databases come from `GameScene.Database`, and asset paths use injected/default `IEngineContext`. Static bridges remain only for frozen WinForms callers. | `Engine/ECS/GameScene.cs:62-83`, `:153-172`; `Engine/Gameplay/Map.cs:328-344`; `Engine/Utilities/AssetManager.cs:60`, `:113` |
| Dirty tracking is inconsistent | Set from property edits, tile paint, map changes — but not from component add, most hierarchy edits, or gizmo drags. `SaveScene()` also writes `_needsToBeSaved = false` directly, bypassing the setter's title refresh. | `Editor/Form1.cs:78-92`, `:1110-1116`, `:642`, `EditorInputService.cs:634-640`, `Form1.cs:2113-2119`, `Form1.SceneHierarchy.cs:139-345` |
| **Resolved (P0-4):** Converter filenames now match their classes | `DatabaseReferenceDropdownConverter.cs` declares `DatabaseReferenceDropdownConverter` and `GameEventTypeConverter.cs` declares `GameEventTypeConverter`; their call sites in `FilteredPropertyWrapper` and `GameEventPropertyDescriptor` were updated. | `Editor/PropertyWrappers/DatabaseReferenceDropdownConverter.cs:14`, `GameEventTypeConverter.cs:13`, `Editor/FilteredPropertyWrapper.cs:110`, `Editor/PropertyWrappers/GameEventPropertyDescriptor.cs:33` |
| Two inspector build paths | `RebuildInspectorPanel` creates a new `FlowLayoutPanel`, while hierarchy `AfterSelect` rebuilds the designer's `InspectorFlowPanel`; selection can be cleared mid-flight by `ComponentCardFactory.ClearSelection()`. | `Editor/Form1.cs:832-925`, `:884-890`, `Editor/Form1.SceneHierarchy.cs:549-586` |
| Hot reload does not rehydrate live instances (policy chosen) | The host explicitly reports `ReloadRequired` after an assembly swap and clears it only after `scene.reload`. WinForms still just reloads the assembly and refreshes the UI; it is not wired to the new host event. Attached scenes without a file path refuse `scene.reload`. | `Engine/Runtime/GameHost.cs:88-99`, `:101-134`; `Editor/Form1.cs:216-260` |
| `PropertyGrid` internals are reflected into | `ComponentCardFactory` reaches private fields (`gridView`, `edit`, `dropDownHolder`, `MoveSplitterTo`) — fragile across .NET versions. | `Editor/ComponentCardFactory.cs:201-338` |
| Two assembly-loading paths | `Assembly.LoadFrom` in the launcher vs collectible `AssemblyLoadContext` in `ScriptAssenblyManager`. | `Editor/Tesseract2DLauncher.cs:139-162`, `Editor/Utilities/ScriptAssenblyManager.cs:21-84` |
| Two inspector-build paths | Form-level and hierarchy-level card builders duplicate logic. | `Editor/Form1.cs:832-925`, `Editor/Form1.SceneHierarchy.cs:549-586` |
| Filename is misspelled | `ScriptAssenblyManager.cs` (missing `e`) — preserve it when referencing. | `Editor/Utilities/ScriptAssenblyManager.cs` |
| Theme is disabled | `ApplySynthwaveTheme` is commented out in the launcher. | `Editor/Tesseract2DLauncher.cs:24` |
| Stopping simulation reloads from disk | Stop reloads the saved `.scene` rather than restoring in-memory state; unsaved changes are lost. | `Editor/Form1.cs:2156-2237` |
| No editor API | No editor service of any kind exists. The chosen replacement is a .NET console host with a Photino message bridge and a native MonoGame viewport — not HTTP/WebSocket/SignalR. | repository scan; see [08-react-editor-boundary.md](08-react-editor-boundary.md) and [12-future-roadmap.md Phase 5](12-future-roadmap.md) |

See [07-editor-internals.md](07-editor-internals.md) for the editor mechanics and [01-architecture.md](01-architecture.md) for the boundary discussion.

## 9. Authoring and templates

| Issue | Detail | Source |
|---|---|---|
| Template comments drift from code | Private backing fields are claimed serializable; they are not. Template prose describes intent, not guaranteed behaviour. | `Editor/Templates/.cs/DataComponent.tmpl:36-38`, `ComponentSerializer.cs:15` |
| Template folder requires the dot | `CodeTemplateGenerator` builds `Editor/Templates/<language>/<type>.tmpl`; the real folder is `.cs`, so callers must pass `".cs"`. A miss writes empty content. | `Editor/Utilities/CodeTemplateGenerator.cs:18-35` |
| **Resolved (P0-3):** New System template wrote an empty file | The project-explorer menu now passes `SystemComponent`, matching `Editor/Templates/.cs/SystemComponent.tmpl` (`Editor/Form1.ProjectExplorer.cs:95`). A generated sample compiled with only the template's pre-existing nullability warning. | `Editor/Form1.ProjectExplorer.cs:95`, `Editor/Utilities/CodeTemplateGenerator.cs:20-35` |
| **Resolved (P3-2):** Script source vs. compile vs. watch paths disagreed | `ProjectPaths` is now the single source: `Content/Assets` for scripts and `Source/<name>.Gameplay/bin/Debug/net8.0` for compiler output. Generator, watcher, compiler, reload scanner, and launcher all consume it. | `Engine/Runtime/ProjectPaths.cs:12-43`; `Editor/Utilities/SolutionGenerator.cs:10-41`; `Editor/Form1.cs:173-199`; `Editor/Utilities/ScriptCompiler.cs:21-77`; `Editor/Utilities/ScriptAssenblyManager.cs:48-89`; `Editor/Tesseract2DLauncher.cs:140-162` |
| `Engine.Gameplay` is a placeholder | Contains only `testSystem.cs`; not a real game. | `Engine.Gameplay/` |
| GISM is abandoned | Custom markup experiment, not in the solution. Ignore it. | `GISM/` |

See [06-authoring-guide.md](06-authoring-guide.md).

## See also

- [00-status-and-scope.md](00-status-and-scope.md) — maturity matrix
- [01-architecture.md](01-architecture.md) — stated vs. actual separation
- Every subsystem doc has its own shorter "Gotchas / Known Limitations" section
