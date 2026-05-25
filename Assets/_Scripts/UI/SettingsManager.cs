using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject settingsRoot;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject pauseMenuPanel;

    [Header("Buttons")]
    [SerializeField] private Button backButton;

    [Header("General")]
    [SerializeField] private Slider mouseSlider;
    [SerializeField] private TMP_InputField mouseInput;

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Audio - Master")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private TMP_InputField masterInput;

    [Header("Audio - Music")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private TMP_InputField musicInput;

    [Header("Audio - SFX")]
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private TMP_InputField sfxInput;

    [Header("Graphics")]
    [SerializeField] private TMP_Dropdown windowModeDropdown;
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    private const string KEY_MOUSE = "MouseSensitivity";

    private const string KEY_MASTER = "MasterVolume";
    private const string KEY_MUSIC = "MusicVolume";
    private const string KEY_SFX = "SFXVolume";

    private const string KEY_MODE = "GFX_WindowMode";
    private const string KEY_W = "GFX_Width";
    private const string KEY_H = "GFX_Height";

    private const float DEFAULT_MOUSE = 1f;
    private const float DEFAULT_VOL = 0.7f;
    private const int DEFAULT_MODE = 2;

    private bool initializing;
    private List<(int w, int h)> resolutionList = new();

    private void Awake()
    {
        initializing = true;

        if (backButton != null)
            backButton.onClick.AddListener(CloseSettings);

        InitializeGeneral();
        InitializeAudio();
        InitializeGraphics();

        initializing = false;
    }

    #region Open / Close

    public void OpenSettings()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (settingsRoot != null)
            settingsRoot.SetActive(true);

        if(pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
    }

    public void CloseSettings()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        if (settingsRoot != null)
            settingsRoot.SetActive(false);

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);
    }

    #endregion

    #region General Settings

    private void InitializeGeneral()
    {
        if (mouseSlider == null || mouseInput == null)
            return;

        mouseSlider.minValue = 0.1f;
        mouseSlider.maxValue = 5f;
        mouseSlider.wholeNumbers = false;

        float savedMouse = PlayerPrefs.GetFloat(KEY_MOUSE, DEFAULT_MOUSE);

        mouseSlider.SetValueWithoutNotify(savedMouse);
        mouseInput.SetTextWithoutNotify(savedMouse.ToString("0.00"));

        mouseSlider.onValueChanged.AddListener(OnMouseSliderChanged);
        mouseInput.onEndEdit.AddListener(OnMouseInputChanged);
    }

    private void OnMouseSliderChanged(float value)
    {
        if (initializing)
            return;

        value = Mathf.Clamp(value, 0.1f, 5f);

        if (mouseInput != null)
            mouseInput.SetTextWithoutNotify(value.ToString("0.00"));

        PlayerPrefs.SetFloat(KEY_MOUSE, value);
        PlayerPrefs.Save();
    }

    private void OnMouseInputChanged(string text)
    {
        if (!float.TryParse(text, out float value))
            value = DEFAULT_MOUSE;

        value = Mathf.Clamp(value, 0.1f, 5f);

        if (mouseSlider != null)
            mouseSlider.SetValueWithoutNotify(value);

        if (mouseInput != null)
            mouseInput.SetTextWithoutNotify(value.ToString("0.00"));

        PlayerPrefs.SetFloat(KEY_MOUSE, value);
        PlayerPrefs.Save();
    }

    #endregion

    #region Audio Settings

    private void InitializeAudio()
    {
        SetupVolumeSlider(masterSlider);
        SetupVolumeSlider(musicSlider);
        SetupVolumeSlider(sfxSlider);

        float master = PlayerPrefs.GetFloat(KEY_MASTER, DEFAULT_VOL);
        float music = PlayerPrefs.GetFloat(KEY_MUSIC, DEFAULT_VOL);
        float sfx = PlayerPrefs.GetFloat(KEY_SFX, DEFAULT_VOL);

        SetVolumeUI(masterSlider, masterInput, master);
        SetVolumeUI(musicSlider, musicInput, music);
        SetVolumeUI(sfxSlider, sfxInput, sfx);

        if (masterSlider != null)
            masterSlider.onValueChanged.AddListener(v => OnVolumeSliderChanged(masterInput, KEY_MASTER, "MasterVolume", v));

        if (musicSlider != null)
            musicSlider.onValueChanged.AddListener(v => OnVolumeSliderChanged(musicInput, KEY_MUSIC, "MusicVolume", v));

        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(v => OnVolumeSliderChanged(sfxInput, KEY_SFX, "SFXVolume", v));

        if (masterInput != null)
            masterInput.onEndEdit.AddListener(v => OnVolumeInputChanged(masterSlider, KEY_MASTER, "MasterVolume", v));

        if (musicInput != null)
            musicInput.onEndEdit.AddListener(v => OnVolumeInputChanged(musicSlider, KEY_MUSIC, "MusicVolume", v));

        if (sfxInput != null)
            sfxInput.onEndEdit.AddListener(v => OnVolumeInputChanged(sfxSlider, KEY_SFX, "SFXVolume", v));

        ApplyVolume("MasterVolume", master);
        ApplyVolume("MusicVolume", music);
        ApplyVolume("SFXVolume", sfx);
    }

    private void SetupVolumeSlider(Slider slider)
    {
        if (slider == null)
            return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
    }

    private void OnVolumeSliderChanged(TMP_InputField input, string prefKey, string mixerParam, float value)
    {
        if (initializing)
            return;

        value = Mathf.Clamp01(value);

        if (input != null)
            input.SetTextWithoutNotify(ToPercent(value));

        ApplyVolume(mixerParam, value);
        SaveFloat(prefKey, value);
    }

    private void OnVolumeInputChanged(Slider slider, string prefKey, string mixerParam, string text)
    {
        float value = ParsePercent(text);

        if (slider != null)
            slider.SetValueWithoutNotify(value);

        ApplyVolume(mixerParam, value);
        SaveFloat(prefKey, value);
    }

    private void SetVolumeUI(Slider slider, TMP_InputField input, float value)
    {
        value = Mathf.Clamp01(value);

        if (slider != null)
            slider.SetValueWithoutNotify(value);

        if (input != null)
            input.SetTextWithoutNotify(ToPercent(value));
    }

    private void ApplyVolume(string mixerParam, float value)
    {
        if (audioMixer == null)
            return;

        float db = Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f;
        audioMixer.SetFloat(mixerParam, db);
    }

    public void ResetAudioToDefault()
    {
        SetVolumeUI(masterSlider, masterInput, DEFAULT_VOL);
        SetVolumeUI(musicSlider, musicInput, DEFAULT_VOL);
        SetVolumeUI(sfxSlider, sfxInput, DEFAULT_VOL);

        ApplyVolume("MasterVolume", DEFAULT_VOL);
        ApplyVolume("MusicVolume", DEFAULT_VOL);
        ApplyVolume("SFXVolume", DEFAULT_VOL);

        SaveFloat(KEY_MASTER, DEFAULT_VOL);
        SaveFloat(KEY_MUSIC, DEFAULT_VOL);
        SaveFloat(KEY_SFX, DEFAULT_VOL);
    }

    #endregion

    #region Graphics Settings

    private void InitializeGraphics()
    {
        BuildWindowModeOptions();
        BuildResolutionOptions();

        int savedMode = PlayerPrefs.GetInt(KEY_MODE, DEFAULT_MODE);
        int savedWidth = PlayerPrefs.GetInt(KEY_W, Screen.currentResolution.width);
        int savedHeight = PlayerPrefs.GetInt(KEY_H, Screen.currentResolution.height);

        if (windowModeDropdown != null)
            windowModeDropdown.SetValueWithoutNotify(Mathf.Clamp(savedMode, 0, 2));

        if (resolutionDropdown != null)
            resolutionDropdown.SetValueWithoutNotify(IndexOfResolution(savedWidth, savedHeight));

        if (windowModeDropdown != null)
            windowModeDropdown.onValueChanged.AddListener(_ => ApplyGraphics());

        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.AddListener(_ => ApplyGraphics());

        ApplyGraphics(true);
    }

    private void BuildWindowModeOptions()
    {
        if (windowModeDropdown == null)
            return;

        windowModeDropdown.ClearOptions();
        windowModeDropdown.AddOptions(new List<string>
        {
            "Windowed",
            "Borderless",
            "Fullscreen"
        });
    }

    private void BuildResolutionOptions()
    {
        if (resolutionDropdown == null)
            return;

        resolutionDropdown.ClearOptions();
        resolutionList.Clear();

        List<(int width, int height)> resolutions = Screen.resolutions
            .Select(r => (r.width, r.height))
            .Distinct()
            .OrderByDescending(r => r.width * r.height)
            .ToList();

        List<string> options = new();

        foreach (var res in resolutions)
        {
            resolutionList.Add((res.width, res.height));
            options.Add($"{res.width} x {res.height}");
        }

        if (resolutionList.Count == 0)
        {
            resolutionList.Add((Screen.currentResolution.width, Screen.currentResolution.height));
            options.Add($"{Screen.currentResolution.width} x {Screen.currentResolution.height}");
        }

        resolutionDropdown.AddOptions(options);
    }

    private void ApplyGraphics(bool forceApply = false)
    {
        if (initializing && !forceApply)
            return;

        if (windowModeDropdown == null || resolutionDropdown == null || resolutionList.Count == 0)
            return;

        int modeIndex = Mathf.Clamp(windowModeDropdown.value, 0, 2);
        int resolutionIndex = Mathf.Clamp(resolutionDropdown.value, 0, resolutionList.Count - 1);

        int width = resolutionList[resolutionIndex].w;
        int height = resolutionList[resolutionIndex].h;

        FullScreenMode mode = FullScreenMode.Windowed;

        switch (modeIndex)
        {
            case 0:
                mode = FullScreenMode.Windowed;
                resolutionDropdown.interactable = true;
                break;

            case 1:
                mode = FullScreenMode.FullScreenWindow;
                width = Display.main.systemWidth;
                height = Display.main.systemHeight;
                resolutionDropdown.interactable = false;
                break;

            case 2:
                mode = FullScreenMode.ExclusiveFullScreen;
                resolutionDropdown.interactable = true;
                break;
        }

        Screen.SetResolution(width, height, mode);

        PlayerPrefs.SetInt(KEY_MODE, modeIndex);
        PlayerPrefs.SetInt(KEY_W, width);
        PlayerPrefs.SetInt(KEY_H, height);
        PlayerPrefs.Save();
    }

    private int IndexOfResolution(int width, int height)
    {
        for (int i = 0; i < resolutionList.Count; i++)
        {
            if (resolutionList[i].w == width && resolutionList[i].h == height)
                return i;
        }

        return 0;
    }

    #endregion

    #region Utility

    private string ToPercent(float value)
    {
        return Mathf.RoundToInt(Mathf.Clamp01(value) * 100f).ToString();
    }

    private float ParsePercent(string text)
    {
        text = (text ?? "").Trim().TrimEnd('%');

        if (!float.TryParse(text, out float value))
            return DEFAULT_VOL;

        if (value <= 1f)
            return Mathf.Clamp01(value);

        return Mathf.Clamp01(value / 100f);
    }

    private void SaveFloat(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
    }

    #endregion
}