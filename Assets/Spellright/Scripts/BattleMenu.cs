using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spellright
{
    public enum CombatScreen { Commands, Members, Magic, Items, Formation, Sync, AllyTarget, EnemyTarget }

    public class MenuChoice
    {
        public Rect Rect;
        public string Text;
        public bool Enabled;
        public Action Select;
        public MenuChoice(Rect rect, string text, bool enabled, Action select)
        { Rect = rect; Text = text; Enabled = enabled; Select = select; }
    }

    // Menu navigation and pending selections never spend resources. Only confirmation calls BattleActions.
    public class BattleMenu
    {
        readonly BattleFlow battle;
        public CombatScreen Screen { get; private set; } = CombatScreen.Commands;
        public int Cursor { get; private set; }
        public bool ChoosingTarget => Screen == CombatScreen.AllyTarget || Screen == CombatScreen.EnemyTarget;
        public bool TargetsAll => Screen == CombatScreen.EnemyTarget && spell != null && spell.Target == SpellTarget.AllEnemies;
        public string TargetSpellDescription => ChoosingTarget && (pending == Pending.Spell || pending == Pending.Sync) && spell != null
            ? spell.Label(pending == Pending.Spell ? battle.Actions.SpellLevel(spell) : 1) + " — " + SpellEffectDescription(spell)
            : "";
        CombatScreen returnScreen;
        int returnCursor;
        enum Pending { None, Melee, Spell, Item, Sync }
        Pending pending;
        Spell spell;
        bool ether;
        int magicPage;
        FormationKind formation;
        int frontChoice, topChoice, bottomChoice;
        bool totemChoosingBottom;
        public int FormationPreviewMember
        {
            get
            {
                if (Screen != CombatScreen.Formation) return -1;
                if (formation == FormationKind.Line)
                    return Cursor >= 4 && Cursor < 7 ? Cursor - 4 : frontChoice;
                if (formation == FormationKind.Totem)
                {
                    if (Cursor >= 4 && Cursor < 7)
                    {
                        var options = totemChoosingBottom ? BottomOptions() : Enumerable.Range(0, battle.Party.Count).ToArray();
                        int option = Cursor - 4;
                        if (option < options.Length) return options[option];
                    }
                    return totemChoosingBottom ? bottomChoice : topChoice;
                }
                return -1;
            }
        }
        public int FormationTopMarker => Screen == CombatScreen.Formation && formation == FormationKind.Totem && totemChoosingBottom ? topChoice : -1;
        public BattleMenu(BattleFlow flow) { battle = flow; }
        public void Reset() { Screen = CombatScreen.Commands; Cursor = 0; pending = Pending.None; spell = null; }
        void Open(CombatScreen screen, int cursor = 0) { Screen = screen; Cursor = cursor; }
        public void Back()
        {
            if (!battle.Ready) return;
            if (ChoosingTarget) { Open(returnScreen, returnCursor); pending = Pending.None; spell = null; }
            else if (Screen == CombatScreen.Sync) Open(CombatScreen.Magic, 3);
            else if (Screen == CombatScreen.Commands) Open(CombatScreen.Members, battle.ActiveIndex);
            else Reset();
        }
        public void ChooseMember(int index)
        {
            if (!battle.Ready || battle.Party[index] != battle.Formation.NextActor) return;
            battle.Select(index); Reset();
        }
        public void ClickFighter(Combatant fighter)
        {
            if (!battle.Ready) return;
            if (ChoosingTarget)
            {
                var candidates = TargetCandidates();
                int index = Array.IndexOf(candidates, fighter);
                if (index < 0) return;
                Cursor = TargetsAll ? 0 : index;
                Confirm();
            }
            else if (battle.Party.Contains(fighter)) ChooseMember(battle.Party.IndexOf(fighter));
            else if (fighter.Alive) battle.EnemyTarget = battle.Enemies.IndexOf(fighter);
        }
        public Combatant[] TargetCandidates()
        {
            if (Screen == CombatScreen.AllyTarget && pending == Pending.Item && !ether) return battle.Party.ToArray();
            if (pending == Pending.Spell || pending == Pending.Sync)
            {
                var selected = spell;
                if (selected == null) return new Combatant[0];
                if (selected.Target == SpellTarget.DownedAlly) return battle.Party.Where(c => !c.Alive).ToArray();
                if (selected.Target == SpellTarget.AllEnemies) return battle.Enemies.Where(c => c.Alive).ToArray();
                if (selected.Target == SpellTarget.Enemy)
                    return battle.Enemies.Where(c => c.Alive && (selected.Effect != SpellEffect.Combust || c.BurnTurns > 0) &&
                        (selected.Effect != SpellEffect.Backdraft || c.LastAttackedBrimRound == battle.Round - 1)).ToArray();
                if (selected.Target == SpellTarget.SwapAlly) return battle.Party.Where(c => c.Alive && c != battle.Active).ToArray();
                if (selected.Target == SpellTarget.Ally) return battle.Party.Where(c => c.Alive &&
                    (selected.Effect != SpellEffect.Cleanse || c.BurnTurns > 0 || c.ShockTurns > 0 || c.DefenseDownTurns > 0 || c.AttackDownTurns > 0) &&
                    (selected.Effect != SpellEffect.Surge || (c != battle.Active && battle.Formation.CanAct(c)))).ToArray();
            }
            return (Screen == CombatScreen.AllyTarget ? battle.Party.Where(c => c.Alive) : battle.Enemies.Where(c => c.Alive)).ToArray();
        }
        public bool IsMarked(Combatant fighter)
        {
            if (!battle.Ready) return false;
            if (Screen == CombatScreen.Members) return battle.Party.IndexOf(fighter) == Cursor;
            if (!ChoosingTarget) return fighter == battle.Active;
            var targets = TargetCandidates();
            return TargetsAll ? targets.Contains(fighter) : Cursor >= 0 && Cursor < targets.Length && targets[Cursor] == fighter;
        }
        void Target(Pending action, bool ally, Spell chosenSpell = null)
        {
            returnScreen = Screen; returnCursor = Cursor;
            pending = action; spell = chosenSpell;
            Open(ally ? CombatScreen.AllyTarget : CombatScreen.EnemyTarget);
            var candidates = TargetCandidates();
            Combatant selected = ally ? battle.Party[battle.AllyTarget] : battle.Enemies[battle.EnemyTarget];
            if (!ally)
                selected = candidates.FirstOrDefault(c => c.LightningRodTurns > 0) ?? selected;
            Cursor = TargetsAll ? 0 : Mathf.Max(0, Array.IndexOf(candidates, selected));
        }
        void Commit(Combatant target)
        {
            bool revivablePotionTarget = Screen == CombatScreen.AllyTarget && pending == Pending.Item && !ether;
            bool revivableSpellTarget = (pending == Pending.Spell || pending == Pending.Sync) && spell != null && spell.Target == SpellTarget.DownedAlly;
            if (!battle.Ready || (!target.Alive && !revivablePotionTarget && !revivableSpellTarget)) return;
            if (Screen == CombatScreen.AllyTarget) battle.AllyTarget = battle.Party.IndexOf(target);
            else battle.EnemyTarget = battle.Enemies.IndexOf(target);
            switch (pending)
            {
                case Pending.Melee: battle.Actions.Melee(); break;
                case Pending.Spell: battle.Actions.Cast(spell, target); break;
                case Pending.Item: battle.Actions.Item(ether); break;
                case Pending.Sync: battle.Actions.Sync(spell, target); break;
            }
            Reset();
        }
        void SelectSpell(Spell selected)
        {
            spell = selected;
            if (selected.Target == SpellTarget.Self || selected.Target == SpellTarget.Battlefield || selected.Target == SpellTarget.AllAllies)
            { battle.Actions.Cast(selected, battle.Active); Reset(); return; }
            bool ally = selected.Target == SpellTarget.Ally || selected.Target == SpellTarget.DownedAlly || selected.Target == SpellTarget.SwapAlly;
            Target(Pending.Spell, ally, selected);
        }
        void SelectSync(Spell selected)
        {
            spell = selected;
            if (selected.Target == SpellTarget.Battlefield || selected.Target == SpellTarget.Self || selected.Target == SpellTarget.AllAllies)
            { battle.Actions.Sync(selected); Reset(); return; }
            bool ally = selected.Target == SpellTarget.Ally || selected.Target == SpellTarget.DownedAlly || selected.Target == SpellTarget.SwapAlly;
            Target(Pending.Sync, ally, selected);
        }
        string SpellDescription(Spell selected)
        {
            string target = selected.Target == SpellTarget.AllEnemies ? "all foes" : selected.Target == SpellTarget.AllAllies ? "party" :
                selected.Target == SpellTarget.DownedAlly ? "KO ally" : selected.Target == SpellTarget.Ally ? "ally" :
                selected.Target == SpellTarget.Battlefield ? "field" : selected.Target == SpellTarget.Self ? "self" : "single foe";
            return selected.Effect + " / " + target + (selected.StatusChance > 0 ? " / " + Mathf.RoundToInt(selected.StatusChance * 100) + "% status" : "");
        }
        string SpellEffectDescription(Spell selected)
        {
            switch (selected.Effect)
            {
                case SpellEffect.FireStrike: return "Deals " + selected.Power + " Fire damage and may inflict Burn.";
                case SpellEffect.Ignite: return "Inflicts Burn; affected targets take damage before their turn.";
                case SpellEffect.Wildfire: return "Deals Fire damage to all enemies and can spread Burn.";
                case SpellEffect.Combust: return "Deals extra damage to a Burning target and consumes its Burn.";
                case SpellEffect.FuelFlame: return "Sacrifices some HP to increase Fire damage for a few turns.";
                case SpellEffect.Scorch: return "Deals Fire damage and lowers the target's Defense.";
                case SpellEffect.Backdraft: return "Deals heavy Fire damage to an enemy that attacked Brim last round.";
                case SpellEffect.FlameWall: return "Surrounds the party with a wall that retaliates against attackers.";
                case SpellEffect.HeatUp: return "Builds Fire power for consecutive Fire attacks.";
                case SpellEffect.LastSpark: return "Deals more Fire damage as the caster's HP gets lower.";
                case SpellEffect.Inferno: return "Deals heavy Fire damage to all enemies and may Burn them.";
                case SpellEffect.PhoenixFlame: return "Prepares Brim to revive once if knocked out.";
                case SpellEffect.WaterStrike: return "Deals Water damage and may leave the target Wet.";
                case SpellEffect.Mend: return "Restores " + selected.Power + " HP to one ally.";
                case SpellEffect.HealingRain: return "Restores HP to the whole party.";
                case SpellEffect.Cleanse: return "Removes Burn, Shock, and attack or defense penalties from one ally.";
                case SpellEffect.Bubble: return "Gives one ally a small damage-absorbing Bubble.";
                case SpellEffect.TidalGuard: return "Reduces damage taken by the party for a few turns.";
                case SpellEffect.Undertow: return "Deals Water damage and weakens the target's attacks.";
                case SpellEffect.Current: return "Swaps positions with an ally, changing the party order.";
                case SpellEffect.Rejuvenate: return "Restores HP to one ally at the start of its turns.";
                case SpellEffect.Overflow: return "Heals one ally; excess healing becomes a damage-absorbing Bubble.";
                case SpellEffect.HighTide: return "Strengthens Brooke's Water spells for a few turns.";
                case SpellEffect.SecondBreath: return "Revives one knocked-out ally with HP.";
                case SpellEffect.Tsunami: return "Deals Water damage to all enemies and may make them Wet.";
                case SpellEffect.Arc: return "Deals Electric damage to all enemies and may Shock them.";
                case SpellEffect.Taser: return "Deals Electric damage and inflicts Shock.";
                case SpellEffect.ChainLightning: return "Strikes an enemy, then chains weaker hits to others.";
                case SpellEffect.Overcharge: return "Boosts an ally's next spell, but adds to its MP cost.";
                case SpellEffect.QuickCharge: return "Makes Blitz's next Electric spell cheaper and stronger.";
                case SpellEffect.StaticField: return "Surrounds the party with a field that shocks attackers.";
                case SpellEffect.LightningRod: return "Draws enemy attacks toward Blitz.";
                case SpellEffect.Surge: return "Lets an ally act first in the next Party Phase.";
                case SpellEffect.ShortCircuit: return "Deals Electric damage and weakens the target's attacks.";
                case SpellEffect.LiveWire: return "Wires an enemy so its next hit triggers extra Electric damage.";
                case SpellEffect.Conductor: return "Marks an enemy to take increased Electric damage.";
                case SpellEffect.Thunderclap: return "Damages and Shocks all enemies.";
                case SpellEffect.LightningStrike: return "Deals heavy Electric damage to one enemy.";
                case SpellEffect.SteamCloud: return "Weakens all enemies' attacks.";
                case SpellEffect.PressureBurst: return "Deals heavy synchronized Water damage to one enemy.";
                case SpellEffect.Scald: return "Deals Water damage and inflicts Burn.";
                case SpellEffect.ConductiveWave: return "Damages and Shocks all enemies; Wet targets take extra Electric damage.";
                case SpellEffect.StormCloud: return "Calls down repeated Electric damage on all enemies.";
                case SpellEffect.Defibrillate: return "Revives one knocked-out ally with HP.";
                case SpellEffect.PlasmaBolt: return "Deals heavy synchronized Electric damage to one enemy.";
                case SpellEffect.Overheat: return "Deals Fire damage and weakens the target's attacks.";
                case SpellEffect.Flashfire: return "Deals Fire damage to all enemies.";
                default: return "Deals " + selected.Power + " damage.";
            }
        }
        public void Move(int horizontal, int vertical)
        {
            var choices = Choices();
            Cursor = MenuNavigation.Next(choices, Cursor, horizontal, vertical);
        }
        public void Confirm() { Activate(Cursor); }
        public void Activate(int index)
        {
            if (!battle.Ready) return;
            var choices = Choices();
            if (index < 0 || index >= choices.Count || !choices[index].Enabled) return;
            Cursor = index; choices[index].Select();
        }
        public void Hover(int index) { Cursor = index; }
        public List<MenuChoice> Choices()
        {
            var choices = new List<MenuChoice>();
            if (!battle.Ready) return choices;
            void Add(Rect rect, string text, bool enabled, Action action) => choices.Add(new MenuChoice(rect, text, enabled, action));
            Rect Row(int i, int count, float y = 588) => new Rect(34 + i * (840f / count), y, 840f / count - 14, 52);
            Rect RoleRow(int i, float y) => new Rect(34 + i * (530f / 3), y, 530f / 3 - 8, 40);
            if (ChoosingTarget)
            {
                var candidates = TargetCandidates();
                if (TargetsAll)
                    Add(new Rect(34, 568, 824, 52), "All living enemies (" + candidates.Length + ") - confirm attack", candidates.Length > 0, () => Commit(candidates[0]));
                else if (Screen == CombatScreen.EnemyTarget)
                {
                    int columns = Mathf.Min(3, Mathf.Max(1, candidates.Length));
                    float cellWidth = 840f / columns;
                    for (int i = 0; i < candidates.Length; i++)
                    {
                        var target = candidates[i];
                        int column = i % columns, row = i / columns;
                        string effects = string.IsNullOrEmpty(target.StatusText) ? "No active effects" : "Effects: " + target.StatusText;
                        string text = target.Name + "\nHP " + target.HP + "/" + target.MaxHP + "\n" + effects;
                        Add(new Rect(34 + column * cellWidth, 568 + row * 76, cellWidth - 12, 70), text, true, () => Commit(target));
                    }
                }
                else for (int i = 0; i < candidates.Length; i++)
                {
                    var target = candidates[i];
                    string effects = string.IsNullOrEmpty(target.StatusText) ? "No active effects" : "Effects: " + target.StatusText;
                    Add(Row(i, candidates.Length), target.Name + "\nHP " + target.HP + "/" + target.MaxHP + "   MP " + target.MP + "/" + target.MaxMP + "\n" + effects, true, () => Commit(target));
                }
            }
            else if (Screen == CombatScreen.Commands)
            {
                string[] names = { "Melee", "Magic", "Shield", "Item", "Formation", "Flee" };
                for (int i = 0; i < names.Length; i++)
                {
                    int command = i;
                    Add(new Rect(34 + i % 3 * 280, 582 + i / 3 * 64, 266, 52),
                        command == 0 && !battle.Formation.CanUseMelee(battle.Active) ? "Melee (out of reach)" : command == 4 && battle.Formation.LockedByKnockout ? "Formation (locked)" : names[i],
                        (i != 4 || (!battle.Formation.ChangedThisPhase && !battle.Formation.LockedByKnockout)) && (command != 0 || battle.Formation.CanUseMelee(battle.Active)), () =>
                    {
                        switch (command)
                        {
                            case 0: Target(Pending.Melee, false); break;
                            case 1: magicPage = 0; Open(CombatScreen.Magic); break;
                            case 2: battle.Actions.Shield(); Reset(); break;
                            case 3: Open(CombatScreen.Items); break;
                            case 4:
                                formation = battle.Formation.Kind;
                                frontChoice = topChoice = battle.Formation.Order[0];
                                bottomChoice = battle.Formation.Order[2];
                                totemChoosingBottom = false;
                                Open(CombatScreen.Formation); break;
                            case 5: battle.Finish("FLED"); Reset(); break;
                        }
                    });
                }
            }
            else if (Screen == CombatScreen.Members)
            {
                for (int i = 0; i < battle.Party.Count; i++)
                {
                    int member = i;
                    Add(Row(i, 3), battle.Party[i].Name + (battle.Formation.Kind == FormationKind.Totem && battle.Formation.Slot(battle.Party[i]) == 2 ? " (no action)" : ""),
                        battle.Party[i] == battle.Formation.NextActor, () => ChooseMember(member));
                }
            }
            else if (Screen == CombatScreen.Magic)
            {
                var spells = battle.Actions.AvailableSpells;
                int pageCount = 6, pages = Mathf.Max(1, Mathf.CeilToInt(spells.Length / (float)pageCount));
                magicPage = Mathf.Clamp(magicPage, 0, pages - 1);
                for (int i = magicPage * pageCount; i < Mathf.Min(spells.Length, (magicPage + 1) * pageCount); i++)
                {
                    var selectedSpell = spells[i]; int index = i - magicPage * pageCount;
                    string reason = battle.Actions.SpellBlockReason(selectedSpell);
                    string display = selectedSpell.Label(battle.Actions.SpellLevel(selectedSpell)) + "  " + selectedSpell.Cost + " MP\n" + SpellDescription(selectedSpell) +
                        (string.IsNullOrEmpty(reason) ? "" : "  [" + reason + "]");
                    Rect rect = new Rect(34 + index % 2 * 420, 578 + index / 2 * 47, 408, 43);
                    Add(rect, display, string.IsNullOrEmpty(reason), () => SelectSpell(selectedSpell));
                }
                if (pages > 1)
                {
                    Add(new Rect(34, 724, 196, 34), "Previous Spells", magicPage > 0, () => { magicPage--; Cursor = 0; });
                    Add(new Rect(244, 724, 196, 34), "Next Spells " + (magicPage + 1) + "/" + pages, magicPage < pages - 1, () => { magicPage++; Cursor = 0; });
                }
                Add(new Rect(454, 724, 404, 34), "Synchronized Magic (Top + Middle)", battle.Formation.Kind == FormationKind.Totem,
                    () => Open(CombatScreen.Sync));
            }
            else if (Screen == CombatScreen.Items)
            {
                Add(Row(0, 2), "Potion +" + battle.Settings.healingAmount + " HP / revive  (" + battle.Actions.Potions + " left)", battle.Actions.Potions > 0,
                    () => { ether = false; Target(Pending.Item, true); });
                Add(Row(1, 2), "Ether +" + battle.Settings.etherAmount + " MP  (" + battle.Actions.Ethers + " left)", battle.Actions.Ethers > 0,
                    () => { ether = true; Target(Pending.Item, true); });
            }
            else if (Screen == CombatScreen.Formation)
            {
                for (int i = 0; i < 4; i++)
                {
                    var kind = (FormationKind)i;
                    Add(new Rect(34 + i * 210, 584, 196, 36), (formation == kind ? "[Selected] " : "") + kind,
                        !battle.Formation.ChangedThisPhase, () => SelectFormation(kind));
                }
                if (formation == FormationKind.Line)
                {
                    for (int i = 0; i < battle.Party.Count; i++)
                    {
                        int member = i;
                        Add(RoleRow(i, 638), "Front: " + battle.Party[i].Name + (frontChoice == i ? "  [Current]" : ""),
                            !battle.Formation.ChangedThisPhase, () =>
                            {
                                frontChoice = member;
                                battle.Actions.ChangeFormation(FormationKind.Line, BuildOrder());
                                Reset();
                            });
                    }
                }
                else if (formation == FormationKind.Totem)
                {
                    int[] options = totemChoosingBottom ? BottomOptions() : Enumerable.Range(0, battle.Party.Count).ToArray();
                    for (int i = 0; i < options.Length; i++)
                    {
                        int member = options[i];
                        string role = totemChoosingBottom ? "Bottom: " : "Top: ";
                        int selected = totemChoosingBottom ? bottomChoice : topChoice;
                        Add(RoleRow(i, 638), role + battle.Party[member].Name + (selected == member ? "  [Current]" : ""),
                            !battle.Formation.ChangedThisPhase, () =>
                        {
                            if (!totemChoosingBottom)
                            {
                                topChoice = member;
                                if (bottomChoice == topChoice) bottomChoice = BottomOptions()[0];
                                totemChoosingBottom = true;
                                Cursor = 4 + Array.IndexOf(BottomOptions(), bottomChoice);
                            }
                            else
                            {
                                bottomChoice = member;
                                battle.Actions.ChangeFormation(FormationKind.Totem, BuildOrder());
                                Reset();
                            }
                        });
                    }
                }
                else
                {
                    string sequence = formation == FormationKind.Huddle ? "Order: Front, Top Left, Bottom Left\nParty slots assigned automatically"
                        : "Order: Top, Middle, Bottom\nParty slots assigned automatically";
                    Add(new Rect(34, 636, 530, 48), sequence, true, () => { });
                }
            }
            else if (Screen == CombatScreen.Sync)
            {
                var top = battle.Formation.At(0); var middle = battle.Formation.At(1);
                var combos = SpellLibrary.Synchronized(battle.Settings, top.Element, middle.Element);
                for (int i = 0; i < combos.Length; i++)
                {
                    var combo = combos[i]; string reason = battle.Actions.SyncBlockReason(combo);
                    int a = (combo.Cost + 1) / 2, b = combo.Cost / 2;
                    string text = combo.Name + " / " + combo.Cost + " MP\n" + a + " from Top, " + b + " from Middle; uses both actions" +
                        (string.IsNullOrEmpty(reason) ? "" : "\n" + reason);
                    Add(new Rect(34, 578 + i * 58, 824, 52), text, string.IsNullOrEmpty(reason), () => SelectSync(combo));
                }
                if (combos.Length == 0) Add(new Rect(34, 598, 824, 58), "No compatible spell pair for Top (" + top.Element + ") + Middle (" + middle.Element + ").", false, () => { });
            }
            if (Cursor >= choices.Count || Cursor < 0 || !choices[Cursor].Enabled)
                Cursor = Mathf.Max(0, choices.FindIndex(c => c.Enabled));
            return choices;
        }
        int[] BuildOrder()
        {
            if (formation == FormationKind.Line)
                return new[] { frontChoice }.Concat(Enumerable.Range(0, battle.Party.Count).Where(i => i != frontChoice)).ToArray();
            if (formation == FormationKind.Totem)
                return new[] { topChoice, Enumerable.Range(0, battle.Party.Count).First(i => i != topChoice && i != bottomChoice), bottomChoice };
            return Enumerable.Range(0, battle.Party.Count).ToArray();
        }
        int[] BottomOptions() => Enumerable.Range(0, battle.Party.Count).Where(i => i != topChoice).ToArray();
        void SelectFormation(FormationKind kind)
        {
            formation = kind;
            if (kind == FormationKind.Huddle || kind == FormationKind.Spread)
            {
                battle.Actions.ChangeFormation(kind, BuildOrder());
                Reset();
            }
            else if (kind == FormationKind.Line)
            {
                Cursor = 4 + frontChoice;
            }
            else
            {
                totemChoosingBottom = false;
                Cursor = 4 + topChoice;
            }
        }
        public string Help => ChoosingTarget ? "WASD: choose target   Enter: confirm   Esc: cancel\nOr click a capsule, its top card, or its name below. No cost until confirmed."
            : Screen == CombatScreen.Members ? "Turns follow formation order; only the next member can act. In Totem, Bottom has no action."
            : Screen == CombatScreen.Items ? "Potion heals a conscious ally or revives a KO ally. Ether restores MP to a conscious ally. Esc goes back."
            : Screen == CombatScreen.Sync ? "Top + Middle elements unlock three pair moves. Select a move, then its target; both members spend actions."
            : Screen == CombatScreen.Formation ? (formation == FormationKind.Totem ? (totemChoosingBottom ? "Top is set. Select a Bottom member to confirm Totem; the remaining member fills Middle." : "Select a Top member, then choose Bottom. Selecting Bottom confirms Totem.")
                : formation == FormationKind.Line ? "Select a Front member and press Enter (or click) to confirm Line. The other two follow in party order."
                : formation == FormationKind.Huddle ? "Selecting Huddle applies it automatically. Enemy strikes splash the party; members take and deal reduced damage."
                : "Selecting Spread applies it automatically. Positions and turn order follow party order.")
            : "WASD: move arrow   Enter: select   Esc: back / choose acting member\nMouse clicks work too. Space counters during Enemy Phase.";
    }

    public static class MenuNavigation
    {
        public static int Next(IReadOnlyList<MenuChoice> choices, int current, int horizontal, int vertical)
        {
            if (choices.Count == 0) return 0;
            current = Mathf.Clamp(current, 0, choices.Count - 1);
            Vector2 origin = choices[current].Rect.center;
            // Single-row lists accept all four WASD keys.
            if (vertical != 0 && choices.All(c => Mathf.Abs(c.Rect.center.y - origin.y) < 1))
            { horizontal = vertical; vertical = 0; }
            Vector2 direction = new Vector2(horizontal, vertical);
            int best = current;
            float score = float.PositiveInfinity;
            for (int i = 0; i < choices.Count; i++)
            {
                if (i == current || !choices[i].Enabled) continue;
                Vector2 delta = choices[i].Rect.center - origin;
                float forward = Vector2.Dot(delta, direction);
                if (forward <= 1) continue;
                float sideways = Mathf.Abs(delta.x * direction.y - delta.y * direction.x);
                float candidate = forward + sideways * 3;
                if (candidate < score) { score = candidate; best = i; }
            }
            return best;
        }
    }
}
