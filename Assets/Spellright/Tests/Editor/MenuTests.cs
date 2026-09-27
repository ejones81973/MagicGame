using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace Spellright.Tests
{
    public class MenuTests
    {
        [Test]
        public void DirectionalNavigationFollowsGridAndSkipsDisabledChoices()
        {
            var choices = new List<MenuChoice>();
            for (int i = 0; i < 6; i++) choices.Add(new MenuChoice(new Rect(i % 3 * 100, i / 3 * 60, 90, 50), "", i != 4, null));
            Assert.AreEqual(1, MenuNavigation.Next(choices, 0, 1, 0));
            Assert.AreEqual(3, MenuNavigation.Next(choices, 0, 0, 1));
            Assert.AreEqual(5, MenuNavigation.Next(choices, 3, 1, 0));
            Assert.AreEqual(2, MenuNavigation.Next(choices, 5, 0, -1));
            Assert.AreEqual(0, MenuNavigation.Next(choices, 0, -1, 0));
            choices.RemoveRange(3, 3);
            Assert.AreEqual(1, MenuNavigation.Next(choices, 0, 0, 1));
            Assert.AreEqual(0, MenuNavigation.Next(choices, 1, 0, -1));
        }

        static IEnumerator Wait(float seconds)
        {
            float until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until) yield return null;
        }

        [UnityTest]
        public IEnumerator SelectionCancellationAndRecipientCosts()
        {
            EditorSceneManager.OpenScene("Assets/Spellright/Scenes/SpellrightCombat.unity");
            yield return new EnterPlayMode();
            yield return null;
            var battle = Object.FindAnyObjectByType<BattleFlow>();
            var menu = new BattleMenu(battle);
            menu.Activate(3); // Item command
            menu.Activate(0); // Potion, then recipient
            Assert.AreEqual(CombatScreen.AllyTarget, menu.Screen);
            menu.Back();
            Assert.AreEqual(CombatScreen.Items, menu.Screen);
            Assert.AreEqual(5, battle.Actions.Potions);
            Assert.IsFalse(battle.Party[0].Acted);
            menu.Back(); menu.Activate(0); // Melee targeting
            menu.Move(1, 0);
            Assert.IsTrue(menu.IsMarked(battle.Enemies[1]));
            menu.Back();
            Assert.IsFalse(battle.Party[0].Acted);
            menu.Activate(1); menu.Activate(0); // Fireball targeting
            menu.Back(); menu.Back();
            Assert.AreEqual(battle.Party[0].MaxMP, battle.Party[0].MP);

            // A spent hero can receive an item without becoming the active actor.
            battle.Party[1].Acted = true; battle.Party[1].HP = 50;
            menu.Activate(3); menu.Activate(0);
            menu.ClickFighter(battle.Party[1]);
            yield return Wait(0.3f);
            Assert.AreEqual(88, battle.Party[1].HP);
            Assert.IsTrue(battle.Party[0].Acted);
            Assert.AreEqual(4, battle.Actions.Potions);
            Assert.AreEqual(2, battle.ActiveIndex);

            // Enemy target confirmation must hit the chosen enemy only.
            int firstHP = battle.Enemies[0].HP, secondHP = battle.Enemies[1].HP;
            battle.Enemies[1].BurnTurns = 2;
            menu.Activate(0);
            Assert.IsTrue(menu.Choices().Any(choice => choice.Text.Contains("Burn 2")));
            menu.ClickFighter(battle.Enemies[1]);
            yield return Wait(1.3f);
            Assert.AreEqual(firstHP, battle.Enemies[0].HP);
            Assert.Less(battle.Enemies[1].HP, secondHP);
            yield return new ExitPlayMode();
        }
    }
}
