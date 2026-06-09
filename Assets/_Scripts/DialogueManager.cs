using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("Shared Root")]
    [SerializeField] private GameObject storyRoot;
    [SerializeField] private CanvasGroup storyCanvasGroup;

    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialogueRoot;
    [SerializeField] private TMP_Text bodyText;

    [Header("Audio")]
    [SerializeField] private AudioSource voiceSource;

    [Header("Timing")]
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private float typeSpeed = 0.025f;
    [SerializeField] private float delayAfterVoice = 0.35f;

    private DialogueSequence currentSequence;
    private int currentLineIndex;
    private bool isTyping;
    private bool canAdvance;
    private Coroutine typingRoutine;

    public bool IsPlaying { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (storyRoot != null)
            storyRoot.SetActive(true);

        if (storyCanvasGroup != null)
            storyCanvasGroup.alpha = 1f;

        if (dialogueRoot != null)
            dialogueRoot.SetActive(false);
    }

    private void Update()
    {
        if (!IsPlaying)
            return;

        bool pressed =
            Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame ||
            Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame ||
            Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame;

        if (pressed)
            Advance();
    }

    public void ShowSharedBackgroundInstant()
    {
        if (storyRoot != null)
            storyRoot.SetActive(true);

        if (storyCanvasGroup != null)
            storyCanvasGroup.alpha = 1f;
    }

    public IEnumerator FadeSharedBackgroundIn()
    {
        if (storyRoot != null)
            storyRoot.SetActive(true);

        yield return FadeStoryRoot(1f);
    }

    public void Play(DialogueSequence sequence)
    {
        if (sequence == null || sequence.lines == null || sequence.lines.Count == 0)
            return;

        StopAllCoroutines();

        currentSequence = sequence;
        currentLineIndex = 0;

        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        IsPlaying = true;
        canAdvance = false;

        ShowSharedBackgroundInstant();

        if (dialogueRoot != null)
            dialogueRoot.SetActive(true);

        ShowCurrentLine();

        yield break;
    }

    private void ShowCurrentLine()
    {
        if (currentSequence == null)
            return;

        if (currentLineIndex >= currentSequence.lines.Count)
        {
            StartCoroutine(EndRoutine());
            return;
        }

        DialogueLine line = currentSequence.lines[currentLineIndex];

        if (voiceSource != null)
        {
            voiceSource.Stop();

            if (line.voiceClip != null)
            {
                voiceSource.clip = line.voiceClip;
                voiceSource.Play();
            }
        }

        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        typingRoutine = StartCoroutine(TypeLine(line.text));
    }

    private IEnumerator TypeLine(string text)
    {
        isTyping = true;
        canAdvance = false;

        if (bodyText != null)
            bodyText.text = "";

        for (int i = 0; i < text.Length; i++)
        {
            if (bodyText != null)
                bodyText.text += text[i];

            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false;

        DialogueLine line = currentSequence.lines[currentLineIndex];

        if (voiceSource != null && line.voiceClip != null)
        {
            while (voiceSource.isPlaying)
                yield return null;

            yield return new WaitForSeconds(delayAfterVoice);
        }

        canAdvance = true;
    }

    private void Advance()
    {
        if (currentSequence == null)
            return;

        DialogueLine line = currentSequence.lines[currentLineIndex];

        if (isTyping)
        {
            if (typingRoutine != null)
                StopCoroutine(typingRoutine);

            if (bodyText != null)
                bodyText.text = line.text;

            isTyping = false;
            canAdvance = true;
            return;
        }

        if (!canAdvance)
            return;

        currentLineIndex++;
        ShowCurrentLine();
    }

    private IEnumerator EndRoutine()
    {
        canAdvance = false;

        if (voiceSource != null)
            voiceSource.Stop();

        if (dialogueRoot != null)
            dialogueRoot.SetActive(false);

        yield return FadeStoryRoot(0f);

        if (storyRoot != null)
            storyRoot.SetActive(false);

        IsPlaying = false;
        currentSequence = null;
    }

    private IEnumerator FadeStoryRoot(float targetAlpha)
    {
        if (storyCanvasGroup == null)
            yield break;

        float startAlpha = storyCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            storyCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        storyCanvasGroup.alpha = targetAlpha;
    }

    public void HideSharedBackgroundInstant()
    {
        if (storyCanvasGroup != null)
            storyCanvasGroup.alpha = 0f;

        if (storyRoot != null)
            storyRoot.SetActive(false);
    }
}