using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    [Header("Deck Data")]
    [SerializeField]
    private List<string> placeholderNames = new List<string>()
    {
        "Alex",
        "Sam",
        "Jordan",
        "Taylor",
        "Casey",
        "Morgan",
        "Jamie",
        "Drew",
        "Avery",
        "Quinn",
        "Riley",
        "Skyler"
    };

    private Queue<string> drawQueue = new Queue<string>();
    private int nextCardId = 0;

    public void BuildDeck(int maxCards)
    {
        drawQueue.Clear();
        nextCardId = 0;

        if (maxCards <= 0)
            return;

        int cardsToCreate = Mathf.Min(maxCards, placeholderNames.Count);

        for (int i = 0; i < cardsToCreate; i++)
        {
            drawQueue.Enqueue(placeholderNames[i]);
        }
    }

    public bool HasCardsRemaining()
    {
        return drawQueue.Count > 0;
    }

    public string DrawName()
    {
        if (!HasCardsRemaining())
            return null;

        return drawQueue.Dequeue();
    }

    public string GenerateCardId()
    {
        nextCardId++;
        return $"CARD_{nextCardId:D3}";
    }

    public int RemainingCount()
    {
        return drawQueue.Count;
    }
}