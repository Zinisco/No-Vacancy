using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "GuestEmoteDatabase",
    menuName = "No Vacancy/Guest Emote Database"
)]
public class GuestEmoteDatabase : ScriptableObject
{
    [Header("Happy Emotes")]
    [Tooltip("Examples: heart and smiling face.")]
    [SerializeField] private List<Sprite> happyEmotes = new();

    [Header("Unhappy Emotes")]
    [Tooltip("Examples: sad face, angry face, and broken heart.")]
    [SerializeField] private List<Sprite> unhappyEmotes = new();

    public Sprite GetRandomHappyEmote()
    {
        return GetRandomSprite(happyEmotes);
    }

    public Sprite GetRandomUnhappyEmote()
    {
        return GetRandomSprite(unhappyEmotes);
    }

    private Sprite GetRandomSprite(List<Sprite> sprites)
    {
        if (sprites == null || sprites.Count == 0)
            return null;

        int validSpriteCount = 0;

        for (int i = 0; i < sprites.Count; i++)
        {
            if (sprites[i] != null)
                validSpriteCount++;
        }

        if (validSpriteCount == 0)
            return null;

        int selectedIndex = Random.Range(0, validSpriteCount);
        int currentValidIndex = 0;

        for (int i = 0; i < sprites.Count; i++)
        {
            if (sprites[i] == null)
                continue;

            if (currentValidIndex == selectedIndex)
                return sprites[i];

            currentValidIndex++;
        }

        return null;
    }
}