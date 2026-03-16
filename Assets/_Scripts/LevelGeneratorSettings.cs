using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelGeneratorSettings", menuName = "No Vacancy/Level Generator Settings")]
public class LevelGeneratorSettings : ScriptableObject
{
    [Header("Output")]
    public string outputFolder = "Assets/Levels";
    public string levelNamePrefix = "GeneratedLevel";

    [Header("Layout")]
    public int floorCount = 3;
    public int roomsPerFloor = 3;
    public bool topFloorFirst = false;

    [Tooltip("Column index where the elevator is inserted. 0 = before first room, 1 = after first room, etc.")]
    public int elevatorColumnIndex = 1;

    [Header("Generation")]
    public int minTraitsPerRoom = 2;
    public int maxTraitsPerRoom = 4;
    public int minPreferencesPerGuest = 2;
    public int maxPreferencesPerGuest = 3;

    [Header("Trait Pool")]
    public List<RoomTrait> allowedTraits = new List<RoomTrait>();

    [Header("Names")]
    public List<string> possibleGuestNames = new List<string>();

    void OnEnable()
    {
        if (possibleGuestNames == null || possibleGuestNames.Count == 0)
        {
            possibleGuestNames = new List<string>
            {
                "Alex","Avery","Blake","Casey","Chris","Drew","Evan","Finn","Gabe","Jack",
                "Jake","Jade","Joel","John","Jude","Kai","Kyle","Lane","Levi","Liam",
                "Luca","Luke","Milo","Nate","Noah","Owen","Reed","Rhett","Ryan","Sean",
                "Theo","Troy","Wade","Zane",

                "Ava","Chloe","Ella","Emma","Eva","Gia","Ivy","Jade","June","Kate",
                "Lana","Lena","Lila","Lily","Luna","Maya","Mia","Nina","Nora","Nova",
                "Rhea","Ruby","Sara","Skye","Tara","Vera","Zara"
            };
        }
    }
}