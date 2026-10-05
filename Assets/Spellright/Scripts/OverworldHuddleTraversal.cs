using UnityEngine;

namespace Spellright
{
    public sealed class OverworldHuddleTraversal : MonoBehaviour
    {
        OverworldPartyManager party;
        OverworldInputReader input;
        OverworldMovementTuning tuning;
        OverworldHeavyObject carried;
        bool dashUsed;
        public bool IsCarrying => carried != null;
        public bool ConsumedInteract { get; private set; }
        public bool ConsumedAttack { get; private set; }

        public void Initialize(OverworldPartyManager owner, OverworldInputReader reader, OverworldMovementTuning values)
        { party = owner; input = reader; tuning = values; }

        public float Tick()
        {
            ConsumedInteract = false;
            ConsumedAttack = false;
            if (party.Formations.Current != FormationKind.Huddle)
            {
                if (carried) Drop();
                dashUsed = false;
                return 1f;
            }

            if (party.GroupMotor.Grounded) dashUsed = false;
            if (input.Pressed("Dash") && !dashUsed && party.GroupMotor.TryDash(MoveDirection()))
            {
                dashUsed = true;
                party.Say("Huddle Air Dash!");
            }
            if (input.Pressed("Attack"))
            {
                if (carried) Drop();
                else TryPickup();
                ConsumedAttack = true;
            }
            return carried ? tuning.carrySpeedMultiplier : 1f;
        }

        public bool Gliding => party.Formations.Current == FormationKind.Huddle &&
            input.Held("Glide") && !party.GroupMotor.Grounded;

        public void LateTick()
        {
            if (!carried || !party.GroupAnchor) return;
            carried.Place(party.GroupAnchor.position + party.GroupAnchor.forward * 1.25f + Vector3.up * .3f,
                party.GroupAnchor.rotation);
        }

        void TryPickup()
        {
            var candidates = FindObjectsByType<OverworldHeavyObject>(FindObjectsSortMode.None);
            OverworldHeavyObject nearest = null;
            float distance = 2.8f;
            Vector3 origin = party.GroupAnchor.position;
            foreach (var candidate in candidates)
            {
                if (candidate.IsCarried) continue;
                float d = Vector3.Distance(origin, candidate.transform.position);
                if (d < distance) { distance = d; nearest = candidate; }
            }
            if (!nearest) { party.Say("Huddle can carry Heavy objects. Move closer to one."); return; }
            carried = nearest;
            carried.SetCarried(true);
            party.Say("The party lifts the Heavy cube. Movement is slower while carrying.");
        }

        void Drop()
        {
            if (!carried) return;
            carried.SetCarried(false);
            if (party.GroupAnchor)
                carried.Place(party.GroupAnchor.position + party.GroupAnchor.forward * 1.25f + Vector3.up * .55f,
                    party.GroupAnchor.rotation);
            carried = null;
            party.Say("The Heavy object is set down.");
        }

        Vector3 MoveDirection()
        {
            Vector2 move = input.Vector("Move");
            Transform camera = party.ViewCamera ? party.ViewCamera.transform : null;
            Vector3 forward = camera ? camera.forward : party.GroupAnchor.forward;
            forward.y = 0; forward = forward.sqrMagnitude > .001f ? forward.normalized : Vector3.forward;
            Vector3 right = new Vector3(forward.z, 0, -forward.x);
            Vector3 direction = Vector3.ClampMagnitude(forward * move.y + right * move.x, 1f);
            return direction.sqrMagnitude > .01f ? direction : party.GroupAnchor.forward;
        }
    }
}
