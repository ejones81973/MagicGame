using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;

namespace Spellright.Tests
{
    public class CombatTests
    {
        static IEnumerator Wait(float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end) yield return null;
        }
        [TestCase(false, 0, CounterResult.Late)]
        [TestCase(true, 0.02f, CounterResult.Perfect)]
        [TestCase(true, 0.1f, CounterResult.Successful)]
        [TestCase(true, 0.3f, CounterResult.Early)]
        [TestCase(true, -0.02f, CounterResult.Late)]
        public void CounterUsesContact(bool pressed, float before, CounterResult expected)
        { Assert.AreEqual(expected, CounterSystem.Evaluate(pressed, 1 - before, 1, 0.055f, 0.16f)); }

        [Test]
        public void FormationRolesFollowOrder()
        {
            var settings = ScriptableObject.CreateInstance<CombatSettings>();
            var party = new List<Combatant> {
                new Combatant("Brim", Element.Fire, settings.brim, Color.red),
                new Combatant("Brooke", Element.Water, settings.brooke, Color.blue),
                new Combatant("Blitz", Element.Electric, settings.blitz, Color.yellow) };
            var formation = new FormationSystem(settings, party) { Kind = FormationKind.Totem, Order = new[] { 2, 0, 1 } };
            Assert.AreSame(party[2], formation.At(0)); Assert.IsFalse(formation.CanCounter(party[1]));
            Assert.AreEqual(settings.totemTopOutgoing, formation.Outgoing(party[2]));
            formation.Kind = FormationKind.Huddle;
            Assert.AreEqual(settings.huddleIncoming, formation.Incoming(party[0]));
            formation.Kind = FormationKind.Line; party[2].HP = 0;
            Assert.AreSame(party[0], formation.Front);
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void DefaultBubbleIsFiveDamageAndRewardSpellCanBeAssignedToEnemy()
        {
            var settings = ScriptableObject.CreateInstance<CombatSettings>();
            settings.EnsureSpellDefaults();
            Assert.AreEqual(5, settings.FindSpell("water_bubble").Power);
            Assert.AreEqual(5, settings.bubbleAbsorbAmount);
            var tide = settings.enemies.First(enemy => enemy.Name == "Tide Adept");
            Assert.AreEqual("water_bubble", tide.AbsorbableSpellId);
            Object.DestroyImmediate(settings);
        }

        [UnityTest]
        public IEnumerator EncounterCommandsAndPhases()
        {
            EditorSceneManager.OpenScene("Assets/Spellright/Scenes/SpellrightCombat.unity");
            yield return new EnterPlayMode();
            yield return null;
            var battle = Object.FindAnyObjectByType<BattleFlow>();
            Assert.NotNull(battle); Assert.AreEqual(3, battle.Party.Count); Assert.That(battle.Enemies.Count, Is.InRange(2, 5)); Assert.AreEqual(1, battle.WaveNumber);
            foreach (var enemy in battle.Enemies)
            {
                Assert.IsNotEmpty(enemy.EnemySpellIdA); Assert.IsNotEmpty(enemy.EnemySpellIdB);
                Assert.AreNotEqual(enemy.EnemySpellIdA, enemy.EnemySpellIdB);
                Assert.IsTrue(enemy.AbsorbableSpellId == enemy.EnemySpellIdA || enemy.AbsorbableSpellId == enemy.EnemySpellIdB);
            }
            // Brim changes to Totem, spending the Top member's action for this phase.
            battle.Actions.ChangeFormation(FormationKind.Totem, new[] { 0, 1, 2 });
            yield return Wait(0.25f);
            Assert.IsTrue(battle.Party[0].Acted); Assert.IsTrue(battle.Formation.ChangedThisPhase);
            battle.Actions.ChangeFormation(FormationKind.Spread, new[] { 0, 1, 2 });
            Assert.AreEqual(FormationKind.Totem, battle.Formation.Kind);
            var fireWater = SpellLibrary.Synchronized(battle.Settings, Element.Fire, Element.Water);
            Assert.AreEqual(3, fireWater.Length);
            Assert.IsFalse(battle.Actions.CanSync(fireWater[0])); // Top Brim spent the formation action.
            battle.Actions.Shield(); // Middle Brooke spends an action; Totem Bottom cannot act.
            yield return Wait(0.25f);
            yield return Wait(10);
            Assert.AreEqual(BattlePhase.Party, battle.Phase); Assert.AreEqual(2, battle.Round);
            Assert.IsFalse(battle.Formation.ChangedThisPhase);
            Assert.Less(battle.Party[0].HP + battle.Party[1].HP + battle.Party[2].HP, 365);
            Assert.IsTrue(battle.Actions.CanSync(fireWater[0]));
            int topMP = battle.Party[0].MP, middleMP = battle.Party[1].MP;
            battle.Actions.Sync(fireWater[0], battle.Enemies[0]);
            yield return Wait(2);
            Assert.IsTrue(battle.Party[0].Acted && battle.Party[1].Acted);
            Assert.AreEqual(topMP - (fireWater[0].Cost + 1) / 2, battle.Party[0].MP);
            Assert.AreEqual(middleMP - fireWater[0].Cost / 2, battle.Party[1].MP);
            yield return Wait(10);
            Assert.AreEqual(BattlePhase.Party, battle.Phase); Assert.AreEqual(3, battle.Round);
            battle.Party[0].HP = 50; battle.AllyTarget = 0;
            battle.Actions.Item(false); yield return Wait(0.25f);
            Assert.AreEqual(88, battle.Party[0].HP); Assert.AreEqual(4, battle.Actions.Potions);
            battle.Finish("FLED"); Assert.AreEqual(BattlePhase.Finished, battle.Phase);
            Object.FindAnyObjectByType<PrototypeBootstrap>().Restart(); yield return null;
            battle = Object.FindAnyObjectByType<BattleFlow>();
            Assert.AreEqual(BattlePhase.Party, battle.Phase); Assert.AreEqual(1, battle.Round); Assert.AreEqual(1, battle.WaveNumber);
            Assert.AreEqual(battle.Party[0].MaxHP, battle.Party[0].HP);
            yield return new ExitPlayMode();
        }
    }
}

