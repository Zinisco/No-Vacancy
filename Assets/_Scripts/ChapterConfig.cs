using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LevelDifficultyStep
{
    [Range(1, 10)]
    public int startingLevel = 1;

    [Header("Difficulty")]
    public int guestCount = 2;
    public int minPreferencesPerGuest = 1;
    public int maxPreferencesPerGuest = 1;

    [Header("Closed Rooms By Floor")]
    public List<ClosedRoomRule> closedRoomRules = new();

    [Header("New Room Traits")]
    public List<RoomTrait> unlockRoomTraits = new();

    [Header("New Guest Behaviors")]
    public List<GuestBehaviorTrait> unlockBehaviors = new();

    [Header("New Guest Requirements")]
    public List<GuestRequirement> unlockRequirements = new();

    [Header("Mechanics")]
    public bool unlockFloorPreferences;
    public bool unlockAdjacencyRules;
    public bool unlockBehaviorTraits;
    public bool unlockGuestRequirements;
}

[CreateAssetMenu(
    fileName = "ChapterConfig",
    menuName = "No Vacancy/Chapter Config")]
public class ChapterConfig : ScriptableObject
{
    public string chapterName;

    [Header("Chapter Settings")]
    [Min(1)]
    public int levelCount = 5;

    public LevelGeneratorSettings baseGeneratorSettings;

    [Header("Difficulty Milestones")]
    public List<LevelDifficultyStep> difficultySteps = new();
}