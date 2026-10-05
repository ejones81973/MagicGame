using UnityEngine;

namespace Spellright
{
    public enum OverworldMagicKind { Steam, Storm, Plasma }

    public sealed class OverworldElementalStation : MonoBehaviour
    {
        public Element RequiredElement { get; private set; }
        public bool Activated { get; private set; }
        public bool Available { get; set; } = true;
        public string Label { get; private set; }

        public void Initialize(Element element, string label, bool available = true)
        { RequiredElement = element; Label = label; Available = available; }

        public bool Activate(Element element)
        {
            if (!Available || Activated || element != RequiredElement) return false;
            Activated = true;
            var renderer = GetComponentInChildren<Renderer>();
            if (renderer) renderer.material.color = new Color(.25f, 1f, .56f);
            return true;
        }
    }

    public sealed class OverworldMagicTarget : MonoBehaviour
    {
        public OverworldMagicKind RequiredMagic { get; private set; }
        public bool Activated { get; private set; }
        public string Label { get; private set; }
        public OverworldPuzzleDoor LinkedDoor;
        public bool DestroyOnActivate;
        public bool Available { get; set; } = true;

        public void Initialize(OverworldMagicKind magic, string label, bool available = true)
        { RequiredMagic = magic; Label = label; Available = available; }

        public bool Activate(OverworldMagicKind magic)
        {
            if (!Available || Activated || magic != RequiredMagic) return false;
            Activated = true;
            var renderer = GetComponentInChildren<Renderer>();
            if (renderer) renderer.material.color = new Color(.3f, 1f, .55f);
            if (LinkedDoor) LinkedDoor.Open();
            if (DestroyOnActivate) Destroy(gameObject);
            return true;
        }
    }

    public sealed class OverworldPressurePlate : MonoBehaviour
    {
        public int Id { get; private set; }
        public bool HeavyOnly { get; private set; }
        Renderer plateRenderer;
        public void Initialize(int id, bool heavyOnly = false) { Id = id; HeavyOnly = heavyOnly; }
        public void SetOccupied(bool occupied)
        {
            if (!plateRenderer) plateRenderer = GetComponent<Renderer>();
            if (plateRenderer) plateRenderer.material.color = occupied ? new Color(.2f, 1f, .5f) : new Color(.35f, .42f, .5f);
        }
        public bool Contains(Vector3 point, float radius = .85f)
        {
            Vector3 delta = point - transform.position;
            return Mathf.Abs(delta.x) <= radius && Mathf.Abs(delta.z) <= radius && Mathf.Abs(delta.y) <= 1.4f;
        }
    }

    public sealed class OverworldHeavyObject : MonoBehaviour
    {
        public bool IsCarried { get; private set; }
        Rigidbody body;
        Collider objectCollider;

        public void SetCarried(bool carried)
        {
            IsCarried = carried;
            if (!body) body = GetComponent<Rigidbody>();
            if (!objectCollider) objectCollider = GetComponent<Collider>();
            if (body) { body.isKinematic = carried; body.useGravity = !carried; }
            if (objectCollider) objectCollider.isTrigger = carried;
        }

        public void Place(Vector3 position, Quaternion rotation)
        { transform.SetPositionAndRotation(position, rotation); }
    }

    public sealed class OverworldPuzzleDoor : MonoBehaviour
    {
        public bool IsOpen { get; private set; }
        Vector3 closedPosition;
        float openT;
        void Awake() { closedPosition = transform.position; }
        public void Open() { IsOpen = true; }

        void Update()
        {
            if (!IsOpen || openT >= 1f) return;
            openT = Mathf.MoveTowards(openT, 1f, Time.deltaTime * 1.4f);
            transform.position = closedPosition + Vector3.up * (openT * 5f);
            if (openT >= .8f)
            {
                var collider = GetComponent<Collider>();
                if (collider) collider.enabled = false;
            }
        }
    }

    public sealed class OverworldElementalProjectile : MonoBehaviour
    {
        Element element;
        OverworldPartyManager party;
        public void Initialize(Element firedElement, OverworldPartyManager owner)
        { element = firedElement; party = owner; Destroy(gameObject, 4f); }

        void OnTriggerEnter(Collider other)
        {
            var station = other.GetComponent<OverworldElementalStation>();
            if (station)
            {
                bool success = station.Activate(element);
                party.Say(success ? station.Label + " activated with " + element + "." : station.Label + " resists " + element + ".");
                if (success) Destroy(gameObject);
                else Destroy(gameObject);
                return;
            }
            var wall = other.GetComponent<OverworldElementalWall>();
            if (wall)
            {
                wall.Hit(element);
                Destroy(gameObject);
            }
        }
    }

    public sealed class OverworldSynchronizedProjectile : MonoBehaviour
    {
        OverworldMagicKind magic;
        OverworldPartyManager party;

        public void Initialize(OverworldMagicKind firedMagic, OverworldPartyManager owner)
        {
            magic = firedMagic;
            party = owner;
            Destroy(gameObject, 5f);
        }

        void OnTriggerEnter(Collider other)
        {
            var target = other.GetComponent<OverworldMagicTarget>();
            if (!target) target = other.GetComponentInParent<OverworldMagicTarget>();
            if (target && target.Activate(magic))
            {
                string label = string.IsNullOrEmpty(target.Label) ? "the target" : target.Label;
                party.Say(magic + " activates " + label + ".");
            }
            Destroy(gameObject);
        }
    }

    public sealed class OverworldElementalWall : MonoBehaviour
    {
        Element required;
        OverworldPartyManager party;
        public void Initialize(Element element, OverworldPartyManager owner) { required = element; party = owner; }
        public void Hit(Element attack)
        {
            if (attack == required)
            {
                party.Say(attack + " breaks the matching wall!");
                Destroy(gameObject);
            }
            else party.Say("The wall resists " + attack + ". It needs " + required + ".");
        }
    }
}
