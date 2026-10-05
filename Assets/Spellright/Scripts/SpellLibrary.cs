using System;
using System.Linq;
using UnityEngine;

namespace Spellright
{
    [Serializable]
    public class Spell
    {
        public string Id;
        public string Name;
        public Element Element;
        public SpellTarget Target;
        public SpellEffect Effect;
        [Min(0)] public int Cost = 4;
        [Min(0)] public int Power = 20;
        [Range(0, 1)] public float StatusChance;
        [Min(0)] public int Duration;
        [Min(0)] public int StatusPower;
        [Min(0)] public int SecondaryPower;
        public bool Starter;
        public bool Synchronized;

        public string Label(int level = 1) => level > 1 ? Name + " Lv." + level : Name;
        public int ScaledPower(int level) => Mathf.RoundToInt(Power * (1f + Mathf.Max(0, level - 1) * 0.12f));
    }

    [Serializable]
    public class EnemyPrototype
    {
        public string Name = "Test Adept";
        public Element Element = Element.Fire;
        [Tooltip("A signature spell for this enemy type. Each spawned enemy also receives a second random spell of the same element. One of its two is randomly selected as the absorption reward.")]
        public string AbsorbableSpellId = "fire_combust";
        public int HP = 100;
        public int Defense = 2;
        public Color Color = Color.magenta;
    }

    public static class SpellLibrary
    {
        static Spell S(string id, string name, Element element, SpellTarget target, SpellEffect effect, int mp, int power = 0,
            float chance = 0, int duration = 0, int statusPower = 0, int secondary = 0, bool starter = false, bool sync = false)
            => new Spell { Id = id, Name = name, Element = element, Target = target, Effect = effect, Cost = mp, Power = power,
                StatusChance = chance, Duration = duration, StatusPower = statusPower, SecondaryPower = secondary, Starter = starter, Synchronized = sync };

