using UnityEngine;

namespace Spellright
{
    public class PrototypeBootstrap : MonoBehaviour
    {
        public CombatSettings settings;
        GameObject encounter;
        bool ownsSettings;
        readonly AbsorptionProgression absorption = new AbsorptionProgression();
        void Start()
        {
            if (!settings) { settings = ScriptableObject.CreateInstance<CombatSettings>(); ownsSettings = true; }
            Restart();
        }
        public void Restart()
        {
            if (encounter) { encounter.SetActive(false); Destroy(encounter); }
            encounter = new GameObject("Spellright Encounter"); encounter.transform.SetParent(transform);
            encounter.AddComponent<BattleFlow>().Initialize(settings, absorption);
        }
        void OnDestroy() { if (ownsSettings && settings) Destroy(settings); }
    }
}
