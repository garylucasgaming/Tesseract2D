---
name: Tesseract2D serialization verification
description: Use when changing YAML serialization, DTOs, scenes, databases, or project manifests — verify against the three saved test projects
---

# Tesseract2D serialization verification

Read `docs/03-projects-and-serialization.md`, `docs/04-databases.md`, and `Engine/Serialization/` before editing serializers or DTOs.

## Format rules

- YamlDotNet with `NullNamingConvention`: DTO member names are written verbatim.
- `[Ignore]` is detected by attribute *name string*, not by type. `[Browsable(false)]` and `[DatabaseIgnore]` affect editor UI only.
- Only public instance fields and read-write properties serialize.
- Components need public parameterless constructors so reflection can create and deserialize them.
- Serialization stores `$ref` dictionaries for object references. DTOs should expose stable IDs plus display labels; never send raw engine objects across a boundary.

## Do not use `HierarchyTest.scene` as a reference

It is stale JSON, not current format. Use the DTOs in `Engine/Serialization/` as the format authority.

## The real integration fixtures

The author's saved projects under `D:\GameDevelopment\TestFolderEngine\TestProject\` are valid current-format projects:

| Project | What it exercises |
|---|---|
| `AnotherUITest` | Rich UI `$ref` usage |
| `Fonts` | The only populated database (`tiledb.database`, four `TileDataComponent` rows) and a real tile map |
| `UITest` | A short-name script reference |

All three have literal `{}` `ProjectManifest.db` files. New manifest code must load those defaults without requiring config. See the archived verification asset inventory in `docs/archive/11-health-todo.md`.

**Never modify the saved projects.** Copy them to a scratch location before testing.

## Verification workflow

1. Build the affected project individually.
2. Load a copy of each saved project in the editor and check the console log.
3. Round-trip save/reload and diff the YAML; unexpected formatting changes are a regression.
4. Confirm a `{}` manifest still loads.
5. If the change touches databases, confirm row references resolve by `AssetID` and `DisplayName`.
