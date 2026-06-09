using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[System.Serializable]
public class IntroTextEntry
{
    [TextArea]
    public string text;

    public float startTime;
    public float endTime;
}

public class IntroTextSequenceUI : MonoBehaviour
{
    public static IntroTextSequenceUI Instance { get; private set; }

    [SerializeField] private GameObject textRoot;
    [SerializeField] private CanvasGroup textCanvasGroup;
    [SerializeField] private TMP_Text textField;

    [SerializeField] private float fadeDuration = 1.25f;

    [Header("Sequence")]
    [SerializeField] private List<IntroTextEntry> entries = new();

    public bool IsPlaying { get; private set; }

    private void Awake()
    {
        Instance = this;

        if (textRoot != null)
            textRoot.SetActive(false);

        if (textCanvasGroup != null)
            textCanvasGroup.alpha = 0f;
    }

    public IEnumerator Play(AudioSource audioSource)
    {
        if (entries.Count == 0)
            yield break;

        IsPlaying = true;

        if (DialogueManager.Instance != null)
            yield return DialogueManager.Instance.FadeSharedBackgroundIn();

        if (textRoot != null)
            textRoot.SetActive(true);

        if (audioSource != null)
            audioSource.Play();

        for (int i = 0; i < entries.Count; i++)
        {
            IntroTextEntry entry = entries[i];

            while (audioSource != null && audioSource.time < entry.startTime)
                yield return null;

            if (textField != null)
                textField.text = entry.text;

            yield return FadeText(1f);

            while (audioSource != null && audioSource.time < entry.endTime)
                yield return null;

            yield return FadeText(0f);
        }

        if (textRoot != null)
            textRoot.SetActive(false);

        IsPlaying = false;
    }

    private IEnumerator FadeText(float targetAlpha)
    {
        if (textCanvasGroup == null)
            yield break;

        float startAlpha = textCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            textCanvasGroup.alpha =
                Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);

            yield return null;
        }

        textCanvasGroup.alpha = targetAlpha;
    }
}