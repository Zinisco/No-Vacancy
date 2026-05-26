public static class RoomTraitUtility
{
    public static string GetDisplayName(RoomTrait trait)
    {
        switch (trait)
        {
            case RoomTrait.QueenBed: return "Queen Bed";
            case RoomTrait.KingBed: return "King Bed";
            case RoomTrait.Luxury: return "Luxury Room";
            case RoomTrait.Cheap: return "Cheap Room";
            case RoomTrait.ScenicView: return "Scenic View";
            case RoomTrait.NearElevator: return "Near Elevator";
            case RoomTrait.NearPool: return "Near Pool";
            case RoomTrait.Balcony: return "Has Balcony";
            case RoomTrait.PetFriendly: return "Pet Friendly";
            case RoomTrait.Dirty: return "Dirty Room";
            case RoomTrait.HotTub: return "Has Hot Tub";
            default: return trait.ToString();
        }
    }
}