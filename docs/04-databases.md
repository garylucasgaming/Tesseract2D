# 04 — Databases & DataComponents

The "database" system is Tesseract2D's answer to Unity's ScriptableObjects: reusable, named, YAML-backed tables of pure data that designers author in a spreadsheet-like editor and game code reads at runtime. It lives in `Engine/Collections/`, not in a generic utilities folder.

**Status:** Partial — authoring, serialization, and runtime loading work; generic component→database references do **not** round-trip reliably, version validation exists but there is no migration hook, and no author-authored `.database` source asset is checked in (a sanitized populated fixture is).

### Why this system exists (author's stated intent)

According to the project author, databases were designed for two purposes:

1. **Localization** — store dialogue and displayed names as database rows, then swap the active database per language. This is the original motivation, and it is why localization needs no separate subsystem in [12-future-roadmap.md](12-future-roadmap.md).
2. **Mass-producing data-driven objects** — a `SwordComponent` holds a `SwordDataComponent` reference by ID or name, so 100 swords are 100 table rows rather than 100 individually authored assets. This is the intended analogue of Unity ScriptableObjects.

A real example of the second pattern is checked into the author's `Fonts` test project: `D:\GameDevelopment\TestFolderEngine\TestProject\Fonts\Content\Assets\Databases\tiledb.database` has four `TileDataComponent` rows referenced by `AssetID`. See [archived verification assets](archive/11-health-todo.md).

## 1. Mental model

```text
Subclass DataComponent              (the row type, written in C#)
        ↓
Create Database asset in DatabaseViewer, choose that type
        ↓
Add rows (each is a DataComponent instance with a DisplayName + AssetID)
        ↓
Saved as <Name>.database YAML under Content/Assets/Databases/
        ↓
GameScene loads all *.database files into its DatabaseManager
        ↓
Components / maps look up rows by AssetID or DisplayName
```

`Editor/Templates/.cs/DataComponent.tmpl:17-31` states the intent: data components are for databases, a database is created "based on that type", it is serialized, loaded at runtime, and "acts as a single source of truth for all data."

## 2. Core types

| Type | File | Role |
|---|---|---|
| `Database` | `Engine/Collections/Database.cs:10-16` | Non-generic named container of homogeneous rows |
| `DatabaseIgnoreAttribute` | `Engine/Collections/DatabaseIgnoreAttribute.cs:9-15` | Hides a property from `DatabaseViewer` grid only |
| `DataComponent` | `Engine/ECS/Components/DataComponent.cs:12-53` | Abstract base for data rows; derives `GameComponent`, `ICloneable` |
| `DatabaseManager` | `Engine/Runtime/DatabaseManager.cs:12-71` | Per-scene list of loaded databases + lookup helpers |
| `DatabaseSerializer` | `Engine/Serialization/DatabaseSerializer.cs:14-157` | YAML DTO, save/load |
| `DatabaseViewer` / `DatabaseDialog` | `Editor/DatabaseViewer.cs`, `Editor/DatabaseDialog.cs` | Editor authoring UI |

### `Database`

```csharp
// Engine/Collections/Database.cs:10-16
public class Database
{
    public string Name = string.Empty;
    public Guid ID = Guid.NewGuid();
    public string DatabaseType = string.Empty;
    public Dictionary<Guid, DataComponent> ComponentDatabase = new();
}
```

- `Name` — asset name; controls the file name (`<Name>.database`) and dropdown label.
- `ID` — database file identity; `DatabaseManager` deduplicates on load by this (`DatabaseManager.cs:33-39`).
- `DatabaseType` — short name of the `DataComponent` subtype stored here (`Editor/DatabaseViewer.cs:373`).
- `ComponentDatabase` — rows keyed by `DataComponent.AssetID`.

Lookups: `GetComponent(Guid)` (`Database.cs:18-30`) and `GetComponent(string displayName)` (`:32-42`). The string lookup is **case-sensitive**, unlike most editor converters.

There is **no** generic `Database<T>` and game developers normally do not subclass `Database` — they subclass `DataComponent`.

### `DataComponent`

```csharp
// Engine/ECS/Components/DataComponent.cs:12-53 (abridged)
public abstract class DataComponent : GameComponent, ICloneable
{
    public abstract string DisplayName { get; set; }

    [Browsable(false)]
    public Guid AssetID { get; set; } = Guid.NewGuid();

    [DatabaseIgnore]
    public Database DatabaseReference { get; set; }

    [DatabaseIgnore]
    public DataComponent? DataReference { get; set; }

    public virtual object Clone() => MemberwiseClone();  // shallow
}
```

