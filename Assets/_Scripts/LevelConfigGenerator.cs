using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class LevelConfigGenerator
{
    public static LevelConfig CreateLevelAsset(LevelGeneratorSettings settings, int maxAttempts = 100)
    {
        if (settings == null)
            return null;

        if (settings.floorCount <= 0 || settings.roomsPerFloor <= 0)
            return null;

        if (settings.allowedTraits == null || settings.allowedTraits.Count == 0)
            return null;

        if (settings.possibleGuestNames == null || settings.possibleGuestNames.Count == 0)
            return null;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            List<GeneratedRoomData> generatedRooms = LevelGenerator.GenerateRooms(settings);
            List<GeneratedGuestData> generatedGuests = LevelGenerator.GenerateGuests(settings, generatedRooms);

            if (!LevelValidator.CanSolve(generatedRooms, generatedGuests))
                continue;

            LevelConfig newLevel = ScriptableObject.CreateInstance<LevelConfig>();

            for (int i = 0; i < generatedRooms.Count; i++)
            {
                GeneratedRoomData room = generatedRooms[i];
                LevelRoomEntry roomEntry = new LevelRoomEntry
                {
                    roomNumber = room.roomNumber,
                    traits = new List<RoomTrait>(room.traits)
                };
                newLevel.rooms.Add(roomEntry);
            }

            for (int i = 0; i < generatedGuests.Count; i++)
            {
                GeneratedGuestData guest = generatedGuests[i];
                LevelGuestEntry guestEntry = new LevelGuestEntry
                {
                    guestName = guest.guestName,
                    preferredTraits = new List<RoomTrait>(guest.preferredTraits)
                };
                newLevel.guests.Add(guestEntry);
            }

            string folder = string.IsNullOrWhiteSpace(settings.outputFolder)
                ? "Assets"
                : settings.outputFolder;

            if (!AssetDatabase.IsValidFolder(folder))
            {
                Debug.LogError($"Output folder does not exist: {folder}");
                Object.DestroyImmediate(newLevel);
                return null;
            }

            string baseName = string.IsNullOrWhiteSpace(settings.levelNamePrefix)
                ? "GeneratedLevel"
                : settings.levelNamePrefix;

            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{baseName}.asset");
            AssetDatabase.CreateAsset(newLevel, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = newLevel;

            return newLevel;
        }

        return null;
    }
}