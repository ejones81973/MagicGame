using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spellright
{
    public sealed class OverworldPuzzleDirector : MonoBehaviour
    {
        OverworldPartyManager party;
        OverworldPressurePlate[] spreadPlates;
        OverworldPuzzleDoor spreadDoor;
        OverworldElementalStation[] relayStations;
        OverworldPuzzleDoor[] relayGates;
        OverworldPressurePlate carryPlate;
        OverworldHeavyObject heavyCube;
        OverworldElementalStation[] mixedSpreadStations;
        OverworldMagicTarget mixedSteam;
        OverworldMagicTarget mixedStorm;
        OverworldPuzzleDoor mixedDoor;
        OverworldElementalStation[] lineStations;
        OverworldPuzzleDoor lineDoor, heavyDoor;
        public string ProgressMessage { get; private set; } = "";

        public void Configure(OverworldPartyManager owner, OverworldPressurePlate[] plates,
            OverworldPuzzleDoor plateDoor, OverworldElementalStation[] relay,
            OverworldPuzzleDoor[] relayDoors, OverworldPressurePlate heavyPlate,
            OverworldHeavyObject heavyObject, OverworldElementalStation[] mixedStations,
            OverworldMagicTarget steam, OverworldMagicTarget storm, OverworldPuzzleDoor finalDoor,
            OverworldElementalStation[] basicLineStations, OverworldPuzzleDoor basicLineDoor,
            OverworldPuzzleDoor carryDoor)
        {
            party = owner; spreadPlates = plates; spreadDoor = plateDoor;
            relayStations = relay; relayGates = relayDoors;
            carryPlate = heavyPlate; heavyCube = heavyObject;
            mixedSpreadStations = mixedStations; mixedSteam = steam; mixedStorm = storm; mixedDoor = finalDoor;
            lineStations = basicLineStations; lineDoor = basicLineDoor; heavyDoor = carryDoor;
        }

        void Update()
        {
            if (party == null) return;
            CheckSpreadPlates();
            CheckSpreadRelay();
            CheckLineStations();
            CheckHeavyCarry();
            CheckMixedPuzzle();
        }

        void CheckLineStations()
        {
            if (lineStations == null || lineStations.Length < 3 || !lineDoor) return;
            if (lineStations[0].Activated) lineStations[1].Available = true;
            if (lineStations[1].Activated) lineStations[2].Available = true;
            if (lineStations[2].Activated && !lineDoor.IsOpen)
            {
                lineDoor.Open();
                party.Say("Brim, Brooke, and Blitz activate their matching Line mechanisms.");
            }
        }

        void CheckSpreadPlates()
        {
            if (spreadPlates == null || spreadPlates.Length == 0) return;
            var occupiedMembers = new HashSet<int>();
            bool allOccupied = true;
            for (int i = 0; i < spreadPlates.Length; i++)
            {
                bool occupied = false;
                if (party.Formations.Current == FormationKind.Spread)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        var member = party.Character(j);
                        if (!occupiedMembers.Contains(j) && spreadPlates[i].Contains(member.transform.position))
                        { occupiedMembers.Add(j); occupied = true; break; }
                    }
                }
                spreadPlates[i].SetOccupied(occupied);
                allOccupied &= occupied;
            }
            if (allOccupied && !spreadDoor.IsOpen)
            {
                spreadDoor.Open();
                party.Say("Three heroes hold the plates. The Spread door opens.");
            }
        }

        void CheckSpreadRelay()
        {
            if (relayStations == null) return;
            for (int i = 0; i < relayStations.Length; i++)
            {
                if (!relayStations[i].Activated) break;
                if (i < relayGates.Length) relayGates[i].Open();
                if (i + 1 < relayStations.Length) relayStations[i + 1].Available = true;
            }
            if (relayStations.Length > 0 && relayStations.All(s => s.Activated))
                ProgressMessage = "Spread relay complete: Fire opened Water's route; Water opened Electric's route.";
        }

        void CheckHeavyCarry()
        {
            if (!carryPlate || !heavyCube) return;
            bool occupied = carryPlate.HeavyOnly && carryPlate.Contains(heavyCube.transform.position);
            carryPlate.SetOccupied(occupied);
            if (occupied && heavyDoor && !heavyDoor.IsOpen)
            {
                heavyDoor.Open();
                party.Say("Heavy cube presses the plate. Huddle route open.");
            }
        }

        void CheckMixedPuzzle()
        {
            if (mixedSpreadStations == null || mixedSteam == null || mixedStorm == null) return;
            bool allStations = mixedSpreadStations.All(s => s.Activated);
            mixedSteam.Available = allStations;
            if (mixedSteam.Activated) mixedStorm.Available = true;
            if (mixedStorm.Activated && mixedDoor && !mixedDoor.IsOpen)
            {
                mixedDoor.Open();
                party.Say("Spread, Totem, and synchronized Storm restored the station. The Huddle route is open.");
            }
        }
    }
}
