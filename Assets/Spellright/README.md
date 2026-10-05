# Spellright combat prototype

Open `Assets/Spellright/Scenes/SpellrightCombat.unity` in Unity 6000.5.2f1 and press Play. The `Spellright > Open Combat Prototype` menu opens the same scene. The arena, camera, capsules, projectiles and UI are generated on Play, so the scene is intentionally empty in edit mode. Use a 1280x800 or larger Game view for comfortable reading; the interface scales to smaller views.

## Controls and battle loop

- WASD: move the yellow menu arrow. Enter (including numpad Enter): select or confirm. Esc: go back without spending an action, MP, or an item. From the command menu, Esc opens acting-member selection. Horizontal lists also accept W/S.
- Melee and magic: choose the action/spell, then choose its target with WASD + Enter. Area spells mark all enemies and require confirmation. Shield executes on selection. Formation and synchronized spell choices also support WASD + Enter.
- Items: choose Potion or Ether first, then choose the recipient. Potions heal conscious heroes or revive a KO hero with the potion's HP amount; Ethers restore MP to a conscious hero. The initiating hero pays the action cost. Healing and Bubble Guard use the same ally-target step.
- Mouse: click commands as before. During target selection, click a capsule, its top card, or the recipient/target button to confirm. Turns follow the visible formation order, so the member selector cannot skip ahead. A yellow arrow above the capsule identifies the current actor or pending target.
- Space (or the Counter button): one counter attempt for the currently threatened hero. Focus the Game view first. Holding Space does not repeat; every attack needs a new press.
- Restart: immediately begin a fresh encounter, including during enemy animations.
- Flee: end the encounter; Restart plays again.

Each eligible conscious hero gets one action per Party Phase, in formation order. Line acts Front to Rear; Totem acts Top to Bottom (Bottom has no action); Huddle acts Front, Top Left, then Bottom Left; Spread acts Top to Bottom. The next member is selected automatically and cannot be skipped. If anyone is KO'd, the party is forced into Huddle and cannot change formation until all KO members are revived. In Totem, Top and Middle use ranged magic; all three adepts can use melee attacks that target Bottom, as well as elemental projectiles that target Top or Middle. Enemies take turns using melee on a three-round rotation and use their configured spell on ranged turns. Enemy support spells can protect or heal their allies. One counter press applies to eligible defenders; Totem Bottom takes a volley without countering. Volleys never cause Line knockback or extra collision damage. Party Shields expire after the Enemy Phase; unused Bubble absorption remains until it is consumed. KO heroes cannot act, but a Potion can revive them. Defeating a wave starts another wave after any available drain attempts.

Waves have 2–5 enemies drawn from the configured enemy prototypes, with counts weighted toward 2 or 3. Each enemy has its normal melee attack, its prototype's signature spell, and a second random spell matching its element. It uses tactical priorities for its spells (for example, it applies Burn before using Combust on a burning target), and one of its two spells is chosen at random as its absorption reward. Enemy spells can damage or affect the party and can heal, cleanse, buff, or protect enemy allies. Enemy revival spells are excluded. Wave count is displayed at the top of the UI. Every tenth wave includes a larger, tougher miniboss with a randomized type and spell loadout. Wave-size weights and miniboss interval, HP multiplier, and defense bonus are in the Combat Settings asset.

Melee costs no MP. Shield halves incoming damage for the next Enemy Phase and stacks with formation protection and counters. Potions restore 38 HP or revive a KO ally with 38 HP; Ethers restore 22 MP. Both spend the acting hero's action. Target selection is cancellable until you confirm with Enter or click the recipient. The run ends when the whole party is defeated; Restart begins a new run.

## Magical Drain

Downing an adept pauses combat and opens the ring minigame. Keep the blue pointer on the moving green section of the hollow ring to fill it from top to bottom. The pointer moves faster than the green section; press Space or click Reverse to change its direction. Fill grows while they overlap and falls while they do not. Fill the ring before time runs out to learn the adept's spell. A conscious matching-element hero is required: Brim absorbs Fire, Brooke Water, and Blitz Electric. If that named ability has already been learned, another successful drain raises its level and power. Learned spells appear in the matching hero's Magic menu and remain available when using Restart. If time runs out before the ring is full, or no matching hero is conscious, the enemy escapes and that encounter's ability is lost.

