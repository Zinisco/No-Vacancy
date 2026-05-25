using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GuestBehaviorIconDatabase", menuName = "No Vacancy/Guest Behavior Icon Database")]
public class GuestBehaviorIconDatabase : ScriptableObject
{
    [SerializeField] private List<GuestBehaviorIconEntry> entries = new();

    private Dictionary<GuestBehaviorTrait, Sprite> iconLookup;

    private void OnEnable() => BuildLookup();
    private void OnValidate() => BuildLookup();

    private void BuildLookup()
    {
        iconLookup = new Dictionary<GuestBehaviorTrait, Sprite>();

        for (int i = 0; i < entries.Count; i++)
            iconLookup[entries[i].trait] = entries[i].icon;
    }

    public Sprite GetIcon(GuestBehaviorTrait trait)
    {
        if (iconLookup == null)
            BuildLookup();

        return iconLookup.TryGetValue(trait, out Sprite sprite) ? sprite : null;
    }
}

[Serializable]
public class GuestBehaviorIconEntry
{
    public GuestBehaviorTrait trait;
    public Sprite icon;
}