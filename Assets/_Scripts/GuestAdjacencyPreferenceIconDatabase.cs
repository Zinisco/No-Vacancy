using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GuestAdjacencyPreferenceIconDatabase", menuName = "No Vacancy/Guest Adjacency Preference Icon Database")]
public class GuestAdjacencyPreferenceIconDatabase : ScriptableObject
{
    [SerializeField] private List<GuestAdjacencyPreferenceIconEntry> entries = new();

    private Dictionary<GuestAdjacencyPreferenceType, Sprite> iconLookup;

    private void OnEnable() => BuildLookup();
    private void OnValidate() => BuildLookup();

    private void BuildLookup()
    {
        iconLookup = new Dictionary<GuestAdjacencyPreferenceType, Sprite>();

        for (int i = 0; i < entries.Count; i++)
            iconLookup[entries[i].type] = entries[i].icon;
    }

    public Sprite GetIcon(GuestAdjacencyPreferenceType type)
    {
        if (iconLookup == null)
            BuildLookup();

        return iconLookup.TryGetValue(type, out Sprite sprite) ? sprite : null;
    }
}

[Serializable]
public class GuestAdjacencyPreferenceIconEntry
{
    public GuestAdjacencyPreferenceType type;
    public Sprite icon;
}