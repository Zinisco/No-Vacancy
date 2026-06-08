using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelConfig", menuName = "No Vacancy/Level Config")]
public class LevelConfig : ScriptableObject
{
    public List<LevelRoomEntry> rooms = new List<LevelRoomEntry>();
    public List<LevelGuestEntry> guests = new List<LevelGuestEntry>();
}

[Serializable]
public class LevelRoomEntry
{
    public string roomNumber;
    public SlotType slotType = SlotType.Room;

    public RoomAvailability availability = RoomAvailability.Open;
    public ElevatorStatus elevatorStatus = ElevatorStatus.Working;

    public int floorIndex;
    public int columnIndex;

    public List<RoomTrait> traits = new List<RoomTrait>();
}

[Serializable]
public class LevelGuestEntry
{
    public string guestName;
    public List<RoomTrait> preferredTraits = new List<RoomTrait>();
    public List<FloorPreference> preferredFloorPreferences = new List<FloorPreference>();
    public List<GuestAdjacencyPreference> adjacencyPreferences = new List<GuestAdjacencyPreference>();
    public List<GuestBehaviorTrait> behaviorTraits = new List<GuestBehaviorTrait>();
    public List<GuestRequirement> requirements = new();
}

[Serializable]
public class ClosedRoomRule
{
    public int floorIndex = 1;
    public int closedRoomCount = 0;
}