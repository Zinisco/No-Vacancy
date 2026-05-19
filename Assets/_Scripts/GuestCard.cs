using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// GuestCard represents one guest card in the game.
// It handles:
// - the guest's name
// - the guest's preferred room traits
// - the guest's preferred floor
// - click behavior
// - hover/selected visuals
// - hand fan positioning
// - checking if the guest matches a room
public class GuestCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI")]

    // Text that displays the guest's name.
    [SerializeField] private TMP_Text nameText;

    // Button reference. Not heavily used here, but useful for UI behavior later.
    [SerializeField] private Button button;

    // List of adjacency preferences for this guest, such as hating a smoking neighbor or wanting a specific guest next door
    [SerializeField] private List<GuestAdjacencyPreference> adjacencyPreferences = new();
    public IReadOnlyList<GuestAdjacencyPreference> AdjacencyPreferences => adjacencyPreferences;

    // Room traits this guest wants.
    // Example: Balcony, Scenic, NonSmoking.
    [SerializeField] private List<RoomTrait> preferredTraits = new List<RoomTrait>();

    // Public read-only access to this guest's preferred traits.
    public IReadOnlyList<RoomTrait> PreferredTraits => preferredTraits;

    [SerializeField] private List<GuestBehaviorTrait> behaviorTraits = new();
    public IReadOnlyList<GuestBehaviorTrait> BehaviorTraits => behaviorTraits;

    // Floor preferences this guest has.
    // Example: FirstFloor, SecondFloor, ThirdFloor.
    [SerializeField] private List<FloorPreference> preferredFloorPreferences = new List<FloorPreference>();

    // Public read-only access to this guest's floor preferences.
    public IReadOnlyList<FloorPreference> PreferredFloorPreferences => preferredFloorPreferences;

    [Header("Trait Icons")]

    // Database that converts RoomTrait values into sprites/icons.
    [SerializeField] private RoomTraitIconDatabase traitIconDatabase;

    // Parent object where room trait icons are spawned.
    [SerializeField] private Transform traitIconContainer;

    // Prefab used for each room trait icon.
    [SerializeField] private GameObject traitIconPrefab;

    [Header("Floor Preference Icons")]

    // Database that converts FloorPreference values into sprites/icons.
    [SerializeField] private FloorPreferenceIconDatabase floorPreferenceIconDatabase;

    // Parent object where floor preference icons are spawned.
    [SerializeField] private Transform floorPreferenceIconContainer;

    // Prefab used for each floor preference icon.
    [SerializeField] private GameObject floorPreferenceIconPrefab;

    [Header("Selection Visual")]

    // The visual part of the card.
    // This can be different from the root so the card can move in the hand
    // while only the artwork pops upward on hover/selection.
    [SerializeField] private RectTransform visualRoot;

    // How high the card rises when selected.
    [SerializeField] private float selectedRise = 25f;

    // How much the card grows when selected.
    [SerializeField] private float selectedScale = 1.08f;

    // How quickly the visual hover/selection animation happens.
    [SerializeField] private float visualLerpSpeed = 12f;

    [Header("Hover Visual")]

    // How high the card rises when hovered.
    [SerializeField] private float hoverRise = 10f;

    // How much the card grows when hovered.
    [SerializeField] private float hoverScale = 1.02f;

    [Header("Hand Fan")]

    // How fast the card moves toward its target hand position.
    [SerializeField] private float handPoseLerpSpeed = 14f;

    // If true, the card smoothly moves toward its hand pose.
    // If false, another script can move the card manually during animations.
    private bool handPoseLerpEnabled = true;

    // Unique ID for this card.
    // Example: CARD_001.
    public string CardId { get; private set; }

    // The guest's visible name.
    public string DisplayName { get; private set; }

    // Where the card currently is:
    // Deck, Hand, or Room.
    public CardLocationType CurrentLocationType { get; private set; } = CardLocationType.Deck;

    // If the card is in a room, this stores which room.
    public RoomSlot CurrentRoom { get; private set; }

    // Reference to the GameManager.
    // This lets the card tell the GameManager when it was clicked.
    private GameManager gameManager;

    // Whether this card is currently selected.
    private bool isSelected;

    // Whether the mouse is hovering this card.
    private bool isHovered;

    // The visual RectTransform of the card art.
    private RectTransform visualRect;

    // The root RectTransform of the whole card.
    private RectTransform rootRect;

    // Target visual offset for hover/selection movement.
    private Vector2 targetVisualOffset;

    // Target visual scale for hover/selection growth.
    private Vector3 targetVisualScale = Vector3.one;

    // Target position for this card in the hand fan.
    private Vector2 targetHandAnchoredPos;

    // Target rotation for this card in the hand fan.
    private float targetHandRotationZ;

    private void Awake()
    {
        // Cache this card's root RectTransform.
        rootRect = transform as RectTransform;

        // If a visualRoot was assigned, use it.
        // Otherwise, use the whole card as the visual.
        visualRect = visualRoot != null ? visualRoot : rootRect;

        // Set default target values.
        targetVisualOffset = Vector2.zero;
        targetVisualScale = Vector3.one;
        targetHandAnchoredPos = Vector2.zero;
        targetHandRotationZ = 0f;
    }

    private void Update()
    {
        // Smoothly move and rotate the card toward its hand position.
        // This is what creates the fan layout movement.
        if (rootRect != null && handPoseLerpEnabled)
        {
            rootRect.anchoredPosition = Vector2.Lerp(
                rootRect.anchoredPosition,
                targetHandAnchoredPos,
                Time.deltaTime * handPoseLerpSpeed
            );

            Quaternion targetRot = Quaternion.Euler(0f, 0f, targetHandRotationZ);

            rootRect.localRotation = Quaternion.Lerp(
                rootRect.localRotation,
                targetRot,
                Time.deltaTime * handPoseLerpSpeed
            );
        }

        // Smoothly move and scale the visual part of the card.
        // This handles hover and selected effects.
        if (visualRect != null)
        {
            visualRect.anchoredPosition = Vector2.Lerp(
                visualRect.anchoredPosition,
                targetVisualOffset,
                Time.deltaTime * visualLerpSpeed
            );

            visualRect.localScale = Vector3.Lerp(
                visualRect.localScale,
                targetVisualScale,
                Time.deltaTime * visualLerpSpeed
            );
        }
    }

    // Called when the card is first created.
    // GameManager passes in the ID, name, and itself.
    public void Initialize(string cardId, string displayName, GameManager manager)
    {
        CardId = cardId;
        DisplayName = displayName;
        gameManager = manager;

        // Show the guest name on the card.
        if (nameText != null)
            nameText.text = displayName;

        isSelected = false;
        isHovered = false;

        // Reset visuals and create icons.
        RefreshVisualTargets(true);
        SetHandPose(Vector2.zero, 0f, true);
        RefreshTraitIcons();
        RefreshFloorPreferenceIcons();
    }

    // Sets whether this card is selected.
    public void SetSelected(bool selected, bool instant = false)
    {
        isSelected = selected;
        RefreshVisualTargets(instant);
    }

    // Sets where this card should sit in the hand fan.
    public void SetHandPose(Vector2 anchoredPos, float rotationZ, bool instant = false)
    {
        targetHandAnchoredPos = anchoredPos;
        targetHandRotationZ = rotationZ;

        // If instant is true, immediately move there instead of lerping.
        if (instant && rootRect != null)
        {
            rootRect.anchoredPosition = targetHandAnchoredPos;
            rootRect.localRotation = Quaternion.Euler(0f, 0f, targetHandRotationZ);
        }
    }

    public bool IsSelected()
    {
        return isSelected;
    }

    // Fully resets the card's visual state immediately.
    public void ResetVisualInstant()
    {
        if (rootRect != null)
        {
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.localRotation = Quaternion.identity;
        }

        if (visualRect != null)
        {
            visualRect.anchoredPosition = Vector2.zero;
            visualRect.localScale = Vector3.one;
        }

        targetHandAnchoredPos = Vector2.zero;
        targetHandRotationZ = 0f;
        targetVisualOffset = Vector2.zero;
        targetVisualScale = Vector3.one;
        isSelected = false;
        isHovered = false;
    }

    // Marks this card as being in the player's hand.
    public void SetInHand()
    {
        CurrentLocationType = CardLocationType.Hand;
        CurrentRoom = null;
    }

    // Marks this card as being inside a room.
    public void SetInRoom(RoomSlot room)
    {
        CurrentLocationType = CardLocationType.Room;
        CurrentRoom = room;

        isHovered = false;
        isSelected = false;

        ClearHandPoseInstant();
        RefreshVisualTargets(true);
    }

    // Called when the mouse enters the card.
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Only cards in the hand get hover effects.
        if (CurrentLocationType != CardLocationType.Hand)
            return;

        isHovered = true;
        RefreshVisualTargets(false);
    }

    // Called when the mouse leaves the card.
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        RefreshVisualTargets(false);
    }

    // Recalculates how high and large the card should visually be.
    private void RefreshVisualTargets(bool instant)
    {
        float rise = 0f;
        float scale = 1f;

        // Hover makes the card rise a little.
        if (isHovered)
        {
            rise += hoverRise;
            scale *= hoverScale;
        }

        // Selection makes the card rise more and grow more.
        if (isSelected)
        {
            rise += selectedRise;
            scale *= selectedScale;
        }

        targetVisualOffset = new Vector2(0f, rise);
        targetVisualScale = Vector3.one * scale;

        // Apply instantly if requested.
        if (instant && visualRect != null)
        {
            visualRect.anchoredPosition = targetVisualOffset;
            visualRect.localScale = targetVisualScale;
        }
    }

    // Called automatically when the player clicks this card.
    public void OnPointerClick(PointerEventData eventData)
    {
        if (gameManager == null || eventData == null)
            return;

        // Left click selects, places, swaps, etc.
        // The GameManager decides what actually happens.
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            gameManager.OnGuestCardLeftClicked(this);
        }
        // Right click usually returns the card from a room to the hand.
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            gameManager.OnGuestCardRightClicked(this);
        }
    }

    // Resets this card's hand fan position/rotation instantly.
    public void ClearHandPoseInstant()
    {
        targetHandAnchoredPos = Vector2.zero;
        targetHandRotationZ = 0f;

        if (rootRect != null)
        {
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.localRotation = Quaternion.identity;
        }
    }

    // Counts how many of this guest's preferred traits the room has.
    // Example:
    // Guest wants Scenic and Balcony.
    // Room has Scenic only.
    // Result = 1.
    public int CountMatchedTraits(RoomSlot room)
    {
        if (room == null)
            return 0;

        int matches = 0;

        for (int i = 0; i < preferredTraits.Count; i++)
        {
            if (room.HasTrait(preferredTraits[i]))
                matches++;
        }

        return matches;
    }

    // Checks whether this room fully satisfies this guest.
    public bool IsPerfectMatch(RoomSlot room)
    {
        if (room == null)
            return false;

        // First, check every room trait the guest wants.
        // If the room is missing even one, it is not perfect.
        for (int i = 0; i < preferredTraits.Count; i++)
        {
            if (!room.HasTrait(preferredTraits[i]))
                return false;
        }

        // Ask GameManager what the top floor is.
        // If GameManager is missing, assume top floor is 1.
        int topFloor = gameManager != null ? gameManager.GetTopFloorIndex() : 1;

        // Then check floor preferences.
        for (int i = 0; i < preferredFloorPreferences.Count; i++)
        {
            if (!room.MatchesFloorPreference(preferredFloorPreferences[i], topFloor))
                return false;
        }

        if (!MatchesAdjacencyPreferences(room))
            return false;

        // If all checks passed, the guest is perfectly matched.
        return true;
    }

    // Replaces this guest's preferred room traits.
    public void SetPreferredTraits(List<RoomTrait> newTraits)
    {
        preferredTraits.Clear();

        if (newTraits != null)
            preferredTraits.AddRange(newTraits);

        RefreshTraitIcons();
    }

    // Replaces this guest's preferred floor preferences.
    public void SetPreferredFloorPreferences(List<FloorPreference> newPreferences)
    {
        preferredFloorPreferences.Clear();

        if (newPreferences != null)
            preferredFloorPreferences.AddRange(newPreferences);

        RefreshFloorPreferenceIcons();
    }

    // Rebuilds the room trait icons on the guest card.
    private void RefreshTraitIcons()
    {
        if (traitIconContainer == null || traitIconPrefab == null || traitIconDatabase == null)
            return;

        // Clear old icons first.
        for (int i = traitIconContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(traitIconContainer.GetChild(i).gameObject);
        }

        // Spawn one icon for each preferred room trait.
        for (int i = 0; i < preferredTraits.Count; i++)
        {
            RoomTrait trait = preferredTraits[i];
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

    // Instantly moves the card to its current target hand position.
    // Useful after layout changes so cards don't visually jump.
    public void SnapToCurrentHandPose()
    {
        if (rootRect != null)
        {
            rootRect.anchoredPosition = targetHandAnchoredPos;
            rootRect.localRotation = Quaternion.Euler(0f, 0f, targetHandRotationZ);
        }

        if (visualRect != null)
        {
            visualRect.anchoredPosition = targetVisualOffset;
            visualRect.localScale = targetVisualScale;
        }
    }

    // Returns the target hand position.
    // CardAnimationController uses this during draw animations.
    public Vector2 GetTargetHandAnchoredPos()
    {
        return targetHandAnchoredPos;
    }

    // Returns the target hand rotation.
    // CardAnimationController uses this during draw animations.
    public float GetTargetHandRotationZ()
    {
        return targetHandRotationZ;
    }

    // Instantly sets the card root position and rotation.
    // Used by animation scripts that want direct control.
    public void SetRootPoseInstant(Vector2 anchoredPos, float rotationZ)
    {
        if (rootRect != null)
        {
            rootRect.anchoredPosition = anchoredPos;
            rootRect.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
        }
    }

    // Turns automatic hand pose smoothing on or off.
    // During special animations, this is turned off so the animation script can move the card.
    public void SetHandPoseLerpEnabled(bool enabled)
    {
        handPoseLerpEnabled = enabled;
    }

    // Sets this guest's adjacency preferences.
    public void SetAdjacencyPreferences(List<GuestAdjacencyPreference> newPreferences) 
    {
        adjacencyPreferences.Clear();

        if (newPreferences != null)
            adjacencyPreferences.AddRange(newPreferences);
    }

    private bool MatchesAdjacencyPreferences(RoomSlot room)
    {
        if (gameManager == null || room == null)
            return true;

        List<RoomSlot> horizontalRooms = gameManager.GetAdjacentRooms(room);
        List<RoomSlot> noiseRooms = gameManager.GetNoiseAdjacentRooms(room);

        for (int i = 0; i < adjacencyPreferences.Count; i++)
        {
            GuestAdjacencyPreference preference = adjacencyPreferences[i];

            switch (preference.type)
            {
                case GuestAdjacencyPreferenceType.HatesSmokingNeighbor:
                    if (HasAdjacentGuestWithBehavior(horizontalRooms, GuestBehaviorTrait.Smokes))
                        return false;
                    break;

                case GuestAdjacencyPreferenceType.HatesLoudNeighbor:
                    if (HasAdjacentGuestWithBehavior(noiseRooms, GuestBehaviorTrait.Noisy))
                        return false;
                    break;

                case GuestAdjacencyPreferenceType.WantsNamedGuestNeighbor:
                    if (!HasAdjacentGuestNamed(horizontalRooms, preference.targetGuestName))
                        return false;
                    break;
            }
        }

        return true;
    }

    private bool HasAdjacentGuestWithBehavior(List<RoomSlot> rooms, GuestBehaviorTrait trait)
    {
        for (int i = 0; i < rooms.Count; i++)
        {
            GuestCard card = rooms[i].CurrentCard;

            if (card != null && card.HasBehaviorTrait(trait))
                return true;
        }

        return false;
    }

    private bool HasAdjacentGuestNamed(List<RoomSlot> rooms, string guestName)
    {
        if (string.IsNullOrWhiteSpace(guestName))
            return false;

        for (int i = 0; i < rooms.Count; i++)
        {
            GuestCard card = rooms[i].CurrentCard;

            if (card != null && card.DisplayName == guestName)
                return true;
        }

        return false;
    }

    public void SetBehaviorTraits(List<GuestBehaviorTrait> newTraits)
    {
        behaviorTraits.Clear();

        if (newTraits != null)
            behaviorTraits.AddRange(newTraits);
    }

    public bool HasBehaviorTrait(GuestBehaviorTrait trait)
    {
        return behaviorTraits.Contains(trait);
    }

    // Rebuilds the floor preference icons on the guest card.
    private void RefreshFloorPreferenceIcons()
    {
        if (floorPreferenceIconContainer == null || floorPreferenceIconPrefab == null || floorPreferenceIconDatabase == null)
            return;

        // Clear old icons first.
        for (int i = floorPreferenceIconContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(floorPreferenceIconContainer.GetChild(i).gameObject);
        }

        // Spawn one icon for each floor preference.
        for (int i = 0; i < preferredFloorPreferences.Count; i++)
        {

            FloorPreference preference = preferredFloorPreferences[i];
            GameObject iconObj = Instantiate(floorPreferenceIconPrefab, floorPreferenceIconContainer);

            Sprite iconSprite = floorPreferenceIconDatabase.GetIcon(preference);

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