using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spellright
{
    public class BattleFlow : MonoBehaviour
    {
        public CombatSettings Settings { get; private set; }
        public readonly List<Combatant> Party = new List<Combatant>();
        public readonly List<Combatant> Enemies = new List<Combatant>();
        public readonly List<string> History = new List<string>();
        public FormationSystem Formation { get; private set; }
        public CounterSystem Counter { get; private set; }
        public BattlePresentation Presentation { get; private set; }
        public BattleActions Actions { get; private set; }
        public AbsorptionProgression Absorption { get; private set; }
        public AbsorptionMinigame Drain { get; private set; }
        public BattlePhase Phase { get; private set; }
        public bool Busy { get; private set; }
        public int Round { get; private set; }
        public int WaveNumber { get; private set; }
        public int ActiveIndex;
        public int EnemyTarget, AllyTarget;
        public Combatant Active => ActiveIndex >= 0 && ActiveIndex < Party.Count ? Party[ActiveIndex] : null;
        EnemyAttackSystem attacks;
        readonly Queue<Combatant> defeatedAbsorbableEnemies = new Queue<Combatant>();
        public void Initialize(CombatSettings settings, AbsorptionProgression absorption = null)
        {
            Settings = settings;
            Settings.EnsureSpellDefaults();
            Absorption = absorption ?? new AbsorptionProgression();
            Party.Add(new Combatant("Brim", Element.Fire, settings.brim, new Color(1, 0.3f, 0.12f)));
            Party.Add(new Combatant("Brooke", Element.Water, settings.brooke, new Color(0.15f, 0.65f, 1)));
            Party.Add(new Combatant("Blitz", Element.Electric, settings.blitz, new Color(1, 0.85f, 0.12f)));
            Presentation = gameObject.AddComponent<BattlePresentation>(); Presentation.Build(settings.runtimePrimitiveMaterial);
            for (int i = 0; i < Party.Count; i++) Presentation.CreateFighter(Party[i], Vector3.zero, settings, i * 0.23f);
            Formation = new FormationSystem(settings, Party); Formation.Apply();
            Counter = gameObject.AddComponent<CounterSystem>();
            Drain = gameObject.AddComponent<AbsorptionMinigame>(); Drain.Initialize(this);
            attacks = gameObject.AddComponent<EnemyAttackSystem>(); attacks.Initialize(this);
            Actions = new BattleActions(this);
            Round = 0; WaveNumber = 1; SpawnWave(); BeginParty();
            gameObject.AddComponent<BattleUI>().Initialize(this);
            Debug.Log("Spellright battle ready: " + Party.Count + " party members, " + Enemies.Count + " enemies, HUD initialized.");
        }
        public void Log(string message) { History.Add(message); if (History.Count > 6) History.RemoveAt(0); }
        public bool Ready => Phase == BattlePhase.Party && !Busy && ActiveIndex >= 0 && ActiveIndex < Party.Count &&
            Formation != null && Party[ActiveIndex] == Formation.NextActor;
        public void Select(int index) { if (Phase == BattlePhase.Party && !Busy && Party[index] == Formation.NextActor) ActiveIndex = index; }
        public void Perform(IEnumerator action) { if (Ready) { Busy = true; StartCoroutine(Resolve(action)); } }
        IEnumerator Resolve(IEnumerator action)
        {
            yield return action;
            Presentation.Refresh(Party.Concat(Enemies));
            yield return ResolvePendingDrains(BattlePhase.Party);
            if (CheckEnd()) yield break;
            if (!Enemies.Any(c => c.Alive)) { yield return AdvanceWave(); yield break; }
            Phase = BattlePhase.Party;
            if (!Enemies[EnemyTarget].Alive) EnemyTarget = Enemies.FindIndex(c => c.Alive);
            Busy = false;
            var nextActor = Formation.NextActor;
            var next = nextActor == null ? -1 : Party.IndexOf(nextActor);
            if (next < 0) { Busy = true; StartCoroutine(EnemyPhase()); }
            else ActiveIndex = next;
        }
        IEnumerator ResolvePendingDrains(BattlePhase returnPhase)
        {
            while (defeatedAbsorbableEnemies.Count > 0)
            {
                var defeated = defeatedAbsorbableEnemies.Dequeue();
                Phase = BattlePhase.Drain;
                Presentation.Say("A magical essence remains. Match its element to absorb it.", defeated.Color);
                yield return Drain.Run(defeated);
                Presentation.Refresh(Party.Concat(Enemies));
                Phase = returnPhase;
            }
        }
        public void EnemyDefeated(Combatant enemy)
        {
            if (enemy != null && !string.IsNullOrEmpty(enemy.AbsorbableSpellId) && !defeatedAbsorbableEnemies.Contains(enemy))
                defeatedAbsorbableEnemies.Enqueue(enemy);
        }
        IEnumerator EnemyPhase()
        {
            Phase = BattlePhase.Enemy; Log("Enemy Phase: watch physical contact. SPACE to Counter.");
            Actions.ProcessEnemyPhaseStart();
            yield return ResolvePendingDrains(BattlePhase.Enemy);
            var enemyOrder = Enemies.Where(c => c.Alive).OrderByDescending(c => c.SurgeTurns > 0).ToArray();
            for (int i = 0; i < enemyOrder.Length; i++)
            {
                if (CheckEnd()) yield break;
                if (enemyOrder[i].Alive) yield return attacks.Act(enemyOrder[i], i);
                if (CheckEnd()) yield break;
            }
            yield return ResolvePendingDrains(BattlePhase.Enemy);
            if (CheckEnd()) yield break;
            Actions.EndRoundStatuses();
            if (!Enemies.Any(c => c.Alive)) yield return AdvanceWave();
            else BeginParty();
        }
        void BeginParty()
        {
            Round++; Phase = BattlePhase.Party; Busy = false; Formation.ChangedThisPhase = false;
            foreach (var c in Party) { c.Acted = false; c.Shielded = false; c.Bubbled = c.BubbleAbsorb > 0; }
            Actions.ProcessPartyPhaseStart();
            if (!Party.Any(c => c.Alive)) { Finish("DEFEAT"); return; }
            foreach (var c in Party.Where(c => Formation.CanAct(c) && c.ShockTurns > 0))
            {
                if (Random.value < Settings.shockSkipChance)
                {
                    c.Acted = true;
                    Log(c.Name + " is disrupted by Shock and loses their action!");
                    Presentation.Say(c.Name + " is disrupted by Shock!", Color.yellow);
                    Presentation.ShowStatus(c, "SHOCKED!", Color.yellow);
                }
            }
            var nextActor = Formation.NextActor;
            if (nextActor == null) Formation.ClearPriority();
            ActiveIndex = nextActor == null ? -1 : Party.IndexOf(nextActor);
            Presentation.Say("Choose a character and command.", Color.white);
            Log("Party Phase " + Round);
            if (ActiveIndex < 0) { Busy = true; StartCoroutine(EnemyPhase()); }
        }
        bool CheckEnd()
        {
            if (Party.Any(c => c.Alive)) return false;
            Finish("DEFEAT"); return true;
        }

        void SpawnWave()
        {
            var prototypes = (Settings.enemies ?? new EnemyPrototype[0]).Where(p => p != null).ToArray();
            if (prototypes.Length == 0) prototypes = CombatSettings.CreateDefaultEnemies();
            int minimum = Mathf.Clamp(Settings.waveMinimumEnemies, 2, 5);
            int maximum = Mathf.Clamp(Settings.waveMaximumEnemies, minimum, 5);
            int count = ChooseWaveEnemyCount(minimum, maximum);
            bool minibossWave = Settings.minibossEveryWaves > 0 && WaveNumber % Settings.minibossEveryWaves == 0;
            var spells = Settings.spells ?? SpellLibrary.CreateDefaults();
            for (int i = 0; i < count; i++)
            {
                var prototype = prototypes[Random.Range(0, prototypes.Length)];
                var compatible = spells.Where(s => s != null && s.Element == prototype.Element && s.Element != Element.None &&
                    !IsEnemyForbiddenSpell(s)).ToList();
                if (compatible.Count == 0) compatible = SpellLibrary.CreateDefaults().Where(s => s.Element == prototype.Element && !IsEnemyForbiddenSpell(s)).ToList();
                Spell first = compatible.FirstOrDefault(s => s.Id == prototype.AbsorbableSpellId);
                if (first == null && compatible.Count > 0) first = compatible[Random.Range(0, compatible.Count)];
                if (first != null) compatible.Remove(first);
                Spell second = compatible.Count > 0 ? compatible[Random.Range(0, compatible.Count)] : first;
                Spell reward = first == null ? null : (second != null && Random.value < .5f ? second : first);
                int baseHp = Mathf.Max(1, Settings.enemyHP);
                bool miniboss = minibossWave && i == 0;
                int hp = miniboss ? Mathf.RoundToInt(baseHp * Mathf.Max(1f, Settings.minibossHPMultiplier)) : baseHp;
                int defense = prototype.Defense + (miniboss ? Mathf.Max(0, Settings.minibossDefenseBonus) : 0);
                string baseName = (miniboss ? "MINIBOSS " : "") + prototype.Name;
                string name = baseName;
                int suffix = 2;
                while (Enemies.Any(e => e.Name == name)) name = baseName + " " + suffix++;
                var enemy = new Combatant(name, prototype.Element,
                    new Vector4(hp, 0, 0, defense), prototype.Color, reward != null ? reward.Id : null);
                enemy.EnemySpellIdA = first != null ? first.Id : null;
                enemy.EnemySpellIdB = second != null ? second.Id : null;
                Enemies.Add(enemy);
            }
            for (int i = 0; i < Enemies.Count; i++)
            {
                float spacing = Mathf.Min(2.7f, 9f / Mathf.Max(1, Enemies.Count - 1));
                Presentation.CreateFighter(Enemies[i], new Vector3(4, 1, (i - (Enemies.Count - 1) / 2f) * spacing), Settings, 0.15f + i * 0.31f);
                if (Enemies[i].Name.StartsWith("MINIBOSS") && Enemies[i].View.childCount > 0)
                    Enemies[i].View.GetChild(0).localScale *= 1.25f;
            }
            EnemyTarget = 0;
            string label = minibossWave ? "WAVE " + WaveNumber + " — MINIBOSS WAVE! " : "WAVE " + WaveNumber + " — ";
            Presentation.Say(label + count + " enemies approach.", minibossWave ? new Color(1f, .35f, .12f) : Color.white);
            Log(label + count + " enemies approach.");
        }

        int ChooseWaveEnemyCount(int minimum, int maximum)
        {
            float[] weights = { Settings.waveEnemyCountWeights.x, Settings.waveEnemyCountWeights.y,
                Settings.waveEnemyCountWeights.z, Settings.waveEnemyCountWeights.w };
            float total = 0;
            for (int count = minimum; count <= maximum; count++) total += Mathf.Max(0, weights[count - 2]);
            if (total <= 0) return Random.Range(minimum, maximum + 1);
            float roll = Random.value * total;
            for (int count = minimum; count <= maximum; count++)
            {
                roll -= Mathf.Max(0, weights[count - 2]);
                if (roll < 0) return count;
            }
            return maximum;
        }

        static bool IsEnemyForbiddenSpell(Spell spell)
            => spell.Target == SpellTarget.DownedAlly || spell.Effect == SpellEffect.SecondBreath ||
                spell.Effect == SpellEffect.Defibrillate || spell.Effect == SpellEffect.PhoenixFlame || spell.Synchronized;

        IEnumerator AdvanceWave()
        {
            Busy = true;
            Presentation.Say("WAVE " + WaveNumber + " CLEARED!", new Color(.45f, 1f, .55f));
            Log("Wave " + WaveNumber + " cleared.");
            yield return new WaitForSeconds(1.1f);
            foreach (var enemy in Enemies) Presentation.RemoveFighter(enemy);
            Enemies.Clear();
            WaveNumber++;
            SpawnWave();
            BeginParty();
        }
        public void Finish(string result)
        { Phase = BattlePhase.Finished; Busy = false; Counter.Cancel(); Presentation.Say(result + " - Restart to try again", Color.white); Log(result); }
    }
}