The default enemy prototypes are Cinder Adept (Fire / Combust), Tide Adept (Water / Bubble), and Arc Adept (Electric / Chain Lightning). Their configured signature spell is paired with a second random spell of the same element; either spell can be the absorption reward. Enemy support spells can protect, heal, cleanse, and buff their allies. Fire and Electric attacks can Burn or Shock party members; Water attacks can make them Wet. Tide Adept’s Bubble absorbs 5 damage by default. Every enemy keeps its rotating melee attack. Enemy elemental color marks projectiles. The ring controls are in the Game view; keyboard input requires the Game view to have focus.

Active statuses also appear directly on each living capsule: orange motes show Burn, cyan motes Wet, yellow sparks Shock, red markers Attack Down, purple markers Defense Down, and green motes regeneration or positive buffs. These indicators update from the combatant's status durations, and disappear when the effect expires, is cleansed, or the fighter is KO'd. Shield and Bubble retain their blue barrier visual.

## Formation experiments

Formation setup is simplified by shape: select Line, then select a Front member and confirm to apply it; the other two follow in party order. For Totem, select Top and then Bottom; choosing Bottom applies the formation, and the remaining member fills Middle. Huddle and Spread apply immediately and assign all slots automatically from party order. Formation changes spend the initiating hero's action and can happen once per Party Phase. A KO forces Huddle and resets party order; Potion revival unlocks formation changes again. Totem does not collapse before the forced Huddle.

| Formation | Experiment |
|---|---|
| Line | Normal melee and single-target projectiles hit only the first conscious hero in party order. A late/no counter pushes Front into Middle, then Middle into Rear; each collision deals reduced collateral damage and flashes the struck teammate red. Capsules return to their slots afterward. Perfect/Successful/Early counters do not cause this chain. Multi-member attacks such as Tide Adept's even-round volley still attack every living hero. |
| Totem | Top and Middle can act and use ranged magic; single-target ranged attacks hit them. Bottom cannot act or counter, is targeted by melee while alive, and takes 25% extra damage. Multi-member attacks hit everyone, including Bottom. |
| Huddle | The action sequence is Front, Top Left, then Bottom Left. Incoming damage is multiplied by 0.70 and outgoing by 0.80. Every enemy strike splashes the other living heroes for 60% base power. Only the primary defender counters the attack. |
| Spread | The action sequence is Top to Bottom. Normal damage. Tide Adept's even-round volley sends a projectile to each member, all arriving together. Press once at contact to counter the group hit. |

In Totem and Huddle, a status effect successfully inflicted on a party member spreads to all living party members. This includes Burn, Wet, Shock, and attack/defense reductions; each member retains their own Wet interaction when Shock duration is calculated. Line and Spread keep statuses on the character who was hit.

## Synchronized Magic

Set the party in Totem and choose the Top and Middle members. Their elements determine which three synchronized spells appear. Either Top or Middle can initiate the action; both must be conscious, unused this phase, and have enough MP for their share of the total cost. Bottom cannot initiate or join the cast. Synchronized spells consume both actions and use a shared blue link effect before resolving. The available names are Steam Cloud, Pressure Burst and Scald (Fire + Water); Conductive Wave, Storm Cloud and Defibrillate (Water + Electric); Plasma Bolt, Overheat and Flashfire (Fire + Electric).

## Counter practice

Each capsule has radius 0.5 units. Melee makes the attacking capsule pause, then hop into the defender, resolving contact when their sides touch (1 unit between centers). Projectiles have radius 0.3 and use a swept distance check. Both interpolate contact timestamps within the frame. Watch Cinder Adept's body hop or an elemental projectile reach the target; there is no timing meter.

- Perfect: press within the final **55 ms before contact**. Cyan flash, high tone, 10% damage.
- Successful: press **55â€“160 ms before contact**. Green flash, medium tone, 45% damage.
- Early: press more than **160 ms before contact**, such as during Cinder Adept's approach. Orange flash, low tone, 140% damage. A second press cannot fix an early attempt.
- Late: do nothing or press after contact. Red flash, lowest tone, normal damage; Line front also collides with rear members for 30% base power each.
- Totem Bottom: has no action; counter is unavailable; receives melee damage at the Bottom vulnerability multiplier.

