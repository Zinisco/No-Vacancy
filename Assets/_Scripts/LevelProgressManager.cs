using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelProgressManager : MonoBehaviour
{
    public static LevelProgressManager Instance { get; private set; }

    [SerializeField] private List<LevelConfig> levels = new();

    private int currentLevelIndex;

    private const string HighestUnlockedKey = "HighestUnlockedLevel";
    private const string StarKeyPrefix = "LevelStars_";

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

    public void CompleteCurrentLevel(int starsEarned)
    {
        int currentLevel = SceneManager.GetActiveScene().buildIndex;

        SaveBestStars(currentLevel, starsEarned);
        UnlockNextLevel(currentLevel);
    }

    public void RetryCurrentLevel()
    {
        FindFirstObjectByType<GameManager>().LoadLevel(levels[currentLevelIndex]);
    }

    public void ContinueToNextLevel()
    {
        currentLevelIndex++;

        if (currentLevelIndex >= levels.Count)
        {
            Debug.Log("No next level.");
            return;
        }

        FindFirstObjectByType<GameManager>().LoadLevel(levels[currentLevelIndex]);
    }

    public int GetBestStars(int levelIndex)
    {
        return PlayerPrefs.GetInt(StarKeyPrefix + levelIndex, 0);
    }

    public bool IsLevelUnlocked(int levelIndex)
    {
        int highestUnlocked = PlayerPrefs.GetInt(HighestUnlockedKey, 0);
        return levelIndex <= highestUnlocked;
    }

    private void SaveBestStars(int levelIndex, int starsEarned)
    {
        int currentBest = GetBestStars(levelIndex);

        if (starsEarned > currentBest)
        {
            PlayerPrefs.SetInt(StarKeyPrefix + levelIndex, starsEarned);
            PlayerPrefs.Save();
        }
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