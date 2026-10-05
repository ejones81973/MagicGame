using UnityEngine;

namespace Spellright
{
    public sealed class OverworldTotemMagic : MonoBehaviour
    {
        OverworldPartyManager party;
        OverworldInputReader input;
        OverworldMovementTuning tuning;
        GameObject stormCloud;
        bool aimingStorm;
        public bool IsAimingStorm => aimingStorm;

        public void Initialize(OverworldPartyManager owner, OverworldInputReader reader, OverworldMovementTuning values)
        { party = owner; input = reader; tuning = values; }

        public void Tick()
        {
            if (party.Formations.Current != FormationKind.Totem)
            {
                if (aimingStorm) CancelStormAim();
                return;
            }
            if (aimingStorm) { UpdateStormAim(); return; }
            if (!input.Pressed("Attack")) return;

            Element top = party.Character(party.Formations.Top).Element;
            Element middle = party.Character(party.Formations.Middle).Element;
            if (PairIs(top, middle, Element.Fire, Element.Water)) CastSteam();
            else if (PairIs(top, middle, Element.Water, Element.Electric)) BeginStormAim();
            else if (PairIs(top, middle, Element.Fire, Element.Electric)) CastPlasma();
            else party.Say("No test synchronized overworld magic for " + party.Formations.TotemPair + ". Reorder Top and Middle.");
        }

        static bool PairIs(Element a, Element b, Element first, Element second) =>
            (a == first && b == second) || (a == second && b == first);

        void CastSteam()
        {
            FireMagicProjectile(OverworldMagicKind.Steam, new Color(.72f, .94f, 1f));
            party.Say("Steam cast.");
        }

        void BeginStormAim()
        {
            stormCloud = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            stormCloud.name = "Aimed Storm Cloud";
            stormCloud.transform.position = party.GroupAnchor.position + party.GroupAnchor.forward * 4f + Vector3.up * 3.2f;
            stormCloud.transform.localScale = new Vector3(1.8f, 1.1f, 1.8f);
            Destroy(stormCloud.GetComponent<Collider>());
            Color stormColor = new Color(.4f, .75f, 1f);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            material.color = stormColor;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", stormColor * .65f);
            stormCloud.GetComponent<Renderer>().material = material;
            aimingStorm = true;
            party.Say("Storm Cloud: move to position it, then press Attack to strike. Interact cancels.");
        }

        void UpdateStormAim()
        {
            Vector2 move = input.Vector("Move");
            Transform camera = party.ViewCamera ? party.ViewCamera.transform : null;
            Vector3 forward = camera ? camera.forward : party.GroupAnchor.forward;
            forward.y = 0; forward = forward.sqrMagnitude > .001f ? forward.normalized : Vector3.forward;
            Vector3 right = new Vector3(forward.z, 0, -forward.x);
            Vector3 planar = Vector3.ClampMagnitude(forward * move.y + right * move.x, 1f);
            stormCloud.transform.position += planar * tuning.stormAimSpeed * Time.deltaTime;
            if (input.Pressed("Jump")) stormCloud.transform.position += Vector3.up * 1.25f;
            if (input.Held("Glide")) stormCloud.transform.position += Vector3.down * tuning.stormAimSpeed * .35f * Time.deltaTime;
            if (input.Pressed("Interact")) { CancelStormAim(); party.Say("Storm Cloud cancelled."); return; }
            if (input.Pressed("Attack")) ResolveStorm();
        }

        void ResolveStorm()
        {
            var targets = FindObjectsByType<OverworldMagicTarget>(FindObjectsSortMode.None);
            OverworldMagicTarget nearest = null;
            float distance = 3.5f;
            foreach (var candidate in targets)
            {
                if (candidate.RequiredMagic != OverworldMagicKind.Storm || candidate.Activated) continue;
                float d = Vector3.Distance(candidate.transform.position, stormCloud.transform.position);
                if (d < distance) { nearest = candidate; distance = d; }
            }
            if (nearest && nearest.Activate(OverworldMagicKind.Storm))
            {
                CreateLightning(nearest.transform.position);
                party.Say("Storm Cloud strikes " + nearest.Label + " and powers it.");
            }
            else
            {
                CreateLightning(stormCloud.transform.position);
                party.Say("Storm Cloud releases a lightning strike at the aimed location.");
            }
            CancelStormAim();
        }

        void CastPlasma()
        {
            FireMagicProjectile(OverworldMagicKind.Plasma, new Color(1f, .55f, .16f));
            party.Say("Plasma cast.");
        }

        void FireMagicProjectile(OverworldMagicKind kind, Color color)
        {
            Vector3 forward = party.GroupAnchor.forward;
            var projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = kind + " Combination Spell";
            projectile.transform.position = party.GroupAnchor.position + forward * 1.1f + Vector3.up * 1.25f;
            projectile.transform.localScale = Vector3.one * (kind == OverworldMagicKind.Steam ? .65f : .55f);
            var sphere = projectile.GetComponent<SphereCollider>();
            sphere.isTrigger = true;
            var body = projectile.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.linearVelocity = forward * tuning.synchronizedProjectileSpeed;
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            material.color = color;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 1.2f);
            projectile.GetComponent<Renderer>().material = material;
            projectile.AddComponent<OverworldSynchronizedProjectile>().Initialize(kind, party);
        }

        void CancelStormAim()
        {
            if (stormCloud) Destroy(stormCloud);
            stormCloud = null;
            aimingStorm = false;
        }

        static void CreateLightning(Vector3 position)
        {
            var bolt = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bolt.name = "Storm Cloud Lightning Strike";
            bolt.transform.position = position + Vector3.up * 2f;
            bolt.transform.localScale = new Vector3(.12f, 2f, .12f);
            Destroy(bolt.GetComponent<Collider>());
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            material.color = new Color(.55f, .85f, 1f);
            material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", Color.cyan * 2f);
            bolt.GetComponent<Renderer>().material = material;
            Destroy(bolt, .22f);
        }

    }
}
