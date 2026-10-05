# Change Log

## 2026-10-05 — WebGL battle visuals

- Battle geometry now clones a serialized URP Lit material referenced by `CombatSettings`, ensuring the WebGL build includes the shader instead of relying only on runtime `Shader.Find`.
- Added a safe fallback so missing shader references log a clear error instead of aborting battle initialization before fighters and the HUD are created.

## 2026-10-04 — Overworld formation test course

- Replaced the single-purpose movement demo builder with a graybox course for Line, Spread, Totem, Huddle, and a mixed-formation finale.
- Added modular party, formation, character identity/motor, Input Actions reader, elemental interaction, synchronized magic, huddle traversal, puzzle element, and puzzle director scripts under `Assets/Spellright/Scripts/`.
- Added `Assets/Spellright/OverworldMovementTuning.asset` so movement, following, dash, glide, carry speed, and magic ranges can be tuned without editing scripts.
- Updated `Assets/InputSystem_Actions.inputactions` with formation, dash, glide, Totem order, leader cycling, and cursor actions for keyboard and controller.
- Bound F to the elemental bolt (with X and controller X alternatives) and G to interaction (with controller Y), preserving the earlier request that F fire the character's element.
- Wired `Assets/Spellright/Scenes/OverworldTest.unity` to the shared Input Actions asset and tuning asset.
- Added a Line leader elemental route, three-plate Spread room, sequential Spread elemental relay, Totem Steam/Storm/Plasma trials, Huddle carry/dash/glide course, and a mixed Spread → Steam → Storm → Huddle route.
- Adjusted the glide course climb and landing span for the configured slow-fall value; the course remains a graybox prototype with runtime-generated placeholder meshes.
- Final review found an initialization-time HUD null reference in the last available Unity editor log; `OverworldTestScene.OnGUI` now guards formation and leader references until initialization completes.
- The fix has not yet been recompiled in Unity: the project was already open, and Unity refused a second process for the same project. `git diff --check` and Input Actions JSON parsing succeeded.

## 2026-10-04 — Overworld input repair

- Rebuilt the Player action bindings from the repository's clean asset to remove malformed keyboard paths.
- Restored the Move keyboard composite to Unity's `2DVector`, added valid keyboard/gamepad bindings for the four formations, Dash, Glide, Totem order, and cursor toggle, and mapped Q/E and the shoulder buttons to leader or Spread selection. D-pad directions remain dedicated to formation switching.
- Bound F/X to elemental attack and G/Y to interaction.
- A fresh Unity batch verification was attempted after the editor closed, but Unity exited before compiling because its local Package Manager process could not connect over IPC; the corrected action asset passes structural validation, but still needs import/Play Mode confirmation in the editor.

## 2026-10-05 — Line follower recovery

- Kept Line followers' CharacterControllers enabled so their shared motors can apply following movement and gravity; only the controlled leader is driven by the party anchor.
- Preserved the original smooth, ordered leader-following behavior. Followers respawn behind the leader only after falling below the course; no distance or height catch-up snap is used.
- Set the fall recovery boundary in `OverworldMovementTuning.asset` to Y = -14, matching the earlier movement prototype.

## 2026-10-05 — Selection indicator

- Removed the selected-character scale-up and body glow. Selection is shown only by the existing sphere marker above the character.

## 2026-10-05 — Elemental station hitboxes

- Increased all graybox elemental station height and moved their centers to align with the elemental projectile flight path, making the red, blue, and yellow boxes easier to hit.

## 2026-10-05 — Totem controls and camera

- Entering Totem now places the current Line leader in the top slot. Q cycles the Top/Middle/Bottom order, and the new top character becomes the party leader; controller left trigger cycles the same order.
- Totem movement uses a reduced speed multiplier and ignores sprint and jump input.
- Camera follow distance expands in Totem and returns to the normal distance after leaving it.
- F/X continues to use the Attack action, which Totem resolves as the current Top + Middle combination spell.

## 2026-10-05 — Free Totem combination casts

- Totem Steam and Plasma can be cast anywhere, without a nearby compatible puzzle target. Each launches a visible forward projectile; matching magic targets still activate when hit.
- Totem Storm can be aimed and released anywhere. A nearby compatible generator powers on, while an empty aim location still receives a visible lightning strike.
- Added `synchronizedProjectileSpeed` to the overworld tuning asset so Steam and Plasma travel speed is adjustable.

