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
- a book-style submenu with `Cultivation`, `Events`, `Equipment`, `Backpack`, `Shop`, `Stats`, `Config Validation`, `Bug Feedback`, and `Settings` tabs (progressive unlock, see `docs/design/02_systems.md` §21)
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

For a fuller directory guide and republish checklist, see the Directory Responsibilities section below.

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

To quickly verify the ore → shards → enhancement and suppression-talisman routes in an isolated Godot session on Windows:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify_supply.ps1 -GodotPath "path/to/Godot.NET_console.exe"
```

Add `-RenderArt` to render the 80px/24px character and inventory contact sheet to `docs/art/asset_contact_sheet.png`. The checks skip real player saves and global input hooks. Open `docs/art/asset_gallery.html` for a searchable offline asset preview; task status and validation records live only in `docs/design/10_todo.md`.

Quiet-time growth and the shared online/offline time budget have a separate quick check:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify_growth.ps1 -GodotPath "path/to/Godot.NET_console.exe" -WriteReport
```

`-WriteReport` regenerates `docs/balance/shared_growth_pace.json` from runtime formulas for 1/3/7-day scenarios. It is a budget model, not a full playthrough or player study.

## Documentation

- design hub & agent guide: `docs/design/AGENT_PROMPT.md`
- input system notes: `docs/INPUT_SYSTEM.md`
- design documents: `docs/design/00_vision.md` through `docs/design/14_save_migration.md`
- master task list: `docs/design/10_todo.md`
- operation depth blueprint: `docs/design/16_subsystem_deepening.md`, with authoritative rules in `02_systems.md` §24
- player routes and chapter pacing: `docs/design/22_play_routes.md`
- design-only route/action index: `docs/design/23_deepening_routes.json` (not loaded by the game)

Check route dependencies, unlock gates, recipe references, and mastery budget snapshots:

```bash
python tools/design/validate_deepening.py
```

## Directory Responsibilities

- `assets/` — committed game assets and their import metadata, not exports or temporary captures.
- `scenes/` — runtime scenes organized by usage. `ui/` and `tests/` subdirectories keep things separate.
- `scripts/game/` — scene-driven orchestration and root controllers.
- `scripts/services/` — core logic layer (pure rules, state, persistence). Most valuable area to keep stable and testable.
- `scripts/ui/` — UI controllers, separated from gameplay rule code.
- `tests/Xiuxian2.Tests/` — standalone .NET test project for xUnit regression suite.

## Republish Checklist

- Confirm `README.md` matches the actual directory layout.
- Run `dotnet test tests/Xiuxian2.Tests/Xiuxian2.Tests.csproj`.
- Do not commit `.godot/`, `bin/`, `obj/`, `.vs/`, or `.idea/`.
- Decide whether to add a formal `LICENSE` file before making the repository public.
- Use `main` as the default branch name for the new repository.

## Still Prototype-Only

- progression is real and persistent, but still prototype-oriented
- equipment acquisition is fixed-rule and debug-oriented, not full content-driven loot yet
- equipment UI is minimal and focused on verification rather than final UX
- some settings are intentionally marked as reserved or experimental rather than fully wired product features
- cloud sync, online features, and richer pet systems remain out of current product scope
