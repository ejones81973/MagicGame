using System;
using UnityEngine;

namespace Spellright
{
    /// <summary>Resources references retain the shaders and variants used by procedural geometry in every build.</summary>
    public static class RuntimeMaterials
    {
        public static Material Lit(Color color, bool emissive = false)
            => Create(emissive ? "LitEmissive" : "Lit", color);

        public static Material Unlit(Color color, bool transparent = false)
            => Create(transparent ? "UnlitTransparent" : "Unlit", color);

        static Material Create(string name, Color color)
        {
            var template = Resources.Load<Material>("SpellrightMaterials/" + name);
            if (!template || !template.shader)
                throw new InvalidOperationException("Missing runtime material: Resources/SpellrightMaterials/" + name);
            return new Material(template) { color = color };
        }
    }
}
