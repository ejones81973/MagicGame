using System;
using System.Collections;
using UnityEngine;

namespace Spellright
{
    public static class MeleeAnimation
    {
        public static IEnumerator Move(Transform actor, Vector3 destination, float seconds)
        {
            Vector3 start = actor.position;
            for (float elapsed = 0; elapsed < seconds; elapsed += Time.deltaTime)
            {
                actor.position = Vector3.Lerp(start, destination, Mathf.SmoothStep(0, 1, elapsed / seconds));
                yield return null;
            }
            actor.position = destination;
        }

        public static IEnumerator Hop(Combatant actor, Combatant target,
            CombatSettings settings, Action<float> onContact)
        {
            Vector3 start = actor.View.position;
            Vector3 away = start - target.View.position;
            away.y = 0;
            // Two upright capsules each have radius .5: stop when their sides touch.
            Vector3 contact = target.View.position + away.normalized;
            float duration = Mathf.Max(0.18f, Vector3.Distance(start, contact) / Mathf.Max(0.1f, settings.meleeStrikeSpeed));
            float height = Mathf.Clamp(settings.meleeHopHeight, 0, 0.9f);
            actor.Motion.SetAttackLean(0);
            float elapsed = 0;
            while (elapsed < duration)
            {
                yield return null;
                float remaining = duration - elapsed;
                elapsed = Mathf.Min(duration, elapsed + Time.deltaTime);
                float t = elapsed / duration;
                actor.View.position = Vector3.Lerp(start, contact, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * height);
                if (elapsed >= duration)
                {
                    actor.View.position = contact;
                    // Timestamp the surface touch within the final frame, as with projectiles.
                    onContact(Time.time - Time.deltaTime + remaining);
                }
            }
        }
    }
}
