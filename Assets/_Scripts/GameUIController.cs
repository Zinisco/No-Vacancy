using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUIController : MonoBehaviour
{
    [SerializeField] private TMP_Text debugText;
    [SerializeField] private Button submitButton;

    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private List<StarRatingUI> stars = new();
    [SerializeField] private TMP_Text ratingText;

    public void SetDebugMessage(string message)
    {
        Debug.Log(message);

        if (debugText != null)
            debugText.text = message;
    }

    public void ShowGuestTooltip(GuestCard card)
    {
        if (TraitTooltipPanel.Instance != null && card != null)
            TraitTooltipPanel.Instance.ShowGuest(card);
    }

    public void ShowRoomTooltip(RoomSlot room)
    {
        if (TraitTooltipPanel.Instance != null && room != null)
            TraitTooltipPanel.Instance.ShowRoom(room);
    }

    public void HideTooltip()
    {
        if (TraitTooltipPanel.Instance != null)
            TraitTooltipPanel.Instance.Hide();
    }

    public void SetSubmitButtonState(bool canSubmit)
    {
        if (submitButton != null)
            submitButton.interactable = canSubmit;
    }

    public void ShowLevelCompletePanel(int starsEarned, int satisfiedGuests, int totalGuests)
    {
        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(true);

        if (ratingText != null)
            ratingText.text = $"Satisfied Guests: {satisfiedGuests} / {totalGuests}";

        StartCoroutine(AnimateStarsRoutine(starsEarned));
    }

    public void HideLevelCompletePanel()
    {
        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);

        for (int i = 0; i < stars.Count; i++)
        {
            if (stars[i] != null)
                stars[i].SetFilled(false);
        }
    }

    private IEnumerator AnimateStarsRoutine(int starsEarned)
    {
        starsEarned = Mathf.Clamp(starsEarned, 0, stars.Count);

        for (int i = 0; i < stars.Count; i++)
        {
            if (stars[i] != null)
                stars[i].SetFilled(false);
        }

        yield return new WaitForSeconds(0.3f);

        for (int i = 0; i < starsEarned; i++)
        {
            if (stars[i] != null)
                stars[i].SetFilled(true);

            yield return new WaitForSeconds(0.3f);
        }
    }
}