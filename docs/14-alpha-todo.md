# 14 — Alpha v1 TODO

**Status:** `Active`. This is the working queue for Alpha v1. It replaces the archived [11 — Engine Health TODO](11-health-todo.md); scope and rationale remain in [12 — Future Roadmap](12-future-roadmap.md).

The hardening pass is closed: P0-1 through P0-10, P1-1 through P1-10, P2-1 through P2-5, and P3-1 through P3-3 are complete. Remaining health items were carried forward here or into the roadmap, not left as open hardening work. See [11-health-todo.md](11-health-todo.md) for the archive index and [archive/11-health-todo.md](archive/11-health-todo.md) for the historical checklist.

## How to use this list

- Each item carries a roadmap phase and Alpha exit criterion ID from [12-future-roadmap.md §2](12-future-roadmap.md).
- Checkboxes are the active todo state. Do not add items here unless they are needed for an Alpha phase or exit criterion.
- Engine/runtime refactors without an Alpha acceptance criterion stay in [13-refactor-todo.md](13-refactor-todo.md).
- WinForms is frozen. Do not add features to it; carry requirements forward to the new host.
- DesktopGL is the only Alpha platform target. Mobile, Web, Multiplatform, GISM, 3D, networking, Lua, and localization-as-a-new-feature stay out of scope.

## Current baseline

- Core decoupling, per-session state, and the shared frame loop are landed: `IEngineContext`/`EngineContext` (`Engine/Runtime/EngineContext.cs:13-20`, `:46-63`), `SessionGlobals` (`Engine/Runtime/SessionGlobals.cs:11-41`), and `GameHost` (`Engine/Runtime/GameHost.cs:141-177`, `:207-230`).
- Canonical gameplay script paths are landed via `ProjectPaths` (`Engine/Runtime/ProjectPaths.cs:12-43`).
- CI builds four maintained targets and runs 71 xUnit facts (`scripts/build.ps1:23-50`, `.github/workflows/ci.yml:7-29`).
- The project manifest is still minimal (`Engine/Serialization/ProjectManifest.cs:12-38`); it is not the Alpha config backbone.
- Scene activation exists but serialized load is still a TODO (`Engine/Runtime/SceneManager.cs:34-102`, `:104-121`), and `GameHost` attaches one scene directly (`Engine/Runtime/GameHost.cs:69-99`).
- There is no new editor backend contract yet; see [08-react-editor-boundary.md](08-react-editor-boundary.md).

## Active queue

### [ ] T1 — Phase 2: project config v1

**Criterion:** A1, A2, A4. **Status:** not started.

- Define and version the manifest schema: quality tier, graphics-backend selector, per-project editor preferences, registered scene list + startup scene, content roots + source/output paths, and target platform.
- Keep `{}` manifests loadable with defaults; the author's saved projects and the portable fixture both rely on that (`Engine.Tests/TestContent/Fonts/README.md:7-24`).
- Record a decision on the migration hook. Version validation exists (`Engine/Serialization/SerializationVersion.cs:12-22`), but there is no migration hook yet.
- Replace hardcoded editor path assumptions with manifest-backed values; preserve the canonical `Content/Assets` and `Source/<Project>.Gameplay/bin/Debug/net8.0` conventions from `ProjectPaths`.
- Acceptance: editor and runtime both read the same manifest fields; a new project can define its startup scene and content roots without hand-editing files.

### [ ] T2 — Phase 3: additive scene runtime

**Criterion:** A3. **Status:** partial — manager hooks landed, scene switching not wired.

- Make `GameHost` consume `SceneManager` as the owner of the active scene set instead of attaching one scene directly.
- Support named registration, explicit activate/deactivate/unload, multiple active scenes, and defined render order.
- Decide which API owns disk loading: serialized `SceneManager.LoadScene`/`PreLoadScene` or host-level loading through `SceneSerializer.LoadScene`. The TODOs are still at `Engine/Runtime/SceneManager.cs:104-121`.
- Carry manager lifecycle into the multi-scene model: active manager init/update, shutdown-all semantics, and a decision for `IsPersistent`.
- Acceptance: two named scenes can be active with deterministic update/render order, and scenes can be activated, deactivated, and unloaded through the host.

### [ ] T3 — Phase 5: new editor host foundation

**Criterion:** A6; absorbs former P2-6 and P3-5. **Status:** not started.

- Build the .NET console host, Photino webview, React/Vite app, native MonoGame viewport, JS↔.NET command/event bridge, and session model described in [08-react-editor-boundary.md](08-react-editor-boundary.md).
- Implement command-level transactions and dirty events from former P2-6. Do not reproduce the WinForms direct `_needsToBeSaved = false` write.
- Validate whether `MockGame` can be replaced by a native `Game`-derived viewport host; do not delete or wire behavior into the sentinel until validated.
- Decide former P3-5: retire non-DesktopGL stubs from the main solution surface or leave them explicitly template-level and excluded from Alpha acceptance/CI.
- Acceptance: the backend contract is callable from the React host without reaching into WinForms or engine statics, and mutations flow through commands that emit dirty state once.

### [ ] T4 — Phase 4: core 2D subsystems

