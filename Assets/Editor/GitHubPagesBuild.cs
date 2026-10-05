using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Spellright.Editor
{
    /// <summary>Builds the two playable scenes into the folders consumed by the Pages menu.</summary>
    public static class GitHubPagesBuild
    {
        const string BattleScene = "Assets/Spellright/Scenes/SpellrightCombat.unity";
        const string OverworldScene = "Assets/Spellright/Scenes/OverworldTest.unity";

        [MenuItem("Spellright/Build Pages WebGL (Battle + Overworld)")]
        public static void BuildAll()
        {
            AssetDatabase.Refresh();
            foreach (string name in new[] { "Lit", "LitEmissive", "Unlit", "UnlitTransparent" })
            {
                string path = "Assets/Spellright/Resources/SpellrightMaterials/" + name + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!material || !material.shader)
                    throw new BuildFailedException("Pages runtime material or shader is missing: " + path);
            }
            // GitHub Pages does not set Content-Encoding for Unity's compressed payload files.
            // An uncompressed WebGL payload works with its static hosting headers.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            Build("battle", BattleScene);
            Build("overworld", OverworldScene);
        }

        static void Build(string folder, string scene)
        {
            if (!File.Exists(scene))
                throw new BuildFailedException("Pages build scene is missing: " + scene);

            string output = Path.GetFullPath(Path.Combine("docs", folder));
            Directory.CreateDirectory(output);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { scene },
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("WebGL build failed for " + folder + ": " + report.summary.result);

            Debug.Log("GitHub Pages WebGL build complete: " + output);
        }
    }
}
