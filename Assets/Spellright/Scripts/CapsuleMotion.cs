using UnityEngine;

namespace Spellright
{
    // Animate only the visible child: formation positions, attack paths and counter contact
    // continue to use the stable combat root. Bracing puts the mesh exactly on that root.
    public class CapsuleMotion : MonoBehaviour
    {
        Combatant fighter;
        CombatSettings settings;
        float phase;
        float bounceWeight = 1f;
        bool braced;
        float attackLean;
        float recoilStarted = float.NegativeInfinity;
        Vector3 recoilDirection;

        public void Recoil(Vector3 awayFromAttacker)
        {
            awayFromAttacker.y = 0;
            recoilDirection = awayFromAttacker.sqrMagnitude > 0.001f ? awayFromAttacker.normalized : Vector3.right;
            recoilStarted = Time.time;
            bounceWeight = 0;
        }

        float RecoilAmount()
        {
            float t = (Time.time - recoilStarted) / Mathf.Max(0.1f, settings.enemyRecoilSeconds);
            if (t >= 1) return 0;
            // Snap back quickly, then ease into the original stance.
            return t < 0.22f ? Mathf.SmoothStep(0, 1, t / 0.22f) : 1 - Mathf.SmoothStep(0, 1, (t - 0.22f) / 0.78f);
        }

        public void SetAttackLean(float degrees) { attackLean = degrees; }

        public void Initialize(Combatant combatant, CombatSettings tuning, float phaseOffset)
        { fighter = combatant; settings = tuning; phase = phaseOffset; }

        public void SetBraced(bool value)
        {
            braced = value;
            if (value && fighter.Alive)
            {
                bounceWeight = 0;
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            }
        }

        void LateUpdate()
        {
            if (fighter == null) return;
            float recoil = RecoilAmount();
            Vector3 recoilOffset = recoilDirection * (recoil * Mathf.Max(0, settings.enemyRecoilDistance));
            if (!fighter.Alive)
            {
                // A horizontal capsule has half the standing height. Keep it resting
                // on its formation slot, including elevated Totem slots.
                transform.localPosition = Vector3.MoveTowards(transform.localPosition,
                    Vector3.down * 0.5f + recoilOffset, Time.deltaTime * 5f);
                transform.localRotation = Quaternion.RotateTowards(transform.localRotation,
                    Quaternion.Euler(0, 0, 90), Time.deltaTime * 360f);
                return;
            }

            bounceWeight = braced || recoil > 0 ? 0 : Mathf.MoveTowards(bounceWeight, 1, Time.deltaTime * 5f);
            float wave = Mathf.Sin((Time.time * Mathf.Max(0, settings.idleBounceFrequency) + phase) * Mathf.PI);
            float height = wave * wave * Mathf.Max(0, settings.idleBounceHeight) * bounceWeight;
            transform.localPosition = Vector3.up * height + recoilOffset;
            transform.localRotation = Quaternion.Euler(recoilDirection.z * recoil * 16f, 0,
                attackLean - recoilDirection.x * recoil * 16f);
        }
    }
}
