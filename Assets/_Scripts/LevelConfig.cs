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
    public List<RoomTrait> traits = new List<RoomTrait>();
}

[Serializable]
public class LevelGuestEntry
{
    public string guestName;
    public List<RoomTrait> preferredTraits = new List<RoomTrait>();
}