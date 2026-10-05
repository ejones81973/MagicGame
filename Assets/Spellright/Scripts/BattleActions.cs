using System.Collections;
using System.Linq;
using UnityEngine;

namespace Spellright
{
    public class BattleActions
    {
        readonly BattleFlow battle;
        public int Potions, Ethers;
        public BattleActions(BattleFlow battle) { this.battle = battle; Potions = battle.Settings.startingPotions; Ethers = battle.Settings.startingEthers; }
        Combatant Enemy => battle.Enemies[battle.EnemyTarget];
        Combatant Ally => battle.Party[battle.AllyTarget];
        public Spell[] AvailableSpells => SpellLibrary.For(battle.Settings, battle.Active.Element)
            .Concat(battle.Absorption.SpellsFor(battle.Active.Element, battle.Settings))
            .GroupBy(spell => spell.Id).Select(group => group.First()).ToArray();
        public int SpellLevel(Spell spell) => Mathf.Max(1, battle.Absorption.Level(spell.Id));

        public void Melee() { if (battle.Ready && battle.Formation.CanUseMelee(battle.Active) && Enemy.Alive) battle.Perform(Attack(battle.Active, Enemy)); }
        IEnumerator Attack(Combatant actor, Combatant target)
        {
            actor.Acted = true; Vector3 home = actor.View.position;
            yield return battle.Presentation.FocusAttack(new[] { actor, target }, battle.Settings);
            actor.Motion.SetBraced(true); target.Motion.SetBraced(true);
            yield return MeleeAnimation.Move(actor.View, target.View.position + Vector3.left * 2.3f, .28f);
            yield return new WaitForSeconds(battle.Settings.partyMeleePauseSeconds);
            yield return MeleeAnimation.Hop(actor, target, battle.Settings, _ => Damage(actor, target, actor.Attack));
            yield return new WaitForSeconds(.12f); actor.Motion.SetAttackLean(0);
            yield return MeleeAnimation.Move(actor.View, home, .28f);
            actor.Motion.SetBraced(false); target.Motion.SetBraced(false);
            yield return battle.Presentation.ReturnCamera(battle.Settings); ResetHeat(actor);
        }
        public void Damage(Combatant actor, Combatant target, float power)
        {
            if (actor.AttackDownTurns > 0) power *= actor.AttackDownMultiplier;
            float guard = target.TidalGuardTurns > 0 ? target.TidalGuardMultiplier : 1f;
            int incoming = Mathf.Max(1, Mathf.RoundToInt((power * battle.Formation.Outgoing(actor) - EffectiveDefense(target)) * guard));
            if (target.BubbleAbsorb > 0)
            {
                int absorbed = Mathf.Min(target.BubbleAbsorb, incoming);
                target.BubbleAbsorb -= absorbed; incoming -= absorbed;
                if (target.BubbleAbsorb == 0) target.Bubbled = false;
                battle.Log(target.Name + "'s Bubble absorbs " + absorbed + " damage.");
            }
            int hit = incoming > 0 ? target.Hurt(incoming) : 0;
            if (hit > 0) { target.Motion.Recoil(target.View.position - actor.View.position); battle.Presentation.EnemyHit(target); battle.Presentation.ShowDamage(target, hit); }
            if (battle.Enemies.Contains(target))
            {
                TriggerEnemyField(target, actor);
                TriggerLiveWire(target);
                if (!target.Alive) battle.EnemyDefeated(target);
            }
            battle.Log(actor.Name + " -> " + target.Name + ": " + hit + " damage");
        }
        void TriggerEnemyField(Combatant target, Combatant attacker)
        {
            if (!target.Alive || target.FlameWallTurns <= 0 && target.StaticFieldTurns <= 0) return;
            int damage = (target.FlameWallTurns > 0 ? target.FlameWallDamage : 0) +
                (target.StaticFieldTurns > 0 ? target.StaticFieldDamage : 0);
            if (damage <= 0) return;
            int hit = attacker.Hurt(damage);
            if (target.StaticFieldTurns > 0) attacker.ShockTurns = Mathf.Max(attacker.ShockTurns, 1);
            battle.Presentation.ShowDamage(attacker, hit);
            battle.Log(target.Name + "'s field retaliates for " + hit + " damage.");
            if (!attacker.Alive && battle.Formation.ForceHuddle())
                battle.StartCoroutine(battle.Formation.JumpIntoPosition(battle.Settings.formationJumpSeconds, battle.Settings.formationJumpHeight));
        }
        int EffectiveDefense(Combatant c) => Mathf.Max(0, c.Defense - (c.DefenseDownTurns > 0 ? Mathf.RoundToInt(c.DefenseDownAmount) : 0));
        public void Shield() { if (battle.Ready) battle.Perform(Guard(battle.Active)); }
        IEnumerator Guard(Combatant actor) { actor.Acted = true; actor.Shielded = true; ResetHeat(actor); battle.Log(actor.Name + " shields through the next Enemy Phase."); yield return new WaitForSeconds(.15f); }