        public static Spell[] CreateDefaults() => new[]
        {
            S("fire_strike", "Fire Strike", Element.Fire, SpellTarget.Enemy, SpellEffect.FireStrike, 3, 24, .15f, 2, 6, starter:true),
            S("fire_ignite", "Ignite", Element.Fire, SpellTarget.Enemy, SpellEffect.Ignite, 4, 2, 1, 3, 6, starter:true),
            S("fire_wildfire", "Wildfire", Element.Fire, SpellTarget.AllEnemies, SpellEffect.Wildfire, 8, 16, .2f, 2, 6),
            S("fire_combust", "Combust", Element.Fire, SpellTarget.Enemy, SpellEffect.Combust, 8, 26, .35f, 2, 6),
            S("fire_fuel", "Fuel the Flame", Element.Fire, SpellTarget.Self, SpellEffect.FuelFlame, 5, 0, 0, 3, 35, 20),
            S("fire_scorch", "Scorch", Element.Fire, SpellTarget.Enemy, SpellEffect.Scorch, 7, 31, 1, 2, 4),
            S("fire_backdraft", "Backdraft", Element.Fire, SpellTarget.Enemy, SpellEffect.Backdraft, 7, 40),
            S("fire_wall", "Flame Wall", Element.Fire, SpellTarget.Battlefield, SpellEffect.FlameWall, 8, 18, 0, 2),
            S("fire_heat_up", "Heat Up", Element.Fire, SpellTarget.Self, SpellEffect.HeatUp, 5, 0, 0, 3, 0, 10),
            S("fire_last_spark", "Last Spark", Element.Fire, SpellTarget.Enemy, SpellEffect.LastSpark, 8, 58),
            S("fire_inferno", "Inferno", Element.Fire, SpellTarget.AllEnemies, SpellEffect.Inferno, 14, 38, .55f, 3, 6),
            S("fire_phoenix", "Phoenix Flame", Element.Fire, SpellTarget.Self, SpellEffect.PhoenixFlame, 12, 0, 0, 3, 0, 40),

            S("water_strike", "Water Strike", Element.Water, SpellTarget.Enemy, SpellEffect.WaterStrike, 3, 22, .25f, 2, starter:true),
            S("water_mend", "Mend", Element.Water, SpellTarget.Ally, SpellEffect.Mend, 4, 40, starter:true),
            S("water_rain", "Healing Rain", Element.Water, SpellTarget.AllAllies, SpellEffect.HealingRain, 8, 24),
            S("water_cleanse", "Cleanse", Element.Water, SpellTarget.Ally, SpellEffect.Cleanse, 5),
            S("water_bubble", "Bubble", Element.Water, SpellTarget.Ally, SpellEffect.Bubble, 6, 5, .25f, 2),
            S("water_tidal_guard", "Tidal Guard", Element.Water, SpellTarget.AllAllies, SpellEffect.TidalGuard, 10, 0, 0, 2, 0, 35),
            S("water_undertow", "Undertow", Element.Water, SpellTarget.Enemy, SpellEffect.Undertow, 7, 15, 1, 2, 0, 65),
            S("water_current", "Current", Element.Water, SpellTarget.SwapAlly, SpellEffect.Current, 6),
            S("water_rejuvenate", "Rejuvenate", Element.Water, SpellTarget.Ally, SpellEffect.Rejuvenate, 7, 10, 0, 3),
            S("water_overflow", "Overflow", Element.Water, SpellTarget.Ally, SpellEffect.Overflow, 7, 30, 0, 0, 0, 24),
            S("water_high_tide", "High Tide", Element.Water, SpellTarget.Self, SpellEffect.HighTide, 7, 0, 0, 3, 0, 20),
            S("water_second_breath", "Second Breath", Element.Water, SpellTarget.DownedAlly, SpellEffect.SecondBreath, 13, 55),
            S("water_tsunami", "Tsunami", Element.Water, SpellTarget.AllEnemies, SpellEffect.Tsunami, 14, 34, .35f, 2),

            S("electric_arc", "Arc", Element.Electric, SpellTarget.AllEnemies, SpellEffect.Arc, 4, 20, .18f, 2, starter:true),
            S("electric_taser", "Taser", Element.Electric, SpellTarget.Enemy, SpellEffect.Taser, 4, 4, 1, 2, starter:true),
            S("electric_chain", "Chain Lightning", Element.Electric, SpellTarget.Enemy, SpellEffect.ChainLightning, 8, 34, .25f, 2),
            S("electric_overcharge", "Overcharge", Element.Electric, SpellTarget.Ally, SpellEffect.Overcharge, 7, 0, 0, 2, 40, 3),
            S("electric_quick_charge", "Quick Charge", Element.Electric, SpellTarget.Self, SpellEffect.QuickCharge, 4, 0, 0, 2, 0, 4),
            S("electric_static", "Static Field", Element.Electric, SpellTarget.Battlefield, SpellEffect.StaticField, 7, 16, 0, 2),
            S("electric_rod", "Lightning Rod", Element.Electric, SpellTarget.Self, SpellEffect.LightningRod, 6, 0, 0, 2),
            S("electric_surge", "Surge", Element.Electric, SpellTarget.Ally, SpellEffect.Surge, 7, 0, 0, 1),
            S("electric_short_circuit", "Short Circuit", Element.Electric, SpellTarget.Enemy, SpellEffect.ShortCircuit, 7, 8, 1, 2, 0, 55),
            S("electric_live_wire", "Live Wire", Element.Electric, SpellTarget.Enemy, SpellEffect.LiveWire, 7, 0, 0, 3, 0, 22),
            S("electric_conductor", "Conductor", Element.Electric, SpellTarget.Enemy, SpellEffect.Conductor, 7, 0, 0, 3, 0, 35),
            S("electric_thunderclap", "Thunderclap", Element.Electric, SpellTarget.AllEnemies, SpellEffect.Thunderclap, 12, 16, 1, 1),
            S("electric_lightning_strike", "Lightning Strike", Element.Electric, SpellTarget.Enemy, SpellEffect.LightningStrike, 14, 72),

            S("sync_steam_cloud", "Steam Cloud", Element.None, SpellTarget.Battlefield, SpellEffect.SteamCloud, 6, 0, 0, 2, 0, 55, sync:true),
            S("sync_pressure_burst", "Pressure Burst", Element.None, SpellTarget.Enemy, SpellEffect.PressureBurst, 8, 78, sync:true),
            S("sync_scald", "Scald", Element.None, SpellTarget.Enemy, SpellEffect.Scald, 7, 42, 1, 2, 7, sync:true),
            S("sync_conductive_wave", "Conductive Wave", Element.None, SpellTarget.AllEnemies, SpellEffect.ConductiveWave, 7, 26, 1, 2, sync:true),
            S("sync_storm_cloud", "Storm Cloud", Element.None, SpellTarget.Battlefield, SpellEffect.StormCloud, 8, 18, 1, 2, sync:true),
            S("sync_defibrillate", "Defibrillate", Element.None, SpellTarget.DownedAlly, SpellEffect.Defibrillate, 10, 38, sync:true),
            S("sync_plasma_bolt", "Plasma Bolt", Element.None, SpellTarget.Enemy, SpellEffect.PlasmaBolt, 9, 88, sync:true),
            S("sync_overheat", "Overheat", Element.None, SpellTarget.Enemy, SpellEffect.Overheat, 8, 36, 1, 2, 0, 55, sync:true),
            S("sync_flashfire", "Flashfire", Element.None, SpellTarget.AllEnemies, SpellEffect.Flashfire, 9, 32, sync:true)
        };

        public static Spell[] For(CombatSettings settings, Element element)
            => settings.spells.Where(s => s != null && s.Element == element && s.Starter).ToArray();
        public static Spell Find(CombatSettings settings, string id)
            => settings.spells.FirstOrDefault(s => s != null && s.Id == id);
        public static Spell[] Synchronized(CombatSettings settings, Element top, Element middle)
            => settings.spells.Where(s => s != null && s.Synchronized && Compatible(s.Effect, top, middle)).ToArray();
        static bool Compatible(SpellEffect effect, Element a, Element b)
        {
            if (a == b || a == Element.None || b == Element.None) return false;
            bool fw = (a == Element.Fire && b == Element.Water) || (a == Element.Water && b == Element.Fire);
            bool we = (a == Element.Water && b == Element.Electric) || (a == Element.Electric && b == Element.Water);
            bool fe = (a == Element.Fire && b == Element.Electric) || (a == Element.Electric && b == Element.Fire);
            if (fw) return effect == SpellEffect.SteamCloud || effect == SpellEffect.PressureBurst || effect == SpellEffect.Scald;
            if (we) return effect == SpellEffect.ConductiveWave || effect == SpellEffect.StormCloud || effect == SpellEffect.Defibrillate;
            return fe && (effect == SpellEffect.PlasmaBolt || effect == SpellEffect.Overheat || effect == SpellEffect.Flashfire);
        }
    }
}
