using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RoomSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Slot Data")]
    [SerializeField] private string roomNumber = "101";
    [SerializeField] private SlotType slotType = SlotType.Room;
    [SerializeField] private int floorIndex;
    [SerializeField] private int columnIndex;

    [SerializeField] private List<RoomTrait> traits = new List<RoomTrait>();
    public IReadOnlyList<RoomTrait> Traits => traits;

    [Header("Refs")]
    [SerializeField] private TMP_Text roomNumberText;
    [SerializeField] private Transform cardAnchor;
    [SerializeField] private Button button;

    [Header("Trait Icons")]
    [SerializeField] private RoomTraitIconDatabase traitIconDatabase;
    [SerializeField] private Transform traitIconContainer;
    [SerializeField] private GameObject traitIconPrefab;

    [Header("Hover Scale")]
    [SerializeField] private RectTransform scaleTarget;
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float scaleSpeed = 10f;

    public GuestCard CurrentCard { get; private set; }
    public string RoomNumber => roomNumber;
    public SlotType Type => slotType;
    public bool IsElevator => slotType == SlotType.Elevator;
    public bool CanAcceptGuest => slotType == SlotType.Room;

    public int FloorIndex => floorIndex;
    public int ColumnIndex => columnIndex;

    private GameManager gameManager;

    private Vector3 baseScale;
    private Vector3 targetScale;

    private void Awake()
    {
        if (scaleTarget == null)
            scaleTarget = transform as RectTransform;

        if (scaleTarget != null)
        {
            baseScale = scaleTarget.localScale;
            targetScale = baseScale;
        }
    }

    private void Update()
    {
        if (scaleTarget == null)
            return;

        scaleTarget.localScale = Vector3.Lerp(
            scaleTarget.localScale,
            targetScale,
            Time.deltaTime * scaleSpeed
        );
    }

    public void Initialize(GameManager manager, LevelRoomEntry data)
    {
        gameManager = manager;

        roomNumber = data.roomNumber;
        slotType = data.slotType;
        floorIndex = data.floorIndex;
        columnIndex = data.columnIndex;

        traits.Clear();
        if (data.traits != null)
            traits.AddRange(data.traits);

        if (roomNumberText != null)
            roomNumberText.text = roomNumber;

        RefreshTraitIcons();
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

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = baseScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = baseScale;
    }

    public bool HasCard()
    {
        return CurrentCard != null;
    }

    public void SetCard(GuestCard card)
    {
        if (!CanAcceptGuest)
            return;

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
        return IsElevator ? "Elevator" : $"Room {roomNumber}";
    }

    public bool HasTrait(RoomTrait trait)
    {
        return traits.Contains(trait);
    }

    public void SetTraits(List<RoomTrait> newTraits)
    {
        traits.Clear();

        if (newTraits != null)
            traits.AddRange(newTraits);

        RefreshTraitIcons();
    }

    public void AddTrait(RoomTrait trait)
    {
        if (!traits.Contains(trait))
        {
            traits.Add(trait);
            RefreshTraitIcons();
        }
    }

    public void RemoveTrait(RoomTrait trait)
    {
        if (traits.Remove(trait))
            RefreshTraitIcons();
    }

    public bool IsAdjacentTo(RoomSlot other)
    {
        if (other == null)
            return false;

        if (floorIndex != other.floorIndex)
            return false;

        return Mathf.Abs(columnIndex - other.columnIndex) == 1;
    }

    public bool MatchesFloorPreference(FloorPreference preference, int topFloor)
    {
        switch (preference)
        {
            case FloorPreference.FirstFloor:
                return floorIndex == 1;

            case FloorPreference.SecondFloor:
                return floorIndex == 2;

            case FloorPreference.TopFloor:
                return floorIndex == topFloor;

            default:
                return false;
        }
    }

    private void RefreshTraitIcons()
    {
        if (traitIconContainer == null || traitIconPrefab == null || traitIconDatabase == null)
            return;

        for (int i = traitIconContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(traitIconContainer.GetChild(i).gameObject);
        }

        // Elevator doesn't need trait icons unless you want them.
        if (IsElevator)
            return;

        for (int i = 0; i < traits.Count; i++)
        {
            RoomTrait trait = traits[i];
            GameObject iconObj = Instantiate(traitIconPrefab, traitIconContainer);

            Sprite iconSprite = traitIconDatabase.GetIcon(trait);

            TraitIconUI iconUI = iconObj.GetComponent<TraitIconUI>();
            if (iconUI != null)
            {
                iconUI.SetSprite(iconSprite);
            }
            else
            {
                Image image = iconObj.GetComponent<Image>();
                if (image != null)
                {
                    image.sprite = iconSprite;
                    image.enabled = iconSprite != null;
                }
            }
        }
    }
}