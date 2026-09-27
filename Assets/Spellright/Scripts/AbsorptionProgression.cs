using System.Collections.Generic;
using System.Linq;

namespace Spellright
{
    // Prototype-only battle-to-battle spell learning. Restarting an encounter preserves these ranks.
    public class AbsorptionProgression
    {
        readonly Dictionary<string, int> levels = new Dictionary<string, int>();
        public int Level(string ability) => levels.TryGetValue(ability, out int value) ? value : 0;
        public int Absorb(string ability)
        {
            int next = Level(ability) + 1;
            levels[ability] = next;
            return next;
        }
        public Spell[] SpellsFor(Element element, CombatSettings settings)
        {
            return levels.Where(pair => SpellLibrary.Find(settings, pair.Key)?.Element == element)
                .Select(pair => SpellLibrary.Find(settings, pair.Key)).Where(spell => spell != null).ToArray();
        }
    }
}
