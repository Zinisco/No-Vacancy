using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUIController : MonoBehaviour
{
    [SerializeField] private TMP_Text debugText;
    [SerializeField] private Button submitButton;

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
}