**Criterion:** supports A1/A2 and Phase 6 tools. **Status:** not tracked as a completed phase.

- Implement or explicitly scope the runtime subsystems named by the roadmap Phase 4: audio, animation, particles, prefabs, and runtime camera.
- Treat prefab support as real work: the WinForms menu items are stubs (`Editor/Form1.SceneHierarchy.cs:37-47`).
- Preserve the existing UI rule from [12-future-roadmap.md §3.2](12-future-roadmap.md): UI stays in the engine's scene world.
- Decide the deferred collision-callback signature from former P1-10 before Alpha; implementation detail is in [13-refactor-todo.md §2A](13-refactor-todo.md).
- Acceptance: each subsystem has a usable runtime path and a source-derived doc entry before the corresponding editor tool is built.

### [ ] T5 — Phase 6: editor parity and tools

**Criterion:** A9; absorbs former P2-7. **Status:** not started.

- Build hierarchy/inspector/undo-redo, viewport selection and gizmos, `UsedInEditor` play-mode gating, and entity inspection in the new host.
- Port the semantics of `GameEvent` wiring, not the WinForms `PropertyGrid` private reflection. Former P2-7 becomes a backend property-schema service.
- Add input action mapping, tilemap parity and auto-tiling, sprite sheet slicing, basic animation and particle tools, assets browser, CSV↔database round-trip, mod folder convention, and UI usability/scene-data binding.
- Acceptance: the React host reaches the WinForms capability list in [12-future-roadmap.md §2 A9](12-future-roadmap.md) without depending on WinForms internals.

### [ ] T6 — Phase 7: debug overlays

**Criterion:** supports A8/A9 acceptance. **Status:** not started.

- FPS and frame/system timings.
- Physics debug draw using the existing Aether diagnostics dependency.
- Entity/scene stats, especially per active scene.
- Runtime log console.
- Grid/tile info: cursor tile coordinates, active map, layer order.
- Camera bounds and quality-tier readout.
- Acceptance: all six overlays are available in the new host and are documented in [12-future-roadmap.md §4 Phase 7](12-future-roadmap.md).

### [ ] T7 — Phase 8: content and alpha gate

**Criterion:** A1, A2, A5, A8; absorbs former P3-6. **Status:** partial — build/CI landed, acceptance open.

- Validate A5 against the author's dev-branch content pipeline: adding an asset must require no manual `.mgcb` bookkeeping.
- Carry former P3-6 here as validation, not blind repair. Validate stale `BuildContent.targets` tokens, `ContentOutPut`, empty `.mgcb` manifests, and path mismatches against the authoritative workflow; prefer deleting dead config.
- Apply the former P3-5 decision from T3: remove non-DesktopGL stubs from the Alpha solution surface or leave them explicitly template-level and excluded from Alpha acceptance/CI.
- Extend CI only when a new editor smoke path exists; current CI covers four maintained targets and 71 facts.
- Complete the end-to-end run for A1/A2: create/open/edit/save a project, build DesktopGL, and launch outside the editor with scenes, databases, and content loaded.
- Complete the manual GUI checks that were left as rulings in the previous pass: WinForms `Fonts` load, DesktopGL window launch/visual confirmation, and live watcher smoke where applicable to the new host.
- Acceptance: every Alpha exit criterion in [12-future-roadmap.md §2](12-future-roadmap.md) has evidence, and source/docs are synchronized.

## Deferred from this TODO

| Item | Reason |
|---|---|
| Mobile/iOS/Android host completion | Alpha is DesktopGL only; stubs handled by T3 decision and T7 cleanup. |
| Web/Multiplatform internals | Isolated KNI experiments with no `Engine.Core` reference. |
| GISM | Abandoned experiment; do not touch. |
| `MockGame` behavior | Sentinel for the external dev-branch builder; T3 only validates replacement. |
| Serialization format rewrite / migration implementation | T1 records the decision; implementation only if Alpha acceptance requires it. |
| Performance refactors and query planner | Tracked in [13-refactor-todo.md](13-refactor-todo.md); measure first. |
| 3D, networking, Lua, Vulkan/other backends, localization-as-a-new-feature | Explicitly out of scope in [12-future-roadmap.md](12-future-roadmap.md). |

## Suggested order

1. **T1 Phase 2** — unlocks scene lists, startup scene, content roots, and host config.
2. **T2 Phase 3** — turns the scene runtime into the host-owned additive model.
3. **T3 Phase 5** — creates the editor backend contract that absorbs command/dirty work.
4. **T4 Phase 4** — can overlap after T1, before the matching tools in T5.
5. **T5 Phase 6** — editor parity and tools.
6. **T6 Phase 7** — debug overlays.
7. **T7 Phase 8** — content and alpha acceptance gate.

## See also

- [12-future-roadmap.md](12-future-roadmap.md) — phases, scope decisions, and exit criteria
- [13-refactor-todo.md](13-refactor-todo.md) — non-scheduled refactor candidates
- [11-health-todo.md](11-health-todo.md) — archive index for the closed hardening pass
- [archive/11-health-todo.md](archive/11-health-todo.md) — historical checklist and verification asset inventory
