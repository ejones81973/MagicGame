using System;
using UnityEngine;

namespace Spellright
{
    public sealed class OverworldFormationController : MonoBehaviour
    {
        public FormationKind Current { get; private set; } = FormationKind.Line;
        readonly int[] totemOrder = { 0, 1, 2 };
        OverworldPartyManager party;
        public int Top => totemOrder[0];
        public int Middle => totemOrder[1];
        public int Bottom => totemOrder[2];
        public string TotemPair => party ? party.NameOf(Top) + " + " + party.NameOf(Middle) : "";

        public void Initialize(OverworldPartyManager owner) { party = owner; }

        public void SetFormation(FormationKind formation)
        {
            if (Current == formation) return;
            FormationKind previous = Current;
            var currentLeader = Current == FormationKind.Spread ? party.Controlled : party.Leader;
            if (formation == FormationKind.Totem && currentLeader)
                PutAtTop(currentLeader.Index);
            Current = formation;
            party.BeginFormationTransition(previous, formation);
        }

        public void RotateTotemOrder()
        {
            int top = totemOrder[0];
            totemOrder[0] = totemOrder[1];
            totemOrder[1] = totemOrder[2];
            totemOrder[2] = top;
            if (Current == FormationKind.Totem)
            {
                party.SetLeaderFromTotemTop(Top);
                party.BeginFormationTransition(Current, Current);
            }
        }

        void PutAtTop(int characterIndex)
        {
            int slot = SlotOf(characterIndex);
            if (slot <= 0) return;
            int selected = totemOrder[slot];
            while (slot > 0)
            {
                totemOrder[slot] = totemOrder[slot - 1];
                slot--;
            }
            totemOrder[0] = selected;
        }

        public int SlotOf(int characterIndex)
        {
            for (int i = 0; i < totemOrder.Length; i++)
                if (totemOrder[i] == characterIndex) return i;
            return -1;
        }

        public Vector3 GroupOffset(int characterIndex)
        {
            switch (Current)
            {
                case FormationKind.Totem:
                    return Vector3.up * (2 - SlotOf(characterIndex)) * 1.25f;
                case FormationKind.Huddle:
                    if (characterIndex == party.Leader.Index) return Vector3.zero;
                    int[] order = LineOrder(party.Leader.Index);
                    return characterIndex == order[1]
                        ? new Vector3(-.68f, 0, -.55f)
                        : new Vector3(.68f, 0, -.55f);
                default: return Vector3.zero;
            }
        }

        public int[] LineOrder(int leaderIndex) => new[] { leaderIndex, (leaderIndex + 1) % 3, (leaderIndex + 2) % 3 };
    }
}
