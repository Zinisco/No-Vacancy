using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ChapterConfig", menuName = "No Vacancy/Chapter Config")]
public class ChapterConfig : ScriptableObject
{
    public string chapterName;
    public List<LevelConfig> levels = new();
}