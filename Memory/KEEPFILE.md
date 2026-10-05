# Spellright keepfile

Read this file before working on the project. Add a concise entry whenever you make a meaningful change.

## Project state

- Unity project: Unity `6000.5.2f1`.
- Active development and Pages branch: `overworld-pages`.
- `main` was reverted to the pre-overworld/battle revamp state. Do not merge or force-push branches without checking with the user.
- GitHub Pages URL: `https://ejones81973.github.io/MagicGame/`.
- Pages currently uses the legacy branch-root source from `overworld-pages`, not the GitHub Actions artifact source. Root [`index.html`](../index.html) links to `docs/battle/` and `docs/overworld/`.
- The GitHub credential available to the workspace can push and run workflows, but could not change the repository Pages configuration through the GitHub API.

## Overworld prototype

- Scene: `Assets/Spellright/Scenes/OverworldTest.unity`.
- Contains the graybox test course for Line, Spread, Totem, Huddle, and a mixed-formation route.
- Characters: Brim (Fire), Brooke (Water), Blitz (Electric).
- Formation controls: `1` Line, `2` Spread, `3` Totem, `4` Huddle. See `Assets/Spellright/Scripts/OverworldTestScene.cs` HUD for the remaining controls.
- The formation system is split across party manager, formation controller, input reader, motors, elemental interaction, Totem magic, Huddle traversal, and puzzle scripts. Preserve that separation.
- Movement and interaction tuning lives in `Assets/Spellright/OverworldMovementTuning.asset`.

## Battle prototype

- Scene: `Assets/Spellright/Scenes/SpellrightCombat.unity`.
- The scene starts `PrototypeBootstrap`, which creates the battle presentation, combatants, UI, and enemies at runtime.
- Enemy health is set to 100 in `Assets/Spellright/CombatSettings.asset`.
- Element damage rules and status effects are implemented in the battle scripts. Water extinguishes Burn; Electric damage is increased against Wet targets.

## WebGL and Pages

- Build command in Unity: **Spellright → Build Pages WebGL (Battle + Overworld)**.
- Build script: `Assets/Editor/GitHubPagesBuild.cs`.
- Generated files are tracked under `docs/battle/` and `docs/overworld/`; commit them after each Pages rebuild.
- Runtime-generated geometry must use `RuntimeMaterials` and the assets in `Assets/Spellright/Resources/SpellrightMaterials/`. Do not add new `Shader.Find` calls for runtime visuals: WebGL strips shaders that are only found by name.
- `Assets/Spellright/link.xml` preserves primitive renderer and collider classes that WebGL otherwise strips.
- Browser verification completed on 2026-10-05 after commit `66052d8`: Battle loaded the full party, HUD, spells, shields, status effects, and enemy turn; Overworld loaded the full course and all four formations. The Pages root launcher was added in `f497495`.

## Working tree note

At the last handoff, Unity had uncommitted settings changes under `Assets/Settings/` and `ProjectSettings/`, plus an untracked `Data/` folder. These were not created as part of the Pages fix. Inspect them before staging or discarding anything.

## Change log format

Add entries at the top of this section:

```md
## YYYY-MM-DD — Short title

- Changed: `path/to/file`.
- Why: concise reason.
- Verified: command, Unity build, or manual test.
- Remaining: limitation, or `None`.
```

## 2026-10-05 — Added project keepfile

- Changed: `AGENTS.md`, `Memory/KEEPFILE.md`.
- Why: preserve project context and require future sessions to read it before work.
- Verified: reviewed the current branch, WebGL deployment history, and tracked implementation state.
- Remaining: maintain this file as future work is completed.
