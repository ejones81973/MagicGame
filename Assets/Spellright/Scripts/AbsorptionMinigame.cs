using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Spellright
{
    public class AbsorptionMinigame : MonoBehaviour
    {
        public float Fill { get; private set; }
        public float Remaining { get; private set; }
        public float PointerAngle { get; private set; }
        public float GreenAngle { get; private set; }
        public int Direction { get; private set; } = 1;
        public Combatant Enemy { get; private set; }
        public Combatant MatchingHero { get; private set; }
        public bool Active { get; private set; }
        public bool Won { get; private set; }
        BattleFlow battle;

        public void Initialize(BattleFlow flow) { battle = flow; }
        public void Reverse() { if (Active) Direction *= -1; }
        public void Update()
        {
            if (!Active) return;
            Remaining = Mathf.Max(0, Remaining - Time.deltaTime);
            float dt = Time.deltaTime;
            GreenAngle = Mathf.Repeat(GreenAngle + battle.Settings.drainGreenDegreesPerSecond * dt, 360f);
            PointerAngle = Mathf.Repeat(PointerAngle + Direction * battle.Settings.drainPointerDegreesPerSecond * dt, 360f);
            if (Mathf.Abs(Mathf.DeltaAngle(PointerAngle, GreenAngle)) <= battle.Settings.drainGreenArcDegrees * 0.5f)
                Fill = Mathf.Min(1, Fill + battle.Settings.drainFillPerSecond * dt);
            else Fill = Mathf.Max(0, Fill - battle.Settings.drainLossPerSecond * dt);
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) Reverse();
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Space)) Reverse();
#endif
            if (Fill >= 1) { Won = true; Active = false; }
            else if (Remaining <= 0) { Won = false; Active = false; }
        }

        public IEnumerator Run(Combatant enemy)
        {
            Enemy = enemy;
            Spell reward = battle.Settings.FindSpell(enemy.AbsorbableSpellId);
            if (reward == null)
            {
                battle.Presentation.Say(enemy.Name + " has no valid spell reward configured.", new Color(1, .45f, .35f));
                yield return Escape(enemy);
                yield break;
            }
            MatchingHero = null;
            foreach (var member in battle.Party)
                if (member.Alive && member.Element == reward.Element) { MatchingHero = member; break; }
            if (MatchingHero == null)
            {
                Won = false;
                battle.Presentation.Say("No conscious " + reward.Element + " user can absorb " + reward.Name + ". The essence escapes!", new Color(1, .45f, .35f));
                yield return Escape(enemy);
                yield break;
            }

            Fill = 0.08f; Remaining = battle.Settings.drainDuration; PointerAngle = 6f; GreenAngle = 0f; Direction = 1;
            Won = false; Active = true;
            battle.Presentation.Say(MatchingHero.Name + " is drawing in " + reward.Name + "...", new Color(.45f, 1f, .55f));
            while (Active) yield return null;

            if (Won)
            {
                int previous = battle.Absorption.Level(reward.Id);
                int level = battle.Absorption.Absorb(reward.Id);
                battle.Presentation.Say(previous == 0
                    ? MatchingHero.Name + " learned " + reward.Name + "!"
                    : reward.Name + " strengthened to Lv." + level + "!", new Color(.45f, 1f, .55f));
                battle.Log(previous == 0 ? MatchingHero.Name + " learned " + reward.Name : reward.Name + " reached Lv." + level);
                yield return new WaitForSeconds(.9f);
                if (enemy.View) enemy.View.gameObject.SetActive(false);
            }
            else
            {
                battle.Presentation.Say("Drain failed! " + enemy.Name + " escapes with " + reward.Name + ".", new Color(1f, .45f, .35f));
                battle.Log(enemy.Name + " escaped; " + reward.Name + " was lost.");
                yield return Escape(enemy);
            }
        }

        IEnumerator Escape(Combatant enemy)
        {
            if (enemy.View)
            {
                Vector3 start = enemy.View.position;
                Vector3 end = start + Vector3.right * 8f + Vector3.up * 1.4f;
                for (float t = 0; t < .7f; t += Time.deltaTime)
                {
                    if (enemy.View) enemy.View.position = Vector3.Lerp(start, end, t / .7f);
                    yield return null;
                }
                if (enemy.View) enemy.View.gameObject.SetActive(false);
            }
            else yield return new WaitForSeconds(.2f);
        }
    }
}
