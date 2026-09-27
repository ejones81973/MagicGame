using UnityEngine;

namespace Spellright
{
    public partial class BattleUI
    {
        GUIStyle drainTitle, drainText;
        void DrawDrain()
        {
            GUI.color = new Color(.015f, .025f, .06f, .94f);
            GUI.DrawTexture(new Rect(0, 0, 1280, 800), Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (drainTitle == null)
            {
                drainTitle = new GUIStyle(GUI.skin.label) { fontSize = 31, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                drainText = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            }
            var drain = battle.Drain;
            if (drain.MatchingHero == null)
            {
                GUI.Label(new Rect(250, 300, 780, 110), battle.Presentation.Feedback, drainTitle);
                return;
            }
            GUI.Label(new Rect(250, 42, 780, 48), "MAGICAL DRAIN", drainTitle);
            GUI.Label(new Rect(250, 92, 780, 34), drain.Enemy.Name + "  |  " + drain.Enemy.Element + " essence", drainText);
            Spell reward = battle.Settings.FindSpell(drain.Enemy.AbsorbableSpellId);
            GUI.Label(new Rect(250, 127, 780, 32), "Matching user: " + drain.MatchingHero.Name + "     Spell: " + (reward != null ? reward.Name : "Unconfigured"), drainText);

            const float cx = 640, cy = 410, radius = 154;
            // The pale dotted ridge is a physical-looking track; green dots show the moving catch window.
            for (int i = 0; i < 90; i++)
            {
                float angle = i * 4f;
                DrawDrainDot(cx, cy, radius, angle, 5, new Color(.38f, .48f, .58f));
            }
            int arcDots = Mathf.CeilToInt(battle.Settings.drainGreenArcDegrees / 4f);
            for (int i = -arcDots / 2; i <= arcDots / 2; i++)
                DrawDrainDot(cx, cy, radius, drain.GreenAngle + i * 4f, 7, new Color(.2f, 1f, .34f));
            DrawDrainDot(cx, cy, radius, drain.PointerAngle, 13, new Color(.35f, .8f, 1f));

            // Fill the hollow ring interior from the top down as timing is maintained.
            float inner = 125;
            float filledHeight = inner * 2 * drain.Fill;
            int rows = Mathf.Clamp(Mathf.CeilToInt(filledHeight / 3f), 0, 84);
            for (int row = 0; row < rows; row++)
            {
                float y = -inner + row * 3f;
                if (y > -inner + filledHeight) break;
                float halfWidth = Mathf.Sqrt(Mathf.Max(0, inner * inner - y * y));
                GUI.color = Color.Lerp(new Color(.05f, .62f, .22f), new Color(.23f, 1f, .42f), 1f - row / 84f);
                GUI.DrawTexture(new Rect(cx - halfWidth, cy + y, halfWidth * 2, 3.3f), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
            GUI.Label(new Rect(cx - 100, cy - 30, 200, 58), Mathf.RoundToInt(drain.Fill * 100) + "%", drainTitle);
            GUI.Label(new Rect(440, 593, 400, 32), "Time   " + Mathf.CeilToInt(drain.Remaining) + "s", drainText);
            GUI.Label(new Rect(250, 640, 780, 56), "Keep the blue pointer in the green spot. It moves faster than the green spot.\nPress SPACE to reverse the pointer. Outside the green area, progress drains.", drainText);
            if (GUI.Button(new Rect(515, 710, 250, 50), "REVERSE  [SPACE]", button)) drain.Reverse();
            GUI.Label(new Rect(24, 752, 1232, 28), "Failure: the essence escapes. Success: learn the matching spell or strengthen its level.", small);
        }

        void DrawDrainDot(float cx, float cy, float radius, float degrees, float size, Color color)
        {
            float angle = degrees * Mathf.Deg2Rad;
            GUI.color = color;
            GUI.DrawTexture(new Rect(cx + Mathf.Cos(angle) * radius - size / 2, cy + Mathf.Sin(angle) * radius - size / 2, size, size), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