        public string SpellBlockReason(Spell spell)
        {
            if (spell == null) return "Spell data is missing.";
            if (!battle.Ready) return "Wait for this character's turn.";
            if (battle.Active.Element != spell.Element) return "Only " + spell.Element + " users can cast this spell.";
            int cost = EffectiveCost(battle.Active, spell);
            if (battle.Active.MP < cost) return "Needs " + cost + " MP (have " + battle.Active.MP + ").";
            if (spell.Effect == SpellEffect.Combust && !battle.Enemies.Any(e => e.Alive && e.BurnTurns > 0)) return "Requires a Burning enemy.";
            if (spell.Effect == SpellEffect.Backdraft && !battle.Enemies.Any(e => e.Alive && e.LastAttackedBrimRound == battle.Round - 1)) return "Needs an enemy that attacked Brim last round.";
            if (spell.Effect == SpellEffect.FuelFlame && battle.Active.HP <= spell.SecondaryPower) return "Brim needs more HP to pay the sacrifice.";
            if ((spell.Target == SpellTarget.Enemy || spell.Target == SpellTarget.AllEnemies) && !battle.Enemies.Any(e => e.Alive)) return "No living enemy.";
            if ((spell.Target == SpellTarget.Ally || spell.Target == SpellTarget.AllAllies) && !battle.Party.Any(c => c.Alive)) return "No conscious ally.";
            if (spell.Effect == SpellEffect.Cleanse && !battle.Party.Any(c => c.Alive && (c.BurnTurns > 0 || c.ShockTurns > 0 || c.DefenseDownTurns > 0 || c.AttackDownTurns > 0))) return "No removable status.";
            if (spell.Target == SpellTarget.DownedAlly && !battle.Party.Any(c => !c.Alive)) return "No downed ally.";
            if (spell.Target == SpellTarget.SwapAlly && !battle.Party.Any(c => c.Alive && c != battle.Active)) return "No other ally to swap with.";
            if (spell.Effect == SpellEffect.Surge && !battle.Party.Any(c => c.Alive && c != battle.Active && battle.Formation.CanAct(c))) return "No other eligible ally for priority.";
            return "";
        }
        public bool CanCast(Spell spell) => string.IsNullOrEmpty(SpellBlockReason(spell));
        public void Cast(Spell spell, Combatant chosen = null) { if (CanCast(spell)) battle.Perform(CastRoutine(spell, chosen, SpellLevel(spell))); }
        int EffectiveCost(Combatant caster, Spell spell)
        {
            int cost = spell.Cost;
            if (spell.Element == Element.Electric && caster.QuickChargeTurns > 0) cost = Mathf.Max(0, cost - caster.NextSpellCostReduction);
            if (caster.OverchargeTurns > 0) cost += Mathf.RoundToInt(caster.NextMagicExtraMp);
            return cost;
        }
        IEnumerator CastRoutine(Spell spell, Combatant chosen, int level)
        {
            var actor = battle.Active; int cost = EffectiveCost(actor, spell);
            actor.Acted = true; actor.MP -= cost; battle.Log(actor.Name + " casts " + spell.Label(level) + " (" + cost + " MP)");
            bool offensiveFire = spell.Element == Element.Fire && (spell.Target == SpellTarget.Enemy || spell.Target == SpellTarget.AllEnemies);
            if (!offensiveFire && spell.Effect != SpellEffect.HeatUp) ResetHeat(actor);
            else if (offensiveFire && spell.Effect != SpellEffect.HeatUp) actor.HeatStacks = Mathf.Min(5, actor.HeatStacks + 1);
            yield return ResolveSpell(actor, spell, level, chosen);
        }
        IEnumerator ResolveSpell(Combatant actor, Spell spell, int level, Combatant chosen)
        {
            Combatant[] targets = GetTargets(actor, spell, chosen);
            if (targets.Length > 0) yield return battle.Presentation.FocusAttack(new[] { actor }.Concat(targets), battle.Settings);
            for (int i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                if (EnemyTargetSpell(spell)) yield return battle.Presentation.Bolt(actor, target, actor.Color);
                ApplySpellTarget(actor, target, spell, level, i);
            }
            if (actor.OverchargeTurns > 0 && EnemyTargetSpell(spell))
            { actor.OverchargeTurns = 0; actor.NextMagicBonus = actor.NextMagicExtraMp = 0; }
            ApplyCasterOrField(actor, spell, targets);
            if (targets.Length > 0 || spell.Target == SpellTarget.Battlefield || spell.Target == SpellTarget.Self)
                yield return battle.Presentation.ReturnCamera(battle.Settings);
        }
        Combatant[] GetTargets(Combatant actor, Spell spell, Combatant chosen)
        {
            switch (spell.Target)
            {
                case SpellTarget.Enemy:
                    if (spell.Effect == SpellEffect.ChainLightning)
                    {
                        var first = chosen ?? Enemy;
                        return new[] { first }.Concat(battle.Enemies.Where(e => e.Alive && e != first)).ToArray();
                    }
                    return new[] { chosen ?? Enemy };
                case SpellTarget.AllEnemies: return battle.Enemies.Where(e => e.Alive).ToArray();
                case SpellTarget.Ally: case SpellTarget.DownedAlly: case SpellTarget.SwapAlly: return new[] { chosen ?? Ally };
                case SpellTarget.AllAllies: return battle.Party.Where(c => c.Alive).ToArray();
                case SpellTarget.Self: return new[] { actor };
                default: return new Combatant[0];
            }
        }
        bool EnemyTargetSpell(Spell s) => s.Target == SpellTarget.Enemy || s.Target == SpellTarget.AllEnemies;

