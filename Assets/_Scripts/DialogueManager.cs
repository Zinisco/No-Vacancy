using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject root;
    [SerializeField] private CanvasGroup canvasGroup;
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

        if (root != null)
            root.SetActive(false);

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
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

        if (root != null)
            root.SetActive(true);

        yield return Fade(1f);

        ShowCurrentLine();
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

        yield return Fade(0f);

        if (root != null)
            root.SetActive(false);

        IsPlaying = false;
        currentSequence = null;
    }

    private IEnumerator Fade(float targetAlpha)
    {
        if (canvasGroup == null)
            yield break;

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }
}