These are deliberately pre-contact windows: input after an authoritative hit never retroactively cancels damage. One counter attempt covers one strike; splash collateral has no extra counter opportunity. Damage is reduced by Defense before formation, Shield, Bubble and counter multipliers, then rounded with a minimum of 1 HP. Lower projectile speed or widen the windows while learning. Timing is frame-sampled, so test at a stable frame rate.

## Tuning

Multi-member volleys use separate pace settings: `Volley Projectile Speed` (9 units/second), `Volley Windup Seconds` (0.12), and `Volley Recovery Seconds` (0.15). Projectiles launch together and adjust travel speed to arrive together. One counter attempt covers the wave; each hero takes damage once, with no Line collision damage.

Capsules bounce gently with staggered rhythms. Formation changes and KO-forced Huddle make them hop to their new slots. The targeted party member plants its feet as soon as an enemy attack starts and stays still through impact/recovery. Defeated party members and enemies stop bouncing and tip onto their sides. The visual child moves independently of the combat root so bracing preserves the existing counter contact point.

`Idle Bounce Height` and `Idle Bounce Frequency` in the Combat Settings asset adjust the motion live (frequency is bounces per second). `Formation Jump Seconds` and `Formation Jump Height` control formation hops. `Camera Move Seconds` and `Camera Focus Size` tune the attack camera. Set bounce height to zero to disable idle bouncing.

The Drain Minigame section of Combat Settings controls time available, the green arc width, pointer/arc speeds and how quickly fill grows or drains. Adjust `Drain Fill Per Second` and `Drain Loss Per Second` first when tuning difficulty.

Enemies recoil away from the attacker and lean backward on melee, magic, and synchronized damage, then ease back into position. Lethal hits recoil into the sideways defeat pose. `Enemy Recoil Distance` and `Enemy Recoil Seconds` control the displacement and recovery time; this is visual motion and does not move their combat positions.

Melee capsules approach, pause, then physically hop into the target and smoothly return to their formation slot. There is no spawned melee sphere or trail. Both party and enemy melee use this presentation; damage and enemy counter timing resolve when the capsules touch. `Party Melee Pause Seconds` controls the party's pause, `Melee Windup Seconds` controls the enemy's pause, `Melee Hop Height` controls the bounce, and `Melee Strike Speed` controls both sides' hops.

Select `Assets/Spellright/CombatSettings.asset` or use `Spellright > Select Combat Settings`. Inspector fields control party HP/MP/Attack/Defense (Vector4 X/Y/Z/W), enemy HP, attack damage, approach/windup/speeds, counter windows/multipliers, formation multipliers, shields, bubble, collateral, spell power, sync power, item quantities and restoration. Most multipliers update live; character base stats, enemy HP and starting item counts require Restart. ScriptableObject edits during Play can persist; record values you want to keep.

## Expanded spell library

The `CombatSettings` ScriptableObject contains a `Spells` list. Each `Spell` entry holds an ID, display name, elemental owner, target type, effect behavior, MP cost, power, status chance, duration and secondary tuning values. Brim's menu only offers Fire spells, Brooke's only Water, and Blitz's only Electric. The six starter spells are Fire Strike and Ignite; Water Strike and Mend; Arc and Taser. Absorbed spells are added by ID to the compatible character and gain levels on duplicate successful drains.

To add a spell, duplicate a row in the `Spells` list, give it a unique ID, set the element, target, `Effect`, MP cost, power and status values, then set an enemy prototype's `Absorbable Spell Id` to make it that type's signature spell. Each spawned enemy gets a second same-element spell from the catalog, and one of the pair is randomly chosen for Drain. A new arrangement of existing effects needs no code. A wholly new behavior needs a new `SpellEffect` value and its resolution in `BattleActions` and, if enemies should use it, the corresponding enemy spell behavior in `EnemyAttackSystem`.