        void ApplySpellTarget(Combatant actor, Combatant target, Spell spell, int level, int index)
        {
            int power = spell.ScaledPower(level);
            if (spell.Target == SpellTarget.AllEnemies) power = Mathf.RoundToInt(power * battle.Settings.multiTargetScale);
            if (spell.Effect == SpellEffect.ChainLightning) power = Mathf.RoundToInt(power * Mathf.Pow(battle.Settings.chainTargetScale, index));
            if (DamageElement(spell) == Element.Water && target.BurnTurns > 0)
            {
                target.BurnTurns = 0;
                battle.Presentation.ShowStatus(target, "extinguished", Color.cyan);
                battle.Log(target.Name + "'s Burn is extinguished by Water.");
            }
            switch (spell.Effect)
            {
                case SpellEffect.FireStrike: case SpellEffect.WaterStrike: case SpellEffect.Ignite: case SpellEffect.Wildfire:
                case SpellEffect.Scorch: case SpellEffect.Backdraft: case SpellEffect.LastSpark: case SpellEffect.Inferno:
                case SpellEffect.Tsunami: case SpellEffect.Arc: case SpellEffect.Taser: case SpellEffect.ChainLightning:
                case SpellEffect.ShortCircuit: case SpellEffect.Thunderclap: case SpellEffect.LightningStrike:
                case SpellEffect.PressureBurst: case SpellEffect.Scald: case SpellEffect.ConductiveWave:
                case SpellEffect.PlasmaBolt: case SpellEffect.Overheat: case SpellEffect.Flashfire: case SpellEffect.Undertow:
                    ApplyAttackSpell(actor, target, spell, power);
                    break;
                case SpellEffect.Combust:
                    power += target.BurnTurns * battle.Settings.burnDamagePerTurn; target.BurnTurns = 0;
                    HitSpell(actor, target, Element.Fire, power);
                    if (target.WetTurns > 0) { target.WetTurns = 0; target.AttackDownTurns = Mathf.Max(1, target.AttackDownTurns); battle.Log("Combust + Wet bursts into steam."); }
                    Feedback(target.Name + "'s Burn is consumed by Combust!", new Color(1f, .4f, .15f)); break;
                case SpellEffect.Mend: case SpellEffect.HealingRain: Heal(target, Mathf.RoundToInt(power * (1 + actor.WaterEffectBonus))); break;
                case SpellEffect.Overflow: HealOverflow(target, Mathf.RoundToInt(power * (1 + actor.WaterEffectBonus)), spell.SecondaryPower); break;
                case SpellEffect.Cleanse: target.Cleanse(); Feedback(target.Name + " is cleansed.", Color.cyan); break;
                case SpellEffect.Bubble:
                    int bubble = spell.Power > 0 ? spell.Power : battle.Settings.bubbleAbsorbAmount;
                    target.BubbleAbsorb += bubble; target.Bubbled = true; Feedback(target.Name + " gains a " + bubble + " HP Bubble.", Color.cyan); break;
                case SpellEffect.Rejuvenate: target.RegenTurns = spell.Duration; target.RegenAmount = spell.Power; Feedback(target.Name + " is regenerating HP.", Color.cyan); break;
                case SpellEffect.SecondBreath: case SpellEffect.Defibrillate: Revive(target, power); break;
                case SpellEffect.Overcharge: target.OverchargeTurns = spell.Duration; target.NextMagicBonus = spell.StatusPower / 100f; target.NextMagicExtraMp = spell.SecondaryPower; Feedback(target.Name + "'s next spell is overcharged (+" + spell.SecondaryPower + " MP).", Color.yellow); break;
                case SpellEffect.Surge: target.SurgePending = true; target.SurgeTurns = 1; Feedback(target.Name + " will act first next Party Phase.", Color.yellow); break;
            }
        }
        void ApplyAttackSpell(Combatant actor, Combatant target, Spell spell, int power)
        {
            bool wasBurning = target.BurnTurns > 0;
            if (spell.Effect == SpellEffect.Backdraft) power = Mathf.RoundToInt(power * battle.Settings.backdraftDamageMultiplier);
            if (spell.Effect == SpellEffect.LastSpark) power = Mathf.RoundToInt(power * (1 + (float)(actor.MaxHP - actor.HP) / actor.MaxHP));
            if (spell.Effect == SpellEffect.Combust) power += target.BurnTurns * battle.Settings.burnDamagePerTurn;
            int hit = HitSpell(actor, target, DamageElement(spell), power, spell.Synchronized);
            if (spell.Effect == SpellEffect.FireStrike || spell.Effect == SpellEffect.Wildfire || spell.Effect == SpellEffect.Inferno || spell.Effect == SpellEffect.Scald)
                TryBurn(target, spell, spell.Effect == SpellEffect.Scald);
            if (spell.Effect == SpellEffect.Ignite) TryBurn(target, spell, true);
            if (spell.Effect == SpellEffect.WaterStrike || spell.Effect == SpellEffect.Tsunami) TryWet(target, spell);
            if (spell.Effect == SpellEffect.Arc || spell.Effect == SpellEffect.Taser || spell.Effect == SpellEffect.ChainLightning || spell.Effect == SpellEffect.Thunderclap || spell.Effect == SpellEffect.ConductiveWave)
                if (spell.Effect == SpellEffect.Taser || spell.Effect == SpellEffect.Thunderclap || spell.Effect == SpellEffect.ConductiveWave || Random.value <= spell.StatusChance) ApplyShock(target, spell.Duration);
            if (spell.Effect == SpellEffect.Wildfire && wasBurning)
            {
                var next = battle.Enemies.FirstOrDefault(e => e.Alive && e != target && e.BurnTurns == 0);
                if (next != null) { next.BurnTurns = spell.Duration; battle.Log("Wildfire spreads Burn to " + next.Name + "."); }
            }
            if (spell.Effect == SpellEffect.Scorch) { target.DefenseDownTurns = spell.Duration; target.DefenseDownAmount = spell.StatusPower; }
            if (spell.Effect == SpellEffect.Undertow || spell.Effect == SpellEffect.ShortCircuit || spell.Effect == SpellEffect.Overheat)
            { target.AttackDownTurns = spell.Duration; target.AttackDownMultiplier = spell.SecondaryPower > 0 ? spell.SecondaryPower / 100f : battle.Settings.shockAttackMultiplier; }
            if (spell.Effect == SpellEffect.LiveWire) { target.LiveWireTurns = spell.Duration; target.LiveWireCharges = 1; target.LiveWireDamage = spell.SecondaryPower; Feedback(target.Name + " is wired for the next hit.", Color.yellow); }
            if (spell.Effect == SpellEffect.Conductor) { target.ConductorTurns = spell.Duration; target.ConductorBonus = spell.SecondaryPower / 100f; Feedback(target.Name + " becomes a Conductor.", Color.yellow); }
            if (spell.Effect == SpellEffect.SteamCloud) { target.AttackDownTurns = spell.Duration; target.AttackDownMultiplier = spell.StatusPower / 100f; }
            if (spell.Effect == SpellEffect.Overheat) Feedback(target.Name + " is Overheated and weakened.", new Color(1, .5f, .15f));
            if (spell.Effect == SpellEffect.Thunderclap) Feedback(target.Name + " is disrupted by Shock.", Color.yellow);
            if (spell.Effect == SpellEffect.ConductiveWave && target.WetTurns > 0) ApplyShock(target, spell.Duration);
        }
        int HitSpell(Combatant actor, Combatant target, Element element, int raw, bool synchronized = false)
        {
            float amount = raw;
            float matchup = ElementMatchupMultiplier(element, target.Element);
            amount *= matchup;
            if (element == Element.Fire)
            {
                amount *= 1 + actor.FireDamageBonus + actor.HeatStacks * .05f;
                if (target.WetTurns > 0)
                {
                    amount *= battle.Settings.wetFireDamageMultiplier; target.WetTurns = 0;
                    target.AttackDownTurns = Mathf.Max(1, target.AttackDownTurns); target.AttackDownMultiplier = Mathf.Min(target.AttackDownMultiplier, .85f);
                    battle.Log("Wet + Fire creates steam and weakens " + target.Name + ".");
                }
            }
            amount *= battle.Settings.spellPower;
            if (element == Element.Water) amount *= 1 + actor.WaterEffectBonus;
            if (element == Element.Electric && target.WetTurns > 0) amount *= battle.Settings.wetElectricDamageMultiplier;
            if (element == Element.Electric && target.ShockTurns > 0) amount *= 1.15f;
            if (element == Element.Electric && target.ConductorTurns > 0) amount *= 1 + target.ConductorBonus;
            if (actor.ShockTurns > 0) amount *= battle.Settings.shockAttackMultiplier;
            if (synchronized) amount *= battle.Settings.synchronizedPower;
            if (actor.OverchargeTurns > 0) amount *= 1 + actor.NextMagicBonus;
            int before = target.HP;
            Damage(actor, target, amount);
            ShowElementMatchup(target, element, matchup);
            return before - target.HP;
        }
        internal static float ElementMatchupMultiplier(Element attack, Element defense)
        {
            if (attack == Element.None || defense == Element.None) return 1f;
            if (attack == defense) return .7f;
            bool weak = (attack == Element.Water && defense == Element.Fire) ||
                (attack == Element.Fire && defense == Element.Electric) ||
                (attack == Element.Electric && defense == Element.Water);
            return weak ? 1.3f : 1f;
        }
        void ShowElementMatchup(Combatant target, Element attack, float multiplier)
        {
            if (multiplier == 1f) return;
            bool weak = multiplier > 1f;
            string text = weak ? "weak" : "resist";
            Color color = weak ? new Color(1f, .35f, .25f) : new Color(.55f, .75f, 1f);
            battle.Presentation.ShowStatus(target, text, color);
            battle.Log(target.Name + " is " + text + " to " + attack + ".");
        }
        Element DamageElement(Spell spell)
        {
            switch (spell.Effect)
            {
                case SpellEffect.PressureBurst: case SpellEffect.Scald: return Element.Water;
                case SpellEffect.ConductiveWave: case SpellEffect.StormCloud: case SpellEffect.PlasmaBolt: return Element.Electric;
                case SpellEffect.Overheat: case SpellEffect.Flashfire: return Element.Fire;
                default: return spell.Element == Element.None ? battle.Active.Element : spell.Element;
            }
        }

