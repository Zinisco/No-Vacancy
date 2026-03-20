using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FloorPreferenceIconDatabase", menuName = "No Vacancy/Floor Preference Icon Database")]
public class FloorPreferenceIconDatabase : ScriptableObject
{
    [SerializeField] private List<FloorPreferenceIconEntry> entries = new List<FloorPreferenceIconEntry>();

    private Dictionary<FloorPreference, Sprite> iconLookup;

    private void OnEnable()
    {
        BuildLookup();
    }

    private void OnValidate()
    {
        BuildLookup();
    }

    private void BuildLookup()
    {
        iconLookup = new Dictionary<FloorPreference, Sprite>();

        for (int i = 0; i < entries.Count; i++)
        {
            FloorPreferenceIconEntry entry = entries[i];

            if (iconLookup.ContainsKey(entry.preference))
                iconLookup[entry.preference] = entry.icon;
            else
                iconLookup.Add(entry.preference, entry.icon);
        }
    }

    public Sprite GetIcon(FloorPreference preference)
    {
        if (iconLookup == null)
            BuildLookup();

        if (iconLookup.TryGetValue(preference, out Sprite sprite))
        {
            if (sprite == null)
                Debug.LogWarning($"FloorPreferenceIconDatabase: Preference {preference} exists but has no sprite assigned.");

            return sprite;
        }

        Debug.LogWarning($"FloorPreferenceIconDatabase: No icon entry found for preference {preference}.");
        return null;
    }
}

[Serializable]
public class FloorPreferenceIconEntry
{
    public FloorPreference preference;
    public Sprite icon;
}