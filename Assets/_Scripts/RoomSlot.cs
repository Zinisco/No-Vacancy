using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RoomSlot : MonoBehaviour, IPointerClickHandler
{
    [Header("Room Data")]
    [SerializeField] private string roomNumber = "101";

    [Header("Refs")]
    [SerializeField] private TMP_Text roomNumberText;
    [SerializeField] private Transform cardAnchor;
    [SerializeField] private Button button;

    public GuestCard CurrentCard { get; private set; }
    public string RoomNumber => roomNumber;

    private GameManager gameManager;

    public void Initialize(GameManager manager, string newRoomNumber)
    {
        gameManager = manager;
        roomNumber = newRoomNumber;

        if (roomNumberText != null)
            roomNumberText.text = roomNumber;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (gameManager == null || eventData == null)
            return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            gameManager.OnRoomLeftClicked(this);
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            gameManager.OnRoomRightClicked(this);
        }
    }

    public bool HasCard()
    {
        return CurrentCard != null;
    }

    public void SetCard(GuestCard card)
    {
        CurrentCard = card;
    }

    public void ClearCard()
    {
        CurrentCard = null;
    }

    public Transform GetCardAnchor()
    {
        return cardAnchor != null ? cardAnchor : transform;
    }

    public string GetHolderName()
    {
        return $"Room {roomNumber}";
    }
}