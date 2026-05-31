public static class GuestRequirementUtility
{
    public static string GetDisplayName(GuestRequirement requirement)
    {
        switch (requirement)
        {
            case GuestRequirement.HatesDirtyRoom:
                return "Hates Dirty Rooms";

            case GuestRequirement.HatesElevatorNoise:
                return "Hates Elevator Noise";

            default:
                return requirement.ToString();
        }
    }
}