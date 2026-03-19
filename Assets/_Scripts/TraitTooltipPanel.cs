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

        Show(title, subtitle, room.Traits);
    }

    public void ShowGuest(GuestCard guest)
    {
        if (guest == null)
            return;

        Show(guest.DisplayName, "Guest Preferences", guest.PreferredTraits);
    }

    public void Show(string title, string subtitle, IReadOnlyList<RoomTrait> traits)
    {
        if (root != null)
            root.SetActive(true);

        if (titleText != null)
            titleText.text = title;

        if (subtitleText != null)
            subtitleText.text = subtitle;

        ClearRows();

        if (traits == null || traits.Count == 0)
            return;

        for (int i = 0; i < traits.Count; i++)
        {
            RoomTrait trait = traits[i];
            TooltipTraitRowUI row = Instantiate(rowPrefab, rowContainer);

            Sprite icon = traitIconDatabase != null ? traitIconDatabase.GetIcon(trait) : null;
            string label = RoomTraitUtility.GetDisplayName(trait);

            row.SetData(icon, label);
        }
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