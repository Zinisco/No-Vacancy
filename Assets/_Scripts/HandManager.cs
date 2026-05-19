using System.Collections.Generic;
using UnityEngine;

// HandManager controls:
// - which cards are currently in the player's hand
// - the maximum hand size
// - the curved fan layout of cards
// - adding/removing cards from the hand
public class HandManager : MonoBehaviour
{
    [Header("Refs")]

    // The UI container that holds all guest cards in the hand.
    // Usually this is a RectTransform in your Canvas.
    [SerializeField] private RectTransform handContainer;

    [Header("Settings")]

    // The default maximum number of cards the player can hold.
    [SerializeField] private int baseMaxHandSize = 8;

    [Header("Fan Layout")]

    // How far apart cards are horizontally.
    [SerializeField] private float cardSpacing = 140f;

    // The maximum tilt angle of cards at the edges of the hand.
    // Cards on the left tilt left.
    // Cards on the right tilt right.
    [SerializeField] private float maxFanRotation = 10f;

    // How much the cards curve upward in the middle.
    // Higher value = more curved hand.
    [SerializeField] private float curveHeight = 22f;

    // The actual list of cards currently in the player's hand.
    // "readonly" means the list reference itself cannot be replaced,
    // but we can still add/remove cards from the list.
    private readonly List<GuestCard> cardsInHand = new();

    // The current hand size limit.
    // This may change during gameplay.
    private int currentMaxHandSize;

    // Public read-only properties.
    // Other scripts can check these values safely.

    // Original/default hand size.
    public int BaseMaxHandSize => baseMaxHandSize;

    // Current hand size limit.
    public int CurrentMaxHandSize => currentMaxHandSize;

    // Number of cards currently in hand.
    public int CurrentHandCount => cardsInHand.Count;

    // Returns true if there is room for another card.
    public bool HasSpace => cardsInHand.Count < currentMaxHandSize;

    private void Awake()
    {
        // Start with the default hand size.
        currentMaxHandSize = baseMaxHandSize;

        // If we have a hand container assigned...
        if (handContainer != null)
        {
            // Set the anchors and pivot to the center.
            // This makes the hand easier to position and fan correctly.
            handContainer.anchorMin = new Vector2(0.5f, 0.5f);
            handContainer.anchorMax = new Vector2(0.5f, 0.5f);
            handContainer.pivot = new Vector2(0.5f, 0.5f);

            // Keep the current Y position,
            // but force the X position to be centered.
            Vector2 pos = handContainer.anchoredPosition;
            handContainer.anchoredPosition = new Vector2(0f, pos.y);
        }
    }

    // Changes the maximum hand size.
    // Mathf.Max prevents the value from going below 0.
    public void SetMaxHandSize(int newMax)
    {
        currentMaxHandSize = Mathf.Max(0, newMax);
    }

    // Adds a card to the player's hand.
    public void AddToHand(GuestCard card, bool reparent = true)
    {
        // Stop if the card is null.
        if (card == null) return;

        // Stop if the card is already in the hand.
        if (cardsInHand.Contains(card)) return;

        // Add the card to the internal list.
        cardsInHand.Add(card);

        // If reparent is true,
        // move the card into the hand container hierarchy.
        if (reparent)
        {
            // Use the handContainer if assigned,
            // otherwise use this object's transform.
            Transform parent = handContainer != null ? handContainer : transform;

            // Parent the card to the hand container.
            // false = preserve local UI coordinates.
            card.transform.SetParent(parent, false);

            // Reset transform values so the card starts clean.
            card.transform.localScale = Vector3.one;
            card.transform.localPosition = Vector3.zero;
            card.transform.localRotation = Quaternion.identity;
        }

        // Tell the card that it is now in the hand.
        card.SetInHand();

        // Recalculate the hand layout.
        RefreshHandLayout();
    }

    // Removes a card from the hand.
    public void RemoveFromHand(GuestCard card)
    {
        if (card == null) return;

        // Remove the card from the list.
        // If removal succeeded...
        if (cardsInHand.Remove(card))
        {
            // Recalculate the hand layout.
            RefreshHandLayout();
        }
    }

    // Returns true if this card exists in the hand.
    public bool Contains(GuestCard card)
    {
        return cardsInHand.Contains(card);
    }

    // Gives other scripts read-only access to the hand cards.
    public IReadOnlyList<GuestCard> GetCards()
    {
        return cardsInHand;
    }

    // Returns the hand container transform.
    // If no handContainer exists, fallback to this object's transform.
    public Transform GetHandContainer()
    {
        return handContainer != null ? handContainer : transform;
    }

    // Rebuilds the curved hand layout.
    public void RefreshHandLayout(bool instant = false)
    {
        // Number of cards currently in hand.
        int count = cardsInHand.Count;

        // If there are no cards, stop.
        if (count == 0) return;

        // Calculate total width of the hand.
        // Example:
        // 5 cards with 140 spacing = 560 width.
        float totalWidth = (count - 1) * cardSpacing;

        // Calculate where the first card starts.
        // This centers the entire hand.
        float startX = -totalWidth * 0.5f;

        // Loop through every card in the hand.
        for (int i = 0; i < count; i++)
        {
            GuestCard card = cardsInHand[i];

            if (card == null) continue;

            // Convert card index into a 0-1 range.
            // Example:
            // left card = 0
            // middle = 0.5
            // right = 1
            float normalized = count == 1 ? 0.5f : i / (float)(count - 1);

            // Calculate horizontal position.
            float x = startX + i * cardSpacing;

            // Convert normalized into a center offset.
            // Middle card becomes 0.
            float centerOffset = normalized - 0.5f;

            // Create curved vertical movement.
            // Cards near the center move upward more.
            float y = (1f - Mathf.Abs(centerOffset) * 2f) * curveHeight;

            // Rotate cards outward in a fan shape.
            // Left side rotates positive.
            // Right side rotates negative.
            float zRot = Mathf.Lerp(maxFanRotation, -maxFanRotation, normalized);

            // Tell the card where it should move/rotate to.
            card.SetHandPose(new Vector2(x, y), zRot, instant);
        }
    }
}