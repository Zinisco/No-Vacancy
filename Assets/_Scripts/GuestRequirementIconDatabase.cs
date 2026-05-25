using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GuestRequirementIconDatabase", menuName = "No Vacancy/Guest Requirement Icon Database")]
public class GuestRequirementIconDatabase : ScriptableObject
{
    [SerializeField] private List<GuestRequirementIconEntry> entries = new();

    private Dictionary<GuestRequirement, Sprite> iconLookup;

    private void OnEnable() => BuildLookup();
    private void OnValidate() => BuildLookup();

    private void BuildLookup()
    {
        iconLookup = new Dictionary<GuestRequirement, Sprite>();

        for (int i = 0; i < entries.Count; i++)
            iconLookup[entries[i].requirement] = entries[i].icon;
    }

    public Sprite GetIcon(GuestRequirement requirement)
    {
        if (iconLookup == null)
            BuildLookup();

        return iconLookup.TryGetValue(requirement, out Sprite sprite) ? sprite : null;
    }
}

[Serializable]
public class GuestRequirementIconEntry
{
    public GuestRequirement requirement;
    public Sprite icon;
}