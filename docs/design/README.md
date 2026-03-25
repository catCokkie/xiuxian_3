# Xiuxian 2 - Design Hub

This directory stores the working design documents for the Godot 4 + C# desktop cultivation prototype. Most detailed design notes in this folder are currently maintained in Chinese.

## Document Index

- `00_vision.md`: product vision, scope boundaries, and target experience
- `01_core_loop.md`: the keyboard and mouse driven core loop
- `02_systems.md`: system breakdown, data fields, and technical constraints
- `03_progression_and_balance.md`: formulas, pacing, and balance guardrails
- `04_milestones.md`: milestones and acceptance targets
- `05_ui_style.md`: style guidance for the bottom bar and expanded panels
- `06_bottom_exploration_battle.md`: detailed design for the bottom exploration and battle UI
- `07_content_template.md`: reusable template for realms, levels, monsters, and drops
- `08_content_sample_qi_refining.md`: sample early-stage cultivation content
- `09_level_monster_drop_sample.md` and `.json`: example level, monster, and drop configuration
- `10_todo.md`: global task pool with priority, status, and acceptance notes
- `11_equipment_content_system.md`: formal equipment content system design
- `12_equipment_sample_qi_refining.json`: example early-stage equipment data
- `13_offline_settlement.md`: offline gain and settlement design

## Working Rules

- Update the design docs before updating the code when product behavior changes.
- Write formulas, caps, and tuning hooks explicitly whenever balance changes are involved.
- Define inputs, outputs, save fields, and UI entry points for each system.
- Input collection should only store counts and intensity, not raw key text or mouse trajectories.
- Prefer maintaining shared UI copy in `scripts/ui/UiText.cs` instead of scattering hard-coded strings.
- Keep Godot scene files `*.tscn` in UTF-8 without BOM to avoid parser and dependency loading errors.

## Regression Tests

- The repository-level test entry point is `./run-regression-tests.sh`.
- The automated regression project lives in `tests/Xiuxian2.Tests/`.
