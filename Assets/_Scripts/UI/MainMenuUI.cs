using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private SettingsManager settingsManager;

    public void NewGame()
    {
        LevelProgressManager.Instance.StartNewGame();
    }

    public void ContinueGame()
    {
        LevelProgressManager.Instance.ContinueGame();
    }

    public void OpenSettings()
    {
        if (settingsManager != null)
            settingsManager.OpenSettings();
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }
}