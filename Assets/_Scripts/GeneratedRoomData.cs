using System.Collections.Generic;

public class GeneratedRoomData
{
    public string roomNumber;
    public SlotType slotType = SlotType.Room;

    public int floorIndex;
    public int columnIndex;

    public List<RoomTrait> traits = new List<RoomTrait>();
}

public class GeneratedGuestData
{
    public string guestName;
    public List<RoomTrait> preferredTraits = new List<RoomTrait>();
    public List<FloorPreference> preferredFloorPreferences = new List<FloorPreference>();
}