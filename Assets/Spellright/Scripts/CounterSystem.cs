using UnityEngine;
using System.Collections.Generic;
using System.Linq;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Spellright
{
    public class CounterSystem : MonoBehaviour
    {
        public Combatant Target { get; private set; }
        public bool Open { get; private set; }
        public bool Allowed { get; private set; }
        public bool Pressed { get; private set; }
        public float PressTime { get; private set; }
        readonly HashSet<Combatant> targets = new HashSet<Combatant>();
        public bool GroupAttack => targets.Count > 1;
        public bool IsTargeted(Combatant fighter) => Open && targets.Contains(fighter);
        public string Prompt => !Open ? "" : GroupAttack ? "PARTY incoming! One SPACE press counters the simultaneous hits."
            : !Allowed ? Target.Name + " is at Bottom: cannot Counter" : Target.Name + " incoming!  SPACE / Counter at contact";
        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) Press();
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Space)) Press();
#endif
        }
        public void Begin(Combatant target, bool allowed) { BeginGroup(new[] { target }, allowed); }
        public void BeginGroup(IEnumerable<Combatant> defenders, bool allowed)
        {
            targets.Clear();
            foreach (var defender in defenders) targets.Add(defender);
            Target = targets.FirstOrDefault(); Allowed = allowed; Open = targets.Count > 0; Pressed = false;
        }
        public void Press() { if (Open && Allowed && !Pressed) { Pressed = true; PressTime = Time.time; } }
        public CounterResult Contact(float contactTime, CombatSettings settings)
        {
            Open = false;
            return !Allowed ? CounterResult.Unavailable : Evaluate(Pressed, PressTime, contactTime, settings.perfectWindow, settings.successWindow);
        }
        public void Cancel() { Open = false; Target = null; targets.Clear(); }
        // One press per physical strike. Post-contact input cannot retroactively prevent damage.
        public static CounterResult Evaluate(bool pressed, float input, float contact, float perfect, float success)
        {
            if (!pressed || input > contact) return CounterResult.Late;
            float before = contact - input;
            return before <= perfect ? CounterResult.Perfect : before <= success ? CounterResult.Successful : CounterResult.Early;
        }
    }
}