Tune individual spell values in `Assets/Spellright/CombatSettings.asset` (or select the asset through `Spellright > Select Combat Settings`). `Cost` is MP; `Power` is damage, healing, barrier, or field strength depending on the effect; `Status Chance` is 0–1; `Duration` is in rounds; `Status Power` and `Secondary Power` set effect-specific percentages, reductions, or extra values. Global controls include Burn damage per Enemy Phase, Bubble absorption, Wet Fire/Electric multipliers, Shock disruption chance and damage multiplier, Backdraft scaling, synchronized damage, multi-target scaling, chain falloff, defense/guard values, and item healing/revival. The relevant per-spell values and global tuning controls are grouped under `Spells and items` and `Elemental status balance`.

Burn deals configurable damage at the start of each Enemy Phase. Fire attacks can apply it; Combust consumes it for added damage. Wet has no tick damage: Electric damage is increased against Wet targets and Shock lasts an extra round, with Shock able to conduct to another Wet enemy. Fire instead consumes Wet, has reduced damage against that target, and weakens its attack. Shock lowers enemy attack output and can randomly interrupt an enemy action; Electric attacks also gain a damage bonus against Shocked targets. Water attacks can apply Wet; Cleanse removes Burn, Shock and attack/defense reductions.

Current implemented spells:

- Fire: Fire Strike, Ignite, Wildfire, Combust, Fuel the Flame, Scorch, Backdraft, Flame Wall, Heat Up, Last Spark, Inferno, Phoenix Flame.
- Water: Water Strike, Mend, Healing Rain, Cleanse, Bubble, Tidal Guard, Undertow, Current, Rejuvenate, Overflow, High Tide, Second Breath, Tsunami.
- Electric: Arc, Taser, Chain Lightning, Overcharge, Quick Charge, Static Field, Lightning Rod, Surge, Short Circuit, Live Wire, Conductor, Thunderclap, Lightning Strike.
- Synchronized: Steam Cloud, Pressure Burst, Scald, Conductive Wave, Storm Cloud, Defibrillate, Plasma Bolt, Overheat, Flashfire.

All specified effects are represented by the prototype's direct damage, heal, status, field, target, turn, and formation mechanics. There is no separate enemy spell-casting ruleset: enemy attacks retain the existing melee/projectile/volley system, while each enemy's configured spell reward controls what can be absorbed.

## Code map

- `PrototypeBootstrap`: creates and resets a single encounter.
- `BattleFlow`: phase transitions, action scheduling, drain queue, endless randomized waves and defeat.
- `Combatant`: runtime battle stats and visible statuses; `SpellLibrary`: spell and enemy-reward data types/default catalog.
- `AbsorptionMinigame` / `AbsorptionProgression`: ring timing, escape/success result, learned spells and duplicate levels between restarted encounters.
- `CombatSettings`: Inspector spell/enemy catalog and balance asset.
- `BattleActions`: commands, items, spell effects/status interactions and atomic synchronization costs.
- `FormationSystem`: positions, party order, target weights and modifiers.
- `CounterSystem`: one-attempt input and contact-based classification.
- `EnemyAttackSystem`: enemy melee and spell loadouts, tactical spell priorities, support casts, projectile motion, swept contact, damage and knockback.
- `BattlePresentation`: primitives, camera, spell bolts, flashes and generated tones.
- `BattleUI` / `BattleUI.Navigation` / `BattleUI.Drain`: scalable battle and ring UI, keyboard input, arrows and capsule picking.
- `BattleMenu` / `MenuNavigation`: menu state, pending actions, cancellation and directional navigation.
- `Editor/PrototypeMenu`: scene/settings shortcuts.
- `Tests/Editor/CombatTests`: counter boundary cases, formation roles and a play-mode encounter smoke test launched by the EditMode test runner.

SampleScene is preserved. The project also contains an isolated, graybox overworld formation course in `Scenes/OverworldTest.unity`; it does not continue into the real Spellright overworld.

## Overworld formation test

Open `Assets/Spellright/Scenes/OverworldTest.unity` and press Play. The party is generated from Brim (Fire/red), Brooke (Water/blue), and Blitz (Electric/yellow) placeholders. The course and puzzle objects are generated at runtime by `OverworldTestScene`.

### Controls

