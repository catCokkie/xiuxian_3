# Xiuxian 2

Xiuxian 2 is a Godot 4 + C# prototype for an input-driven desktop cultivation game. This repository is organized to keep runtime scenes, pure gameplay rules, UI controllers, tests, and design documentation clearly separated for public sharing and future refactors.

## Current Playable Loop

The current prototype already supports a minimal but real gameplay loop:

- keyboard and mouse activity drive exploration progress
- monsters advance toward the player and trigger combat on proximity
- battle rounds resolve from accumulated input counts
- combat grants resources and recent battle logs
- first clears can grant fixed equipment rewards
- new equipment goes into the backpack first and is only equipped manually
- equipped gear changes player combat stats and persists in save data

The current UI surface also includes:

- a bottom main bar for lightweight always-on progress visibility
- a book-style submenu with `Cultivation`, `Battle Log`, `Equipment`, `Stats`, `Bug Feedback`, and `Settings`
- a config validation view with battle-drop simulation filters and quick-run actions
- manual breakthrough when realm progress is full
- a submenu config validation view with scope and active-level filters
- persisted recent battle logs and basic local feedback export tools

## Repository Structure

```text
.
|-- .github/workflows/        GitHub Actions workflows
|-- assets/                   Art assets and import metadata
|-- docs/                     Technical notes and repository guides
|-- docs/design/              Product and system design documents
|-- scenes/                   Godot scenes
|-- scripts/game/             Scene orchestration and root controllers
|-- scripts/services/         Gameplay rules, state, loaders, and persistence helpers
|-- scripts/tests/            In-engine debug or test scripts
|-- scripts/ui/               UI controllers and shared UI text
|-- tests/Xiuxian2.Tests/     xUnit regression suite
|-- project.godot             Godot project entry
|-- xiuxian2.csproj           Main Godot .NET project
|-- xiuxian2.sln              Solution entry
|-- README.md                 Public repository overview
`-- TESTING.md                Regression coverage notes
```

For a fuller directory guide and republish checklist, see `docs/README.md`.

## Requirements

- Godot 4.5.1 with .NET support
- .NET 8 SDK

The default runtime entry scene is `scenes/PrototypeRoot.tscn`.

## Regression Tests

Run the current automated regression suite from the project root:

```bash
./run-regression-tests.sh
```

Or run the test project directly:

```bash
dotnet test tests/Xiuxian2.Tests/Xiuxian2.Tests.csproj
```

GitHub Actions runs the same regression suite automatically on pushes to `main` and on pull requests.

This currently covers the refactor-stable core rules around:

- input-driven exploration progress
- 100% level completion switching semantics
- battle start, round, lifecycle, and reward formalization
- pity, soft-cap, and daily-cap behavior
- player, monster, and equipment stat pipeline
- starter equipment and manual equip loop

For a fuller coverage summary and current testing gaps, see `TESTING.md`.

## Documentation

- repository and directory guide: `docs/README.md`
- design hub: `docs/design/README.md`
- input system notes: `docs/INPUT_SYSTEM.md`

## Still Prototype-Only

- progression is real and persistent, but still prototype-oriented
- equipment acquisition is fixed-rule and debug-oriented, not full content-driven loot yet
- equipment UI is minimal and focused on verification rather than final UX
- some settings are intentionally marked as reserved or experimental rather than fully wired product features
- cloud sync, online features, and richer pet systems remain out of current product scope
