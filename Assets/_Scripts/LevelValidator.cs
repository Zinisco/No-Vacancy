using System.Collections.Generic;

public static class LevelValidator
{
    public static bool CanSolve(List<GeneratedRoomData> slots, List<GeneratedGuestData> guests)
    {
        if (slots == null || guests == null)
            return false;

        List<GeneratedRoomData> rooms = new();

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].slotType == SlotType.Room)
                rooms.Add(slots[i]);
        }

        if (guests.Count > rooms.Count)
            return false;

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

        List<int> usedRoomIndices = new();

        for (int i = 0; i < guests.Count; i++)
        {
            bool foundMatch = false;

            for (int r = 0; r < rooms.Count; r++)
            {
                if (usedRoomIndices.Contains(r))
                    continue;

                if (RoomMatchesGuest(rooms[r], guests[i]))
                {
                    usedRoomIndices.Add(r);
                    foundMatch = true;
                    break;
                }
            }

            if (!foundMatch)
                return false;
        }

        return true;
    }

    private static bool RoomMatchesGuest(GeneratedRoomData room, GeneratedGuestData guest)
    {
        for (int i = 0; i < guest.preferredTraits.Count; i++)
        {
            if (!room.traits.Contains(guest.preferredTraits[i]))
                return false;
        }

        return true;
    }
}