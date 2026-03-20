using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GuestCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Button button;

    [SerializeField] private List<RoomTrait> preferredTraits = new List<RoomTrait>();
    public IReadOnlyList<RoomTrait> PreferredTraits => preferredTraits;

    [SerializeField] private List<FloorPreference> preferredFloorPreferences = new List<FloorPreference>();
    public IReadOnlyList<FloorPreference> PreferredFloorPreferences => preferredFloorPreferences;

    [Header("Trait Icons")]
    [SerializeField] private RoomTraitIconDatabase traitIconDatabase;
    [SerializeField] private Transform traitIconContainer;
    [SerializeField] private GameObject traitIconPrefab;

    [Header("Floor Preference Icons")]
    [SerializeField] private FloorPreferenceIconDatabase floorPreferenceIconDatabase;
    [SerializeField] private Transform floorPreferenceIconContainer;
    [SerializeField] private GameObject floorPreferenceIconPrefab;

    [Header("Selection Visual")]
    [SerializeField] private RectTransform visualRoot;
    [SerializeField] private float selectedRise = 25f;
    [SerializeField] private float selectedScale = 1.08f;
    [SerializeField] private float visualLerpSpeed = 12f;

    [Header("Hover Visual")]
    [SerializeField] private float hoverRise = 10f;
    [SerializeField] private float hoverScale = 1.02f;

    [Header("Hand Fan")]
    [SerializeField] private float handPoseLerpSpeed = 14f;

    public string CardId { get; private set; }
    public string DisplayName { get; private set; }

    public CardLocationType CurrentLocationType { get; private set; } = CardLocationType.Deck;
    public RoomSlot CurrentRoom { get; private set; }

    private GameManager gameManager;
    private bool isSelected;
    private bool isHovered;

    private RectTransform visualRect;
    private RectTransform rootRect;

    private Vector2 targetVisualOffset;
    private Vector3 targetVisualScale = Vector3.one;

    private Vector2 targetHandAnchoredPos;
    private float targetHandRotationZ;

    private void Awake()
    {
        rootRect = transform as RectTransform;
        visualRect = visualRoot != null ? visualRoot : rootRect;

        targetVisualOffset = Vector2.zero;
        targetVisualScale = Vector3.one;
        targetHandAnchoredPos = Vector2.zero;
        targetHandRotationZ = 0f;
    }

    private void Update()
    {
        if (rootRect != null)
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

    public void Initialize(string cardId, string displayName, GameManager manager)
    {
        CardId = cardId;
        DisplayName = displayName;
        gameManager = manager;

        if (nameText != null)
            nameText.text = displayName;

        isSelected = false;
        isHovered = false;

        RefreshVisualTargets(true);
        SetHandPose(Vector2.zero, 0f, true);
        RefreshTraitIcons();
        RefreshFloorPreferenceIcons();
    }

    public void SetSelected(bool selected, bool instant = false)
    {
        isSelected = selected;
        RefreshVisualTargets(instant);
    }

    public void SetHandPose(Vector2 anchoredPos, float rotationZ, bool instant = false)
    {
        targetHandAnchoredPos = anchoredPos;
        targetHandRotationZ = rotationZ;

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

    public void SetInHand()
    {
        CurrentLocationType = CardLocationType.Hand;
        CurrentRoom = null;
    }

    public void SetInRoom(RoomSlot room)
    {
        CurrentLocationType = CardLocationType.Room;
        CurrentRoom = room;

        isHovered = false;
        isSelected = false;

        ClearHandPoseInstant();
        RefreshVisualTargets(true);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (CurrentLocationType != CardLocationType.Hand)
            return;

        isHovered = true;
        RefreshVisualTargets(false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        RefreshVisualTargets(false);
    }

    private void RefreshVisualTargets(bool instant)
    {
        float rise = 0f;
        float scale = 1f;

        if (isHovered)
        {
            rise += hoverRise;
            scale *= hoverScale;
        }

        if (isSelected)
        {
            rise += selectedRise;
            scale *= selectedScale;
        }

        targetVisualOffset = new Vector2(0f, rise);
        targetVisualScale = Vector3.one * scale;

        if (instant && visualRect != null)
        {
            visualRect.anchoredPosition = targetVisualOffset;
            visualRect.localScale = targetVisualScale;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (gameManager == null || eventData == null)
            return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            gameManager.OnGuestCardLeftClicked(this);
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            gameManager.OnGuestCardRightClicked(this);
        }
    }

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

    public bool IsPerfectMatch(RoomSlot room)
    {
        if (room == null)
            return false;

        for (int i = 0; i < preferredTraits.Count; i++)
        {
            if (!room.HasTrait(preferredTraits[i]))
                return false;
        }

        int topFloor = gameManager != null ? gameManager.GetTopFloorIndex() : 1;

        for (int i = 0; i < preferredFloorPreferences.Count; i++)
        {
            if (!room.MatchesFloorPreference(preferredFloorPreferences[i], topFloor))
                return false;
        }

        return true;
    }

    public void SetPreferredTraits(List<RoomTrait> newTraits)
    {
        preferredTraits.Clear();

        if (newTraits != null)
            preferredTraits.AddRange(newTraits);

        RefreshTraitIcons();
    }

    public void SetPreferredFloorPreferences(List<FloorPreference> newPreferences)
    {
        preferredFloorPreferences.Clear();

        if (newPreferences != null)
            preferredFloorPreferences.AddRange(newPreferences);

        RefreshFloorPreferenceIcons();
    }

    private void RefreshTraitIcons()
    {
        if (traitIconContainer == null || traitIconPrefab == null || traitIconDatabase == null)
            return;

        for (int i = traitIconContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(traitIconContainer.GetChild(i).gameObject);
        }

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

    private void RefreshFloorPreferenceIcons()
    {
        if (floorPreferenceIconContainer == null || floorPreferenceIconPrefab == null || floorPreferenceIconDatabase == null)
            return;

        for (int i = floorPreferenceIconContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(floorPreferenceIconContainer.GetChild(i).gameObject);
        }

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