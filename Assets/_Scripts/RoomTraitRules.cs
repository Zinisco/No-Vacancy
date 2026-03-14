using System.Collections.Generic;

public static class RoomTraitRules
{
    private static readonly Dictionary<RoomTrait, HashSet<RoomTrait>> conflicts = new()
    {
        { RoomTrait.Smoking,      new HashSet<RoomTrait> { RoomTrait.NonSmoking } },
        { RoomTrait.NonSmoking,   new HashSet<RoomTrait> { RoomTrait.Smoking } },

        { RoomTrait.OneBed,       new HashSet<RoomTrait> { RoomTrait.TwoBeds } },
        { RoomTrait.TwoBeds,      new HashSet<RoomTrait> { RoomTrait.OneBed } },

        { RoomTrait.Budget,       new HashSet<RoomTrait> { RoomTrait.Luxury } },
        { RoomTrait.Luxury,       new HashSet<RoomTrait> { RoomTrait.Budget } },
    };

    public static bool ConflictsWithAny(RoomTrait candidate, List<RoomTrait> existingTraits)
    {
        if (existingTraits == null || existingTraits.Count == 0)
            return false;

        if (!conflicts.TryGetValue(candidate, out HashSet<RoomTrait> blocked))
            return false;

        for (int i = 0; i < existingTraits.Count; i++)
        {
            if (blocked.Contains(existingTraits[i]))
                return true;
        }

        return false;
    }

    public static bool HasAnyConflict(List<RoomTrait> traits)
    {
        if (traits == null || traits.Count <= 1)
            return false;

        for (int i = 0; i < traits.Count; i++)
        {
            for (int j = i + 1; j < traits.Count; j++)
            {
                if (AreConflicting(traits[i], traits[j]))
                    return true;
            }
        }

        return false;
    }

    public static bool AreConflicting(RoomTrait a, RoomTrait b)
    {
        if (conflicts.TryGetValue(a, out HashSet<RoomTrait> blockedA) && blockedA.Contains(b))
            return true;

        if (conflicts.TryGetValue(b, out HashSet<RoomTrait> blockedB) && blockedB.Contains(a))
            return true;

        return false;
    }
}