Every row must define `DisplayName`. `AssetID` is the stable identity used for references. `Clone()` is `MemberwiseClone()` (`:50-53`) — shallow.

### `DatabaseIgnoreAttribute`

Property-only, inherited, non-repeatable (`DatabaseIgnoreAttribute.cs:9-15`). Enforced only in the editor grid:

```csharp
// Editor/DatabaseViewer.cs:232-241
if(Attribute.IsDefined(prop, typeof(DatabaseIgnoreAttribute)))
{
    col.Visible = false;
    continue;
}
```

It does **not** affect serialization — `ComponentSerializer.ShouldIgnore` checks only for an attribute whose type name is exactly `"IgnoreAttribute"` (`ComponentSerializer.cs:17-27`). Used on `DataComponent.DatabaseReference`/`DataReference` and on several `Map` members.

## 3. Runtime loading

`GameScene.InitializeManagers()` creates a `DatabaseManager` and immediately loads from the editor project path:

```csharp
// Engine/ECS/GameScene.cs:74-83
Database = database ?? new DatabaseManager();
_databaseExternallyOwned = database != null;
```

```csharp
// Engine/ECS/GameScene.cs:105-123
if(!_databasesLoaded && !_databaseExternallyOwned)
{
    Database.LoadAllDatabasesFromFolder(Context.DatabasePath);
    _databasesLoaded = true;
}
```

`DatabasePath => <project>/Content/Assets/Databases` on `IEngineContext` (`Engine/Runtime/EngineContext.cs:34`, `:74`). `DatabaseManager.LoadAllDatabasesFromFolder` uses `Directory.GetFiles(folder, "*.database")` (`Engine/Runtime/DatabaseManager.cs:48-58`). An injected `DatabaseManager` is not reloaded from disk, and `Map.ResolveDatabase` reads only `GameScene.Database`.

```mermaid
flowchart LR
    Files["Content/Assets/Databases/*.database"] --> DM["DatabaseManager.LoadAllDatabasesFromFolder"]
    DM --> DS["DatabaseSerializer.LoadDatabase"]
    DS --> TR["TypeResolver.FindType(typeName, DataComponent)"]
    TR --> Rows["Dictionary&lt;Guid, DataComponent&gt;"]
    Rows --> API["GetDatabaseByName / GetDatabaseByTypeName"]
    API --> Game["Components, systems, maps"]
```

Lookup from game code:

```csharp
Database? weapons = scene.Database.GetDatabaseByName("Weapons");
DataComponent? sword = weapons?.GetComponent(assetId);
// or by exact DisplayName:
DataComponent? named = weapons?.GetComponent("Iron Sword");
```

Because `GameScene` loads from `EditorContextManager.DatabasePath`, a standalone runtime with no editor project mounted gets an empty path and **no databases load**. This is one of the concrete editor-coupling problems described in [01-architecture.md](01-architecture.md).

## 4. Authoring in the editor

Entry point: `Editor/Form1.cs:2380-2385` reloads gameplay assemblies and opens `DatabaseViewer`.

| Action | Source |
|---|---|
| Load existing databases into combo | `DatabaseViewer.cs:85-99` |
| Create new database (choose filename + `DataComponent` subtype) | `DatabaseViewer.cs:329-388` |
| New-database dialog | `Editor/DatabaseDialog.cs:10-58` |
| Add/delete rows | `DatabaseViewer.cs:273-315`, `:56-83` |
| Configure grid columns, enum combos, hide `[DatabaseIgnore]` | `DatabaseViewer.cs:218-271` |
| Edit a row via component card | `DatabaseViewer.cs:163-195` |
| Save | `DatabaseViewer.cs:318-327` (writes `<Name>.database`; no Save As) |

New databases are discovered by scanning assemblies for concrete `DataComponent` subclasses:

```csharp
// Editor/DatabaseViewer.cs:334-358 (abridged)
var found = assembly.GetTypes()
    .Where(t => t.IsClass && !t.IsAbstract && typeof(DataComponent).IsAssignableFrom(t));
```

The new asset is constructed at `DatabaseViewer.cs:370-375` and saved to `EditorContextManager.DatabasePath` at `:377-378`.

## 5. YAML format

DTO: `Engine/Serialization/DatabaseSerializer.cs:14-32`:

```csharp
public class DatabaseDataDTO
{
    public int Version { get; set; } = SerializationVersion.Current;

    public string ID
    {
        get; set;
    }
    public string DataType
    {
        get; set;
    }
    public string Name
    {
        get; set;
    }
    // Key: Human-readable Name String, Value: Serialized component field/property map
    public Dictionary<string, Dictionary<string, object>> DataEntries { get; set; } = new Dictionary<string, Dictionary<string, object>>();
}
```

