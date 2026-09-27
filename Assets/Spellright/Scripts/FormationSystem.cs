using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine;

namespace Spellright
{
    public class FormationSystem
    {
        public FormationKind Kind = FormationKind.Line;
        public int[] Order = { 0, 1, 2 };
        public bool ChangedThisPhase;
        Combatant priorityActor;
        public bool LockedByKnockout => party.Any(c => !c.Alive);
        readonly CombatSettings settings;
        readonly List<Combatant> party;
        public FormationSystem(CombatSettings settings, List<Combatant> party) { this.settings = settings; this.party = party; }
        public int Slot(Combatant c) => System.Array.IndexOf(Order, party.IndexOf(c));
        public Combatant At(int slot) => party[Order[slot]];
        public Combatant NextActor => priorityActor != null && CanAct(priorityActor) ? priorityActor : Order.Select(i => party[i]).FirstOrDefault(CanAct);
        public Combatant Front => Order.Select(i => party[i]).FirstOrDefault(c => c.Alive);
        public bool CanAct(Combatant c) => c.Alive && !c.Acted && !(Kind == FormationKind.Totem && Slot(c) == 2);
        public void QueuePriority(Combatant actor) { priorityActor = actor; }
        public void ClearPriority() { priorityActor = null; }
        public void Swap(Combatant first, Combatant second)
        {
            int a = Slot(first), b = Slot(second);
            if (a < 0 || b < 0 || a == b) return;
            int temp = Order[a]; Order[a] = Order[b]; Order[b] = temp;
        }
        public bool CanUseMelee(Combatant c) => Kind != FormationKind.Totem || Slot(c) == 2;
        public bool CanCounter(Combatant c) => c.Alive && !(Kind == FormationKind.Totem && Slot(c) == 2);
        public float Outgoing(Combatant c) => Kind == FormationKind.Huddle ? settings.huddleOutgoing : Kind == FormationKind.Totem && Slot(c) == 0 ? settings.totemTopOutgoing : 1f;
        public float Incoming(Combatant c) => Kind == FormationKind.Huddle ? settings.huddleIncoming : Kind == FormationKind.Totem && Slot(c) == 2 ? settings.totemBottomIncoming : 1f;
        public Vector3 Position(int slot)
        {
            switch (Kind)
            {
                case FormationKind.Totem: return new Vector3(-3, 1 + (2 - slot) * 2, 0);
                case FormationKind.Huddle: return slot == 0
                    ? new Vector3(-2.65f, 1, 0)
                    : new Vector3(-3.55f, 1, slot == 1 ? 0.85f : -0.85f);
                case FormationKind.Spread: return new Vector3(-3.4f, 1, (1 - slot) * 3.5f);
                default: return new Vector3(-1.8f - slot * 1.9f, 1, 0);
            }
        }
        public void Apply() { for (int i = 0; i < 3; i++) if (At(i).View) At(i).View.position = Position(i); }
        public IEnumerator JumpIntoPosition(float duration, float height)
        {
            var movers = Enumerable.Range(0, 3).Select(i => At(i)).ToArray();
            var starts = movers.Select(c => c.View ? c.View.position : Vector3.zero).ToArray();
            var targets = Enumerable.Range(0, 3).Select(Position).ToArray();
            duration = Mathf.Max(0.05f, duration);
            for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * t * (3f - 2f * t);
                for (int i = 0; i < movers.Length; i++)
                {
                    if (!movers[i].View) continue;
                    Vector3 point = Vector3.Lerp(starts[i], targets[i], eased);
                    point.y += Mathf.Sin(t * Mathf.PI) * Mathf.Max(0, height);
                    movers[i].View.position = point;
                }
                yield return null;
            }
            for (int i = 0; i < movers.Length; i++)
                if (movers[i].View) movers[i].View.position = targets[i];
        }
        public bool ForceHuddle()
        {
            if (Kind == FormationKind.Huddle) return false;
            Kind = FormationKind.Huddle;
            Order = new[] { 0, 1, 2 };
            return true;
        }
        public Combatant[] MultiTargetTargets() => party.Where(c => c.Alive).ToArray();
        public Combatant ChooseTarget(bool melee)
        {
            var living = party.Where(c => c.Alive).ToList();
            if (Kind == FormationKind.Line) return Front;
            if (Kind == FormationKind.Totem)
            {
                if (melee) return At(2).Alive ? At(2) : null;
                var elevated = new[] { At(0), At(1) }.Where(c => c.Alive).ToList();
                return PickTarget(elevated);
            }
            if (Kind == FormationKind.Huddle) return Front;
            return PickTarget(living);
        }
        Combatant PickTarget(List<Combatant> choices)
        {
            if (choices.Count == 0) return null;
            var rod = choices.Where(c => c.LightningRodTurns > 0).ToList();
            if (rod.Count > 0 && Random.value < .75f) return rod[Random.Range(0, rod.Count)];
            return choices[Random.Range(0, choices.Count)];
        }
        public string Description => Kind == FormationKind.Line ? "Normal attacks hit Front only. Late counters knock into teammates. Multi-target attacks hit everyone."
            : Kind == FormationKind.Totem ? "Top + Middle act and use ranged attacks. Single-target ranged attacks hit them; multi-member attacks hit everyone. Bottom cannot act or counter and takes extra melee damage."
            : Kind == FormationKind.Huddle ? "All enemy strikes splash the party. Members take reduced damage and deal reduced damage."
            : "Normal damage. Volley projectiles must reach each defender separately.";
    }
}
