using UnityEngine;

namespace Spellright
{
    public sealed class OverworldCharacterIdentity : MonoBehaviour
    {
        public int Index { get; private set; }
        public Element Element { get; private set; }
        public Color ElementColor { get; private set; }
        public Renderer BodyRenderer { get; private set; }
        GameObject selectedMarker;

        public void Initialize(int index, Element element, Color color, Renderer body, GameObject marker)
        {
            Index = index; Element = element; ElementColor = color;
            BodyRenderer = body; selectedMarker = marker;
        }

        public void SetSelected(bool selected)
        {
            if (selectedMarker) selectedMarker.SetActive(selected);
        }
    }
}
