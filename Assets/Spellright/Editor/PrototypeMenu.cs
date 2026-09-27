using UnityEditor;
using UnityEditor.SceneManagement;

namespace Spellright.Editor
{
    public static class PrototypeMenu
    {
        [MenuItem("Spellright/Open Combat Prototype")]
        public static void Open()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene("Assets/Spellright/Scenes/SpellrightCombat.unity");
        }
        [MenuItem("Spellright/Select Combat Settings")]
        public static void Settings() { Selection.activeObject = AssetDatabase.LoadAssetAtPath<CombatSettings>("Assets/Spellright/CombatSettings.asset"); }
    }
}
