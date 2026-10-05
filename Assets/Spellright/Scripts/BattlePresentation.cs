using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spellright
{
    public class BattlePresentation : MonoBehaviour
    {
        public Camera Camera { get; private set; }
        public string Feedback = "Choose a character and command.";
        public Color FeedbackColor = Color.white;
        readonly List<Material> materials = new List<Material>();
        readonly List<AudioClip> clips = new List<AudioClip>();
        readonly Dictionary<Combatant, Coroutine> flashes = new Dictionary<Combatant, Coroutine>();
        AudioSource audioSource;
        FloatingDamageNumbers damageNumbers;
        Material primitiveMaterialTemplate;
        Vector3 cameraHomePosition, cameraHomeFocus = new Vector3(0, 2, 0);
        Quaternion cameraHomeRotation;
        float cameraHomeSize;
        public void Build(Material materialTemplate)
        {
            primitiveMaterialTemplate = materialTemplate;
            Camera = new GameObject("Battle Camera").AddComponent<Camera>();
            Camera.transform.SetParent(transform);
            Camera.transform.position = new Vector3(1, 9, -18);
            Camera.transform.LookAt(cameraHomeFocus);
            Camera.orthographic = true; Camera.orthographicSize = 7.8f;
            cameraHomePosition = Camera.transform.position;
            cameraHomeRotation = Camera.transform.rotation;
            cameraHomeSize = Camera.orthographicSize;
            Camera.backgroundColor = new Color(0.035f, 0.05f, 0.09f);
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.gameObject.AddComponent<AudioListener>();
            damageNumbers = gameObject.AddComponent<FloatingDamageNumbers>();
            damageNumbers.Initialize(Camera);
            var light = new GameObject("Arena Light").AddComponent<Light>();
            light.transform.SetParent(transform); light.type = LightType.Directional; light.intensity = 1.5f;
            light.transform.rotation = Quaternion.Euler(45, -30, 0);
            Shape("Arena", PrimitiveType.Cube, new Vector3(0, -0.25f, 0), new Vector3(17, 0.5f, 12), new Color(0.14f, 0.18f, 0.24f));
            for (int i = -4; i <= 4; i++) Shape("Floor stripe", PrimitiveType.Cube, new Vector3(i * 2, 0.01f, 0), new Vector3(0.025f, 0.02f, 12), new Color(0.22f, 0.27f, 0.34f));
            audioSource = gameObject.AddComponent<AudioSource>(); audioSource.volume = 0.22f;
            foreach (float frequency in new[] { 1047f, 660f, 180f, 100f, 300f })
            {
                const int length = 6615; var data = new float[length];
                for (int i = 0; i < length; i++) data[i] = Mathf.Sin(i * frequency * 2 * Mathf.PI / 44100) * (1f - (float)i / length);
                var clip = AudioClip.Create("Counter tone", length, 1, 44100, false); clip.SetData(data, 0); clips.Add(clip);
            }
        }
        public Transform Shape(string name, PrimitiveType primitive, Vector3 position, Vector3 scale, Color color)
        {
            var obj = GameObject.CreatePrimitive(primitive); obj.name = name; obj.transform.SetParent(transform);
            obj.transform.position = position; obj.transform.localScale = scale;
            var renderer = obj.GetComponent<Renderer>();
            var material = primitiveMaterialTemplate ? new Material(primitiveMaterialTemplate) : RuntimeMaterials.Lit(color);
            material.color = color;
            materials.Add(material);
            renderer.sharedMaterial = material;
            Destroy(obj.GetComponent<Collider>());
            return obj.transform;
        }
        public void CreateFighter(Combatant fighter, Vector3 position, CombatSettings settings, float phase)
        {
            var root = new GameObject(fighter.Name).transform;
            root.SetParent(transform); root.position = position;
            var body = Shape("Capsule", PrimitiveType.Capsule, position, Vector3.one, fighter.Color);
            body.SetParent(root, true);
            fighter.View = root;
            fighter.Motion = body.gameObject.AddComponent<CapsuleMotion>();
            fighter.Motion.Initialize(fighter, settings, phase);
            body.gameObject.AddComponent<ShieldVisual>().Initialize(fighter);
            body.gameObject.AddComponent<StatusEffectVisual>().Initialize(fighter);
        }
        public void RemoveFighter(Combatant fighter)
        {
            if (fighter == null || !fighter.View) return;
            var renderer = fighter.View.GetComponentInChildren<Renderer>();
            if (renderer && materials.Remove(renderer.sharedMaterial) && renderer.sharedMaterial) Destroy(renderer.sharedMaterial);
            Destroy(fighter.View.gameObject);
            fighter.View = null;
        }
        public void Say(string text, Color color) { Feedback = text; FeedbackColor = color; }
        public void Result(Combatant target, CounterResult result, int damage)
        {
            Color color = result == CounterResult.Perfect ? Color.cyan : result == CounterResult.Successful ? Color.green : result == CounterResult.Early ? new Color(1, 0.5f, 0.1f) : Color.red;
            Say(target.Name + ": " + (result == CounterResult.Unavailable ? "NO COUNTER" : result.ToString().ToUpper()) + "  -" + damage + " HP", color);
            audioSource.PlayOneShot(clips[(int)result]); StartFlash(target, color, 0.25f);
        }
        public void EnemyHit(Combatant target) { StartFlash(target, Color.white, 0.14f); }
        public void ShowDamage(Combatant target, int damage) { damageNumbers.Show(target, damage); }
        public void ShowHealing(Combatant target, int restored) { damageNumbers.Show(target, restored, true); }
        public void ShowManaRestored(Combatant target, int restored) { damageNumbers.Show(target, restored, true, true); }
        public void ShowStatus(Combatant target, string status, Color color) { damageNumbers.ShowText(target, status, color); }
        public GroundAttackRing CreateAttackRing(Combatant target, bool group, IEnumerable<Combatant> party)
        {
            var members = party.Where(c => c != null && c.Alive && c.View).ToArray();
            if (members.Length == 0) return null;
            Vector3 center;
            float radius;
            Color color;
            if (group)
            {
                center = Vector3.zero;
                foreach (var member in members) center += member.View.position;
                center /= members.Length;
                radius = members.Max(c => Vector3.Distance(new Vector2(c.View.position.x, c.View.position.z), new Vector2(center.x, center.z))) + 0.9f;
                color = Color.white;
            }
            else
            {
                if (target == null || !target.View) return null;
                center = target.View.position;
                radius = 0.78f;
                switch (target.Element)
                {
                    case Element.Fire: color = new Color(1f, .12f, .08f); break;
                    case Element.Water: color = new Color(.08f, .5f, 1f); break;
                    case Element.Electric: color = new Color(1f, .85f, .05f); break;
                    default: color = Color.white; break;
                }
            }
            var ringObject = new GameObject(group ? "Party Attack Warning" : target.Name + " Attack Warning");
            ringObject.transform.SetParent(transform, true);
            ringObject.transform.position = new Vector3(center.x, .035f, center.z);
            var ring = ringObject.AddComponent<GroundAttackRing>();
            ring.Initialize(radius, color);
            return ring;
        }
        public void CollisionHit(Combatant target) { StartFlash(target, Color.red, 0.18f); }
        public IEnumerator FocusAttack(IEnumerable<Combatant> fighters, CombatSettings settings)
        {
            Vector3 focus = Vector3.zero;
            int count = 0;
            foreach (var fighter in fighters)
            {
                if (fighter == null || !fighter.View) continue;
                focus += fighter.View.position;
                count++;
            }
            if (count == 0) yield break;
            focus /= count;
            Vector3 offset = cameraHomePosition - cameraHomeFocus;
            Vector3 targetPosition = focus + offset;
            Quaternion targetRotation = Quaternion.LookRotation(focus - targetPosition, Vector3.up);
            yield return MoveCamera(targetPosition, targetRotation, settings.cameraFocusSize, settings.cameraMoveSeconds);
        }
        public IEnumerator ReturnCamera(CombatSettings settings)
            => MoveCamera(cameraHomePosition, cameraHomeRotation, cameraHomeSize, settings.cameraMoveSeconds);
        IEnumerator MoveCamera(Vector3 position, Quaternion rotation, float size, float duration)
        {
            Vector3 startPosition = Camera.transform.position;
            Quaternion startRotation = Camera.transform.rotation;
            float startSize = Camera.orthographicSize;
            duration = Mathf.Max(0.05f, duration);
            for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);
                Camera.transform.position = Vector3.Lerp(startPosition, position, t);
                Camera.transform.rotation = Quaternion.Slerp(startRotation, rotation, t);
                Camera.orthographicSize = Mathf.Lerp(startSize, size, t);
                yield return null;
            }
            Camera.transform.position = position;
            Camera.transform.rotation = rotation;
            Camera.orthographicSize = size;
        }
        void StartFlash(Combatant target, Color color, float seconds)
        {
            if (flashes.TryGetValue(target, out var previous)) StopCoroutine(previous);
            flashes[target] = StartCoroutine(Flash(target, color, seconds));
        }
        IEnumerator Flash(Combatant target, Color color, float seconds)
        {
            var renderer = target.View.GetComponentInChildren<Renderer>(); renderer.sharedMaterial.color = color;
            yield return new WaitForSeconds(seconds);
            renderer.sharedMaterial.color = target.Alive ? target.Color : Color.gray;
            flashes.Remove(target);
        }
        public IEnumerator Bolt(Combatant from, Combatant to, Color color)
        {
            var orb = Shape("Spell", PrimitiveType.Sphere, from.View.position, Vector3.one * 0.4f, color);
            while (Vector3.Distance(orb.position, to.View.position) > 0.05f)
            { orb.position = Vector3.MoveTowards(orb.position, to.View.position, 16 * Time.deltaTime); yield return null; }
            Destroy(orb.gameObject);
        }
        public void Refresh(IEnumerable<Combatant> fighters)
        {
            foreach (var c in fighters)
                if (!flashes.ContainsKey(c))
                    c.View.GetComponentInChildren<Renderer>().sharedMaterial.color = c.Alive ? c.Color : Color.gray;
        }
        void OnDestroy() { foreach (var m in materials) if (m) Destroy(m); foreach (var c in clips) if (c) Destroy(c); }
    }
}
