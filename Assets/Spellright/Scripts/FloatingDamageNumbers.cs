using System.Collections.Generic;
using UnityEngine;

namespace Spellright
{
    public class FloatingDamageNumbers : MonoBehaviour
    {
        class Popup
        {
            public Transform Target;
            public string Text;
            public float Started;
            public float Offset;
            public Color Color;
        }

        const float Lifetime = 1.1f;
        readonly List<Popup> popups = new List<Popup>();
        Camera battleCamera;
        GUIStyle style;

        public void Initialize(Camera camera) { battleCamera = camera; }

        public void Show(Combatant target, int amount, bool healing = false, bool mana = false)
        {
            if (amount <= 0) return;
            int overlapping = popups.FindAll(p => p.Target == target.View).Count;
            popups.Add(new Popup { Target = target.View, Text = (healing ? "+" : "-") + amount + (mana ? " MP" : " HP"),
                Color = mana ? new Color(0.35f, 0.8f, 1) : healing ? new Color(0.35f, 1, 0.45f) : new Color(1, 0.35f, 0.3f),
                Started = Time.time, Offset = overlapping * 26f });
        }

        public void ShowText(Combatant target, string text, Color color)
        {
            if (target == null || !target.View || string.IsNullOrEmpty(text)) return;
            int overlapping = popups.FindAll(p => p.Target == target.View).Count;
            popups.Add(new Popup { Target = target.View, Text = text, Color = color,
                Started = Time.time, Offset = overlapping * 26f });
        }

        void Update() { popups.RemoveAll(p => !p.Target || Time.time - p.Started >= Lifetime); }

        void OnGUI()
        {
            if (!battleCamera || popups.Count == 0) return;
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label) { fontSize = 25, fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter };
                style.normal.textColor = Color.white;
            }
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 800f);
            Vector3 margin = new Vector3((Screen.width - 1280 * scale) / 2, (Screen.height - 800 * scale) / 2);
            GUI.matrix = Matrix4x4.TRS(margin, Quaternion.identity, Vector3.one * scale);
            GUI.depth = -10;
            foreach (var popup in popups)
            {
                if (!popup.Target) continue;
                float age = Mathf.Clamp01((Time.time - popup.Started) / Lifetime);
                Vector3 screen = battleCamera.WorldToScreenPoint(popup.Target.position + Vector3.up * 1.8f);
                if (screen.z <= 0) continue;
                float x = (screen.x - margin.x) / scale;
                float y = (Screen.height - screen.y - margin.y) / scale - age * 38f - popup.Offset;
                var rect = new Rect(x - 80, y - 18, 160, 36);
                float alpha = 1 - Mathf.InverseLerp(0.55f, 1, age);
                GUI.color = new Color(0, 0, 0, alpha);
                GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), popup.Text, style);
                GUI.color = new Color(popup.Color.r, popup.Color.g, popup.Color.b, alpha);
                GUI.Label(rect, popup.Text, style);
            }
            GUI.matrix = previousMatrix; GUI.color = previousColor; GUI.depth = previousDepth;
        }
    }
}
