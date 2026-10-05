using UnityEngine;

namespace Spellright
{
    [RequireComponent(typeof(LineRenderer))]
    public class GroundAttackRing : MonoBehaviour
    {
        Material material;

        public void Initialize(float radius, Color color)
        {
            var line = GetComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 64;
            line.startWidth = line.endWidth = 0.065f;
            line.numCapVertices = 4;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            material = RuntimeMaterials.Unlit(color);
            line.sharedMaterial = material;
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / line.positionCount;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius));
            }
        }

        void OnDestroy()
        {
            if (material) Destroy(material);
        }
    }
}