        void ApplyCasterOrField(Combatant actor, Spell spell, Combatant[] targets)
        {
            switch (spell.Effect)
            {
                case SpellEffect.FuelFlame:
                    Hurt(actor, Mathf.Max(1, spell.SecondaryPower)); actor.FireDamageBonus = spell.StatusPower / 100f; actor.FireBuffTurns = spell.Duration;
                    Feedback("Brim sacrifices HP to strengthen Fire magic.", new Color(1, .45f, .2f)); break;
                case SpellEffect.HeatUp:
                    actor.FireBuffTurns = spell.Duration; actor.FireDamageBonus = Mathf.Max(actor.FireDamageBonus, spell.StatusPower / 100f);
                    Feedback("Heat Up: Fire power builds with consecutive attacks.", new Color(1, .45f, .2f)); break;
                case SpellEffect.PhoenixFlame:
                    actor.PhoenixTurns = spell.Duration; actor.PhoenixHP = Mathf.Max(1, actor.MaxHP * spell.SecondaryPower / 100);
                    Feedback("Phoenix Flame will revive Brim once.", new Color(1, .55f, .18f)); break;
                case SpellEffect.FlameWall:
                    foreach (var c in battle.Party) { c.FlameWallTurns = spell.Duration; c.FlameWallDamage = spell.Power; }
                    Feedback("Flame Wall is active around the party.", new Color(1, .45f, .2f)); break;
                case SpellEffect.TidalGuard:
                    foreach (var c in battle.Party.Where(c => c.Alive)) { c.TidalGuardTurns = spell.Duration; c.TidalGuardMultiplier = Mathf.Min(battle.Settings.guardedIncoming, 1 - spell.StatusPower / 100f); }
                    Feedback("Tidal Guard protects the party.", Color.cyan); break;
                case SpellEffect.HighTide:
                    actor.WaterBuffTurns = spell.Duration; actor.WaterEffectBonus = spell.StatusPower / 100f; Feedback("High Tide strengthens Water spells.", Color.cyan); break;
                case SpellEffect.StaticField:
                    foreach (var c in battle.Party) { c.StaticFieldTurns = spell.Duration; c.StaticFieldDamage = spell.Power; }
                    Feedback("Static Field is active around the party.", Color.yellow); break;
                case SpellEffect.LightningRod:
                    actor.LightningRodTurns = spell.Duration; Feedback(actor.Name + " draws enemy attacks.", Color.yellow); break;
                case SpellEffect.QuickCharge:
                    actor.QuickChargeTurns = spell.Duration; actor.NextSpellCostReduction = spell.SecondaryPower; Feedback("Next Electric spell costs less MP.", Color.yellow); break;
                case SpellEffect.SteamCloud:
                    foreach (var e in battle.Enemies.Where(e => e.Alive)) { e.AttackDownTurns = spell.Duration; e.AttackDownMultiplier = spell.StatusPower / 100f; }
                    Feedback("Steam Cloud weakens the enemy side.", Color.cyan); break;
                case SpellEffect.StormCloud:
                    foreach (var e in battle.Enemies.Where(e => e.Alive)) { e.StormCloudTurns = spell.Duration; e.StormCloudDamage = Mathf.RoundToInt(spell.Power * battle.Settings.spellPower * battle.Settings.synchronizedPower); }
                    Feedback("Storm Cloud gathers over the enemy side.", Color.yellow); break;
                case SpellEffect.Current:
                    if (targets.Length > 0 && targets[0] != actor) { battle.Formation.Swap(actor, targets[0]); battle.StartCoroutine(battle.Formation.JumpIntoPosition(battle.Settings.formationJumpSeconds, battle.Settings.formationJumpHeight)); Feedback(actor.Name + " swaps places with " + targets[0].Name + ".", Color.cyan); }
                    break;
            }
        }
        void TryBurn(Combatant target, Spell spell, bool guaranteed = false)
        {
            if (guaranteed || Random.value <= spell.StatusChance) { target.BurnTurns = Mathf.Max(target.BurnTurns, spell.Duration); Feedback(target.Name + " is Burning!", new Color(1, .4f, .12f)); }
        }
        void TryWet(Combatant target, Spell spell) { if (Random.value <= spell.StatusChance) { target.WetTurns = Mathf.Max(target.WetTurns, spell.Duration); Feedback(target.Name + " is Wet.", Color.cyan); } }
        void ApplyShock(Combatant target, int duration)
        {
            bool wet = target.WetTurns > 0; target.ShockTurns = Mathf.Max(target.ShockTurns, duration + (wet ? 1 : 0));
            Feedback(wet ? "Wet amplifies Shock!" : target.Name + " is Shocked.", Color.yellow);
            if (wet)
            {
                var spread = battle.Enemies.FirstOrDefault(e => e.Alive && e != target && e.WetTurns > 0 && e.ShockTurns == 0);
                if (spread != null) { spread.ShockTurns = Mathf.Max(1, duration); battle.Log("Wet conducts Shock to " + spread.Name + "."); }
            }
        }
        void TriggerLiveWire(Combatant target)
        {
            if (target.LiveWireTurns <= 0 || target.LiveWireCharges <= 0) return;
            target.LiveWireCharges--; int extra = target.Hurt(target.LiveWireDamage);
            battle.Presentation.ShowDamage(target, extra); battle.Presentation.EnemyHit(target); battle.Log("Live Wire triggers for " + extra + " Electric damage.");
            if (!target.Alive) battle.EnemyDefeated(target);
        }
        void Heal(Combatant target, int amount)
        {
            int before = target.HP; target.Heal(amount); int restored = target.HP - before;
            battle.Presentation.ShowHealing(target, restored); if (restored > 0) Feedback(target.Name + " recovers " + restored + " HP.", Color.green);
        }
        void HealOverflow(Combatant target, int amount, int barrier)
        {
            int missing = target.MaxHP - target.HP; Heal(target, amount); int excess = Mathf.Max(0, amount - missing);
            if (excess > 0) { target.BubbleAbsorb += Mathf.Min(excess, barrier); Feedback("Overflow turns excess healing into a Bubble.", Color.cyan); }
        }
        void Revive(Combatant target, int amount)
        {
            int restored = target.Revive(Mathf.Max(1, amount));
            if (restored > 0) { battle.Presentation.ShowHealing(target, restored); Feedback(target.Name + " returns with " + restored + " HP.", Color.cyan); }
        }
        void Hurt(Combatant target, int amount)
        {
            int hit = target.Hurt(amount); battle.Presentation.ShowDamage(target, hit);
            if (!target.Alive && battle.Formation.ForceHuddle()) battle.StartCoroutine(battle.Formation.JumpIntoPosition(battle.Settings.formationJumpSeconds, battle.Settings.formationJumpHeight));
        }
        void Feedback(string message, Color color) { battle.Presentation.Say(message, color); battle.Log(message); }
        void ResetHeat(Combatant actor) { actor.HeatStacks = 0; }

