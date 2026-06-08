using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelProgressManager : MonoBehaviour
{
    public static LevelProgressManager Instance { get; private set; }

    public LevelConfig CurrentLevelConfig { get; private set; }

    public bool ShouldPlayIntroDialogue { get; private set; }

    [SerializeField] private string gameplaySceneName = "Game";

    private const string HighestUnlockedKey = "HighestUnlockedLevel";
    private const string StarKeyPrefix = "LevelStars_";

    [SerializeField] private List<ChapterConfig> chapters = new();

    private int currentChapterIndex;
    private int currentLevelIndex;

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

    public void StartNewGame()
    {
        currentChapterIndex = 0;
        currentLevelIndex = 0;

        PlayerPrefs.SetInt(HighestUnlockedKey, 0);
        PlayerPrefs.Save();

        ShouldPlayIntroDialogue = true;

        CurrentLevelConfig = chapters[currentChapterIndex].levels[currentLevelIndex];

        SceneManager.LoadScene(gameplaySceneName);
    }

    public void ContinueGame()
    {
        int highestUnlocked = PlayerPrefs.GetInt(HighestUnlockedKey, 0);

        SetCurrentLevelFromFlatIndex(highestUnlocked);

        ShouldPlayIntroDialogue = false;

        CurrentLevelConfig = chapters[currentChapterIndex].levels[currentLevelIndex];

        SceneManager.LoadScene(gameplaySceneName);
    }

    public void RetryCurrentLevel()
    {
        ShouldPlayIntroDialogue = false;

        CurrentLevelConfig = chapters[currentChapterIndex].levels[currentLevelIndex];
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void CompleteCurrentLevel(int starsEarned)
    {
        int flatIndex = GetCurrentFlatLevelIndex();

        SaveBestStars(flatIndex, starsEarned);
        UnlockNextLevel(flatIndex);
    }

    public void ContinueToNextLevel()
    {
        currentLevelIndex++;

        if (currentLevelIndex >= chapters[currentChapterIndex].levels.Count)
        {
            currentChapterIndex++;
            currentLevelIndex = 0;
        }

        if (currentChapterIndex >= chapters.Count)
        {
            Debug.Log("No more chapters.");
            return;
        }

        CurrentLevelConfig = chapters[currentChapterIndex].levels[currentLevelIndex];
        SceneManager.LoadScene(gameplaySceneName);
    }

    private int GetCurrentFlatLevelIndex()
    {
        int index = 0;

        for (int c = 0; c < currentChapterIndex; c++)
            index += chapters[c].levels.Count;

        index += currentLevelIndex;
        return index;
    }

    private void SetCurrentLevelFromFlatIndex(int flatIndex)
    {
        int runningIndex = flatIndex;

        for (int c = 0; c < chapters.Count; c++)
        {
            if (runningIndex < chapters[c].levels.Count)
            {
                currentChapterIndex = c;
                currentLevelIndex = runningIndex;
                return;
            }

            runningIndex -= chapters[c].levels.Count;
        }

        currentChapterIndex = 0;
        currentLevelIndex = 0;
    }

    private void SaveBestStars(int levelIndex, int starsEarned)
    {
        int currentBest = PlayerPrefs.GetInt(StarKeyPrefix + levelIndex, 0);

        if (starsEarned > currentBest)
        {
            PlayerPrefs.SetInt(StarKeyPrefix + levelIndex, starsEarned);
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
        int highestUnlocked = PlayerPrefs.GetInt(HighestUnlockedKey, 0);

        if (nextLevel > highestUnlocked)
        {
            PlayerPrefs.SetInt(HighestUnlockedKey, nextLevel);
            PlayerPrefs.Save();
        }
    }
}