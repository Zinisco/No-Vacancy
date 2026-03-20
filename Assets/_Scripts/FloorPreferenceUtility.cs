public static class FloorPreferenceUtility
{
    public static string GetDisplayName(FloorPreference preference)
    {
        switch (preference)
        {
            case FloorPreference.FirstFloor: return "First Floor";
            case FloorPreference.SecondFloor: return "Second Floor";
            case FloorPreference.ThirdFloor: return "Third Floor";
            default: return preference.ToString();
        }
    }
}