        public void Item(bool mp) { if (battle.Ready && (mp ? Ally.Alive && Ethers > 0 : Potions > 0)) battle.Perform(ItemRoutine(battle.Active, Ally, mp)); }
        IEnumerator ItemRoutine(Combatant actor, Combatant ally, bool mp)
        {
            actor.Acted = true; ResetHeat(actor);
            if (mp)
            {
                Ethers--;
                int before = ally.MP;
                ally.MP = Mathf.Min(ally.MaxMP, ally.MP + battle.Settings.etherAmount);
                int restored = ally.MP - before;
                battle.Presentation.ShowManaRestored(ally, restored);
                battle.Log(ally.Name + " restores " + restored + " MP.");
            }
            else if (ally.Alive) { Potions--; Heal(ally, battle.Settings.healingAmount); }
            else { Potions--; Revive(ally, battle.Settings.healingAmount); }
            battle.Log(actor.Name + " uses " + (mp ? "Ether" : "Potion") + " on " + ally.Name); yield return new WaitForSeconds(.15f);
        }
        public void ChangeFormation(FormationKind kind, int[] order)
        {
            if (!battle.Ready || battle.Formation.ChangedThisPhase || battle.Formation.LockedByKnockout || order.Length != 3 || order.Distinct().Count() != 3 || order.Any(i => i < 0 || i > 2)) return;
            battle.Perform(Form(battle.Active, kind, (int[])order.Clone()));
        }
        IEnumerator Form(Combatant actor, FormationKind kind, int[] order)
        {
            actor.Acted = true; battle.Formation.ChangedThisPhase = true; battle.Formation.Kind = kind; battle.Formation.Order = order; ResetHeat(actor);
            battle.Log(actor.Name + " changes formation to " + kind); yield return battle.Formation.JumpIntoPosition(battle.Settings.formationJumpSeconds, battle.Settings.formationJumpHeight);
        }

