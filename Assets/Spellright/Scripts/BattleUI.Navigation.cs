using System.Linq;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Spellright
{
    public partial class BattleUI
    {
        GUIStyle arrowStyle;

        void Update()
        {
            if (battle == null || navigation == null) return;
            if (!battle.Ready) { navigation.Reset(); return; }
            navigation.Choices();
            bool back = false, confirm = false;
            int horizontal = 0, vertical = 0;
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                back = keyboard.escapeKey.wasPressedThisFrame;
                confirm = keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame;
                if (keyboard.aKey.wasPressedThisFrame) horizontal = -1;
                else if (keyboard.dKey.wasPressedThisFrame) horizontal = 1;
                else if (keyboard.wKey.wasPressedThisFrame) vertical = -1;
                else if (keyboard.sKey.wasPressedThisFrame) vertical = 1;
            }
            if (Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0)
                HoverAt(Mouse.current.position.ReadValue());
#elif ENABLE_LEGACY_INPUT_MANAGER
            back = Input.GetKeyDown(KeyCode.Escape);
            confirm = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
            if (Input.GetKeyDown(KeyCode.A)) horizontal = -1;
            else if (Input.GetKeyDown(KeyCode.D)) horizontal = 1;
            else if (Input.GetKeyDown(KeyCode.W)) vertical = -1;
            else if (Input.GetKeyDown(KeyCode.S)) vertical = 1;
#endif
            if (back) navigation.Back();
            else if (horizontal != 0 || vertical != 0) navigation.Move(horizontal, vertical);
            else if (confirm) navigation.Confirm();
        }

        void HoverAt(Vector2 screen)
        {
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 800f);
            Vector2 point = new Vector2((screen.x - (Screen.width - 1280 * scale) / 2) / scale,
                (Screen.height - screen.y - (Screen.height - 800 * scale) / 2) / scale);
            var choices = navigation.Choices();
            for (int i = 0; i < choices.Count; i++)
                if (choices[i].Enabled && choices[i].Rect.Contains(point)) { navigation.Hover(i); return; }
            if (navigation.ChoosingTarget && !navigation.TargetsAll)
            {
                var targets = navigation.TargetCandidates();
                for (int i = 0; i < targets.Length; i++)
                    if (FighterRect(targets[i], scale).Contains(point)) { navigation.Hover(i); return; }
            }
        }

        bool CanClickFighter(Combatant fighter)
        {
            if (navigation.ChoosingTarget)
                return navigation.TargetCandidates().Contains(fighter);
            if (!fighter.Alive) return false;
            return battle.Enemies.Contains(fighter) || fighter == battle.Formation.NextActor;
        }

        Rect FighterRect(Combatant fighter, float scale)
        {
            // Screen-space picking uses the visible capsule, without reintroducing physics colliders.
            var bounds = fighter.Motion.GetComponent<Renderer>().bounds;
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 world = bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                Vector3 screen = battle.Presentation.Camera.WorldToScreenPoint(world);
                Vector2 point = new Vector2((screen.x - (Screen.width - 1280 * scale) / 2) / scale,
                    (Screen.height - screen.y - (Screen.height - 800 * scale) / 2) / scale);
                min = Vector2.Min(min, point); max = Vector2.Max(max, point);
            }
            // Top status cards and bottom command UI take priority over the 3D scene.
            return Rect.MinMaxRect(min.x - 5, Mathf.Max(222, min.y - 5), max.x + 5, Mathf.Max(222, Mathf.Min(485, max.y + 5)));
        }

        void DrawArrow(Rect rect, bool downward = false)
            => DrawArrow(rect, downward, Color.yellow);

        void DrawArrow(Rect rect, bool downward, Color arrowColor)
        {
            if (arrowStyle == null)
            {
                arrowStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 22,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    clipping = TextClipping.Overflow
                };
            }
            Color previous = GUI.color;
            GUI.color = arrowColor;
            GUI.Label(rect, downward ? "▼" : "▶", arrowStyle);
            GUI.color = previous;
        }

        void DrawCommands()
        {
            GUI.Label(new Rect(24, 540, 710, 30), "Active: " + battle.Active.Name + "   |   " + navigation.Screen + (battle.Busy ? " (resolving...)" : ""), label);
            GUI.enabled = battle.Ready;
            if (GUI.Button(new Rect(748, 538, 125, 32), "Back [Esc]", button)) { navigation.Back(); GUI.enabled = true; return; }
            var choices = navigation.Choices();
            int originalFontSize = button.fontSize;
            if (navigation.Screen == CombatScreen.EnemyTarget) button.fontSize = 14;
            for (int i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                GUI.enabled = choice.Enabled;
                Color background = GUI.backgroundColor;
                if (i == navigation.Cursor && choice.Enabled) GUI.backgroundColor = new Color(1, 0.9f, 0.45f);
                bool clicked = GUI.Button(choice.Rect, choice.Text, button);
                GUI.backgroundColor = background;
                // During ally/enemy targeting the world-space arrow marks the chosen
                // character, so do not also draw the menu-side arrow.
                if (choice.Enabled && i == navigation.Cursor && !navigation.ChoosingTarget)
                    DrawArrow(new Rect(choice.Rect.x - 27, choice.Rect.center.y - 12, 24, 24));
                if (clicked) { button.fontSize = originalFontSize; navigation.Activate(i); GUI.enabled = true; return; }
            }
            button.fontSize = originalFontSize;
            GUI.enabled = true;
            if (navigation.ChoosingTarget && !string.IsNullOrEmpty(navigation.TargetSpellDescription))
            {
                float descriptionY;
                float descriptionHeight;
                if (navigation.Screen == CombatScreen.AllyTarget)
                { descriptionY = 650; descriptionHeight = 104; }
                else if (navigation.TargetsAll)
                { descriptionY = 630; descriptionHeight = 124; }
                else if (navigation.TargetCandidates().Length > 3)
                { descriptionY = 716; descriptionHeight = 44; }
                else
                { descriptionY = 644; descriptionHeight = 116; }
                var descriptionRect = new Rect(24, descriptionY, 850, descriptionHeight);
                GUI.Box(descriptionRect, "");
                GUI.Label(new Rect(descriptionRect.x + 10, descriptionRect.y + 5, descriptionRect.width - 20,
                    descriptionRect.height - 10), navigation.TargetSpellDescription, small);
            }
            Rect helpRect = navigation.Screen == CombatScreen.Magic || navigation.Screen == CombatScreen.Sync
                ? new Rect(24, 764, 850, 24) : navigation.ChoosingTarget
                    ? new Rect(24, 764, 850, 24) : new Rect(24, 710, 850, 74);
            GUI.Label(helpRect, navigation.Help, small);
        }

    }
}
