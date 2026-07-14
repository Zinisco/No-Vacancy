using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GuestEmoteUI : MonoBehaviour
{
    private enum GuestMood
    {
        None,
        Happy,
        Unhappy
    }

    [Header("References")]
    [SerializeField] private Image emoteImage;
    [SerializeField] private RectTransform emoteTransform;
    [SerializeField] private GuestEmoteDatabase emoteDatabase;

    [Header("Happy Emote")]
    [SerializeField] private float happyDisplayDuration = 2.5f;

    [Header("Pop Animation")]
    [SerializeField] private float popDuration = 0.18f;
    [SerializeField] private float overshootScale = 1.2f;
    [SerializeField] private float settleDuration = 0.08f;

    private Coroutine hideHappyRoutine;
    private Coroutine popRoutine;

    private GuestMood currentMood = GuestMood.None;

    private void Awake()
    {
        if (emoteImage == null)
            emoteImage = GetComponent<Image>();

        if (emoteTransform == null && emoteImage != null)
            emoteTransform = emoteImage.rectTransform;

        Hide();
    }

    public void ShowHappy()
    {
        if (currentMood == GuestMood.Happy)
            return;

        StopHideRoutine();

        currentMood = GuestMood.Happy;

        Sprite sprite = emoteDatabase != null
            ? emoteDatabase.GetRandomHappyEmote()
            : null;

        ShowSprite(sprite);

        if (sprite == null)
            return;

        PlayPopAnimation();
        hideHappyRoutine = StartCoroutine(HideHappyAfterDelay());
    }

    public void ShowUnhappy()
    {
        if (currentMood == GuestMood.Unhappy)
            return;

        StopHideRoutine();

        currentMood = GuestMood.Unhappy;

        Sprite sprite = emoteDatabase != null
            ? emoteDatabase.GetRandomUnhappyEmote()
            : null;

        ShowSprite(sprite);

        if (sprite != null)
            PlayPopAnimation();
    }

    public void Hide()
    {
        StopHideRoutine();
        StopPopRoutine();

        currentMood = GuestMood.None;

        if (emoteImage != null)
        {
            emoteImage.sprite = null;
            emoteImage.enabled = false;
        }

        if (emoteTransform != null)
            emoteTransform.localScale = Vector3.zero;
    }

    private void ShowSprite(Sprite sprite)
    {
        if (emoteImage == null)
            return;

        emoteImage.sprite = sprite;
        emoteImage.enabled = sprite != null;
    }

    private void PlayPopAnimation()
    {
        StopPopRoutine();

        if (emoteTransform == null)
            return;

        popRoutine = StartCoroutine(PopRoutine());
    }

    private IEnumerator PopRoutine()
    {
        emoteTransform.localScale = Vector3.zero;

        float elapsed = 0f;

        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / popDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);

            emoteTransform.localScale = Vector3.Lerp(
                Vector3.zero,
                Vector3.one * overshootScale,
                eased
            );

            yield return null;
        }

        elapsed = 0f;

        while (elapsed < settleDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / settleDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);

            emoteTransform.localScale = Vector3.Lerp(
                Vector3.one * overshootScale,
                Vector3.one,
                eased
            );

            yield return null;
        }

        emoteTransform.localScale = Vector3.one;
        popRoutine = null;
    }

    private IEnumerator HideHappyAfterDelay()
    {
        yield return new WaitForSeconds(happyDisplayDuration);

        if (emoteImage != null)
        {
            emoteImage.sprite = null;
            emoteImage.enabled = false;
        }

        if (emoteTransform != null)
            emoteTransform.localScale = Vector3.zero;

        hideHappyRoutine = null;
    }

    private void StopHideRoutine()
    {
        if (hideHappyRoutine == null)
            return;

        StopCoroutine(hideHappyRoutine);
        hideHappyRoutine = null;
    }

    private void StopPopRoutine()
    {
        if (popRoutine == null)
            return;

        StopCoroutine(popRoutine);
        popRoutine = null;
    }

    private void OnDisable()
    {
        StopHideRoutine();
        StopPopRoutine();
    }
}