- Move: WASD or left stick. Camera: mouse or right stick.
- Jump: Space or gamepad A. Sprint: Left Alt or left stick press.
- Change formation: 1 Line, 2 Spread, 3 Totem, 4 Huddle; gamepad D-pad directions.
- Cycle Line leader or Spread control: Q/E or gamepad bumpers.
- Fire the controlled character's elemental bolt: F (also X or left mouse) or gamepad X.
- Interact with mechanisms: G or gamepad Y. In Huddle, F or gamepad X picks up/drops a Heavy object; the same button fires the selected character's element outside Huddle.
- Rotate the Totem order: Q or gamepad left trigger. Top + Middle determine the spell.
- Huddle air dash: Left Shift or gamepad B. During the dash, the party holds its current height until the dash ends. Glide: hold C or right trigger while airborne.
- Escape toggles mouse cursor lock.

### Course checkpoints

The route runs from Line elemental stations, to Spread's simultaneous plates and sequential element relay, to Totem's Steam pump, aimable Storm target, and Plasma barrier. Huddle then uses a Heavy cube plate, an Air Dash gap, and a stepped climb to a distant lower glide platform. The finale requires Spread switches, Steam, Storm, and a Huddle dash.

When entering Totem, the current Line leader moves to the top slot. Totem movement is slower and disables sprint and jump. Press Q (or gamepad left trigger) to cycle the stack; the new top character becomes the leader. F/X casts the current Top + Middle pair: Brim + Brooke casts Steam, Brooke + Blitz casts an aimable Storm Cloud, and Blitz + Brim casts Plasma. Move the cloud with the movement controls, raise/lower it with Jump/Glide, then press F/X to strike. Interact cancels aiming.

In Huddle, the current leader stands at the front center, with the other two characters close behind to either side. F/X picks up or drops Heavy objects, and Shift/B performs the mid-air dash. The dash pauses vertical movement for its duration, then gravity resumes.

Q/E character swapping is disabled while Huddled. The air dash duration is set by `dashDuration` in `OverworldMovementTuning`.

### Architecture and tuning

- `OverworldPartyManager` owns the persistent three-character party, leader/control selection, camera, and transitions.
- `OverworldFormationController` owns formation and Totem order. `OverworldCharacterMotor` handles shared movement; Line following uses the same motors as Spread control.
- `OverworldInputReader` reads the `Player` map from `Assets/InputSystem_Actions.inputactions`. Add bindings there and consume named actions through this reader.
- `OverworldElementalInteraction`, `OverworldTotemMagic`, and `OverworldHuddleTraversal` keep basic elements, synchronized magic, and cooperative movement separate.
- `OverworldPuzzleElements` contains reusable stations, targets, plates, Heavy objects, doors, and projectiles. `OverworldPuzzleDirector` gates the course puzzles.

Edit `Assets/Spellright/OverworldMovementTuning.asset` to change walk/sprint speed, acceleration, turn speed, jump/gravity, follow distance/speed/catch-up/recovery, formation transition, Totem movement multiplier, default and Totem camera distances, carry multiplier, dash distance/duration, glide gravity, and Steam/Plasma range or Storm aiming speed.

### Making a puzzle

In `OverworldTestScene.BuildCourse`, place graybox objects with the existing helper methods (`Station`, `Magic`, `Plate`, `Door`, `Heavy`, `Platform`). Use `OverworldElementalStation.Initialize` for a single-element mechanism and `OverworldMagicTarget.Initialize` for Steam, Storm, or Plasma. Set `Available` to gate a target, and assign `LinkedDoor` if success should open a route. Pressure plate logic can be added to `OverworldPuzzleDirector`, which already checks party positions, formation state, and puzzle completion each frame. Configure the new objects in `ConfigurePuzzles` rather than adding puzzle rules to the party controller.

This is a runtime-generated prototype: it has no saved character prefabs, production art, checkpoints beyond the start fall recovery, or polished camera/animation behavior. The glide stairs and all gaps are graybox approximations. Formation transitions use short hops; Spread regroups around the selected character. The last available Unity log contained an initialization-time HUD null reference; a guard is in place. A later batch verification exited before compilation because Unity could not connect to its local Package Manager process, so the editor still needs to confirm the compile and Play Mode behavior.

