using System.Collections;
using System.Linq;
using UnityEngine;

namespace Spellright
{
    public class EnemyAttackSystem : MonoBehaviour
    {
        BattleFlow battle;
        public void Initialize(BattleFlow flow) { battle = flow; }
        public IEnumerator Act(Combatant enemy, int index)
        {
            if (enemy.ShockTurns > 0 && Random.value < battle.Settings.shockSkipChance)
            { battle.Log(enemy.Name + " is disrupted by Shock and loses its action!"); battle.Presentation.Say(enemy.Name + " is disrupted by Shock!", Color.yellow); battle.Presentation.ShowStatus(enemy, "SHOCKED!", Color.yellow); yield break; }
            bool melee = battle.Round % 3 == index;
            float weaken = (enemy.Weakened ? 0.55f : 1f) * (enemy.AttackDownTurns > 0 ? enemy.AttackDownMultiplier : 1f); enemy.Weakened = false;
            Spell[] loadout = new[] { battle.Settings.FindSpell(enemy.EnemySpellIdA), battle.Settings.FindSpell(enemy.EnemySpellIdB) }
                .Where(s => s != null && !IsForbidden(s)).Distinct().ToArray();

            // Preserve each enemy's melee turn. On ranged turns, use its configured
            // absorbable spell for real attacks or support instead of a generic orb.
            if (melee)
            {
                var target = battle.Formation.ChooseTarget(true);
                battle.Log(enemy.Name + " uses a close-range melee attack.");
                if (target != null) yield return Strike(enemy, target, true,
                    battle.Settings.meleeDamage * weaken, battle.Formation.Kind == FormationKind.Huddle);
                yield break;
            }

            Spell spell = ChooseSpell(enemy, loadout);
            if (spell != null && IsSupportSpell(spell))
            { yield return CastSupport(enemy, spell); enemy.EnemyLastSpellId = spell.Id; yield break; }

            if (spell != null && IsMultiTarget(spell))
            {
                yield return Volley(enemy, spell.Power * battle.Settings.spellPower * weaken * battle.Settings.multiTargetScale, spell);
                enemy.EnemyLastSpellId = spell.Id;
                yield break;
            }

            var chosen = ChooseTarget(enemy, spell);
            if (chosen == null) yield break;
            if (spell != null)
            {
                float power = Mathf.Max(1, spell.Power) * battle.Settings.spellPower * weaken;
                if (spell.Effect == SpellEffect.Backdraft) power *= battle.Settings.backdraftDamageMultiplier;
                if (spell.Effect == SpellEffect.LastSpark) power *= 1 + (float)(enemy.MaxHP - enemy.HP) / Mathf.Max(1, enemy.MaxHP);
                battle.Log(enemy.Name + " casts " + spell.Name + " at " + chosen.Name + ".");
                yield return Strike(enemy, chosen, false, power, battle.Formation.Kind == FormationKind.Huddle, spell);
                enemy.EnemyLastSpellId = spell.Id;
                yield break;
            }
            battle.Log(enemy.Name + " fires a basic " + enemy.Element + " projectile at " + chosen.Name + ".");
            yield return Strike(enemy, chosen, false, battle.Settings.projectileDamage * weaken,
                battle.Formation.Kind == FormationKind.Huddle, spell);
        }

