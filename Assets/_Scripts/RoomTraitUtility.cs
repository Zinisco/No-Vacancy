public static class RoomTraitUtility
{
    public static string GetDisplayName(RoomTrait trait)
    {
        switch (trait)
        {
            case RoomTrait.OneBed: return "One Bed";
            case RoomTrait.TwoBeds: return "Two Beds";
            case RoomTrait.Luxury: return "Luxury";
            case RoomTrait.Budget: return "Budget";
            case RoomTrait.Scenic: return "Scenic";
            case RoomTrait.Smoking: return "Smoking";
            case RoomTrait.NonSmoking: return "Non-Smoking";
            case RoomTrait.NearElevator: return "Near Elevator";
            case RoomTrait.NearPool: return "Near Pool";
            case RoomTrait.Balcony: return "Balcony";
            default: return trait.ToString();
        }
    }
}