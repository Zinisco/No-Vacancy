using System.Collections.Generic;
using UnityEngine;

public class HandManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform handContainer;

    [Header("Settings")]
    [SerializeField] private int baseMaxHandSize = 8;

    [Header("Fan Layout")]
    [SerializeField] private float cardSpacing = 140f;
    [SerializeField] private float maxFanRotation = 10f;
    [SerializeField] private float curveHeight = 22f;

    private readonly List<GuestCard> cardsInHand = new();
    private int currentMaxHandSize;

    public int BaseMaxHandSize => baseMaxHandSize;
    public int CurrentMaxHandSize => currentMaxHandSize;
    public int CurrentHandCount => cardsInHand.Count;
    public bool HasSpace => cardsInHand.Count < currentMaxHandSize;

    private void Awake()
    {
        currentMaxHandSize = baseMaxHandSize;

        if (handContainer != null)
        {
            handContainer.anchorMin = new Vector2(0.5f, 0.5f);
            handContainer.anchorMax = new Vector2(0.5f, 0.5f);
            handContainer.pivot = new Vector2(0.5f, 0.5f);

            Vector2 pos = handContainer.anchoredPosition;
            handContainer.anchoredPosition = new Vector2(0f, pos.y);
        }
    }

    public void SetMaxHandSize(int newMax)
    {
        currentMaxHandSize = Mathf.Max(0, newMax);
    }

    public void AddToHand(GuestCard card, bool reparent = true)
    {
        if (card == null) return;
        if (cardsInHand.Contains(card)) return;

        cardsInHand.Add(card);

        if (reparent)
        {
            Transform parent = handContainer != null ? handContainer : transform;
            card.transform.SetParent(parent, false);
            card.transform.localScale = Vector3.one;
            card.transform.localPosition = Vector3.zero;
            card.transform.localRotation = Quaternion.identity;
        }

        card.SetInHand();
        RefreshHandLayout();
    }

    public void RemoveFromHand(GuestCard card)
    {
        if (card == null) return;

        if (cardsInHand.Remove(card))
        {
            RefreshHandLayout();
        }
    }

    public bool Contains(GuestCard card)
    {
        return cardsInHand.Contains(card);
    }

    public IReadOnlyList<GuestCard> GetCards()
    {
        return cardsInHand;
    }

    public Transform GetHandContainer()
    {
        return handContainer != null ? handContainer : transform;
    }

    public void RefreshHandLayout(bool instant = false)
    {
        int count = cardsInHand.Count;
        if (count == 0) return;

        float totalWidth = (count - 1) * cardSpacing;
        float startX = -totalWidth * 0.5f;

        for (int i = 0; i < count; i++)
        {
            GuestCard card = cardsInHand[i];
            if (card == null) continue;

            float normalized = count == 1 ? 0.5f : i / (float)(count - 1);
            float x = startX + i * cardSpacing;

            float centerOffset = normalized - 0.5f;
            float y = (1f - Mathf.Abs(centerOffset) * 2f) * curveHeight;

            float zRot = Mathf.Lerp(maxFanRotation, -maxFanRotation, normalized);

            card.SetHandPose(new Vector2(x, y), zRot, instant);
        }
    }
}