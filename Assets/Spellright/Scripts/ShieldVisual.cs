using UnityEngine;
using UnityEngine.Rendering;

namespace Spellright
{
    public class ShieldVisual : MonoBehaviour
    {
        Combatant fighter;
        Transform bubble;
        Material material;

        public void Initialize(Combatant target)
        {
            fighter = target;
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Shield Bubble";
            Destroy(sphere.GetComponent<Collider>());
            bubble = sphere.transform;
            bubble.SetParent(transform, false);
            bubble.localScale = new Vector3(1.65f, 2.2f, 1.65f);

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            bool urp = shader != null;
            if (!shader) shader = Shader.Find("Standard");
            material = new Material(shader);
            material.color = new Color(0.15f, 0.65f, 1f, 0.22f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            if (urp)
            {
                material.SetFloat("_Surface", 1);
                material.SetFloat("_Blend", 0);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else
            {
                material.SetFloat("_Mode", 2);
                material.EnableKeyword("_ALPHABLEND_ON");
            }
            material.renderQueue = (int)RenderQueue.Transparent;
            var renderer = sphere.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            sphere.SetActive(false);
        }

        void LateUpdate()
        {
            if (fighter == null || !bubble) return;
            bool protectedNow = fighter.Alive && (fighter.Shielded || fighter.Bubbled);
            bubble.gameObject.SetActive(protectedNow);
            if (protectedNow)
            {
                float pulse = 1 + Mathf.Sin(Time.time * 3f) * 0.025f;
                bubble.localScale = new Vector3(1.65f, 2.2f, 1.65f) * pulse;
            }
        }

        void OnDestroy() { if (material) Destroy(material); }
    }
}