        public string SyncBlockReason(Spell spell)
        {
            if (battle.Formation.Kind != FormationKind.Totem) return "Totem formation is required.";
            if (!battle.Ready) return "Wait for an available Party Phase action.";
            var top = battle.Formation.At(0); var middle = battle.Formation.At(1);
            if (battle.Active != top && battle.Active != middle) return "The acting member must be Top or Middle.";
            if (!top.Alive || !middle.Alive) return "Top and Middle must both be conscious.";
            if (top.Acted || middle.Acted) return "Top and Middle must both have an unused action.";
            if (!SpellLibrary.Synchronized(battle.Settings, top.Element, middle.Element).Contains(spell)) return "This move is not available for the current elemental pair.";
            int a = (spell.Cost + 1) / 2, b = spell.Cost / 2;
            if (top.MP < a || middle.MP < b) return top.Name + " needs " + a + " MP and " + middle.Name + " needs " + b + " MP.";
            if (spell.Target == SpellTarget.DownedAlly && !battle.Party.Any(c => !c.Alive)) return "No downed ally.";
            if ((spell.Target == SpellTarget.Enemy || spell.Target == SpellTarget.AllEnemies) && !battle.Enemies.Any(c => c.Alive)) return "No living enemies.";
            return "";
        }
        public bool CanSync(Spell spell) => string.IsNullOrEmpty(SyncBlockReason(spell));
        public void Sync(Spell spell, Combatant target = null) { if (CanSync(spell)) battle.Perform(SyncRoutine(spell, target)); }
        IEnumerator SyncRoutine(Spell spell, Combatant target)
        {
            var top = battle.Formation.At(0); var middle = battle.Formation.At(1);
            top.Acted = middle.Acted = true; top.MP -= (spell.Cost + 1) / 2; middle.MP -= spell.Cost / 2;
            battle.Log(spell.Name + " synchronized move (2 actions, " + spell.Cost + " MP)");
            if (target != null)
            {
                if (battle.Party.Contains(target)) battle.AllyTarget = battle.Party.IndexOf(target);
                else battle.EnemyTarget = battle.Enemies.IndexOf(target);
            }
            yield return battle.Presentation.Bolt(middle, top, new Color(0.8f, 0.95f, 1f));
            Feedback(top.Name + " + " + middle.Name + " synchronize " + spell.Name + "!", new Color(0.75f, 0.95f, 1f));
            yield return ResolveSpell(battle.Active, spell, 1, target);
        }

