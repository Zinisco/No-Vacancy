
using System.Collections.Generic;
using UnityEngine;

public static class RuntimeLevelGenerator
{
    public static LevelConfig Generate(
        LevelGeneratorSettings settings,
        int seed,
        int maxAttempts = 300)
    {
        if (settings == null || settings.ruleset == null ||
            settings.ruleset.allowedTraits == null ||
            settings.ruleset.allowedTraits.Count == 0 ||
            settings.possibleGuestNames == null ||
            settings.possibleGuestNames.Count == 0 ||
            settings.floorCount <= 0 ||
            settings.roomsPerFloor <= 0 ||
            settings.guestCount <= 0)
        {
            Debug.LogError("Invalid level generator settings.");
            return null;
        }

        // Preserve the random state used by the rest of the game.
        Random.State previousState = Random.state;

        try
        {
            // Same seed + same settings = same generated puzzle.
            Random.InitState(seed);

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                List<GeneratedRoomData> rooms =
                    LevelGenerator.GenerateRooms(settings);

                List<GeneratedGuestData> guests =
                    LevelGenerator.GenerateGuests(settings, rooms);

                if (!LevelValidator.IsInterestingPuzzle(rooms, guests))
                    continue;

                return CreateLevelConfig(rooms, guests);
            }

            Debug.LogError(
                $"Failed to generate a valid puzzle after {maxAttempts} attempts.");

            return null;
        }
        finally
        {
            Random.state = previousState;
        }
    }

    private static LevelConfig CreateLevelConfig(
        List<GeneratedRoomData> rooms,
        List<GeneratedGuestData> guests)
    {
        LevelConfig level = ScriptableObject.CreateInstance<LevelConfig>();

        foreach (GeneratedRoomData room in rooms)
        {
            level.rooms.Add(new LevelRoomEntry
            {
                roomNumber = room.roomNumber,
                slotType = room.slotType,
                availability = room.availability,
                elevatorStatus = room.elevatorStatus,
                floorIndex = room.floorIndex,
                columnIndex = room.columnIndex,
                traits = new List<RoomTrait>(room.traits)
            });
        }

        foreach (GeneratedGuestData guest in guests)
        {
            List<GuestAdjacencyPreference> adjacency = new();

            foreach (GuestAdjacencyPreference pref in guest.adjacencyPreferences)
            {
                adjacency.Add(new GuestAdjacencyPreference
                {
                    type = pref.type,
                    targetGuestName = pref.targetGuestName
                });
            }

            level.guests.Add(new LevelGuestEntry
            {
                guestName = guest.guestName,
                preferredTraits =
                    new List<RoomTrait>(guest.preferredTraits),
                preferredFloorPreferences =
                    new List<FloorPreference>(guest.preferredFloorPreferences),
                adjacencyPreferences = adjacency,
                behaviorTraits =
                    new List<GuestBehaviorTrait>(guest.behaviorTraits),
                requirements =
                    new List<GuestRequirement>(guest.requirements)
            });
        }

        return level;
    }
}
