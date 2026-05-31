using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TraitTooltipPanel : MonoBehaviour
{
    public static TraitTooltipPanel Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;

    [Header("Content")]
    [SerializeField] private Transform rowContainer;
    [SerializeField] private TooltipTraitRowUI rowPrefab;

    [Header("Icons")]
    [SerializeField] private RoomTraitIconDatabase traitIconDatabase;
    [SerializeField] private FloorPreferenceIconDatabase floorPreferenceIconDatabase;
    [SerializeField] private GuestBehaviorIconDatabase behaviorIconDatabase;
    [SerializeField] private GuestRequirementIconDatabase requirementIconDatabase;
    [SerializeField] private GuestAdjacencyPreferenceIconDatabase adjacencyIconDatabase;

    private const int MaxGuestTooltipRows = 5;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Hide();
    }

    public void ShowRoom(RoomSlot room)
    {
        if (room == null)
            return;

        string title = room.IsElevator ? room.RoomNumber : $"Room {room.RoomNumber}";
        string subtitle = room.IsElevator ? "Elevator" : "Amenities";

        ShowRoomContent(title, subtitle, room.Traits);
    }

    public void ShowGuest(GuestCard guest)
    {
        if (guest == null)
            return;

        ShowGuestContent(
    guest.DisplayName,
    "Guest Preferences",
    guest.PreferredTraits,
    guest.PreferredFloorPreferences,
    guest.BehaviorTraits,
    guest.Requirements,
    guest.AdjacencyPreferences
);
    }

    private void ShowRoomContent(string title, string subtitle, IReadOnlyList<RoomTrait> traits)
    {
        ShowRoot(title, subtitle);
        ClearRows();

        if (traits == null || traits.Count == 0)
            return;

        for (int i = 0; i < traits.Count; i++)
        {
            AddTraitRow(traits[i]);
        }
    }

    private void ShowGuestContent(
    string title,
    string subtitle,
    IReadOnlyList<RoomTrait> traits,
    IReadOnlyList<FloorPreference> floorPreferences,
    IReadOnlyList<GuestBehaviorTrait> behaviorTraits,
    IReadOnlyList<GuestRequirement> requirements,
    IReadOnlyList<GuestAdjacencyPreference> adjacencyPreferences)
    {
        ShowRoot(title, subtitle);
        ClearRows();

        int added = 0;

        for (int i = 0; traits != null && i < traits.Count && added < MaxGuestTooltipRows; i++, added++)
            AddTraitRow(traits[i]);

        for (int i = 0; floorPreferences != null && i < floorPreferences.Count && added < MaxGuestTooltipRows; i++, added++)
            AddFloorPreferenceRow(floorPreferences[i]);

        for (int i = 0; behaviorTraits != null && i < behaviorTraits.Count && added < MaxGuestTooltipRows; i++, added++)
            AddBehaviorRow(behaviorTraits[i]);

        for (int i = 0; requirements != null && i < requirements.Count && added < MaxGuestTooltipRows; i++, added++)
            AddRequirementRow(requirements[i]);

        for (int i = 0; adjacencyPreferences != null && i < adjacencyPreferences.Count && added < MaxGuestTooltipRows; i++, added++)
            AddAdjacencyRow(adjacencyPreferences[i]);
    }

    private void ShowRoot(string title, string subtitle)
    {
        if (root != null)
            root.SetActive(true);

        if (titleText != null)
            titleText.text = title;

        if (subtitleText != null)
            subtitleText.text = subtitle;
    }

    private void AddTraitRow(RoomTrait trait)
    {
        TooltipTraitRowUI row = Instantiate(rowPrefab, rowContainer);
        Sprite icon = traitIconDatabase != null ? traitIconDatabase.GetIcon(trait) : null;
        string label = RoomTraitUtility.GetDisplayName(trait);
        row.SetData(icon, label);
    }

    private void AddFloorPreferenceRow(FloorPreference preference)
    {
        TooltipTraitRowUI row = Instantiate(rowPrefab, rowContainer);
        Sprite icon = floorPreferenceIconDatabase != null ? floorPreferenceIconDatabase.GetIcon(preference) : null;
        string label = FloorPreferenceUtility.GetDisplayName(preference);
        row.SetData(icon, label);
    }

    private void AddBehaviorRow(GuestBehaviorTrait behavior)
    {
        TooltipTraitRowUI row = Instantiate(rowPrefab, rowContainer);
        Sprite icon = behaviorIconDatabase != null ? behaviorIconDatabase.GetIcon(behavior) : null;
        string label = behavior.ToString();
        row.SetData(icon, label);
    }

    private void AddRequirementRow(GuestRequirement requirement)
    {
        TooltipTraitRowUI row = Instantiate(rowPrefab, rowContainer);
        Sprite icon = requirementIconDatabase != null ? requirementIconDatabase.GetIcon(requirement) : null;
        string label = GuestRequirementUtility.GetDisplayName(requirement);
        row.SetData(icon, label);
    }

    private void AddAdjacencyRow(GuestAdjacencyPreference preference)
    {
        TooltipTraitRowUI row = Instantiate(rowPrefab, rowContainer);
        Sprite icon = adjacencyIconDatabase != null ? adjacencyIconDatabase.GetIcon(preference.type) : null;
        string label = preference.type.ToString();
        row.SetData(icon, label);
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);

        ClearRows();
    }

    private void ClearRows()
    {
        if (rowContainer == null)
            return;

        for (int i = rowContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(rowContainer.GetChild(i).gameObject);
        }
    }
}