using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelGeneratorSettings))]
public class LevelGeneratorSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);

        LevelGeneratorSettings settings = (LevelGeneratorSettings)target;

        if (GUILayout.Button("Generate New LevelConfig"))
        {
            LevelConfig created = LevelConfigGenerator.CreateLevelAsset(settings);

            if (created != null)
            {
                Debug.Log($"Created new LevelConfig: {created.name}");
            }
            else
            {
                Debug.LogWarning("Failed to generate a valid LevelConfig.");
            }
        }
    }
}