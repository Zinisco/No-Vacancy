using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// RoomSlot represents one space on the hotel board.
// This could be a normal guest room or an elevator slot.
public class RoomSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Slot Data")]

    // The visible room number, like "101", "102", etc.
    [SerializeField] private string roomNumber = "101";

    // Tells us whether this slot is a Room or an Elevator.
    [SerializeField] private SlotType slotType = SlotType.Room;

    // Which floor this room is on.
    // Example: 1 = first floor, 2 = second floor.
    [SerializeField] private int floorIndex;

    // Which horizontal position this room is in on its floor.
    // This is used for adjacency checks.
    [SerializeField] private int columnIndex;

    // The traits this room has.
    // Example: Smoking, Balcony, Scenic, Budget.
    [SerializeField] private List<RoomTrait> traits = new List<RoomTrait>();

    // Public read-only access to the room traits.
    // Other scripts can look at the traits, but they cannot directly change the list.
    public IReadOnlyList<RoomTrait> Traits => traits;

    [Header("Refs")]

    // Text component that shows the room number on screen.
    [SerializeField] private TMP_Text roomNumberText;

    // Where the guest card should sit when placed in this room.
    [SerializeField] private Transform cardAnchor;

    // Button reference, currently not used much, but useful if needed later.
    [SerializeField] private Button button;

    [Header("Trait Icons")]

    // Database that knows which sprite/icon belongs to each RoomTrait.
    [SerializeField] private RoomTraitIconDatabase traitIconDatabase;

    // Parent object that holds all the trait icons for this room.
    [SerializeField] private Transform traitIconContainer;

    // Prefab used to create each trait icon.
    [SerializeField] private GameObject traitIconPrefab;

    [Header("Hover Scale")]

    // The object that visually scales when hovering.
    [SerializeField] private RectTransform scaleTarget;

    // How large the room becomes on hover.
    [SerializeField] private float hoverScale = 1.1f;

    // How quickly the room scales up/down.
    [SerializeField] private float scaleSpeed = 10f;

    // The guest card currently placed in this room.
    // Only this script can set it directly.
    public GuestCard CurrentCard { get; private set; }

    // Public shortcuts so other scripts can read room info.
    public string RoomNumber => roomNumber;
    public SlotType Type => slotType;

    // Returns true if this slot is an elevator.
    public bool IsElevator => slotType == SlotType.Elevator;

    // Returns true if this slot can accept guests.
    // Elevators cannot accept guests.
    public bool CanAcceptGuest => slotType == SlotType.Room;

    public int FloorIndex => floorIndex;
    public int ColumnIndex => columnIndex;

    // Reference back to the GameManager.
    // This lets the room tell the GameManager when it was clicked.
    private GameManager gameManager;

    // The normal scale of the room.
    private Vector3 baseScale;

    // The scale the room is trying to move toward.
    private Vector3 targetScale;

    private void Awake()
    {
        // If no scale target was assigned, use this object's RectTransform.
        if (scaleTarget == null)
            scaleTarget = transform as RectTransform;

        // Save the starting scale so we can return to it after hover.
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

        // Smoothly scale the room toward the target scale.
        // This creates the hover grow/shrink animation.
        scaleTarget.localScale = Vector3.Lerp(
            scaleTarget.localScale,
            targetScale,
            Time.deltaTime * scaleSpeed
        );
    }

    // Called by GameManager when the room is created.
    // It fills this RoomSlot with data from the level.
    public void Initialize(GameManager manager, LevelRoomEntry data)
    {
        gameManager = manager;

        roomNumber = data.roomNumber;
        slotType = data.slotType;
        floorIndex = data.floorIndex;
        columnIndex = data.columnIndex;

        // Copy traits from the level data into this room.
        traits.Clear();
        if (data.traits != null)
            traits.AddRange(data.traits);

        // Update the visible room number text.
        if (roomNumberText != null)
            roomNumberText.text = roomNumber;

        // Create the visual trait icons.
        RefreshTraitIcons();
    }

    // Called automatically when the player clicks this room.
    public void OnPointerClick(PointerEventData eventData)
    {
        if (gameManager == null || eventData == null)
            return;

        // Left click tells GameManager this room was left-clicked.
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            gameManager.OnRoomLeftClicked(this);
        }
        // Right click tells GameManager this room was right-clicked.
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            gameManager.OnRoomRightClicked(this);
        }
    }

    // Called when the mouse enters this room.
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Make the room grow slightly.
        targetScale = baseScale * hoverScale;
    }

    // Called when the mouse leaves this room.
    public void OnPointerExit(PointerEventData eventData)
    {
        // Return the room to normal size.
        targetScale = baseScale;
    }

    // Returns true if a guest card is currently placed here.
    public bool HasCard()
    {
        return CurrentCard != null;
    }

    // Places a guest card in this room.
    public void SetCard(GuestCard card)
    {
        // Do not allow cards to be placed in elevators.
        if (!CanAcceptGuest)
            return;

        CurrentCard = card;
    }

    // Removes the current guest card reference from this room.
    public void ClearCard()
    {
        CurrentCard = null;
    }

    // Returns the transform where the guest card should visually attach.
    public Transform GetCardAnchor()
    {
        return cardAnchor != null ? cardAnchor : transform;
    }

    // Returns a readable name for logs/debug messages.
    public string GetHolderName()
    {
        return IsElevator ? "Elevator" : $"Room {roomNumber}";
    }

    // Checks if this room has a specific trait.
    public bool HasTrait(RoomTrait trait)
    {
        return traits.Contains(trait);
    }

    // Replaces all room traits with a new list.
    public void SetTraits(List<RoomTrait> newTraits)
    {
        traits.Clear();

        if (newTraits != null)
            traits.AddRange(newTraits);

        RefreshTraitIcons();
    }

    // Adds one trait to the room if it does not already have it.
    public void AddTrait(RoomTrait trait)
    {
        if (!traits.Contains(trait))
        {
            traits.Add(trait);
            RefreshTraitIcons();
        }
    }

    // Removes one trait from the room.
    public void RemoveTrait(RoomTrait trait)
    {
        if (traits.Remove(trait))
            RefreshTraitIcons();
    }

    // Checks if this room is next to another room.
    // Right now, adjacency means:
    // - same floor
    // - column is exactly 1 space apart
    public bool IsAdjacentTo(RoomSlot other)
    {
        if (other == null)
            return false;

        if (floorIndex != other.floorIndex)
            return false;

        return Mathf.Abs(columnIndex - other.columnIndex) == 1;
    }

    // Checks if this room matches a guest's floor preference.
    public bool MatchesFloorPreference(FloorPreference preference, int topFloor)
    {
        switch (preference)
        {
            case FloorPreference.FirstFloor:
                return floorIndex == 1;

            case FloorPreference.SecondFloor:
                return floorIndex == 2;

            case FloorPreference.ThirdFloor:
                return floorIndex == topFloor;

            default:
                return false;
        }
    }

    public bool IsNoiseAdjacentTo(RoomSlot other)
    {
        if (other == null)
            return false;

        int floorDifference = Mathf.Abs(floorIndex - other.floorIndex);
        int columnDifference = Mathf.Abs(columnIndex - other.columnIndex);

        bool horizontalNeighbor = floorDifference == 0 && columnDifference == 1;
        bool verticalNeighbor = floorDifference == 1 && columnDifference == 0;

        return horizontalNeighbor || verticalNeighbor;
    }

    // Rebuilds the little icons shown on the room.
    private void RefreshTraitIcons()
    {
        // If anything important is missing, stop.
        if (traitIconContainer == null || traitIconPrefab == null || traitIconDatabase == null)
            return;

        // Delete old icons first so we do not duplicate them.
        for (int i = traitIconContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(traitIconContainer.GetChild(i).gameObject);
        }

        // Elevators do not show room trait icons.
        if (IsElevator)
            return;

        // Create one icon for each room trait.
        for (int i = 0; i < traits.Count; i++)
        {
            RoomTrait trait = traits[i];

            // Spawn the icon prefab inside the icon container.
            GameObject iconObj = Instantiate(traitIconPrefab, traitIconContainer);

            // Ask the database for the correct sprite.
            Sprite iconSprite = traitIconDatabase.GetIcon(trait);

            // Preferred method: use your TraitIconUI component.
            TraitIconUI iconUI = iconObj.GetComponent<TraitIconUI>();
            if (iconUI != null)
            {
                iconUI.SetSprite(iconSprite);
            }
            else
            {
                // Backup method: directly set the Image component.
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