Serialization uses YamlDotNet `NullNamingConvention` (`:34-41`), so keys are written exactly as declared.

A minimal database (this shape exists in the repo as **build output** at `Editor/bin/Debug/net8.0-windows/TileDatabase.yaml:1-3`, minus `Name`):

```yaml
ID: 3cdab1e7-a22c-4989-8892-92b58bc57834
DataType: TileDataComponent
DataEntries: {}
```

A populated example derived from the serializer is checked in as a sanitized fixture at `Engine.Tests/TestContent/Fonts/Content/Assets/Databases/tiledb.database:1-28` (four `TileDataComponent` rows with stable `AssetID`s); the sample values below remain schematic, and the author's original is at `D:\GameDevelopment\TestFolderEngine\TestProject\Fonts\Content\Assets\Databases\tiledb.database`.

```yaml
ID: 4bd7a71e-4b0a-4da7-9e67-d81f8db7c4a9
DataType: WeaponData
Name: Weapons
DataEntries:
  IronSword:
    AssetID: 18c2e5a8-98ec-4a43-8b95-4a6af82f76d1
    DisplayName: Iron Sword
    Damage: 12
    Reach: 1.5
    Material: Steel
  SteelSword:
    AssetID: bbd4c3d0-cf13-4e9d-a2e4-c3f4c1b5e0a2
    DisplayName: Steel Sword
    Damage: 20
    Reach: 1.75
    Material: Steel
```

- Save (`DatabaseSerializer.cs:45-85`) exports each row via `ComponentSerializer.ExportComponent`, injects `AssetID`, strips spaces from `DisplayName` to form the YAML key (`:64-67`), and adds `_1`, `_2`, … suffixes on key collisions (`:69-78`). The DTO now includes `Version`.
- Load (`DatabaseSerializer.cs:87-174`) validates `Version` (`:98`), resolves the row type with `TypeResolver.FindType(typeName, typeof(DataComponent))` (`:121-134`), recovers a missing `AssetID` with a new GUID by default or throws in strict mode (`:142-162`), and imports each entry's component state (`:164-169`).
- An unresolved `DataType` causes the **whole database** to be dropped (`DatabaseSerializer.cs:125-134`). A malformed `ID` is recovered with a generated GUID and a warning (`DatabaseSerializer.cs:100-110`).
- Supported field value types are those of `ComponentSerializer.ExportValue` (`ComponentSerializer.cs:72-139`): primitives, enums, `Vector2`, specific `$ref` kinds, and lists. Other nested objects are silently omitted.

## 6. Referencing database entries

### Editor pickers

| Converter | Purpose |
|---|---|
| `DatabaseReferenceConverter` | Dropdown of databases, filtered by `Database.DatabaseType` or row assignability (`Editor/PropertyWrappers/DatabaseReferenceConverter.cs:14-108`) |
| `DatabaseReferenceDropdownConverter` | Entry dropdown filtered by the selected database and target type (`DatabaseReferenceDropdownConverter.cs:14-97`) |
| `DataComponentReferenceConverter` | All assignable rows across every loaded database (`DataComponentReferenceConverter.cs:13-90`) |

`FilteredPropertyWrapper.cs:93-130` selects these by property name/type; changing `DatabaseReference` clears `DataReference` (`:174-179`).

### Serialization reality

`ComponentSerializer` has **no database-aware `$ref` format**. It supports GameObject, GameComponent, GameEvent, primitives/enums/Vector2, and lists (`ComponentSerializer.cs:72-139`). Consequences:

- A `DatabaseReference` property (type `Database`) is **omitted** from exported YAML because `ExportValue` returns null for it.
- A `DataReference` property (type `DataComponent`) is exported as a **GameComponent** `$ref` with an empty owner ID, and load-time resolution only searches GameObject IDs (`ComponentSerializer.cs:255-272`) — it never consults `DatabaseManager`.
- Therefore editor object references are **not reliably reconstructed** from scene YAML. Prefer storing explicit `string` names/IDs and resolving them yourself.

### Tilemaps: the string-based workaround

`Map` uses explicit string binding instead of object references:

- `TileDatabaseName` (`Engine/Gameplay/Map.cs:96-104`), `TileDatabase` marked `[Browsable(false)] [DatabaseIgnore]` (`:120-126`), `TileIndexDataDictionary` (`:183-189`).
- `Map.ResolveDatabase()` (`:328-344`) strips path/extension and finds a database by `Name` (case-insensitive), then calls `RebuildTileDataGrid()`.
- `TileMapSerializer.SaveMap` now emits `TileIndexDataDictionary` with each row's `AssetID` (`Engine/Serialization/TileMapSerializer.cs:57-86`), and `LoadMap` rebinds tile data by `AssetID` or `DisplayName` (`:140-180`). Per-tile bindings therefore round-trip; see [03-projects-and-serialization.md](03-projects-and-serialization.md) §Map and database persistence.

## 7. Comparison with Unity ScriptableObjects

| Unity ScriptableObject | Tesseract2D Database |
|---|---|
| One asset object per `.asset` file | One YAML file containing many homogeneous rows |
| Asset is directly an instance of the user class | `Database` container with `DatabaseType` string |
| Strong serialized asset GUID references | `GetComponent(Guid)` + fragile editor object refs |
| Unity serializer handles nested serializable structs | Primitives, enums, `Vector2`, lists, hardcoded `$ref` kinds only |
| AssetDatabase / Resources / Addressables | `Directory.GetFiles(folder, "*.database")` |
| Lifecycle callbacks (`OnEnable`, etc.) | None found |
| Typed inspector references | Three string/display-name `TypeConverter`s |
| Versioned YAML / migration | `Version` field + future-version rejection / stale warning; no migration hook ([03-projects-and-serialization.md](03-projects-and-serialization.md) §YAML conventions and schemas) |
| Editor asset pipeline integration | `.database` is **not** copied by `DynamicBuilder.cs:43-55` |
| Global asset database | Per-scene `DatabaseManager` list |

In practice: a lightweight YAML-backed homogeneous data table, not a one-file-per-instance asset system.

## 8. Authoring a data type (reference)

```csharp
using Engine.Core.ECS.Components;

public sealed class WeaponData : DataComponent
{
    public override string DisplayName { get; set; } = "";

    private int _damage;
    public int Damage
    {
        get => _damage;
        set => _damage = value;
    }

    private float _reach;
    public float Reach
    {
        get => _reach;
        set => _reach = value;
    }
}
```

Only the **public properties** are serialized. The template `DataComponent.tmpl:36-38` claims private backing fields are serialized; that is incorrect for the current `ComponentSerializer` (`BindingFlags.Public | BindingFlags.Instance`, `ComponentSerializer.cs:15`). See [06-authoring-guide.md](06-authoring-guide.md) for the other authoring types.

## Gotchas / Known Limitations

- No author-authored `.database` source asset is checked in; the portable `Engine.Tests/TestContent/Fonts/Content/Assets/Databases/tiledb.database` is a sanitized fixture, not the author's original project asset.
- `.database` files are not copied by the content builder (`Engine.Content/Builder/DynamicBuilder.cs:43-55`).
- `Database.ID` and `DataComponent.AssetID` are distinct identities.
- `DatabaseType` is stored as a short type name (`DatabaseViewer.cs:373`); the combo is populated with database names but the new-database flow selects `DatabaseType` (`DatabaseViewer.cs:92-95`, `:381`).
- YAML row keys strip spaces, but `DisplayName` does not; collision suffixes do not make duplicate display names unique after load (`DatabaseSerializer.cs:62-74`).
- Malformed `ID` values are recovered with a generated GUID and a warning (`DatabaseSerializer.cs:100-110`); unresolved types drop the entire database (`DatabaseSerializer.cs:125-134`).
- `TypeResolver`'s static cache is cleared by the editor after gameplay assembly load, reload, and unload (`Engine/Utilities/TypeResolver.cs:79-85`, `Editor/Utilities/ScriptAssenblyManager.cs:43`, `:88`, `:97`).
- `DatabaseIgnore` is grid-only; `[Ignore]` is the serializer attribute.
- Generic `DatabaseReference`/`DataReference` do not round-trip through YAML (see §6).
- `Clone()` is shallow (`DataComponent.cs:50-53`).
- Runtime loading uses an injected `DatabaseManager` and the scene's `Context.DatabasePath`, not editor global state (`Engine/ECS/GameScene.cs:62-83`, `:105-123`).
- String lookup `GetComponent(string)` is case-sensitive (`Database.cs:32-42`).

## See also

- [02-ecs.md](02-ecs.md) — components and the scene that owns the `DatabaseManager`
- [03-projects-and-serialization.md](03-projects-and-serialization.md) — YAML mechanics shared with databases
- [06-authoring-guide.md](06-authoring-guide.md) — writing data components and other game types
- [10-known-issues.md](10-known-issues.md) — consolidated issue catalogue