## 2026-10-05 — Huddle formation spacing and turning

- Huddle now places the current leader at the front-center of a triangular party shape, with the other two characters behind on either side.
- The camera follows the centered leader while Huddled.
- Huddle turning now eases the party anchor and characters into their new facing direction using adjustable tuning values instead of snapping their angles.

## 2026-10-05 — Huddle pickup and dash controls

- Tightened the triangular Huddle spacing while keeping the current leader centered at the front.
- F/X now picks up or drops Heavy objects in Huddle; elsewhere it continues to fire the selected character's elemental attack or the Totem combination spell.
- Shift/B now performs the Huddle air dash. The dash holds the party's current vertical position until its duration ends, then normal gravity resumes.
- Moved keyboard sprint from Shift to Left Alt and updated the on-screen and README control hints.

## 2026-10-05 — Huddle stair access

- Moved the high Glide Launch platform beyond the staircase so it no longer overlaps the upper steps or blocks the climb from above.
- Rebuilt the climb with ten wider, lower-rise treads and kept a gap from the high platform for a short jump onto it.
- Moved the distant Glide Landing platform to preserve a clear glide span from the relocated launch platform.

## 2026-10-05 — Huddle swapping and dash duration

- Disabled Q/E character swapping in Huddle while retaining leader and Spread control switching in their respective formations.
- Reduced the Huddle air dash duration from 0.18 to 0.12 seconds; the configured dash distance is unchanged.
- Updated the on-screen control hint and README to describe formation-specific swapping.

## 2026-10-05 — Huddle dash distance

- Reduced Huddle air dash travel distance from 7 to 4.5 meters; the dash still pauses vertical movement for its configured 0.12-second duration.

## 2026-10-05 — Line jump positioning

- Line leader jumps with neutral movement now stop horizontal drift so the character jumps in place.

## 2026-10-05 — GitHub Pages playtest setup

- Added a responsive Pages landing screen with separate Battle and Overworld launch choices.
- Added a Unity editor build method that creates WebGL builds for `SpellrightCombat` and `OverworldTest` under `docs/battle` and `docs/overworld`.
- Added a GitHub Actions workflow to build both scenes and deploy the Pages artifact on pushes to `main` or manual dispatch.
- Documented the Pages source and Unity license secret setup in the root README. GitHub deployment still requires those repository settings/secrets and a successful workflow run.

## 2026-10-05 — Pages build licensing and cache

- Passed the optional `UNITY_SERIAL` secret into GameCI so Professional Unity licenses can authenticate as well as Personal license files.
- Added a keyed Unity `Library` cache to speed up subsequent WebGL workflow runs.
- Documented the exact repository secrets required for Personal and Pro/Plus licenses. These credentials must be added by a repository administrator; they are not stored in the project.

## 2026-10-05 — Stable Line leader switching

- Line leader changes keep the party formation anchor fixed. Characters reorder around that point, preventing repeated Q/E swaps from shifting the whole line backward.

## Earlier prototype changes retained

- Added element matchup damage modifiers and `weak` / `resist` feedback, including Wet amplification and Water extinguishing Burn.
- Set the enemy prototype health to 100 and fixed turn availability after a party member is revived.
- Updated battle targeting arrows and command name plates to use the selected character's element color.
- Recorded those retained combat and UI adjustments here for project history.
- Built the earlier movement-only overworld test scene and elemental wall/bolt prototype; this formation course replaces that scene's old one-controller implementation while retaining the isolated test-scene approach.

## 2026-10-05 — Local WebGL builds for Pages

- Removed Unity/GameCI from the GitHub Pages workflow. GitHub now only validates the prebuilt Battle and Overworld WebGL entry files, packages `docs`, and deploys the site; no Unity credentials are needed in repository secrets.
- Added a Unity menu command to build both Pages games locally into `docs/battle` and `docs/overworld`.
- Added Unity's stable folder metadata for the `Assets/Editor` build-tool folder.
- Updated the root README with local build and publish steps. The WebGL build folders must be committed so the Pages workflow can publish them.
