using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Spellright
{
    /// <summary>Small colored motes make active combat statuses visible on placeholder fighters.</summary>
    public class StatusEffectVisual : MonoBehaviour
    {
        sealed class MarkerGroup
        {
            public GameObject[] markers;
            public Vector3[] origins;
            public float speed;
            public float height;
        }

        Combatant fighter;
        readonly List<MarkerGroup> groups = new List<MarkerGroup>();
        readonly List<Material> materials = new List<Material>();

        public void Initialize(Combatant target)
        {
            fighter = target;
            AddGroup(PrimitiveType.Sphere, new Color(1f, .24f, .035f), 3, .85f, 1.8f, .13f); // Burn
            AddGroup(PrimitiveType.Sphere, new Color(.12f, .78f, 1f), 3, .55f, 1.35f, .105f); // Wet
            AddGroup(PrimitiveType.Cube, new Color(1f, .92f, .12f), 4, 2.5f, 1.55f, .11f); // Shock
            AddGroup(PrimitiveType.Cube, new Color(1f, .18f, .38f), 2, 1.1f, 1.95f, .13f); // Attack down
            AddGroup(PrimitiveType.Cube, new Color(.72f, .28f, 1f), 2, 1.1f, 1.75f, .13f); // Defense down
            AddGroup(PrimitiveType.Sphere, new Color(.2f, 1f, .43f), 3, .8f, 2.05f, .1f); // Regen / buffs
        }

        void AddGroup(PrimitiveType shape, Color color, int count, float speed, float height, float size)
        {
            var group = new MarkerGroup { markers = new GameObject[count], origins = new Vector3[count], speed = speed, height = height };
            var material = RuntimeMaterials.Unlit(color);
            materials.Add(material);
            for (int i = 0; i < count; i++)
            {
                var marker = GameObject.CreatePrimitive(shape);
                marker.name = "Status Indicator";
                Destroy(marker.GetComponent<Collider>());
                marker.transform.SetParent(transform, false);
                marker.transform.localScale = Vector3.one * size;
                marker.GetComponent<Renderer>().sharedMaterial = material;
                marker.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                marker.GetComponent<Renderer>().receiveShadows = false;
                group.markers[i] = marker;
                group.origins[i] = new Vector3(Mathf.Cos(i * Mathf.PI * 2 / count) * .55f, height, Mathf.Sin(i * Mathf.PI * 2 / count) * .48f);
                marker.SetActive(false);
            }
            groups.Add(group);
        }

        void LateUpdate()
        {
            if (fighter == null) return;
            bool alive = fighter.Alive;
            SetGroup(0, alive && fighter.BurnTurns > 0);
            SetGroup(1, alive && fighter.WetTurns > 0);
            SetGroup(2, alive && (fighter.ShockTurns > 0 || fighter.ShockSkipTurns > 0));
            SetGroup(3, alive && fighter.AttackDownTurns > 0);
            SetGroup(4, alive && fighter.DefenseDownTurns > 0);
            SetGroup(5, alive && (fighter.RegenTurns > 0 || fighter.FireBuffTurns > 0 || fighter.WaterBuffTurns > 0 || fighter.OverchargeTurns > 0 || fighter.QuickChargeTurns > 0 || fighter.SurgeTurns > 0 || fighter.ConductorTurns > 0 || fighter.TidalGuardTurns > 0 || fighter.PhoenixTurns > 0));
        }

        void SetGroup(int index, bool visible)
        {
            var group = groups[index];
            float time = Time.time * group.speed;
            for (int i = 0; i < group.markers.Length; i++)
            {
                var marker = group.markers[i];
                if (marker.activeSelf != visible) marker.SetActive(visible);
                if (!visible) continue;
                float angle = time + i * Mathf.PI * 2f / group.markers.Length;
                var origin = group.origins[i];
                marker.transform.localPosition = new Vector3(Mathf.Cos(angle) * .55f, origin.y + Mathf.Sin(time * 1.7f + i) * .07f, Mathf.Sin(angle) * .48f);
                if (index == 2) marker.transform.localRotation = Quaternion.Euler(time * 120f, time * 180f, time * 90f);
            }
        }

        void OnDestroy()
        {
            foreach (var material in materials) if (material) Destroy(material);
        }
    }
}
