using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUIController : MonoBehaviour
{
    [SerializeField] private TMP_Text debugText;
    [SerializeField] private TMP_Text deckCountText;
    [SerializeField] private Button drawButton;

    public void SetDebugMessage(string message)
    {
        Debug.Log(message);

        if (debugText != null)
            debugText.text = message;
    }

    public void SetDeckCount(int deckCount)
    {
        if (deckCountText != null)
            deckCountText.text = deckCount.ToString();
    }

    public void SetDrawButtonState(bool canDraw)
    {
        if (drawButton != null)
            drawButton.interactable = canDraw;
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
}