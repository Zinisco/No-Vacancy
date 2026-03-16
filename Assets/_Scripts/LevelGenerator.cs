using System.Collections.Generic;
using UnityEngine;

public static class LevelGenerator
{
    public static List<GeneratedRoomData> GenerateRooms(LevelGeneratorSettings settings)
    {
        List<GeneratedRoomData> slots = new();

        if (settings == null)
            return slots;

        int totalColumnsPerFloor = settings.roomsPerFloor + 1;
        int elevatorColumn = Mathf.Clamp(settings.elevatorColumnIndex, 0, settings.roomsPerFloor);

        if (settings.topFloorFirst)
        {
            for (int floor = settings.floorCount; floor >= 1; floor--)
            {
                GenerateFloor(settings, slots, floor, elevatorColumn, totalColumnsPerFloor);
            }
        }
        else
        {
            for (int floor = 1; floor <= settings.floorCount; floor++)
            {
                GenerateFloor(settings, slots, floor, elevatorColumn, totalColumnsPerFloor);
            }
        }

        ApplyNearElevatorTraits(slots);

        return slots;
    }

    private static void GenerateFloor(
    LevelGeneratorSettings settings,
    List<GeneratedRoomData> slots,
    int floorNumber,
    int elevatorColumn,
    int totalColumnsPerFloor)
    {
        List<GeneratedRoomData> floorSlots = new();

        for (int column = 0; column < totalColumnsPerFloor; column++)
        {
            if (column == elevatorColumn)
            {
                GeneratedRoomData elevator = new GeneratedRoomData
                {
                    roomNumber = $"E{floorNumber}",
                    slotType = SlotType.Elevator,
                    floorIndex = floorNumber,
                    columnIndex = column,
                    traits = new List<RoomTrait>()
                };

                floorSlots.Add(elevator);
            }
            else
            {
                GeneratedRoomData room = new GeneratedRoomData
                {
                    roomNumber = "",
                    slotType = SlotType.Room,
                    floorIndex = floorNumber,
                    columnIndex = column,
                    traits = GenerateRoomTraits(settings)
                };

                floorSlots.Add(room);
            }
        }

        AssignRoomNumbersByElevatorPattern(floorSlots, floorNumber, elevatorColumn);

        for (int i = 0; i < floorSlots.Count; i++)
            slots.Add(floorSlots[i]);
    }

    private static List<RoomTrait> GenerateRoomTraits(LevelGeneratorSettings settings)
    {
        List<RoomTrait> pool = new();

        for (int i = 0; i < settings.allowedTraits.Count; i++)
        {
            RoomTrait trait = settings.allowedTraits[i];

            // This trait is now derived from layout, not randomly generated.
            if (trait == RoomTrait.NearElevator)
                continue;

            pool.Add(trait);
        }

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

        if (result.Count == 0 && pool.Count > 0)
            result.Add(pool[0]);

        return result;
    }

    private static void AssignRoomNumbersByElevatorPattern(
     List<GeneratedRoomData> floorSlots,
     int floorNumber,
     int elevatorColumn)
    {
        List<GeneratedRoomData> leftRooms = new();
        List<GeneratedRoomData> rightRooms = new();

        for (int i = 0; i < floorSlots.Count; i++)
        {
            GeneratedRoomData slot = floorSlots[i];

            if (slot.slotType != SlotType.Room)
                continue;

            if (slot.columnIndex < elevatorColumn)
                leftRooms.Add(slot);
            else if (slot.columnIndex > elevatorColumn)
                rightRooms.Add(slot);
        }

        // Left side: closest to elevator gets smallest numbers first.
        // So sort descending by column so the room nearest elevator comes first.
        leftRooms.Sort((a, b) => b.columnIndex.CompareTo(a.columnIndex));

        // Right side: closest to elevator gets smallest numbers first.
        // So sort ascending by column.
        rightRooms.Sort((a, b) => a.columnIndex.CompareTo(b.columnIndex));

        int nextRoomNumber = 1;

        for (int i = 0; i < leftRooms.Count; i++)
        {
            leftRooms[i].roomNumber = $"{floorNumber}{nextRoomNumber:00}";
            nextRoomNumber++;
        }

        for (int i = 0; i < rightRooms.Count; i++)
        {
            rightRooms[i].roomNumber = $"{floorNumber}{nextRoomNumber:00}";
            nextRoomNumber++;
        }
    }

    private static void ApplyNearElevatorTraits(List<GeneratedRoomData> slots)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            GeneratedRoomData slot = slots[i];

            if (slot.slotType != SlotType.Room)
                continue;

            bool nearElevator = false;

            for (int j = 0; j < slots.Count; j++)
            {
                GeneratedRoomData other = slots[j];

                if (other.slotType != SlotType.Elevator)
                    continue;

                if (slot.floorIndex != other.floorIndex)
                    continue;

                if (Mathf.Abs(slot.columnIndex - other.columnIndex) == 1)
                {
                    nearElevator = true;
                    break;
                }
            }

            if (nearElevator && !slot.traits.Contains(RoomTrait.NearElevator))
                slot.traits.Add(RoomTrait.NearElevator);

            if (!nearElevator && slot.traits.Contains(RoomTrait.NearElevator))
                slot.traits.Remove(RoomTrait.NearElevator);
        }
    }

    public static List<GeneratedGuestData> GenerateGuests(LevelGeneratorSettings settings, List<GeneratedRoomData> slots)
    {
        List<GeneratedGuestData> guests = new();

        if (settings == null || slots == null || slots.Count == 0)
            return guests;

        List<GeneratedRoomData> actualRooms = new();

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].slotType == SlotType.Room)
                actualRooms.Add(slots[i]);
        }

        if (actualRooms.Count == 0)
            return guests;

        List<string> shuffledNames = new(settings.possibleGuestNames);
        Shuffle(shuffledNames);

        for (int i = 0; i < actualRooms.Count; i++)
        {
            GeneratedRoomData sourceRoom = actualRooms[i];

            GeneratedGuestData guest = new GeneratedGuestData
            {
                guestName = i < shuffledNames.Count ? shuffledNames[i] : $"Guest {i + 1}",
                preferredTraits = GenerateGuestPreferencesFromRoom(settings, sourceRoom)
            };

            guests.Add(guest);
        }

        return guests;
    }

    private static List<RoomTrait> GenerateGuestPreferencesFromRoom(LevelGeneratorSettings settings, GeneratedRoomData room)
    {
        List<RoomTrait> shuffled = new(room.traits);
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