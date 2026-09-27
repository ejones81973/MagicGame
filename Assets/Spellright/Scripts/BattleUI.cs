using System;
using System.Linq;
using UnityEngine;

namespace Spellright
{
    public partial class BattleUI : MonoBehaviour
    {
        BattleFlow battle;
        BattleMenu navigation;
        GUIStyle title, label, button, small, barText;
        public void Initialize(BattleFlow flow) { battle = flow; navigation = new BattleMenu(flow); }
        void Styles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 23, fontStyle = FontStyle.Bold };
            label = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
            small = new GUIStyle(label) { fontSize = 14 };
            button = new GUIStyle(GUI.skin.button) { fontSize = 16, wordWrap = true };
            barText = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter, padding = new RectOffset(0, 0, 0, 0) };
            barText.normal.textColor = Color.white;
        }
        void OnGUI()
        {
            if (battle == null || navigation == null) return;
            Styles();
            // Input is handled once in Update; prevent IMGUI's focused buttons from
            // interpreting the same Enter/Esc key a second time after a menu transition.
            if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return ||
                Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Escape ||
                Event.current.keyCode == KeyCode.W || Event.current.keyCode == KeyCode.A ||
                Event.current.keyCode == KeyCode.S || Event.current.keyCode == KeyCode.D)) Event.current.Use();
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 800f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) / 2, (Screen.height - 800 * scale) / 2), Quaternion.identity, Vector3.one * scale);
            GUI.Box(new Rect(8, 8, 1264, 88), "");
            GUI.Label(new Rect(24, 16, 850, 32), "SPELLRIGHT   |   Wave " + battle.WaveNumber + "   |   " + battle.Phase + " Phase   |   Round " + battle.Round, title);
            GUI.Label(new Rect(24, 54, 990, 30), battle.Formation.Kind + "   |   " + battle.Formation.Description, small);
            if (GUI.Button(new Rect(1120, 24, 135, 45), "Restart", button)) { FindAnyObjectByType<PrototypeBootstrap>().Restart(); return; }

            for (int i = 0; i < 3; i++)
            {
                var c = battle.Party[i]; float x = 15 + i * 260;
                GUI.color = c.Color; GUI.Box(new Rect(x, 103, 250, 130), ""); GUI.color = Color.white;
                bool enabled = GUI.enabled;
                GUI.enabled = battle.Ready && (navigation.ChoosingTarget ? navigation.Screen == CombatScreen.AllyTarget && navigation.TargetCandidates().Contains(c) : c == battle.Formation.NextActor);
                if (GUI.Button(new Rect(x + 6, 108, 238, 32), (battle.ActiveIndex == i ? "> " : "") + c.Name + " / " + c.Element, button)) { navigation.ClickFighter(c); GUI.enabled = enabled; return; }
                GUI.enabled = enabled;
                DrawResourceBar(new Rect(x + 8, 144, 234, 20), "HP", c.HP, c.MaxHP, new Color(0.2f, 0.7f, 0.32f));
                DrawResourceBar(new Rect(x + 8, 168, 234, 20), "MP", c.MP, c.MaxMP, new Color(0.15f, 0.48f, 0.9f));
                string status = !c.Alive ? "KO" : battle.Formation.Kind == FormationKind.Totem && battle.Formation.Slot(c) == 2 ? "Bottom / no action" : c.Acted ? "Action spent" : c == battle.Formation.NextActor ? "Action ready" : "Waiting";
                string activeEffects = c.StatusText;
                GUI.Label(new Rect(x + 10, 190, 240, 40), status + (c.Shielded ? " | Shield" : "") + (c.Bubbled ? " | Bubble" : "") + (activeEffects.Length > 0 ? " | " + activeEffects : ""), small);
            }
            for (int i = 0; i < battle.Enemies.Count; i++)
            {
                var enemy = battle.Enemies[i];
                float slot = 460f / Mathf.Max(1, battle.Enemies.Count);
                float width = Mathf.Min(146, slot - 5);
                float x = 800 + i * slot;
                GUI.color = enemy.Color; GUI.Box(new Rect(x, 103, width, 115), ""); GUI.color = Color.white;
                GUI.enabled = battle.Ready && enemy.Alive && (!navigation.ChoosingTarget || navigation.Screen == CombatScreen.EnemyTarget);
                bool markedEnemy = navigation.Screen == CombatScreen.EnemyTarget ? navigation.IsMarked(enemy) : battle.EnemyTarget == i;
                if (GUI.Button(new Rect(x + 4, 108, width - 8, 32), (markedEnemy ? "TARGET: " : "") + enemy.Name, small)) { navigation.ClickFighter(enemy); GUI.enabled = true; return; }
                GUI.enabled = true;
                DrawResourceBar(new Rect(x + 6, 144, width - 12, 20), "HP", enemy.HP, enemy.MaxHP, new Color(0.2f, 0.7f, 0.32f));
                GUI.Label(new Rect(x + 7, 168, width - 10, 47), !enemy.Alive ? "Defeated" : enemy.Element + (enemy.Weakened ? " / Disrupted" : "") + (enemy.StatusText.Length > 0 ? "\n" + enemy.StatusText : ""), small);
            }
            if (DrawWorldLabels(scale)) return;
            if (battle.Phase == BattlePhase.Drain) { DrawDrain(); return; }
            GUI.color = battle.Presentation.FeedbackColor;
            GUI.Label(new Rect(22, 490, 1000, 34), battle.Presentation.Feedback, title); GUI.color = Color.white;
            GUI.Box(new Rect(8, 530, 1264, 262), "");
            if (battle.Phase == BattlePhase.Enemy)
            {
                GUI.Label(new Rect(24, 546, 790, 40), battle.Counter.Prompt, title);
                GUI.Label(new Rect(24, 592, 780, 58), "Watch the attacking capsule or elemental projectile make contact. One attempt per strike.\nNo input / input after contact = Late. Holding Space does not repeat.", label);
                GUI.enabled = battle.Counter.Open && battle.Counter.Allowed && !battle.Counter.Pressed;
                if (GUI.Button(new Rect(24, 665, 380, 65), "COUNTER  [SPACE]", button)) battle.Counter.Press();
                GUI.enabled = true;
                if (battle.Counter.Pressed) GUI.Label(new Rect(420, 676, 380, 40), "Attempt committed", label);
            }
            else if (battle.Phase == BattlePhase.Finished)
                GUI.Label(new Rect(24, 550, 770, 85), battle.Presentation.Feedback + "\nUse Restart to reset HP, MP, items, formation and actions.", title);
            else DrawCommands();
            GUI.Label(new Rect(900, 542, 355, 28), "BATTLE LOG", label);
            for (int i = 0; i < battle.History.Count; i++) GUI.Label(new Rect(900, 572 + i * 33, 355, 33), battle.History[i], small);
        }
        void DrawResourceBar(Rect rect, string resource, int value, int maximum, Color fill)
        {
            Color previous = GUI.color;
            GUI.color = new Color(0.035f, 0.05f, 0.08f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            float fraction = maximum > 0 ? Mathf.Clamp01((float)value / maximum) : 0;
            if (fraction > 0)
            {
                GUI.color = fill;
                GUI.DrawTexture(new Rect(rect.x + 1, rect.y + 1, (rect.width - 2) * fraction, rect.height - 2), Texture2D.whiteTexture);
            }
            string text = resource + " " + value + "/" + maximum;
            GUI.color = Color.black;
            GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), text, barText);
            GUI.color = Color.white;
            GUI.Label(rect, text, barText);
            GUI.color = previous;
        }
        bool DrawWorldLabels(float scale)
        {
            foreach (var c in battle.Party.Concat(battle.Enemies))
            {
                Vector3 p = battle.Presentation.Camera.WorldToScreenPoint(c.View.position + Vector3.up * 1.25f);
                float x = (p.x - (Screen.width - 1280 * scale) / 2) / scale;
                float y = (Screen.height - p.y - (Screen.height - 800 * scale) / 2) / scale;
                string role = "";
                if (battle.Party.Contains(c)) role = battle.Formation.Kind == FormationKind.Totem ? new[] { "TOP", "MIDDLE", "BOTTOM" }[battle.Formation.Slot(c)] : "";
                GUI.color = battle.Counter.IsTargeted(c) ? Color.yellow : Color.white;
                GUI.Label(new Rect(x - 65, y - 14, 160, 40), c.Name + " " + role, small); GUI.color = Color.white;
                // The always active hero is not a pending target. Reserve the world arrow
                // for an actual target choice; command/menu selection has its own cursor.
                if (navigation.ChoosingTarget && navigation.IsMarked(c))
                    DrawArrow(new Rect(x - 12, y - 46, 24, 24), true);
                if (navigation.Screen == CombatScreen.Formation)
                {
                    if (navigation.FormationTopMarker == battle.Party.IndexOf(c))
                        DrawArrow(new Rect(x - 12, y - 46, 24, 24), true, new Color(0.25f, 0.8f, 1f));
                    if (navigation.FormationPreviewMember == battle.Party.IndexOf(c))
                        DrawArrow(new Rect(x - 12, y - 46, 24, 24), true);
                }
                if (battle.Ready && CanClickFighter(c))
                {
                    Rect hitRect = FighterRect(c, scale);
                    if (GUI.Button(hitRect, GUIContent.none, GUIStyle.none)) { navigation.ClickFighter(c); return true; }
                }
            }
            return false;
        }
    }
}

