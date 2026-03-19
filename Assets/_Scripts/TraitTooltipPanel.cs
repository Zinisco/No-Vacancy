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
    [SerializeField] private TMP_Text floorPreferenceText;

    [Header("Content")]
    [SerializeField] private Transform rowContainer;
    [SerializeField] private TooltipTraitRowUI rowPrefab;

    [Header("Icons")]
    [SerializeField] private RoomTraitIconDatabase traitIconDatabase;

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
        string subtitle = room.IsElevator ? "Elevator" : "Room Traits";

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
            guest.PreferredFloorPreferences
        );
    }

    private void ShowRoomContent(string title, string subtitle, IReadOnlyList<RoomTrait> traits)
    {
        ShowRoot(title, subtitle);
        ClearRows();
        ClearFloorPreferenceText();

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
        IReadOnlyList<FloorPreference> floorPreferences)
    {
        ShowRoot(title, subtitle);
        ClearRows();
        SetFloorPreferenceText(floorPreferences);

        if (traits == null || traits.Count == 0)
            return;

        for (int i = 0; i < traits.Count; i++)
        {
            AddTraitRow(traits[i]);
        }
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

    private void SetFloorPreferenceText(IReadOnlyList<FloorPreference> floorPreferences)
    {
        if (floorPreferenceText == null)
            return;

        if (floorPreferences == null || floorPreferences.Count == 0)
        {
            floorPreferenceText.text = "";
            floorPreferenceText.gameObject.SetActive(false);
            return;
        }

        List<string> labels = new List<string>();

        for (int i = 0; i < floorPreferences.Count; i++)
        {
            labels.Add(FloorPreferenceUtility.GetDisplayName(floorPreferences[i]));
        }

        floorPreferenceText.text = string.Join(", ", labels);
        floorPreferenceText.gameObject.SetActive(true);
    }

    private void ClearFloorPreferenceText()
    {
        if (floorPreferenceText == null)
            return;

        floorPreferenceText.text = "";
        floorPreferenceText.gameObject.SetActive(false);
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);

        ClearRows();
        ClearFloorPreferenceText();
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