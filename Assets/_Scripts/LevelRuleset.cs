using System;
using System.Collections.Generic;

[Serializable]
public class LevelRuleset
{
    public bool allowFloorPreferences;
    public bool allowAdjacencyRules;
    public bool allowBehaviorTraits;
    public bool allowRequirements;

    public List<RoomTrait> allowedTraits;
    public List<GuestBehaviorTrait> allowedBehaviors;
    public List<GuestRequirement> allowedRequirements;
}