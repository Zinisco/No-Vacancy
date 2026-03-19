public static class FloorPreferenceUtility
{
    public static string GetDisplayName(FloorPreference preference)
    {
        switch (preference)
        {
            case FloorPreference.FirstFloor: return "Bottom Floor";
            case FloorPreference.SecondFloor: return "Middle Floor";
            case FloorPreference.TopFloor: return "Top Floor";
            default: return preference.ToString();
        }
    }
}