        bool IsOffensive(Spell spell)
        {
            return spell.Target == SpellTarget.Enemy || spell.Target == SpellTarget.AllEnemies ||
                spell.Effect == SpellEffect.StormCloud || spell.Effect == SpellEffect.StaticField;
        }
        bool IsMultiTarget(Spell spell)
            => spell.Target == SpellTarget.AllEnemies || spell.Effect == SpellEffect.ChainLightning ||
                spell.Effect == SpellEffect.Wildfire || spell.Effect == SpellEffect.Tsunami;
        static bool IsForbidden(Spell spell)
            => spell == null || spell.Target == SpellTarget.DownedAlly || spell.Effect == SpellEffect.SecondBreath ||
                spell.Effect == SpellEffect.Defibrillate || spell.Effect == SpellEffect.PhoenixFlame || spell.Synchronized;
        bool IsSupportSpell(Spell spell)
        {
            switch (spell.Effect)
            {
                case SpellEffect.FuelFlame: case SpellEffect.HeatUp: case SpellEffect.FlameWall: case SpellEffect.HighTide:
                case SpellEffect.Mend: case SpellEffect.HealingRain: case SpellEffect.Cleanse: case SpellEffect.Bubble:
                case SpellEffect.TidalGuard: case SpellEffect.Current: case SpellEffect.Rejuvenate: case SpellEffect.Overflow:
                case SpellEffect.Overcharge: case SpellEffect.QuickCharge: case SpellEffect.StaticField:
                case SpellEffect.LightningRod: case SpellEffect.Surge: case SpellEffect.StormCloud: case SpellEffect.SteamCloud:
                    return true;
                default: return false;
            }
        }
        Spell ChooseSpell(Combatant enemy, Spell[] loadout)
        {
            if (loadout.Length == 0) return null;
            Spell selected = null;
            float best = float.NegativeInfinity;
            foreach (var spell in loadout)
            {
                float score = SpellScore(enemy, spell);
                if (spell.Id == enemy.EnemyLastSpellId) score -= 14f;
                if (score > best) { best = score; selected = spell; }
            }
            return best <= -500 ? null : selected;
        }
        float SpellScore(Combatant enemy, Spell spell)
        {
            if (IsSupportSpell(spell) && !IsSupportReady(enemy, spell)) return -1000;
            float score = IsSupportSpell(spell) ? 28 : 18 + spell.Power * .16f;
            if (spell.Effect == SpellEffect.Combust)
            {
                int burning = RangedTargets().Sum(c => c.BurnTurns);
                return burning > 0 ? 180 + burning * 8 : -1000;
            }
            if (spell.Effect == SpellEffect.Backdraft)
                return enemy.LastAttackedBrimRound == battle.Round - 1 ? 75 : -1000;
            var availableTargets = RangedTargets();
            bool partyWet = availableTargets.Any(c => c.WetTurns > 0);
            bool partyBurning = availableTargets.Any(c => c.BurnTurns > 0);
            if (spell.Effect == SpellEffect.Ignite && !partyBurning && HasEffect(enemy, SpellEffect.Combust)) score += 125;
            else if ((spell.Effect == SpellEffect.Ignite || spell.Effect == SpellEffect.FireStrike) && !partyBurning &&
                (spell.Effect == SpellEffect.Ignite || HasEffect(enemy, SpellEffect.Combust))) score += 58;
            if (spell.Effect == SpellEffect.Wildfire && partyBurning) score += 35;
            if (spell.Element == Element.Electric && partyWet) score += 32;
            if (spell.Effect == SpellEffect.Taser && !availableTargets.Any(c => c.ShockTurns > 0)) score += 42;
            if (spell.Effect == SpellEffect.Conductor && !availableTargets.Any(c => c.ConductorTurns > 0) &&
                battle.Enemies.Any(c => c.Alive && c.Element == Element.Electric && c != enemy)) score += 42;
            if (spell.Effect == SpellEffect.WaterStrike && !partyWet && battle.Enemies.Any(c => c.Alive && c.Element == Element.Electric)) score += 22;
            if (spell.Effect == SpellEffect.LastSpark) score += (1f - (float)enemy.HP / Mathf.Max(1, enemy.MaxHP)) * 30;
            if (IsSupportSpell(spell)) score += SupportValue(enemy, spell);
            return score;
        }
        bool HasEffect(Combatant enemy, SpellEffect effect)
            => new[] { enemy.EnemySpellIdA, enemy.EnemySpellIdB }.Select(battle.Settings.FindSpell).Any(s => s != null && s.Effect == effect);
        float SupportValue(Combatant caster, Spell spell)
        {
            var allies = battle.Enemies.Where(c => c.Alive).ToArray();
            var injured = allies.Select(c => 1f - (float)c.HP / Mathf.Max(1, c.MaxHP)).DefaultIfEmpty(0).Max();
            switch (spell.Effect)
            {
                case SpellEffect.Mend: case SpellEffect.HealingRain: case SpellEffect.Overflow: return 45 * injured;
                case SpellEffect.Rejuvenate: return 30 * injured;
                case SpellEffect.Cleanse: return 55;
                case SpellEffect.Bubble: return 30;
                case SpellEffect.FuelFlame: case SpellEffect.HeatUp: case SpellEffect.HighTide: case SpellEffect.Overcharge: case SpellEffect.QuickCharge: return HasOffensivePartner(caster, spell) ? 42 : 12;
                case SpellEffect.TidalGuard: case SpellEffect.FlameWall: case SpellEffect.StaticField: return 34;
                case SpellEffect.Surge: return 20;
                case SpellEffect.Current: return 8;
                case SpellEffect.LightningRod: return 12;
                case SpellEffect.StormCloud: return 42;
                case SpellEffect.SteamCloud: return 36;
                default: return 0;
            }
        }
        bool HasOffensivePartner(Combatant caster, Spell setup)
            => new[] { caster.EnemySpellIdA, caster.EnemySpellIdB }.Select(battle.Settings.FindSpell)
                .Any(s => s != null && s != setup && (s.Target == SpellTarget.Enemy || s.Target == SpellTarget.AllEnemies));
        Combatant ChooseTarget(Combatant caster, Spell spell)
        {
            var choices = RangedTargets();
            if (choices.Length == 0) return null;
            if (battle.Formation.Kind == FormationKind.Huddle) return battle.Formation.Front;
            if (spell != null && spell.Effect == SpellEffect.Combust)
                return choices.Where(c => c.BurnTurns > 0).OrderByDescending(c => c.BurnTurns).FirstOrDefault() ?? choices[0];
            if (spell != null && spell.Element == Element.Electric)
                return choices.OrderByDescending(c => c.WetTurns > 0 ? 3 : c.ConductorTurns > 0 ? 2 : c.ShockTurns > 0 ? 1 : 0).FirstOrDefault();
            return battle.Formation.ChooseTarget(false);
        }
        Combatant[] RangedTargets()
        {
            if (battle.Formation.Kind == FormationKind.Line)
                return battle.Formation.Front != null ? new[] { battle.Formation.Front } : new Combatant[0];
            if (battle.Formation.Kind == FormationKind.Totem)
                return new[] { battle.Formation.At(0), battle.Formation.At(1) }.Where(c => c.Alive).ToArray();
            return battle.Party.Where(c => c.Alive).ToArray();
        }
        bool IsSupportReady(Combatant caster, Spell spell)
        {
            var allies = battle.Enemies.Where(c => c.Alive).ToArray();
            switch (spell.Effect)
            {
                case SpellEffect.Bubble: return allies.Any(c => c != caster && c.BubbleAbsorb <= 0);
                case SpellEffect.Mend: case SpellEffect.HealingRain: case SpellEffect.Overflow:
                    return allies.Any(c => c.HP < c.MaxHP);
                case SpellEffect.Rejuvenate: return allies.Any(c => c.HP < c.MaxHP && c.RegenTurns <= 0);
                case SpellEffect.Cleanse: return allies.Any(c => c.BurnTurns > 0 || c.ShockTurns > 0 || c.AttackDownTurns > 0 || c.DefenseDownTurns > 0);
                case SpellEffect.TidalGuard: return allies.Any(c => c.TidalGuardTurns <= 0);
                case SpellEffect.FlameWall: return allies.Any(c => c.FlameWallTurns <= 0);
                case SpellEffect.StaticField: return allies.Any(c => c.StaticFieldTurns <= 0);
                case SpellEffect.FuelFlame: case SpellEffect.HeatUp: return caster.FireBuffTurns <= 0 && caster.HP > spell.SecondaryPower;
                case SpellEffect.HighTide: return caster.WaterBuffTurns <= 0;
                case SpellEffect.Overcharge: return caster.OverchargeTurns <= 0;
                case SpellEffect.QuickCharge: return caster.QuickChargeTurns <= 0;
                case SpellEffect.LightningRod: return caster.LightningRodTurns <= 0;
                case SpellEffect.Surge: return allies.Any(c => c != caster && c.SurgeTurns <= 0);
                case SpellEffect.Current: return battle.Party.Count(c => c.Alive) > 1 && !battle.Formation.LockedByKnockout;
                default: return false;
            }
        }
        IEnumerator CastSupport(Combatant caster, Spell spell)
        {
            var allies = battle.Enemies.Where(c => c.Alive).ToArray();
            var injured = allies.OrderBy(c => (float)c.HP / c.MaxHP).ToArray();
            Combatant target;
            switch (spell.Effect)
            {
                case SpellEffect.Bubble: target = injured.FirstOrDefault(c => c != caster && c.BubbleAbsorb <= 0) ?? (caster.BubbleAbsorb <= 0 ? caster : null); break;
                case SpellEffect.Cleanse: target = allies.FirstOrDefault(c => c.BurnTurns > 0 || c.ShockTurns > 0 || c.AttackDownTurns > 0 || c.DefenseDownTurns > 0); break;
                case SpellEffect.Rejuvenate: target = injured.FirstOrDefault(c => c.HP < c.MaxHP && c.RegenTurns <= 0); break;
                case SpellEffect.Overcharge: case SpellEffect.QuickCharge: case SpellEffect.LightningRod:
                case SpellEffect.FuelFlame: case SpellEffect.HeatUp: case SpellEffect.HighTide: target = caster; break;
                case SpellEffect.Surge: target = injured.FirstOrDefault(c => c != caster && c.SurgeTurns <= 0) ?? caster; break;
                case SpellEffect.Current:
                    yield return ReorderParty(caster, spell);
                    yield break;
                default: target = injured.FirstOrDefault(c => c.HP < c.MaxHP) ?? caster; break;
            }
            if (target == null) yield break;
            battle.Log(caster.Name + " casts " + spell.Name + " to support its allies.");
            yield return battle.Presentation.FocusAttack(new[] { caster, target }, battle.Settings);
            yield return battle.Presentation.Bolt(caster, target, new Color(.3f, .85f, 1f));
            switch (spell.Effect)
            {
                case SpellEffect.Bubble:
                    int bubble = spell.Power > 0 ? spell.Power : battle.Settings.bubbleAbsorbAmount;
                    target.BubbleAbsorb += bubble; target.Bubbled = true;
                    battle.Presentation.Say(target.Name + " gains a " + bubble + " HP Bubble.", Color.cyan);
                    break;
                case SpellEffect.Mend: case SpellEffect.HealingRain: case SpellEffect.Overflow:
                    foreach (var ally in spell.Effect == SpellEffect.HealingRain ? allies : new[] { injured.FirstOrDefault(c => c.HP < c.MaxHP) ?? caster })
                    {
                        int before = ally.HP; ally.Heal(Mathf.Max(1, spell.Power));
                        battle.Presentation.ShowHealing(ally, ally.HP - before);
                        if (spell.Effect == SpellEffect.Overflow && ally == target)
                        { int excess = Mathf.Max(0, spell.Power - (ally.MaxHP - before)); ally.BubbleAbsorb += Mathf.Min(excess, spell.SecondaryPower); ally.Bubbled |= ally.BubbleAbsorb > 0; }
                    }
                    break;
                case SpellEffect.Rejuvenate:
                    target.RegenTurns = spell.Duration; target.RegenAmount = spell.Power;
                    battle.Presentation.Say(target.Name + " begins regenerating.", Color.cyan); break;
                case SpellEffect.Cleanse:
                    target.Cleanse(); battle.Presentation.Say(target.Name + " is cleansed.", Color.cyan); break;
                case SpellEffect.TidalGuard:
                    foreach (var ally in allies) { ally.TidalGuardTurns = spell.Duration + 1; ally.TidalGuardMultiplier = Mathf.Min(battle.Settings.guardedIncoming, 1 - spell.StatusPower / 100f); }
                    battle.Presentation.Say("Tidal Guard protects the enemy group.", Color.cyan); break;
                case SpellEffect.FlameWall:
                    foreach (var ally in allies) { ally.FlameWallTurns = spell.Duration + 1; ally.FlameWallDamage = spell.Power; }
                    battle.Presentation.Say("Flame Wall surrounds the enemy group.", new Color(1, .45f, .2f)); break;
                case SpellEffect.StaticField:
                    foreach (var ally in allies) { ally.StaticFieldTurns = spell.Duration + 1; ally.StaticFieldDamage = spell.Power; }
                    battle.Presentation.Say("Static Field surrounds the enemy group.", Color.yellow); break;
                case SpellEffect.FuelFlame: case SpellEffect.HeatUp:
                    if (spell.Effect == SpellEffect.FuelFlame) caster.Hurt(Mathf.Max(1, spell.SecondaryPower));
                    caster.FireBuffTurns = spell.Duration + 1; caster.FireDamageBonus = spell.StatusPower / 100f;
                    battle.Presentation.Say(caster.Name + " strengthens its Fire magic.", new Color(1, .45f, .2f)); break;
                case SpellEffect.HighTide:
                    caster.WaterBuffTurns = spell.Duration + 1; caster.WaterEffectBonus = spell.StatusPower / 100f;
                    battle.Presentation.Say(caster.Name + " strengthens its Water magic.", Color.cyan); break;
                case SpellEffect.PhoenixFlame:
                    caster.PhoenixTurns = spell.Duration + 1; caster.PhoenixHP = Mathf.Max(1, caster.MaxHP * spell.SecondaryPower / 100);
                    battle.Presentation.Say(caster.Name + " prepares a Phoenix revival.", new Color(1, .55f, .18f)); break;
                case SpellEffect.Overcharge:
                    caster.OverchargeTurns = spell.Duration + 1; caster.NextMagicBonus = spell.StatusPower / 100f;
                    battle.Presentation.Say(caster.Name + " overcharges its next spell.", Color.yellow); break;
                case SpellEffect.QuickCharge:
                    caster.QuickChargeTurns = spell.Duration + 1; caster.OverchargeTurns = spell.Duration + 1;
                    caster.NextMagicBonus = Mathf.Max(caster.NextMagicBonus, .2f);
                    battle.Presentation.Say(caster.Name + " quickly charges its next spell.", Color.yellow); break;
                case SpellEffect.Conductor:
                    caster.ConductorTurns = spell.Duration + 1; caster.ConductorBonus = spell.SecondaryPower / 100f;
                    battle.Presentation.Say(caster.Name + " becomes a Conductor.", Color.yellow); break;
                case SpellEffect.LightningRod:
                    caster.LightningRodTurns = spell.Duration + 1;
                    battle.Presentation.Say(caster.Name + " draws attention with Lightning Rod.", Color.yellow); break;
                case SpellEffect.Surge:
                    target.SurgeTurns = spell.Duration + 1;
                    battle.Presentation.Say(target.Name + " will act first next wave of attacks.", Color.yellow); break;
                case SpellEffect.Current:
                    Vector3 position = caster.View.position;
                    caster.View.position = target.View.position;
                    target.View.position = position;
                    battle.Presentation.Say(caster.Name + " shifts places with " + target.Name + ".", Color.cyan); break;
                case SpellEffect.SteamCloud:
                    foreach (var member in battle.Party.Where(c => c.Alive))
                    { member.AttackDownTurns = Mathf.Max(member.AttackDownTurns, spell.Duration + 1); member.AttackDownMultiplier = spell.StatusPower / 100f; }
                    battle.Presentation.Say("Steam Cloud weakens the party.", Color.cyan); break;
                case SpellEffect.StormCloud:
                    foreach (var member in battle.Party.Where(c => c.Alive))
                    { member.StormCloudTurns = spell.Duration + 1; member.StormCloudDamage = Mathf.RoundToInt(spell.Power * battle.Settings.spellPower); }
                    battle.Presentation.Say("Storm Cloud gathers over the party.", Color.yellow); break;
            }
            battle.Presentation.Refresh(battle.Party.Concat(battle.Enemies));
            yield return new WaitForSeconds(.2f);
            yield return battle.Presentation.ReturnCamera(battle.Settings);
        }
        IEnumerator ReorderParty(Combatant caster, Spell spell)
        {
            var front = battle.Formation.At(0);
            var alternatives = Enumerable.Range(1, 2).Select(battle.Formation.At).Where(c => c.Alive).ToArray();
            if (!front.Alive || alternatives.Length == 0) yield break;
            var displaced = alternatives[Random.Range(0, alternatives.Length)];
            battle.Log(caster.Name + " casts " + spell.Name + " to reorder the party.");
            yield return battle.Presentation.FocusAttack(new[] { caster, front, displaced }, battle.Settings);
            yield return battle.Presentation.Bolt(caster, front, Color.cyan);
            yield return battle.Presentation.Bolt(caster, displaced, Color.cyan);
            battle.Formation.Swap(front, displaced);
            yield return battle.Formation.JumpIntoPosition(battle.Settings.formationJumpSeconds, battle.Settings.formationJumpHeight);
            battle.Presentation.Say(caster.Name + " shifts " + front.Name + " and " + displaced.Name + " in the party order!", Color.cyan);
            battle.Log(battle.Presentation.Feedback);
            battle.Presentation.Refresh(battle.Party.Concat(battle.Enemies));
            yield return new WaitForSeconds(.2f);
            yield return battle.Presentation.ReturnCamera(battle.Settings);
        }
        IEnumerator Move(Transform view, Vector3 end, float duration)
        {
            Vector3 start = view.position;
            for (float t = 0; t < duration; t += Time.deltaTime) { view.position = Vector3.Lerp(start, end, t / duration); yield return null; }
            view.position = end;
        }
        IEnumerator Volley(Combatant enemy, float power, Spell spell = null)
        {
            var targets = battle.Formation.MultiTargetTargets();
            if (targets.Length == 0) yield break;
            var settings = battle.Settings;
            var warningRing = battle.Presentation.CreateAttackRing(null, true, targets);
            battle.Log(enemy.Name + (spell != null ? " casts " + spell.Name : " uses Volley") + ": all targets hit together; no collision damage.");
            yield return battle.Presentation.FocusAttack(new[] { enemy }.Concat(targets), settings);
            battle.Counter.BeginGroup(targets, targets.Any(battle.Formation.CanCounter));
            foreach (var target in targets) target.Motion.SetBraced(true);
            yield return new WaitForSeconds(Mathf.Max(0, settings.volleyWindupSeconds));

            var projectiles = new Transform[targets.Length];
            var endpoints = new Vector3[targets.Length];
            Vector3 origin = enemy.View.position;
            float longestPath = 0;
            for (int i = 0; i < targets.Length; i++)
            {
                Vector3 towardEnemy = (origin - targets[i].View.position).normalized;
                endpoints[i] = targets[i].View.position + towardEnemy * 0.8f;
                longestPath = Mathf.Max(longestPath, Vector3.Distance(origin, endpoints[i]));
                projectiles[i] = battle.Presentation.Shape(spell != null ? spell.Name : "Volley projectile", PrimitiveType.Sphere,
                    origin, Vector3.one * 0.6f, enemy.Color);
            }
            // Different travel speeds synchronize physical surface contact for every defender.
            float duration = Mathf.Max(0.15f, longestPath / Mathf.Max(0.1f, settings.volleyProjectileSpeed));
            float elapsed = 0;
            float contactTime = Time.time;
            while (elapsed < duration)
            {
                yield return null;
                float remaining = duration - elapsed;
                elapsed = Mathf.Min(duration, elapsed + Time.deltaTime);
                for (int i = 0; i < projectiles.Length; i++)
                    projectiles[i].position = Vector3.Lerp(origin, endpoints[i], elapsed / duration);
                if (elapsed >= duration) contactTime = Time.time - Time.deltaTime + remaining;
            }
            CounterResult sharedResult = battle.Counter.Contact(contactTime, settings);
            bool sharePartyStatuses = battle.Formation.Kind == FormationKind.Totem || battle.Formation.Kind == FormationKind.Huddle;
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == battle.Party[0]) enemy.LastAttackedBrimRound = battle.Round;
                CounterResult result = battle.Formation.CanCounter(targets[i]) ? sharedResult : CounterResult.Unavailable;
                float targetPower = spell != null && spell.Effect == SpellEffect.ChainLightning
                    ? power * Mathf.Pow(settings.chainTargetScale, i) : power;
                if (spell != null) targetPower = AdjustSpellDamage(enemy, targets[i], spell, targetPower, sharePartyStatuses);
                int damage = Receive(targets[i], targetPower, result);
                if (spell != null) ApplySpellStatuses(enemy, targets[i], spell, sharePartyStatuses);
                battle.Presentation.Result(targets[i], result, damage);
                battle.Log(battle.Presentation.Feedback);
                Destroy(projectiles[i].gameObject);
            }
            if (warningRing) Destroy(warningRing.gameObject);
            battle.Presentation.Say("VOLLEY: " + sharedResult.ToString().ToUpper() + " — all targets hit together", Color.white);
            // Multi-member damage is already applied once to everyone: never run LineKnockback here.
            yield return new WaitForSeconds(Mathf.Max(0, settings.volleyRecoverySeconds));
            foreach (var target in targets) target.Motion.SetBraced(false);
            battle.Presentation.Refresh(battle.Party.Concat(battle.Enemies));
            yield return battle.Presentation.ReturnCamera(settings);
        }
        IEnumerator Strike(Combatant enemy, Combatant target, bool melee, float power, bool splash, Spell spell = null)
        {
            var settings = battle.Settings; var visuals = battle.Presentation;
            Vector3 home = enemy.View.position;
            var warningRing = visuals.CreateAttackRing(target, splash, battle.Party);
            battle.Log(enemy.Name + (melee ? " hops into " : " launches at ") + target.Name + (splash ? " (Huddle splash)" : ""));
            yield return visuals.FocusAttack(new[] { enemy, target }, settings);
            battle.Counter.Begin(target, battle.Formation.CanCounter(target));
            target.Motion.SetBraced(true);
            Transform attack = null;
            float contactTime = 0;
            if (melee)
            {
                enemy.Motion.SetBraced(true);
                yield return MeleeAnimation.Move(enemy.View, target.View.position + Vector3.right * 2.7f, settings.meleeApproachSeconds);
                yield return new WaitForSeconds(settings.meleeWindupSeconds);
                yield return MeleeAnimation.Hop(enemy, target, settings, time => contactTime = time);
            }
            else
            {
                yield return new WaitForSeconds(0.35f);
                Spell attackSpell = spell ?? battle.Settings.FindSpell(enemy.AbsorbableSpellId);
                attack = visuals.Shape(attackSpell != null ? attackSpell.Name : enemy.Element + " spell", PrimitiveType.Sphere, enemy.View.position, Vector3.one * 0.6f, enemy.Color);
            }
            float speed = Mathf.Max(0.1f, settings.projectileSpeed);
            // The capsule radius is .5 and the attack radius is .3. Contact is the first surface touch.
            // Sweep the final frame to recover the contact timestamp instead of quantizing it to frame rate.
            const float contactDistance = 0.8f;
            while (!melee)
            {
                float distance = Vector3.Distance(attack.position, target.View.position);
                float travel = speed * Time.deltaTime;
                if (distance - contactDistance <= travel)
                {
                    float remaining = Mathf.Max(0, distance - contactDistance);
                    attack.position = Vector3.MoveTowards(attack.position, target.View.position, remaining);
                    contactTime = Time.time - Time.deltaTime + remaining / speed;
                    break;
                }
                attack.position = Vector3.MoveTowards(attack.position, target.View.position, travel);
                yield return null;
            }
            bool wasFront = target == battle.Formation.Front;
            CounterResult result = battle.Counter.Contact(contactTime, settings);
            enemy.LastAttackedRound = battle.Round;
            if (target == battle.Party[0]) enemy.LastAttackedBrimRound = battle.Round;
            bool sharePartyStatuses = battle.Formation.Kind == FormationKind.Totem || battle.Formation.Kind == FormationKind.Huddle;
            if (spell != null) power = AdjustSpellDamage(enemy, target, spell, power, sharePartyStatuses);
            int damage = Receive(target, power, result);
            if (spell != null) ApplySpellStatuses(enemy, target, spell, sharePartyStatuses);
            if (melee) Retaliate(target, enemy);
            visuals.Result(target, result, damage);
            battle.Log(visuals.Feedback);
            if (attack) Destroy(attack.gameObject);
            if (warningRing) Destroy(warningRing.gameObject);
            if (splash)
            {
                foreach (var other in battle.Party.Where(c => c != target && c.Alive))
                { int hit = Receive(other, power * 0.6f, CounterResult.Unavailable); battle.Log("Splash: " + other.Name + " -" + hit); }
            }
            if (battle.Formation.Kind == FormationKind.Line && wasFront && result == CounterResult.Late)
                yield return LineKnockback(target, power);
            yield return new WaitForSeconds(0.55f);
            if (melee)
            {
                enemy.Motion.SetAttackLean(0);
                yield return MeleeAnimation.Move(enemy.View, home, 0.3f);
                enemy.Motion.SetBraced(false);
            }
            target.Motion.SetBraced(false);
            visuals.Refresh(battle.Party.Concat(battle.Enemies));
            yield return visuals.ReturnCamera(settings);
        }
        IEnumerator LineKnockback(Combatant front, float power)
        {
            // Keep the original defender even if the primary hit knocked them out.
            var chain = new[] { front }.Concat(battle.Formation.Order
                .Skip(battle.Formation.Slot(front) + 1).Select(i => battle.Party[i]).Where(c => c.Alive)).ToArray();
            var homes = chain.Select(c => c.View.position).ToArray();
            foreach (var member in chain) member.Motion.SetBraced(true);

            for (int next = 1; next < chain.Length; next++)
            {
                // Move the already-contacting capsules together. The front does not
                // pass through Middle to reach Rear: Middle carries the impact onward.
                float shift = chain[next].View.position.x + 1f - chain[next - 1].View.position.x;
                yield return ShiftChain(chain, next, Vector3.right * shift, 0.18f);
                int hit = Receive(chain[next], power * battle.Settings.collateralDamage, CounterResult.Unavailable);
                battle.Presentation.CollisionHit(chain[next]);
                battle.Log(chain[next - 1].Name + " bumps " + chain[next].Name + ": -" + hit + " HP");
                yield return new WaitForSeconds(0.08f);
            }
            yield return ShiftChain(chain, chain.Length, Vector3.left * 0.25f, 0.12f);
            yield return new WaitForSeconds(0.12f);
            var displaced = chain.Select(c => c.View.position).ToArray();
            for (float elapsed = 0; elapsed < 0.3f; elapsed += Time.deltaTime)
            {
                for (int i = 0; i < chain.Length; i++)
                    chain[i].View.position = Vector3.Lerp(displaced[i], homes[i], Mathf.SmoothStep(0, 1, elapsed / 0.3f));
                yield return null;
            }
            for (int i = 0; i < chain.Length; i++)
            {
                chain[i].View.position = homes[i];
                if (chain[i] != front) chain[i].Motion.SetBraced(false);
            }
        }
        IEnumerator ShiftChain(Combatant[] chain, int count, Vector3 shift, float seconds)
        {
            var starts = chain.Take(count).Select(c => c.View.position).ToArray();
            for (float elapsed = 0; elapsed < seconds; elapsed += Time.deltaTime)
            {
                for (int i = 0; i < count; i++) chain[i].View.position = starts[i] + shift * (elapsed / seconds);
                yield return null;
            }
            for (int i = 0; i < count; i++) chain[i].View.position = starts[i] + shift;
        }
        void Retaliate(Combatant target, Combatant enemy)
        {
            if (!battle.Party.Any(c => c.FlameWallTurns > 0 || c.StaticFieldTurns > 0)) return;
            int damage = battle.Party.Max(c => c.FlameWallTurns > 0 ? c.FlameWallDamage : 0) +
                battle.Party.Max(c => c.StaticFieldTurns > 0 ? c.StaticFieldDamage : 0);
            if (damage <= 0 || !enemy.Alive) return;
            int hit = enemy.Hurt(damage);
            if (battle.Party.Any(c => c.StaticFieldTurns > 0)) enemy.ShockTurns = Mathf.Max(enemy.ShockTurns, 1);
            battle.Presentation.ShowDamage(enemy, hit); battle.Presentation.EnemyHit(enemy);
            battle.Log("Melee trigger fields hit " + enemy.Name + " for " + hit + ".");
            if (!enemy.Alive) battle.EnemyDefeated(enemy);
        }
        float AdjustSpellDamage(Combatant caster, Combatant target, Spell spell, float power, bool sharePartyStatuses)
        {
            if (spell.Effect == SpellEffect.Combust && target.BurnTurns > 0)
            { power += target.BurnTurns * battle.Settings.burnDamagePerTurn; target.BurnTurns = 0; }
            if (spell.Element == Element.Fire)
            {
                power *= 1 + caster.FireDamageBonus;
                if (target.WetTurns > 0)
                {
                    power *= battle.Settings.wetFireDamageMultiplier; target.WetTurns = 0;
                    ApplyPartyStatus(target, sharePartyStatuses, member =>
                    {
                        member.AttackDownTurns = Mathf.Max(member.AttackDownTurns, 2);
                        member.AttackDownMultiplier = Mathf.Min(member.AttackDownMultiplier, .85f);
                    });
                    battle.Presentation.Say("Wet + Fire bursts into steam.", Color.cyan);
                }
            }
            if (spell.Element == Element.Water) power *= 1 + caster.WaterEffectBonus;
            if (spell.Element == Element.Electric && target.WetTurns > 0) power *= battle.Settings.wetElectricDamageMultiplier;
            if (spell.Element == Element.Electric && target.ShockTurns > 0) power *= 1.15f;
            if (spell.Element == Element.Electric && target.ConductorTurns > 0) power *= 1 + target.ConductorBonus;
            if (caster.ShockTurns > 0) power *= battle.Settings.shockAttackMultiplier;
            if (caster.OverchargeTurns > 0) power *= 1 + caster.NextMagicBonus;
            return Mathf.Max(1, power);
        }
        void ApplySpellStatuses(Combatant caster, Combatant target, Spell spell, bool sharePartyStatuses)
        {
            if (!target.Alive) return;
            bool procs = spell.StatusChance >= 1 || Random.value <= spell.StatusChance;
            if ((spell.Effect == SpellEffect.FireStrike || spell.Effect == SpellEffect.Ignite || spell.Effect == SpellEffect.Wildfire || spell.Effect == SpellEffect.Combust ||
                spell.Effect == SpellEffect.Scorch || spell.Effect == SpellEffect.Inferno) && procs)
            {
                ApplyPartyStatus(target, sharePartyStatuses, member => member.BurnTurns = Mathf.Max(member.BurnTurns, spell.Duration + 1));
                battle.Presentation.Say(target.Name + " is Burning!", new Color(1, .4f, .12f));
            }
            if ((spell.Effect == SpellEffect.WaterStrike || spell.Effect == SpellEffect.Tsunami ||
                spell.Element == Element.Water && spell.StatusChance > 0) && procs)
            {
                ApplyPartyStatus(target, sharePartyStatuses, member => member.WetTurns = Mathf.Max(member.WetTurns, spell.Duration + 1));
                battle.Presentation.Say(target.Name + " is Wet.", Color.cyan);
            }
            if (spell.Element == Element.Electric && procs && (spell.StatusChance > 0 || spell.Effect == SpellEffect.Taser || spell.Effect == SpellEffect.Thunderclap))
            {
                ApplyPartyStatus(target, sharePartyStatuses, member => member.ShockTurns = Mathf.Max(member.ShockTurns, spell.Duration + 1 + (member.WetTurns > 0 ? 1 : 0)));
                battle.Presentation.Say(target.WetTurns > 0 ? "Wet amplifies Shock!" : target.Name + " is Shocked.", Color.yellow);
                var conductor = battle.Party.FirstOrDefault(c => c.Alive && c != target && c.WetTurns > 0 && c.ShockTurns == 0);
                if (conductor != null) { conductor.ShockTurns = Mathf.Max(1, spell.Duration + 1); battle.Log("Wet conducts Shock to " + conductor.Name + "."); }
            }
            if (spell.Effect == SpellEffect.Scorch)
            { ApplyPartyStatus(target, sharePartyStatuses, member => { member.DefenseDownTurns = spell.Duration + 1; member.DefenseDownAmount = spell.StatusPower; }); }
            if (spell.Effect == SpellEffect.Undertow || spell.Effect == SpellEffect.ShortCircuit || spell.Effect == SpellEffect.Overheat || spell.Effect == SpellEffect.SteamCloud)
            {
                ApplyPartyStatus(target, sharePartyStatuses, member =>
                {
                    member.AttackDownTurns = spell.Duration + 1;
                    member.AttackDownMultiplier = spell.SecondaryPower > 0 ? spell.SecondaryPower / 100f : spell.StatusPower > 0 ? spell.StatusPower / 100f : battle.Settings.shockAttackMultiplier;
                });
            }
            if (spell.Effect == SpellEffect.Conductor)
            { target.ConductorTurns = spell.Duration; target.ConductorBonus = spell.SecondaryPower / 100f; }
            if (spell.Effect == SpellEffect.LiveWire)
            { target.LiveWireTurns = spell.Duration; target.LiveWireCharges = 1; target.LiveWireDamage = spell.SecondaryPower; }
        }
        void ApplyPartyStatus(Combatant target, bool sharePartyStatuses, System.Action<Combatant> apply)
        {
            if (sharePartyStatuses && battle.Party.Contains(target))
            {
                foreach (var member in battle.Party.Where(c => c.Alive)) apply(member);
                battle.Log(target.Name + "'s status spreads through the formation.");
            }
            else apply(target);
        }
        int Receive(Combatant target, float raw, CounterResult result)
        {
            var s = battle.Settings;
            float counter = result == CounterResult.Perfect ? s.perfectDamage : result == CounterResult.Successful ? s.successDamage : result == CounterResult.Early ? s.earlyDamage : 1;
            float protection = (target.Shielded ? s.shieldIncoming : 1) * (target.Bubbled && target.BubbleAbsorb <= 0 ? s.bubbleIncoming : 1) *
                (target.TidalGuardTurns > 0 ? target.TidalGuardMultiplier : 1);
            int defense = Mathf.Max(0, target.Defense - (target.DefenseDownTurns > 0 ? Mathf.RoundToInt(target.DefenseDownAmount) : 0));
            int damage = Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(1, raw - defense) * battle.Formation.Incoming(target) * protection * counter));
            if (target.BubbleAbsorb > 0) { int absorbed = Mathf.Min(target.BubbleAbsorb, damage); target.BubbleAbsorb -= absorbed; damage -= absorbed; if (target.BubbleAbsorb == 0) target.Bubbled = false; battle.Log(target.Name + "'s Bubble absorbs " + absorbed + " damage."); }
            if (damage > 0) damage = target.Hurt(damage);
            if (battle.Party.Contains(target) && target.LiveWireTurns > 0 && target.LiveWireCharges > 0)
            {
                target.LiveWireCharges--;
                int extra = target.Hurt(target.LiveWireDamage);
                battle.Presentation.ShowDamage(target, extra);
                battle.Log("Live Wire triggers against " + target.Name + " for " + extra + " extra damage.");
                damage += extra;
            }
            if (!target.Alive && battle.Party.Contains(target) && battle.Formation.ForceHuddle())
                StartCoroutine(battle.Formation.JumpIntoPosition(battle.Settings.formationJumpSeconds, battle.Settings.formationJumpHeight));
            if (!target.Alive && target.PhoenixTurns > 0)
            {
                int revived = target.Revive(target.PhoenixHP); target.PhoenixTurns = 0; target.PhoenixHP = 0;
                battle.Presentation.ShowHealing(target, revived); battle.Log(target.Name + " revives through Phoenix Flame!");
            }
            battle.Presentation.ShowDamage(target, damage);
            return damage;
        }
    }
}

