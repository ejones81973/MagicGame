using UnityEngine;

namespace Spellright
{
    public enum Element { Fire, Water, Electric, None }
    public enum BattlePhase { Party, Enemy, Drain, Finished }
    public enum FormationKind { Line, Totem, Huddle, Spread }
    public enum CounterResult { Perfect, Successful, Early, Late, Unavailable }

    public enum SpellTarget { Enemy, AllEnemies, Ally, AllAllies, Self, DownedAlly, Battlefield, SwapAlly }
    public enum SpellEffect
    {
        Damage, WaterStrike, FireStrike, Ignite, Wildfire, Combust, FuelFlame, Scorch, Backdraft, FlameWall, HeatUp, LastSpark, Inferno, PhoenixFlame,
        Mend, HealingRain, Cleanse, Bubble, TidalGuard, Undertow, Current, Rejuvenate, Overflow, HighTide, SecondBreath, Tsunami,
        Arc, Taser, ChainLightning, Overcharge, QuickCharge, StaticField, LightningRod, Surge, ShortCircuit, LiveWire, Conductor, Thunderclap, LightningStrike,
        SteamCloud, PressureBurst, Scald, ConductiveWave, StormCloud, Defibrillate, PlasmaBolt, Overheat, Flashfire
    }

    public class Combatant
    {
        public readonly string Name;
        public readonly Element Element;
        public readonly int MaxHP, MaxMP, Attack, Defense;
        public int HP, MP;
        public bool Acted, Shielded, Bubbled, Weakened;
        public Transform View;
        public CapsuleMotion Motion;
        public Color Color;
        public string AbsorbableSpellId;
        public string EnemySpellIdA, EnemySpellIdB, EnemyLastSpellId;
        public int BurnTurns, WetTurns, ShockTurns, DefenseDownTurns, AttackDownTurns;
        public int RegenTurns, RegenAmount, FireBuffTurns, WaterBuffTurns, BubbleAbsorb, PhoenixTurns, PhoenixHP;
        public int FlameWallTurns, StaticFieldTurns, LiveWireTurns, LiveWireCharges, ConductorTurns, OverchargeTurns, QuickChargeTurns, LightningRodTurns, SurgeTurns;
        public int StormCloudTurns, StormCloudDamage, FlameWallDamage, StaticFieldDamage, LiveWireDamage;
        public int HeatStacks, ShockSkipTurns, TidalGuardTurns, LastAttackedRound = -1, LastAttackedBrimRound = -1;
        public float FireDamageBonus, WaterEffectBonus, DefenseDownAmount, AttackDownMultiplier = 1f, NextMagicBonus, NextMagicExtraMp, ConductorBonus;
        public float TidalGuardMultiplier = .6f;
        public int NextSpellCostReduction;
        public bool Overflow, SurgePending;
        public bool Alive => HP > 0;
        public bool CanAct => Alive && !Acted;
        public Combatant(string name, Element element, Vector4 stats, Color color, string absorbableSpellId = null)
        {
            Name = name; Element = element; Color = color; AbsorbableSpellId = absorbableSpellId;
            MaxHP = Mathf.RoundToInt(stats.x); MaxMP = Mathf.RoundToInt(stats.y);
            Attack = Mathf.RoundToInt(stats.z); Defense = Mathf.RoundToInt(stats.w);
            HP = MaxHP; MP = MaxMP;
        }
        public int Hurt(int damage) { int actual = Mathf.Min(HP, Mathf.Max(0, damage)); HP -= actual; return actual; }
        public void Heal(int amount) { if (Alive) HP = Mathf.Min(MaxHP, HP + amount); }
        public int Revive(int amount)
        {
            if (Alive) return 0;
            HP = Mathf.Clamp(amount, 1, MaxHP);
            Acted = false;
            return HP;
        }
        public void Cleanse()
        {
            BurnTurns = ShockTurns = DefenseDownTurns = AttackDownTurns = 0;
        }
        public string StatusText
        {
            get
            {
                var parts = new System.Collections.Generic.List<string>();
                if (BurnTurns > 0) parts.Add("Burn " + BurnTurns);
                if (WetTurns > 0) parts.Add("Wet " + WetTurns);
                if (ShockTurns > 0) parts.Add("Shock " + ShockTurns);
                if (DefenseDownTurns > 0) parts.Add("DEF↓ " + DefenseDownTurns);
                if (AttackDownTurns > 0) parts.Add("ATK↓ " + AttackDownTurns);
                if (RegenTurns > 0) parts.Add("Regen " + RegenTurns);
                if (BubbleAbsorb > 0) parts.Add("Bubble " + BubbleAbsorb);
                if (PhoenixTurns > 0) parts.Add("Phoenix");
                if (FireBuffTurns > 0) parts.Add("Fire+ " + FireBuffTurns);
                if (WaterBuffTurns > 0) parts.Add("Water+ " + WaterBuffTurns);
                if (FlameWallTurns > 0) parts.Add("Flame Wall");
                if (StaticFieldTurns > 0) parts.Add("Static Field");
                if (LiveWireTurns > 0) parts.Add("Live Wire");
                if (ConductorTurns > 0) parts.Add("Conductor");
                if (OverchargeTurns > 0) parts.Add("Overcharge");
                if (QuickChargeTurns > 0) parts.Add("Quick Charge");
                if (LightningRodTurns > 0) parts.Add("Rod");
                if (SurgeTurns > 0) parts.Add("Surge");
                if (TidalGuardTurns > 0) parts.Add("Guard " + TidalGuardTurns);
                if (StormCloudTurns > 0) parts.Add("Storm " + StormCloudTurns);
                return parts.Count == 0 ? "" : string.Join("  ", parts);
            }
        }
    }
}
