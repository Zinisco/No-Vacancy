using System.Collections.Generic;
using UnityEngine;

public static class LevelGenerator
{
    public static List<GeneratedRoomData> GenerateRooms(LevelGeneratorSettings settings)
    {
        List<GeneratedRoomData> rooms = new();

        if (settings == null)
            return rooms;

        if (settings.topFloorFirst)
        {
            for (int floor = settings.floorCount; floor >= 1; floor--)
            {
                for (int room = 1; room <= settings.roomsPerFloor; room++)
                {
                    GeneratedRoomData data = new GeneratedRoomData();
                    data.roomNumber = $"{floor}{room:00}";
                    data.traits = GenerateRoomTraits(settings);
                    rooms.Add(data);
                }
            }
        }
        else
        {
            for (int floor = 1; floor <= settings.floorCount; floor++)
            {
                for (int room = 1; room <= settings.roomsPerFloor; room++)
                {
                    GeneratedRoomData data = new GeneratedRoomData();
                    data.roomNumber = $"{floor}{room:00}";
                    data.traits = GenerateRoomTraits(settings);
                    rooms.Add(data);
                }
            }
        }

        return rooms;
    }

    private static List<RoomTrait> GenerateRoomTraits(LevelGeneratorSettings settings)
    {
        List<RoomTrait> pool = new List<RoomTrait>(settings.allowedTraits);
        Shuffle(pool);

        int targetCount = Random.Range(settings.minTraitsPerRoom, settings.maxTraitsPerRoom + 1);
        targetCount = Mathf.Clamp(targetCount, 1, pool.Count);

        List<RoomTrait> result = new();

        for (int i = 0; i < pool.Count; i++)
        {
            RoomTrait candidate = pool[i];

            if (result.Contains(candidate))
                continue;

            if (RoomTraitRules.ConflictsWithAny(candidate, result))
                continue;

            result.Add(candidate);

            if (result.Count >= targetCount)
                break;
        }

        // Safety fallback so we never return empty if settings are weird.
        if (result.Count == 0 && pool.Count > 0)
            result.Add(pool[0]);

        return result;
    }

    public static List<GeneratedGuestData> GenerateGuests(LevelGeneratorSettings settings, List<GeneratedRoomData> rooms)
    {
        List<GeneratedGuestData> guests = new();

        if (settings == null || rooms == null || rooms.Count == 0)
            return guests;

        int guestCount = rooms.Count;

        List<string> shuffledNames = new(settings.possibleGuestNames);
        Shuffle(shuffledNames);

        for (int i = 0; i < guestCount; i++)
        {
            GeneratedRoomData sourceRoom = rooms[i];

            GeneratedGuestData guest = new GeneratedGuestData();
            guest.guestName = i < shuffledNames.Count ? shuffledNames[i] : $"Guest {i + 1}";
            guest.preferredTraits = GenerateGuestPreferencesFromRoom(settings, sourceRoom);

            guests.Add(guest);
        }

        return guests;
    }

    private static List<RoomTrait> GenerateGuestPreferencesFromRoom(LevelGeneratorSettings settings, GeneratedRoomData room)
    {
        List<RoomTrait> shuffled = new List<RoomTrait>(room.traits);
        Shuffle(shuffled);

        int prefCount = Random.Range(settings.minPreferencesPerGuest, settings.maxPreferencesPerGuest + 1);
        prefCount = Mathf.Clamp(prefCount, 1, shuffled.Count);

        List<RoomTrait> prefs = new();

        for (int i = 0; i < shuffled.Count; i++)
        {
            RoomTrait candidate = shuffled[i];

            if (prefs.Contains(candidate))
                continue;

            if (RoomTraitRules.ConflictsWithAny(candidate, prefs))
                continue;

            prefs.Add(candidate);

            if (prefs.Count >= prefCount)
                break;
        }

        // Because prefs are chosen from room.traits, this should rarely matter,
        // but keep the same safety fallback.
        if (prefs.Count == 0 && shuffled.Count > 0)
            prefs.Add(shuffled[0]);

        return prefs;
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}