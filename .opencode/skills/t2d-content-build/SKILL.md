---
name: Tesseract2D content and build
description: Use when building projects, adding content/assets, touching MonoGame .mgcb, MockGame, or platform targets in Tesseract2D
---

# Tesseract2D content and build

Read `docs/09-build-and-content.md` before changing anything here.

## Build environment

- .NET 8 SDK for the main projects; the editor is `net8.0-windows` with WinForms and `MonoGame.Forms.DX`.
- iOS/Android targets need .NET 9 workloads. Do not run `dotnet build Tesseract2D.sln` and conclude the engine is broken — build individual projects.
- Web and Multiplatform are isolated KNI/nkast experiments with no `Engine.Core` reference. They are not editor backends.

## The dev-branch pipeline is authoritative

The author's MonoGame dev-branch content workflow auto-adds new project files to `.mgcb`, creates build files, and hot-reloads assets, so editor play tests need no project rebuild. Checked-in content config is stale relative to that workflow. Do not "repair" it or add manual `.mgcb` bookkeeping unless the user explicitly asks and the dev-branch behaviour has been validated. See `docs/09-build-and-content.md` §4.4.

## `MockGame` is a sentinel

`MockGame` exists only to satisfy the external experimental content builder. The editor's `MonoGame.Forms` instance was tried as that builder's `Game` reference and rejected. A future native `Game`-derived viewport host may work but must be validated. Never delete `MockGame` or wire gameplay behaviour into it.

## Known path traps

Content is built by `Engine.Content` from `Editor/Form1.cs` at editor startup. Paths in that area are inconsistent, so asset-loading bugs are often path bugs. Check the resolved path before changing asset code.

## Scope

DesktopGL is the only Alpha target. WinForms is frozen after stabilization. Mobile, Web, and Vulkan are not Alpha work.
