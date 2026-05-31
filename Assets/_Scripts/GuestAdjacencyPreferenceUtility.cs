public static class GuestAdjacencyPreferenceUtility
{
    public static string GetDisplayName(GuestAdjacencyPreference preference)
    {
        switch (preference.type)
        {
            case GuestAdjacencyPreferenceType.HatesSmokingNeighbor:
                return "Hates Smoking";

            case GuestAdjacencyPreferenceType.HatesLoudNeighbor:
                return "Hates Loud Noises";

            case GuestAdjacencyPreferenceType.WantsNamedGuestNeighbor:
                return $"Wants to be near {preference.targetGuestName}";

            default:
                return preference.type.ToString();
        }
    }
}