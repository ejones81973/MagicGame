using UnityEngine;

namespace Spellright
{
    [CreateAssetMenu(menuName = "Spellright/Combat Settings")]
    public class CombatSettings : ScriptableObject
    {
        [Header("Runtime visuals")]
        public Material runtimePrimitiveMaterial;

        public void EnsureSpellDefaults()
        {
            if (spells == null || spells.Length == 0) spells = SpellLibrary.CreateDefaults();
            if (enemies == null || enemies.Length == 0) enemies = CreateDefaultEnemies();
        }
        void OnValidate() { EnsureSpellDefaults(); }
        public static EnemyPrototype[] CreateDefaultEnemies() => new[]
        {
            new EnemyPrototype { Name = "Cinder Adept", AbsorbableSpellId = "fire_combust", HP = 100, Defense = 3, Color = new Color(.92f, .28f, .12f), Element = Element.Fire },
            new EnemyPrototype { Name = "Tide Adept", AbsorbableSpellId = "water_bubble", HP = 100, Defense = 2, Color = new Color(.12f, .5f, .95f), Element = Element.Water },
            new EnemyPrototype { Name = "Arc Adept", AbsorbableSpellId = "electric_chain", HP = 100, Defense = 2, Color = new Color(.95f, .75f, .12f), Element = Element.Electric }
        };
        [Header("Capsule movement")]
        [Min(0)] public float idleBounceHeight = 0.18f;
        [Min(0)] public float idleBounceFrequency = 1.6f;
        [Min(0)] public float enemyRecoilDistance = 0.4f;
        [Min(0.1f)] public float enemyRecoilSeconds = 0.35f;
        [Header("Party: HP / MP / Attack / Defense")]
        public Vector4 brim = new Vector4(120, 42, 22, 5);
        public Vector4 brooke = new Vector4(135, 48, 16, 8);
        public Vector4 blitz = new Vector4(110, 44, 19, 4);
        [Header("Enemies")]
        public int enemyHP = 100;
        [Min(2)] public int waveMinimumEnemies = 2;
        [Range(2, 5)] public int waveMaximumEnemies = 5;
        [Min(1)] public int minibossEveryWaves = 10;
        [Min(1f)] public float minibossHPMultiplier = 2.4f;
        [Min(0)] public int minibossDefenseBonus = 4;
        [Tooltip("Relative weights for enemy counts 2, 3, 4, and 5. The defaults favor smaller waves.")]
        public Vector4 waveEnemyCountWeights = new Vector4(4, 4, 1, 1);
        public int meleeDamage = 27, projectileDamage = 23, volleyDamage = 19;
        public float meleeApproachSeconds = 0.65f, meleeWindupSeconds = 0.6f;
        public float meleeStrikeSpeed = 7f, projectileSpeed = 5f;
        [Header("Multi-member volley pace")]
        [Min(0.1f)] public float volleyProjectileSpeed = 9f;
        [Min(0)] public float volleyWindupSeconds = 0.12f;
        [Min(0)] public float volleyRecoverySeconds = 0.15f;
        [Header("Melee presentation")]
        [Min(0)] public float partyMeleePauseSeconds = 0.28f;
        [Range(0, 0.9f)] public float meleeHopHeight = 0.55f;
        [Header("Counter windows in seconds before contact")]
        [Range(0.01f, 0.15f)] public float perfectWindow = 0.055f;
        [Range(0.06f, 0.3f)] public float successWindow = 0.16f;
        public float perfectDamage = 0.1f, successDamage = 0.45f, earlyDamage = 1.4f;
        [Header("Formations and protection")]
        public float huddleIncoming = 0.7f, huddleOutgoing = 0.8f;
        public float totemTopOutgoing = 1.3f, totemBottomIncoming = 1.25f;
        [Min(0.05f)] public float formationJumpSeconds = 0.42f, formationJumpHeight = 0.85f;
        [Header("Attack camera")]
        [Min(0.05f)] public float cameraMoveSeconds = 0.2f;
        [Min(1f)] public float cameraFocusSize = 7f;
        [Header("Drain minigame")]
        [Min(1)] public float drainDuration = 22f;
        [Range(10, 120)] public float drainGreenArcDegrees = 46f;
        [Min(1)] public float drainPointerDegreesPerSecond = 145f;
        [Min(1)] public float drainGreenDegreesPerSecond = 58f;
        [Min(0.1f)] public float drainFillPerSecond = 1.3f;
        [Min(0.01f)] public float drainLossPerSecond = 0.11f;
        public float shieldIncoming = 0.5f, bubbleIncoming = 0.65f, collateralDamage = 0.3f;
        [Header("Spells and items")]
        public Spell[] spells = SpellLibrary.CreateDefaults();
        public EnemyPrototype[] enemies = CreateDefaultEnemies();
        public float spellPower = 1f, synchronizedPower = 1.35f;
        public int healingAmount = 38, etherAmount = 22, startingPotions = 50, startingEthers = 50;
        [Header("Elemental status balance")]
        [Min(0)] public int burnDamagePerTurn = 6;
        [Min(0)] public int bubbleAbsorbAmount = 5;
        [Range(0, 1)] public float wetFireDamageMultiplier = .75f;
        [Min(1)] public float wetElectricDamageMultiplier = 1.3f;
        [Min(0)] public float shockAttackMultiplier = .65f;
        [Range(0, 1)] public float shockSkipChance = .2f;
        [Min(1)] public float backdraftDamageMultiplier = 1.5f;
        [Min(0)] public float multiTargetScale = 1f;
        [Min(0)] public float chainTargetScale = .75f;
        [Min(0)] public float guardedIncoming = .6f;
        public Spell FindSpell(string id) => SpellLibrary.Find(this, id);
    }
}
