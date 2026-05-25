public static class RoomTraitUtility
{
    public static string GetDisplayName(RoomTrait trait)
    {
        switch (trait)
        {
            case RoomTrait.QueenBed: return "Queen Bed";
            case RoomTrait.KingBed: return "King Bed";
            case RoomTrait.Luxury: return "Luxury";
            case RoomTrait.Cheap: return "Cheap";
            case RoomTrait.Scenic: return "Scenic";
            case RoomTrait.NearElevator: return "Near Elevator";
            case RoomTrait.NearPool: return "Near Pool";
            case RoomTrait.Balcony: return "Balcony";
            case RoomTrait.PetFriendly: return "Pet Friendly";
            case RoomTrait.Dirty: return "Dirty";
            default: return trait.ToString();
        }
    }
}