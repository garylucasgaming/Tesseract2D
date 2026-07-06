================================================================================
GISM — Gary's Integrated Special Markup
GISM is a lightweight, human-readable data-serialization markup language designed
specifically for game engine pipelines, visual inspector panels, and rapid design
workflows.

It addresses a fundamental challenge in game development: hand-written design
layout files need to be clean, fast, and minimal, while machine-generated save
states must be explicit, absolute, and uncompromised. GISM bridges this gap
effortlessly by relying on structural indentation for spatial/node hierarchies,
and isolated bracket scopes for data composition lists.

Key Features
Dual-Nature Profile Architecture: Supports minimal, layout-driven design
markup (relying on code defaults) and hyper-explicit state serialization
(tracking absolute types and UUIDs).

Whitespace-Driven Hierarchies: Clear, tab/space indentation definitions
naturally establish engine scene graphs or UI node parent-child relationships
without closing tags.

Context-Driven Parsing via Reflection: Bypasses bloated configuration layers
by mapping text type markers directly to native C# classes and enumerations
at runtime.

Engine-Agnostic Core: Compiled as an isolated, standalone class library
(.dll) with zero external framework dependency layers.

Syntax Guide

GISM uses a strict structural layout style to ensure readability for humans and
high performance for streaming text parsers.

Basic Nodes & Properties:
Nodes are instantiated using a leading dash (-) followed by an explicit type
marker enclosed in angle brackets (< >). Properties are mapped below their
parent using key-value expressions separated by an equals sign (=).

Spatial & Structural Hierarchies:
Indentation outside of collection contexts maps out physical hierarchies (like
a scene graph or a nested UI widget tree). Under-indented nodes are processed
as structural children.

- Main_Menu_Canvas 
    Width = 1920
    Height = 1080

    - Start_Button 
        Text = "Play Game"

Explicit Arrays and Dictionaries:
Collections are isolated inside bracket blocks ([ ] for Lists, { } for Key/Value
maps). While inside a collection scope, standard scene graph hierarchy parsing
sleeps, allowing objects inside lists to parse their fields contextually.

Reusable Class Library Usage

GISM is contained completely within a portable assembly namespace. To utilize
it within an engine project, reference the GaryMarkupEngine framework.

Saving States (Serialization):
To write out a live instance structure exactly as it exists in memory:

using GISM.Core;

var serializer = new GISMSerializer();
string gismOutput = serializer.Serialize(myWeaponsRegistry);

// Save with the dedicated .gism file extension
File.WriteAllText("Content/Data/weapons.gism", gismOutput);
Loading Configurations (Deserialization):
To parse raw GISM data strings back into strongly-typed C# object graphs:

using GISM.Core;

var parser = new GISMParser();
// Register your game project's assembly so GISM can locate your classes
parser.RegisterAssembly(typeof(GamePlayer).Assembly);

string rawGism = File.ReadAllText("Content/Data/weapons.gism");
WeaponsRegistry registry = parser.Parse<WeaponsRegistry>(rawGism);
Ignoring Execution Variables:
If a component has active runtime variables (such as a live graphics device
texture handle or a physics velocity vector) that should never be written down
to a save-state file, mark it with the custom [MarkupIgnore] attribute:

using GISM.Attributes;

public class SpriteComponent 
{
    public string TexturePath { get; set; } // Written to file

    [GISMIgnore]
    public Texture2D LoadedTexture { get; set; } // Safely skipped
}
IDE Development Integration

To integrate GISM cleanly into code environments like VS Code, include the
declarative language extension file inside the project repo under
extensions/gml-vscode/.

This maps files ending in .gism directly to the GISM parser grammar,
providing immediate color syntax highlighting for:

Comments
"Explicit Strings"

Numeric Values

In VS Code's settings.json, map the associations manually if needed:

"files.associations": {
    "*.gism": "gism"
}
================================================================================