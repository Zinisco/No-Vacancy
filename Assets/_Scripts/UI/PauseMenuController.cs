using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Pause menu with: Resume, Restart (hold-to-confirm), Save, Main Menu, Quit.
/// TimeScale is set to 0 while paused; UI uses unscaled time.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    public static PauseMenuController Instance { get; private set; }

    public event EventHandler OnGamePaused;
    public event EventHandler OnGameUnpaused;

    [Header("Root & Focus")]
    [SerializeField] private GameObject pauseMenuRoot;
    [SerializeField] private GameObject firstSelected; // focus when opening pause
    [SerializeField] private GameObject pauseMenuLayout;
    [SerializeField] private GameObject settingsMenu;


    [Header("Top-level Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;  // opens confirm panel
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    [Header("Restart Confirm Panel")]
    [SerializeField] private GameObject restartConfirmPanel;
    [SerializeField] private Button restartConfirmButton;   // NEW - replaces HoldToConfirmButton
    [SerializeField] private Button restartCancelButton;
    [SerializeField] private GameObject restartFirstSelected; // focus when confirm panel opens
                                                           

    [Header("Quit Confirm Panel")]
    [SerializeField] private GameObject quitConfirmPanel;
    [SerializeField] private Button quitConfirmButton;  
    [SerializeField] private Button quitCancelButton;
    [SerializeField] private GameObject quitFirstSelected;

    private float pauseBlockTimer = 0f;

    private bool isGamePaused;
    public bool IsPaused => isGamePaused;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        HideRestartPrompt();

        // Wire buttons
        if (resumeButton) resumeButton.onClick.AddListener(TogglePauseGame);
        if (restartButton) restartButton.onClick.AddListener(ShowRestartPrompt);
        if(settingsButton) settingsButton.onClick.AddListener(() =>
        {
            SettingsManager settingsManager = FindAnyObjectByType<SettingsManager>();
            if (settingsManager != null)
            {
                settingsManager.OpenSettings();
            }
        });

        if (mainMenuButton) mainMenuButton.onClick.AddListener(QuitToMainMenu);
        if (quitButton) quitButton.onClick.AddListener(ShowQuitPrompt);

        if (restartCancelButton) restartCancelButton.onClick.AddListener(HideRestartPrompt);
        if (restartConfirmButton) restartConfirmButton.onClick.AddListener(DoRestartConfirmed);

        if (quitCancelButton) quitCancelButton.onClick.AddListener(HideQuitPrompt);
        if (quitConfirmButton) quitConfirmButton.onClick.AddListener(DoQuitConfirmed);

    }

    private void Start()
    {
        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);

        if (settingsMenu != null)
            settingsMenu.SetActive(false);

        HideRestartPrompt();
        HideQuitPrompt();
    }

    private void Update()
    {
        if (pauseBlockTimer > 0f)
            pauseBlockTimer -= Time.unscaledDeltaTime;

        if (pauseBlockTimer > 0f)
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePauseGame();
        }
    }

    private void GameInput_OnPauseAction(object sender, EventArgs e)
    {
        TogglePauseGame();
    }

    // ---------- Restart flow ----------
    private void ShowRestartPrompt()
    {
        if (!restartConfirmPanel)
        {
            DoRestartConfirmed(); // fallback
            return;
        }

        restartConfirmPanel.SetActive(true);
        pauseMenuLayout.SetActive(false);

        if (restartFirstSelected)
            EventSystem.current?.SetSelectedGameObject(restartFirstSelected);
    }


    private void HideRestartPrompt()
    {
        if (restartConfirmPanel)
        {
            restartConfirmPanel.SetActive(false);
            pauseMenuLayout.SetActive(true);
        }

        // Return focus to top-level pause menu
        if (firstSelected && isGamePaused) EventSystem.current?.SetSelectedGameObject(firstSelected);
    }

    private void DoRestartConfirmed()
    {
        // Restore time before loading
        Time.timeScale = 1f;
        HideRestartPrompt();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
  


    private void HandleCancelAction()
    {
        if (!isGamePaused) return;

        if (restartConfirmPanel != null && restartConfirmPanel.activeSelf)
        {
            HideRestartPrompt();
        }
    }


    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitToDesktop()
    {
        Application.Quit();
    }

    private void ShowQuitPrompt()
    {
        if (!quitConfirmPanel)
        {
            DoQuitConfirmed(); // fallback if panel is missing
            return;
        }

        quitConfirmPanel.SetActive(true);
        pauseMenuLayout.SetActive(false);

        if (quitFirstSelected)
            EventSystem.current?.SetSelectedGameObject(quitFirstSelected);
    }


    private void HideQuitPrompt()
    {
        if (quitConfirmPanel)
        {
            quitConfirmPanel.SetActive(false);
            pauseMenuLayout.SetActive(true);
        }

        if (firstSelected && isGamePaused)
            EventSystem.current?.SetSelectedGameObject(firstSelected);
    }

    private void DoQuitConfirmed()
    {
        Time.timeScale = 1f;
        Application.Quit();
    }

    // ---------- Pause / Unpause ----------

    public void TogglePauseGame()
    {
        isGamePaused = !isGamePaused;

        if (isGamePaused)
        {
            Time.timeScale = 0f;
            pauseMenuRoot.SetActive(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            EventSystem.current?.SetSelectedGameObject(firstSelected);
            OnGamePaused?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            Time.timeScale = 1f;
            pauseMenuRoot.SetActive(false);
            settingsMenu.SetActive(false);
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            HideRestartPrompt();
            HideQuitPrompt();

            OnGameUnpaused?.Invoke(this, EventArgs.Empty);
        }
    }

    private System.Collections.IEnumerator HideAfterSeconds(GameObject go, float t)
    {
        float elapsed = 0f;
        while (elapsed < t)
        {
            elapsed += Time.unscaledDeltaTime; // unscaled so it works while paused
            yield return null;
        }
        if (go) go.SetActive(false);
    }

    public void BlockPauseFor(float duration)
    {
        pauseBlockTimer = duration;
    }

}


/// <summary>
/// Optional save interface—implement this somewhere in your game and assign it in the inspector.
/// </summary>
public interface IGameSaver
{
    void SaveGame();
}
