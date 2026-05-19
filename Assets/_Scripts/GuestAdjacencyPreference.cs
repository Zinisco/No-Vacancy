using System;
using UnityEngine;

public enum GuestAdjacencyPreferenceType
{
    HatesSmokingNeighbor,
    HatesLoudNeighbor,
    WantsNamedGuestNeighbor
}

[Serializable]
public class GuestAdjacencyPreference
{
    public GuestAdjacencyPreferenceType type;

    [Tooltip("Only used for WantsNamedGuestNeighbor")]
    public string targetGuestName;
}