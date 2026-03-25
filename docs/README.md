# Repository Guide

This repository is already close to a clean public GitHub layout. For a Godot project, the safest way to reorganize is to improve discoverability without casually moving scene, script, or asset paths that may be referenced by the engine.

## Directory Overview

```text
.
|-- .github/workflows/        Continuous integration workflows
|-- assets/                   Art assets and import metadata
|-- docs/                     Repository notes and technical documents
|   |-- design/               Product and system design documents
|   |-- INPUT_SYSTEM.md
|   |-- INPUT_SYSTEM_SUMMARY.md
|   `-- README.md
|-- scenes/                   Godot scenes
|   |-- tests/
|   `-- ui/
|-- scripts/                  Godot C# scripts
|   |-- game/                 Scene-level orchestration and root controllers
|   |-- services/             Rules, state, loading, persistence, and settlement logic
|   |-- tests/                In-engine debug or test scripts
|   `-- ui/                   UI controllers and shared UI text
|-- tests/Xiuxian2.Tests/     xUnit regression test project
|-- project.godot             Godot project entry
|-- xiuxian2.csproj           Main project file
|-- xiuxian2.sln              Solution file
|-- README.md                 Public repository overview
`-- TESTING.md                Regression coverage notes
```

## Recommended Responsibilities

- `assets/` should contain committed game assets and their import metadata, not exports or temporary captures.
- `scenes/` should stay organized by runtime usage. Keeping `ui/` and `tests/` separate is a good pattern.
- `scripts/game/` is the right place for scene-driven orchestration and root controllers.
- `scripts/services/` now acts as the core logic layer and is the most valuable area to keep stable and testable.
- `scripts/ui/` being separate from `scripts/game/` keeps presentation work from mixing with gameplay rule code.
- `tests/Xiuxian2.Tests/` as a standalone .NET test project is a good fit for GitHub Actions and future automation.

## Current Cleanup Principles

- Do not move existing files under `scenes/`, `scripts/`, or `assets/` unless the Godot references are updated deliberately.
- Keep the repository guide, testing guide, and design entry points aligned with the real directory layout.
- Ignore generated local content such as `.godot/`, `bin/`, `obj/`, and `.vs/` before republishing.
- Run CI from the repository root so the workflow does not depend on an old folder name.

## Minimal GitHub Republish Checklist

- Confirm `README.md`, `docs/README.md`, and `docs/design/README.md` match the actual directory layout.
- Run `dotnet test tests/Xiuxian2.Tests/Xiuxian2.Tests.csproj`.
- Do not commit `.godot/`, `bin/`, `obj/`, `.vs/`, or `.idea/`.
- Decide whether to add a formal `LICENSE` file before making the repository public.
- Use `main` as the default branch name for the new repository.