        public void ProcessEnemyPhaseStart()
        {
            ProcessStartOfSide(battle.Enemies);
        }
        public void ProcessPartyPhaseStart()
        {
            ProcessStartOfSide(battle.Party);
        }
        void ProcessStartOfSide(System.Collections.Generic.IEnumerable<Combatant> side)
        {
            foreach (var c in side.Where(c => c.Alive).ToArray())
            {
                if (c.BurnTurns > 0)
                {
                    int hit = c.Hurt(battle.Settings.burnDamagePerTurn); battle.Presentation.ShowDamage(c, hit); battle.Log(c.Name + " takes " + hit + " Burn damage.");
                    if (battle.Enemies.Contains(c) && !c.Alive) battle.EnemyDefeated(c);
                    if (battle.Party.Contains(c) && !c.Alive && battle.Formation.ForceHuddle()) battle.StartCoroutine(battle.Formation.JumpIntoPosition(battle.Settings.formationJumpSeconds, battle.Settings.formationJumpHeight));
                }
                if (c.RegenTurns > 0 && c.Alive) Heal(c, c.RegenAmount);
                if (c.StormCloudTurns > 0 && c.Alive)
                {
                    float damage = Mathf.Max(1, c.StormCloudDamage - EffectiveDefense(c));
                    if (c.WetTurns > 0) damage *= battle.Settings.wetElectricDamageMultiplier;
                    int hit = c.Hurt(Mathf.Max(1, Mathf.RoundToInt(damage)));
                    if (c.WetTurns > 0) c.ShockTurns = Mathf.Max(c.ShockTurns, 1);
                    battle.Presentation.ShowDamage(c, hit); battle.Presentation.EnemyHit(c); battle.Log("Storm Cloud hits " + c.Name + " for " + hit + ".");
                    if (battle.Enemies.Contains(c) && !c.Alive) battle.EnemyDefeated(c);
                    if (battle.Party.Contains(c) && !c.Alive && battle.Formation.ForceHuddle())
                        battle.StartCoroutine(battle.Formation.JumpIntoPosition(battle.Settings.formationJumpSeconds, battle.Settings.formationJumpHeight));
                }
            }
        }
        public void EndRoundStatuses()
        {
            foreach (var c in battle.Party.Concat(battle.Enemies))
            {
                Dec(ref c.BurnTurns); Dec(ref c.WetTurns); Dec(ref c.ShockTurns); Dec(ref c.DefenseDownTurns); Dec(ref c.AttackDownTurns);
                Dec(ref c.RegenTurns); Dec(ref c.FireBuffTurns); Dec(ref c.WaterBuffTurns); Dec(ref c.FlameWallTurns); Dec(ref c.StaticFieldTurns);
                Dec(ref c.LiveWireTurns); Dec(ref c.ConductorTurns); Dec(ref c.OverchargeTurns); Dec(ref c.QuickChargeTurns); Dec(ref c.LightningRodTurns);
                Dec(ref c.TidalGuardTurns); Dec(ref c.StormCloudTurns);
                if (c.PhoenixTurns > 0 && --c.PhoenixTurns == 0) c.PhoenixHP = 0;
                if (c.SurgePending) { c.SurgePending = false; battle.Formation.QueuePriority(c); } else Dec(ref c.SurgeTurns);
                if (c.AttackDownTurns == 0) c.AttackDownMultiplier = 1f;
                if (c.DefenseDownTurns == 0) c.DefenseDownAmount = 0;
                if (c.FireBuffTurns == 0) { c.FireDamageBonus = 0; c.HeatStacks = 0; }
                if (c.WaterBuffTurns == 0) c.WaterEffectBonus = 0;
            }
        }
        static void Dec(ref int value) { if (value > 0) value--; }
    }
}
