
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelProgressManager : MonoBehaviour
{
    public static LevelProgressManager Instance { get; private set; }

    public LevelConfig CurrentLevelConfig { get; private set; }
    public bool ShouldPlayIntroDialogue { get; private set; }

    [SerializeField] private string gameplaySceneName = "Game";
    [SerializeField] private List<ChapterConfig> chapters = new();

    private const string HighestUnlockedKey = "HighestUnlockedLevel";
    private const string StarKeyPrefix = "LevelStars_";
    private const string CurrentLevelKey = "CurrentStoryLevel";
    private const string PuzzleSeedKey = "CurrentPuzzleSeed";

    private int currentChapterIndex;
    private int currentLevelIndex;
    private int currentPuzzleSeed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Used when pressing Play directly in the Game scene.
    public void ConfigureForTesting(ChapterConfig chapter)
    {
        chapters.Clear();
        chapters.Add(chapter);
    }

    public void StartNewGame()
    {
        if (GetTotalLevelCount() == 0)
        {
            Debug.LogError("No chapters or levels configured.");
            return;
        }

        currentChapterIndex = 0;
        currentLevelIndex = 0;
        ShouldPlayIntroDialogue = true;
        currentPuzzleSeed = CreateNewSeed();

        SaveCurrentProgress();
        LoadGeneratedLevel();
    }

    public void ContinueGame()
    {
        if (GetTotalLevelCount() == 0)
        {
            Debug.LogError("No chapters or levels configured.");
            return;
        }

        int savedLevel = PlayerPrefs.GetInt(CurrentLevelKey, 0);

        SetCurrentLevelFromFlatIndex(savedLevel);

        currentPuzzleSeed = PlayerPrefs.GetInt(PuzzleSeedKey, 0);

        if (currentPuzzleSeed == 0)
        {
            currentPuzzleSeed = CreateNewSeed();
            SaveCurrentProgress();
        }

        ShouldPlayIntroDialogue = false;
        LoadGeneratedLevel();
    }

    public void RetryCurrentLevel()
    {
        ShouldPlayIntroDialogue = false;

        if (CurrentLevelConfig != null)
        {
            SceneManager.LoadScene(gameplaySceneName);
            return;
        }

        LoadGeneratedLevel();
    }

    public void CompleteCurrentLevel(int starsEarned)
    {
        int flatIndex = GetCurrentFlatLevelIndex();

        SaveBestStars(flatIndex, starsEarned);
        UnlockNextLevel(flatIndex);
    }

    public void ContinueToNextLevel()
    {
        int nextFlatIndex = GetCurrentFlatLevelIndex() + 1;

        if (nextFlatIndex >= GetTotalLevelCount())
        {
            Debug.Log("All story levels completed!");
            return;
        }

        SetCurrentLevelFromFlatIndex(nextFlatIndex);

        ShouldPlayIntroDialogue = false;
        currentPuzzleSeed = CreateNewSeed();

        SaveCurrentProgress();
        LoadGeneratedLevel();
    }

    private void LoadGeneratedLevel()
    {
        if (!TryGetCurrentChapter(out ChapterConfig chapter))
            return;

        LevelGeneratorSettings settings =
            CreateSettingsForCurrentLevel(chapter);

        if (settings == null)
            return;

        LevelConfig generatedLevel;

        try
        {
            generatedLevel = RuntimeLevelGenerator.Generate(
                settings, currentPuzzleSeed);
        }
        finally
        {
            Destroy(settings);
        }

        if (generatedLevel == null)
        {
            Debug.LogError(
                "Could not generate a valid puzzle. " +
                "Check chapter difficulty and generator settings.");
            return;
        }

        LevelConfig previousLevel = CurrentLevelConfig;
        CurrentLevelConfig = generatedLevel;

        if (previousLevel != null &&
            previousLevel != generatedLevel)
        {
            Destroy(previousLevel);
        }

        SceneManager.LoadScene(gameplaySceneName);
    }


    private LevelGeneratorSettings CreateSettingsForCurrentLevel(
        ChapterConfig chapter)
    {
        if (chapter.baseGeneratorSettings == null)
        {
            Debug.LogError(
                $"Chapter '{chapter.chapterName}' has no base generator settings.");
            return null;
        }

        LevelGeneratorSettings settings =
            Instantiate(chapter.baseGeneratorSettings);

        // Create an independent ruleset so we never modify
        // the original ScriptableObject's nested lists.
        LevelRuleset baseRules = settings.ruleset;

        if (baseRules == null)
        {
            Debug.LogError("Base generator settings have no ruleset.");
            Destroy(settings);
            return null;
        }

        settings.ruleset = new LevelRuleset
        {
            allowFloorPreferences = baseRules.allowFloorPreferences,
            allowAdjacencyRules = baseRules.allowAdjacencyRules,
            allowBehaviorTraits = baseRules.allowBehaviorTraits,
            allowRequirements = baseRules.allowRequirements,

            allowedTraits = baseRules.allowedTraits != null
                ? new List<RoomTrait>(baseRules.allowedTraits)
                : new List<RoomTrait>(),

            allowedBehaviors = baseRules.allowedBehaviors != null
                ? new List<GuestBehaviorTrait>(baseRules.allowedBehaviors)
                : new List<GuestBehaviorTrait>(),

            allowedRequirements = baseRules.allowedRequirements != null
                ? new List<GuestRequirement>(baseRules.allowedRequirements)
                : new List<GuestRequirement>()
        };

        settings.closedRoomRules = new List<ClosedRoomRule>();

        // Find the latest difficulty milestone for this level.
        LevelDifficultyStep activeStep = null;
        int highestStartingLevel = 0;
        int chapterLevel = currentLevelIndex + 1;

        if (chapter.difficultySteps != null)
        {
            foreach (LevelDifficultyStep step in chapter.difficultySteps)
            {
                if (step == null)
                    continue;

                if (step.startingLevel <= chapterLevel &&
                    step.startingLevel >= highestStartingLevel)
                {
                    activeStep = step;
                    highestStartingLevel = step.startingLevel;
                }
            }
        }

        // Difficulty values are replaced by the active milestone.
        if (activeStep != null)
        {
            settings.guestCount = activeStep.guestCount;

            settings.minPreferencesPerGuest =
                activeStep.minPreferencesPerGuest;

            settings.maxPreferencesPerGuest =
                Mathf.Max(
                    activeStep.minPreferencesPerGuest,
                    activeStep.maxPreferencesPerGuest);


            settings.closedRoomRules = new List<ClosedRoomRule>();

            if (activeStep.closedRoomRules != null)
            {
                foreach (ClosedRoomRule rule in activeStep.closedRoomRules)
                {
                    if (rule == null)
                        continue;

                    if (rule.floorIndex < 1 ||
                        rule.floorIndex > settings.floorCount)
                        continue;

                    settings.closedRoomRules.Add(new ClosedRoomRule
                    {
                        floorIndex = rule.floorIndex,
                        closedRoomCount = Mathf.Clamp(
                            rule.closedRoomCount,
                            0,
                            settings.roomsPerFloor
                        )
                    });
                }
            }

        }

        // Unlocks accumulate from previous chapters and
        // earlier levels in the current chapter.
        for (int c = 0; c <= currentChapterIndex; c++)
        {
            ChapterConfig sourceChapter = chapters[c];

            if (sourceChapter == null)
                continue;

            // Include traits and mechanics available from the
            // base settings of previously completed chapters.
            if (c < currentChapterIndex &&
                sourceChapter.baseGeneratorSettings != null)
            {
                MergeRuleset(
                    settings.ruleset,
                    sourceChapter.baseGeneratorSettings.ruleset);
            }

            if (sourceChapter.difficultySteps == null)
                continue;

            int lastUnlockedLevel = c < currentChapterIndex
                ? sourceChapter.levelCount
                : chapterLevel;

            foreach (LevelDifficultyStep step in
                     sourceChapter.difficultySteps)
            {
                if (step == null ||
                    step.startingLevel > lastUnlockedLevel)
                    continue;

                ApplyUnlocks(settings.ruleset, step);
            }
        }

        return settings;
    }


    private bool TryGetCurrentChapter(out ChapterConfig chapter)
    {
        chapter = null;

        if (currentChapterIndex < 0 ||
            currentChapterIndex >= chapters.Count)
        {
            Debug.LogError("Invalid chapter index.");
            return false;
        }

        chapter = chapters[currentChapterIndex];

        if (chapter == null)
        {
            Debug.LogError("ChapterConfig is missing.");
            return false;
        }

        return true;
    }

    private int CreateNewSeed()
    {
        return Random.Range(1, int.MaxValue);
    }

    private void SaveCurrentProgress()
    {
        PlayerPrefs.SetInt(
            CurrentLevelKey, GetCurrentFlatLevelIndex());

        PlayerPrefs.SetInt(
            PuzzleSeedKey, currentPuzzleSeed);

        PlayerPrefs.Save();
    }

    private int GetTotalLevelCount()
    {
        int count = 0;

        foreach (ChapterConfig chapter in chapters)
        {
            if (chapter != null)
                count += Mathf.Max(0, chapter.levelCount);
        }

        return count;
    }

    private int GetCurrentFlatLevelIndex()
    {
        int index = 0;

        for (int c = 0; c < currentChapterIndex; c++)
        {
            if (chapters[c] != null)
                index += Mathf.Max(0, chapters[c].levelCount);
        }

        return index + currentLevelIndex;
    }

    private void SetCurrentLevelFromFlatIndex(int flatIndex)
    {
        int runningIndex = Mathf.Max(0, flatIndex);

        for (int c = 0; c < chapters.Count; c++)
        {
            if (chapters[c] == null)
                continue;

            int levelCount = Mathf.Max(0, chapters[c].levelCount);

            if (runningIndex < levelCount)
            {
                currentChapterIndex = c;
                currentLevelIndex = runningIndex;
                return;
            }

            runningIndex -= levelCount;
        }

        currentChapterIndex = 0;
        currentLevelIndex = 0;
    }

    private void SaveBestStars(int levelIndex, int starsEarned)
    {
        int currentBest = PlayerPrefs.GetInt(
            StarKeyPrefix + levelIndex, 0);

        if (starsEarned > currentBest)
        {
            PlayerPrefs.SetInt(
                StarKeyPrefix + levelIndex, starsEarned);

            PlayerPrefs.Save();
        }
    }

    public void MarkIntroDialoguePlayed()
    {
        ShouldPlayIntroDialogue = false;
    }

    private void UnlockNextLevel(int currentLevel)
    {
        int nextLevel = currentLevel + 1;

        if (nextLevel >= GetTotalLevelCount())
            return;

        int highestUnlocked = PlayerPrefs.GetInt(
            HighestUnlockedKey, 0);

        if (nextLevel > highestUnlocked)
        {
            PlayerPrefs.SetInt(
                HighestUnlockedKey, nextLevel);

            PlayerPrefs.Save();
        }
    }


    private void ApplyUnlocks(
        LevelRuleset rules,
        LevelDifficultyStep step)
    {
        if (step.unlockRoomTraits != null)
        {
            foreach (RoomTrait trait in step.unlockRoomTraits)
            {
                if (!rules.allowedTraits.Contains(trait))
                    rules.allowedTraits.Add(trait);
            }
        }

        if (step.unlockBehaviors != null)
        {
            foreach (GuestBehaviorTrait behavior in step.unlockBehaviors)
            {
                if (!rules.allowedBehaviors.Contains(behavior))
                    rules.allowedBehaviors.Add(behavior);
            }
        }

        if (step.unlockRequirements != null)
        {
            foreach (GuestRequirement requirement in step.unlockRequirements)
            {
                if (!rules.allowedRequirements.Contains(requirement))
                    rules.allowedRequirements.Add(requirement);
            }
        }

        if (step.unlockFloorPreferences)
            rules.allowFloorPreferences = true;

        if (step.unlockAdjacencyRules)
            rules.allowAdjacencyRules = true;

        if (step.unlockBehaviorTraits)
            rules.allowBehaviorTraits = true;

        if (step.unlockGuestRequirements)
            rules.allowRequirements = true;
    }


    private void MergeRuleset(
        LevelRuleset target,
        LevelRuleset source)
    {
        if (source == null)
            return;

        target.allowFloorPreferences |= source.allowFloorPreferences;
        target.allowAdjacencyRules |= source.allowAdjacencyRules;
        target.allowBehaviorTraits |= source.allowBehaviorTraits;
        target.allowRequirements |= source.allowRequirements;

        if (source.allowedTraits != null)
        {
            foreach (RoomTrait trait in source.allowedTraits)
            {
                if (!target.allowedTraits.Contains(trait))
                    target.allowedTraits.Add(trait);
            }
        }

        if (source.allowedBehaviors != null)
        {
            foreach (GuestBehaviorTrait behavior in source.allowedBehaviors)
            {
                if (!target.allowedBehaviors.Contains(behavior))
                    target.allowedBehaviors.Add(behavior);
            }
        }

        if (source.allowedRequirements != null)
        {
            foreach (GuestRequirement requirement in source.allowedRequirements)
            {
                if (!target.allowedRequirements.Contains(requirement))
                    target.allowedRequirements.Add(requirement);
            }
        }
    }
}
