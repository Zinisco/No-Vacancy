using System.Collections.Generic;

public class GeneratedRoomData
{
    public string roomNumber;
    public SlotType slotType = SlotType.Room;
    public RoomAvailability availability = RoomAvailability.Open;
    public ElevatorStatus elevatorStatus = ElevatorStatus.Working;

    public int floorIndex;
    public int columnIndex;

    public List<RoomTrait> traits = new List<RoomTrait>();
}

public class GeneratedGuestData
{
    public string guestName;
    public List<RoomTrait> preferredTraits = new();
    public List<FloorPreference> preferredFloorPreferences = new();

    public List<GuestAdjacencyPreference> adjacencyPreferences = new();
    public List<GuestBehaviorTrait> behaviorTraits = new();
    public List<GuestRequirement> requirements = new();
}