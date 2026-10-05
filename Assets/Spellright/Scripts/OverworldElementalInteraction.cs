using UnityEngine;

namespace Spellright
{
    public sealed class OverworldElementalInteraction : MonoBehaviour
    {
        OverworldPartyManager party;
        OverworldInputReader input;
        public void Initialize(OverworldPartyManager owner, OverworldInputReader reader)
        { party = owner; input = reader; }

        public void Tick(bool interactConsumed, bool attackConsumed = false)
        {
            if (party.Formations.Current == FormationKind.Totem) return;
            if (!interactConsumed && input.Pressed("Interact")) InteractWithStation();
            if (!attackConsumed && input.Pressed("Attack")) FireElementalBall();
        }

        void InteractWithStation()
        {
            var stations = FindObjectsByType<OverworldElementalStation>(FindObjectsSortMode.None);
            OverworldElementalStation nearest = null;
            float distance = 3.2f;
            Vector3 origin = party.Controlled.transform.position;
            foreach (var station in stations)
            {
                float d = Vector3.Distance(origin, station.transform.position);
                if (d < distance) { nearest = station; distance = d; }
            }
            if (!nearest) { party.Say("No elemental mechanism nearby. Aim and press Attack to fire a test bolt."); return; }
            Element element = party.Controlled.Element;
            if (nearest.Activate(element)) party.Say(nearest.Label + " responds to " + element + ".");
            else if (!nearest.Available) party.Say(nearest.Label + " is not ready yet.");
            else party.Say(nearest.Label + " needs " + nearest.RequiredElement + ". Switch characters or fire a matching bolt.");
        }

        void FireElementalBall()
        {
            if (party.IsTransitioning) return;
            var shooter = party.Controlled;
            if (!shooter) return;
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = shooter.Element + " Element Ball";
            ball.transform.position = shooter.transform.position + Vector3.up * 1.05f + shooter.transform.forward * .8f;
            ball.transform.localScale = Vector3.one * .42f;
            var collider = ball.GetComponent<SphereCollider>();
            collider.isTrigger = true;
            var body = ball.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.linearVelocity = shooter.transform.forward * 22f;
            var renderer = ball.GetComponent<Renderer>();
            var material = RuntimeMaterials.Lit(shooter.ElementColor, emissive: true);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", shooter.ElementColor * .45f);
            renderer.material = material;
            ball.AddComponent<OverworldElementalProjectile>().Initialize(shooter.Element, party);
            party.Say(shooter.name + " fires a " + shooter.Element + " ball.");
        }
    }
}
