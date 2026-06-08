public static class GuestRequirementUtility
{
    public static string GetDisplayName(GuestRequirement requirement)
    {
        switch (requirement)
        {
            case GuestRequirement.WantsCleanRoom:
                return "Wants Clean Room";

            case GuestRequirement.HatesElevatorNoise:
                return "Hates Elevator Noise";

            default:
                return requirement.ToString();
        }
    }
}