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

    [Header("Drag Reordering")]
    [SerializeField] private float previewGapExtra = 50f;

    [Header("Settings")]

    // The default maximum number of cards the player can hold.
    [SerializeField] private int baseMaxHandSize = 10;

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

    public int PreviewDropIndex => previewDropIndex;
    private GuestCard previewDraggedCard;
    private int previewDropIndex = -1;

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
        AddToHandAtIndex(card, cardsInHand.Count, reparent);
    }

    public void AddToHandAtIndex(
    GuestCard card,
    int index,
    bool reparent = true)
    {
        if (card == null)
            return;

        // If this is the card currently creating the preview gap,
        // clear the preview state before rebuilding the final hand.
        if (previewDraggedCard == card)
        {
            previewDraggedCard = null;
            previewDropIndex = -1;
        }

        cardsInHand.Remove(card);

        index = Mathf.Clamp(
            index,
            0,
            cardsInHand.Count
        );

        cardsInHand.Insert(index, card);

        if (reparent)
        {
            Transform parent = handContainer != null
                ? handContainer
                : transform;

            card.transform.SetParent(parent, false);
            card.transform.localScale = Vector3.one;
            card.transform.localPosition = Vector3.zero;
            card.transform.localRotation = Quaternion.identity;
        }

        card.SetInHand();

        UpdateSiblingOrder();

        // This now builds the normal layout because the preview
        // state was cleared above.
        RefreshHandLayout();
    }

    public void MoveCardToIndex(
    GuestCard card,
    int index)
    {
        if (card == null)
            return;

        int oldIndex = cardsInHand.IndexOf(card);

        if (oldIndex < 0)
            return;

        cardsInHand.RemoveAt(oldIndex);

        index = Mathf.Clamp(
            index,
            0,
            cardsInHand.Count
        );

        cardsInHand.Insert(index, card);

        previewDraggedCard = null;
        previewDropIndex = -1;

        UpdateSiblingOrder();
        RefreshHandLayout();
    }

    public int GetDropIndex(
    Vector2 screenPosition,
    Camera eventCamera,
    GuestCard draggedCard)
    {
        if (handContainer == null)
            return cardsInHand.Count;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                handContainer,
                screenPosition,
                eventCamera,
                out Vector2 localPoint))
        {
            return cardsInHand.Count;
        }

        bool cardAlreadyInHand =
            draggedCard != null &&
            cardsInHand.Contains(draggedCard);

        int remainingCardCount = cardsInHand.Count;

        if (cardAlreadyInHand)
            remainingCardCount--;

        int finalCardCount = remainingCardCount + 1;

        if (finalCardCount <= 1)
            return 0;

        float totalWidth =
            (finalCardCount - 1) * cardSpacing;

        float firstSlotX =
            -totalWidth * 0.5f;

        // An insertion boundary sits halfway between slots.
        float firstBoundaryX =
            firstSlotX - cardSpacing * 0.5f;

        float positionFromFirstBoundary =
            localPoint.x - firstBoundaryX;

        int insertionIndex = Mathf.FloorToInt(
            positionFromFirstBoundary / cardSpacing
        );

        return Mathf.Clamp(
            insertionIndex,
            0,
            remainingCardCount
        );
    }

    public int PreviewDrop(
    Vector2 screenPosition,
    Camera eventCamera,
    GuestCard draggedCard)
    {
        int newIndex = GetDropIndex(
            screenPosition,
            eventCamera,
            draggedCard
        );

        bool previewChanged =
            previewDraggedCard != draggedCard ||
            previewDropIndex != newIndex;

        previewDraggedCard = draggedCard;
        previewDropIndex = newIndex;

        if (previewChanged)
            RefreshHandLayout();

        return previewDropIndex;
    }

    public void ClearDropPreview(bool refreshLayout = true)
    {
        if (previewDraggedCard == null &&
            previewDropIndex < 0)
        {
            return;
        }

        previewDraggedCard = null;
        previewDropIndex = -1;

        if (refreshLayout)
            RefreshHandLayout();
    }

    private void UpdateSiblingOrder()
    {
        for (int i = 0; i < cardsInHand.Count; i++)
        {
            GuestCard card = cardsInHand[i];

            if (card != null &&
                card.transform.parent == handContainer)
            {
                card.transform.SetSiblingIndex(i);
            }
        }
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
        bool showingPreview =
            previewDraggedCard != null &&
            previewDropIndex >= 0;

        if (!showingPreview)
        {
            RefreshNormalHandLayout(instant);
            return;
        }

        RefreshPreviewHandLayout(instant);
    }

    private void RefreshNormalHandLayout(bool instant)
    {
        int count = cardsInHand.Count;

        if (count == 0)
            return;

        float totalWidth = (count - 1) * cardSpacing;
        float startX = -totalWidth * 0.5f;

        for (int i = 0; i < count; i++)
        {
            GuestCard card = cardsInHand[i];

            if (card == null)
                continue;

            float normalized =
                count == 1
                    ? 0.5f
                    : i / (float)(count - 1);

            float x = startX + i * cardSpacing;

            float centerOffset = normalized - 0.5f;

            float y =
                (1f - Mathf.Abs(centerOffset) * 2f) *
                curveHeight;

            float zRotation = Mathf.Lerp(
                maxFanRotation,
                -maxFanRotation,
                normalized
            );

            card.SetHandPose(
                new Vector2(x, y),
                zRotation,
                instant
            );
        }
    }

    private void RefreshPreviewHandLayout(bool instant)
    {
        List<GuestCard> remainingCards = new();

        for (int i = 0; i < cardsInHand.Count; i++)
        {
            GuestCard card = cardsInHand[i];

            if (card == null || card == previewDraggedCard)
                continue;

            remainingCards.Add(card);
        }

        bool draggedCardAlreadyInHand =
            cardsInHand.Contains(previewDraggedCard);

        if (!draggedCardAlreadyInHand && !HasSpace)
        {
            RefreshNormalHandLayout(instant);
            return;
        }

        int finalCardCount =
            remainingCards.Count + 1;

        if (finalCardCount <= 0)
            return;

        int dropIndex = Mathf.Clamp(
            previewDropIndex,
            0,
            remainingCards.Count
        );

        float normalTotalWidth =
            (finalCardCount - 1) * cardSpacing;

        float totalWidth =
            normalTotalWidth + previewGapExtra;

        float startX =
            -totalWidth * 0.5f;

        for (int i = 0; i < remainingCards.Count; i++)
        {
            GuestCard card = remainingCards[i];

            if (card == null)
                continue;

            int visualIndex =
                i < dropIndex
                    ? i
                    : i + 1;

            float x =
                startX + visualIndex * cardSpacing;

            // Push everything to the right of the opening farther right.
            if (visualIndex > dropIndex)
                x += previewGapExtra;

            // Split the extra space around the gap.
            if (visualIndex < dropIndex)
                x -= previewGapExtra * 0.5f;
            else if (visualIndex > dropIndex)
                x += previewGapExtra * 0.5f;

            float normalized =
                finalCardCount == 1
                    ? 0.5f
                    : visualIndex /
                      (float)(finalCardCount - 1);

            float centerOffset =
                normalized - 0.5f;

            float y =
                (1f - Mathf.Abs(centerOffset) * 2f) *
                curveHeight;

            float zRotation = Mathf.Lerp(
                maxFanRotation,
                -maxFanRotation,
                normalized
            );

            card.SetHandPose(
                new Vector2(x, y),
                zRotation,
                instant
            );
        }
    }
}