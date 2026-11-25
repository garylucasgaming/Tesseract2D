# Fonts test fixture

Portable, current-format content for `Engine.Tests`. Copied from the author's
`D:\GameDevelopment\TestFolderEngine\TestProject\Fonts` project; that source
project must never be edited by agents.

## Checked-in files

- `Content/Assets/Scenes/Main.scene` — 7 entities, `$ref: Component` and
  `$ref: GameEvent` payloads, and the short-name `ListboxPopulater` script.
- `Content/Assets/Scenes/Main_TestMap.yaml` — 25×19 tile grid and a populated
  `TileIndexDataDictionary`.
- `Content/Assets/Databases/tiledb.database` — four populated
  `TileDataComponent` rows with stable `AssetID`s.
- `Content/Assets/Scripts/ListboxPopulater.cs` — compiled into the test assembly
  so short-name component resolution is exercised without a gameplay assembly.
- `Content/ProjectManifest.db` — sanitized, populated stand-in for the source
  project's literal `{}` manifest.

## Deliberately omitted

- `Content/Bin`, `.xnb`, textures, `.spritefont`, `.vs`, `obj`, and `bin`.
- `ProjectConfig.json` because it contains absolute developer paths.
- The original `{}` manifest; the fixture manifest uses relative asset paths.

All asset references in this fixture are relative. No graphics device, editor
path, or saved project is required to read these files.
