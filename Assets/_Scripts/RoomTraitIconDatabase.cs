using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RoomTraitIconDatabase", menuName = "No Vacancy/Room Trait Icon Database")]
public class RoomTraitIconDatabase : ScriptableObject
{
    [SerializeField] private List<RoomTraitIconEntry> entries = new List<RoomTraitIconEntry>();

    private Dictionary<RoomTrait, Sprite> iconLookup;

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
        iconLookup = new Dictionary<RoomTrait, Sprite>();

        for (int i = 0; i < entries.Count; i++)
        {
            RoomTraitIconEntry entry = entries[i];

            if (iconLookup.ContainsKey(entry.trait))
            {
                iconLookup[entry.trait] = entry.icon;
            }
            else
            {
                iconLookup.Add(entry.trait, entry.icon);
            }
        }
    }

    public Sprite GetIcon(RoomTrait trait)
    {
        if (iconLookup == null)
            BuildLookup();

        if (iconLookup.TryGetValue(trait, out Sprite sprite))
        {
            if (sprite == null)
                Debug.LogWarning($"RoomTraitIconDatabase: Trait {trait} exists but has no sprite assigned.");

            return sprite;
        }

        Debug.LogWarning($"RoomTraitIconDatabase: No icon entry found for trait {trait}.");
        return null;
    }
}

[Serializable]
public class RoomTraitIconEntry
{
    public RoomTrait trait;
    public Sprite icon;
}