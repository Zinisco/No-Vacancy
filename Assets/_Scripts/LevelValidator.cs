using System.Collections.Generic;
using UnityEngine;

public static class LevelValidator
{
    public static bool CanSolve(List<GeneratedRoomData> slots, List<GeneratedGuestData> guests)
    {
        if (slots == null || guests == null)
            return false;

        List<GeneratedRoomData> rooms = GetRoomSlots(slots);

        if (guests.Count > rooms.Count)
            return false;

        int topFloor = GetTopFloorIndex(rooms);

        for (int i = 0; i < rooms.Count; i++)
        {
            if (RoomTraitRules.HasAnyConflict(rooms[i].traits))
                return false;
        }

        for (int i = 0; i < guests.Count; i++)
        {
            if (RoomTraitRules.HasAnyConflict(guests[i].preferredTraits))
                return false;
        }

        for (int i = 0; i < guests.Count; i++)
        {
            if (CountMatchingRoomsForGuest(rooms, guests[i], topFloor) == 0)
                return false;
        }

        return true;
    }

    public static bool HasUniqueSolution(List<GeneratedRoomData> slots, List<GeneratedGuestData> guests)
    {
        if (!CanSolve(slots, guests))
            return false;

        List<GeneratedRoomData> rooms = GetRoomSlots(slots);
        int solutionCount = CountSolutions(rooms, guests, 2);
        return solutionCount == 1;
    }

    public static bool IsInterestingPuzzle(List<GeneratedRoomData> slots, List<GeneratedGuestData> guests, float ambiguityThreshold = 0.5f)
    {
        if (!CanSolve(slots, guests))
            return false;

        List<GeneratedRoomData> rooms = GetRoomSlots(slots);
        int topFloor = GetTopFloorIndex(rooms);

        int ambiguousGuests = 0;

        for (int i = 0; i < guests.Count; i++)
        {
            int matches = CountMatchingRoomsForGuest(rooms, guests[i], topFloor);

            if (matches == 0)
                return false;

            if (matches >= 2)
                ambiguousGuests++;
        }

        int requiredAmbiguous = Mathf.CeilToInt(guests.Count * ambiguityThreshold);
        if (ambiguousGuests < requiredAmbiguous)
            return false;

        int solutionCount = CountSolutions(rooms, guests, 2);
        return solutionCount == 1;
    }

    public static int CountMatchingRoomsForGuest(List<GeneratedRoomData> slots, GeneratedGuestData guest)
    {
        if (slots == null || guest == null)
            return 0;

        int topFloor = GetTopFloorIndex(slots);
        return CountMatchingRoomsForGuest(slots, guest, topFloor);
    }

    private static int CountMatchingRoomsForGuest(List<GeneratedRoomData> slots, GeneratedGuestData guest, int topFloor)
    {
        if (slots == null || guest == null)
            return 0;

        int count = 0;

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].slotType != SlotType.Room)
                continue;

            if (RoomMatchesGuest(slots[i], guest, topFloor))
                count++;
        }

        return count;
    }

    private static int CountSolutions(List<GeneratedRoomData> rooms, List<GeneratedGuestData> guests, int stopAfter)
    {
        bool[] usedRooms = new bool[rooms.Count];
        int topFloor = GetTopFloorIndex(rooms);
        List<int> guestOrder = BuildMostConstrainedGuestOrder(rooms, guests, topFloor);

        return CountSolutionsRecursive(rooms, guests, guestOrder, 0, usedRooms, stopAfter, topFloor);
    }

    private static int CountSolutionsRecursive(
        List<GeneratedRoomData> rooms,
        List<GeneratedGuestData> guests,
        List<int> guestOrder,
        int guestDepth,
        bool[] usedRooms,
        int stopAfter,
        int topFloor)
    {
        if (guestDepth >= guestOrder.Count)
            return 1;

        int guestIndex = guestOrder[guestDepth];
        GeneratedGuestData guest = guests[guestIndex];

        int totalSolutions = 0;

        for (int roomIndex = 0; roomIndex < rooms.Count; roomIndex++)
        {
            if (usedRooms[roomIndex])
                continue;

            if (!RoomMatchesGuest(rooms[roomIndex], guest, topFloor))
                continue;

            usedRooms[roomIndex] = true;

            totalSolutions += CountSolutionsRecursive(
                rooms,
                guests,
                guestOrder,
                guestDepth + 1,
                usedRooms,
                stopAfter,
                topFloor
            );

            usedRooms[roomIndex] = false;

            if (totalSolutions >= stopAfter)
                return totalSolutions;
        }

        return totalSolutions;
    }

    private static List<int> BuildMostConstrainedGuestOrder(List<GeneratedRoomData> rooms, List<GeneratedGuestData> guests, int topFloor)
    {
        List<int> indices = new List<int>();

        for (int i = 0; i < guests.Count; i++)
            indices.Add(i);

        indices.Sort((a, b) =>
        {
            int countA = CountMatchingRoomsForGuest(rooms, guests[a], topFloor);
            int countB = CountMatchingRoomsForGuest(rooms, guests[b], topFloor);

            return countA.CompareTo(countB);
        });

        return indices;
    }

    private static List<GeneratedRoomData> GetRoomSlots(List<GeneratedRoomData> slots)
    {
        List<GeneratedRoomData> rooms = new();

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].slotType == SlotType.Room)
                rooms.Add(slots[i]);
        }

        return rooms;
    }

    private static bool RoomMatchesGuest(GeneratedRoomData room, GeneratedGuestData guest, int topFloor)
    {
        for (int i = 0; i < guest.preferredTraits.Count; i++)
        {
            if (!room.traits.Contains(guest.preferredTraits[i]))
                return false;
        }

        for (int i = 0; i < guest.preferredFloorPreferences.Count; i++)
        {
            if (!RoomMatchesFloorPreference(room, guest.preferredFloorPreferences[i], topFloor))
                return false;
        }

        return true;
    }

    private static bool RoomMatchesFloorPreference(GeneratedRoomData room, FloorPreference preference, int topFloor)
    {
        switch (preference)
        {
            case FloorPreference.FirstFloor:
                return room.floorIndex == 1;

            case FloorPreference.SecondFloor:
                return room.floorIndex == 2;

            case FloorPreference.TopFloor:
                return room.floorIndex == topFloor;

            default:
                return false;
        }
    }

    private static int GetTopFloorIndex(List<GeneratedRoomData> rooms)
    {
        int topFloor = 1;

        for (int i = 0; i < rooms.Count; i++)
        {
            if (rooms[i].floorIndex > topFloor)
                topFloor = rooms[i].floorIndex;
        }

        return topFloor;
    }
}