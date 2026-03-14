using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GuestCard guestCardPrefab;

    [Header("Board")]
    [SerializeField] private RoomSlot roomSlotPrefab;
    [SerializeField] private Transform roomGridParent;
    [SerializeField] private List<RoomSlot> roomSlots = new();

    [Header("Managers")]
    [SerializeField] private HandManager handManager;

    [Header("Level Data")]
    [SerializeField] private LevelConfig levelConfig;

    [Header("UI")]
    [SerializeField] private TMP_Text debugText;
    [SerializeField] private TMP_Text deckCountText;
    [SerializeField] private Button drawButton;

    [Header("Draw Settings")]
    [SerializeField] private int startingDrawAmount = 5;
    [SerializeField] private int drawAmountPerRefill = 3;

    [Header("Animation")]
    [SerializeField] private RectTransform animationLayer;
    [SerializeField] private RectTransform drawSpawnPoint;
    [SerializeField] private float cardMoveDuration = 0.18f;

    [Header("Draw Animation")]
    [SerializeField] private float drawStartRotationZ = -12f;
    [SerializeField] private float drawOvershootDistance = 24f;
    [SerializeField] private float drawSettleDuration = 0.08f;
    [SerializeField] private float drawStaggerDelay = 0.03f;

    private int nextCardId = 0;
    private Queue<LevelGuestEntry> guestQueue = new();
    private GuestCard selectedCard;
    private bool isBusy;

    private void Start()
    {
        InitializeRooms();
        StartGame();
    }

    private void InitializeRooms()
    {
        roomSlots.Clear();

        if (roomSlotPrefab == null || roomGridParent == null)
        {
            Log("Missing RoomSlot prefab or RoomGrid parent.");
            return;
        }

        if (levelConfig == null)
        {
            Log("Missing LevelConfig.");
            return;
        }

        for (int i = roomGridParent.childCount - 1; i >= 0; i--)
        {
            Destroy(roomGridParent.GetChild(i).gameObject);
        }

        for (int i = 0; i < levelConfig.rooms.Count; i++)
        {
            SpawnRoomSlot(levelConfig.rooms[i]);
        }
    }

    private void StartGame()
    {
        if (handManager == null)
        {
            Log("Missing HandManager.");
            return;
        }

        if (levelConfig == null)
        {
            Log("Missing LevelConfig.");
            return;
        }

        if (levelConfig.rooms.Count == 0)
        {
            Log("LevelConfig has no rooms.");
            return;
        }

        if (levelConfig.guests.Count == 0)
        {
            Log("LevelConfig has no guests.");
            return;
        }

        guestQueue.Clear();
        for (int i = 0; i < levelConfig.guests.Count; i++)
        {
            guestQueue.Enqueue(levelConfig.guests[i]);
        }

        nextCardId = 0;

        int roomCount = roomSlots.Count;
        int clampedHandSize = Mathf.Min(handManager.BaseMaxHandSize, roomCount);

        handManager.SetMaxHandSize(clampedHandSize);

        DrawCards(startingDrawAmount);

        Log($"Game started. Rooms: {roomCount}, Hand Size: {clampedHandSize}");
    }

    public void DrawCards(int amount)
    {
        if (isBusy)
            return;

        StartCoroutine(DrawCardsRoutine(amount));
    }

    public void SelectCard(GuestCard card)
    {
        if (card == null)
            return;

        if (selectedCard == card)
        {
            DeselectCurrentCard();
            Log($"Deselected {card.DisplayName}");
            return;
        }

        if (selectedCard != null)
            selectedCard.SetSelected(false);

        selectedCard = card;
        selectedCard.SetSelected(true);

        string location = card.CurrentLocationType == CardLocationType.Room && card.CurrentRoom != null
            ? card.CurrentRoom.GetHolderName()
            : "Hand";

        Log($"Selected {card.DisplayName} from {location}");
    }

    public void OnGuestCardRightClicked(GuestCard card)
    {
        if (card == null || isBusy)
            return;

        // Only room cards can be sent back to hand with right click.
        if (card.CurrentLocationType == CardLocationType.Room && card.CurrentRoom != null)
        {
            StartCoroutine(ReturnRoomCardToHand(card.CurrentRoom));
        }
    }

    public void OnDrawButtonPressed()
    {
        if (isBusy)
            return;

        if (guestQueue.Count == 0)
        {
            Log("Deck is empty.");
            return;
        }

        if (!handManager.HasSpace)
        {
            Log("Hand is full.");
            return;
        }

        DrawCards(drawAmountPerRefill);
        Log($"Drew up to {drawAmountPerRefill} card(s).");
    }

    public void OnGuestCardLeftClicked(GuestCard card)
    {
        if (card == null || isBusy)
            return;

        // Left click always selects, regardless of hand or room.
        if (selectedCard == null)
        {
            SelectCard(card);
            return;
        }

        // Clicking the same selected card deselects it.
        if (selectedCard == card)
        {
            DeselectCurrentCard();
            return;
        }

        // If a card is already selected, try to resolve interaction.
        StartCoroutine(HandleCardToCardInteraction(selectedCard, card));
    }

    public void OnRoomLeftClicked(RoomSlot room)
    {
        if (room == null || isBusy)
            return;

        // Empty room click
        if (!room.HasCard())
        {
            if (selectedCard == null)
                return;

            StartCoroutine(HandleEmptyRoomInteraction(room));
            return;
        }

        // Occupied room click behaves like clicking the card in that room.
        OnGuestCardLeftClicked(room.CurrentCard);
    }

    public void OnRoomRightClicked(RoomSlot room)
    {
        if (room == null || isBusy)
            return;

        if (room.HasCard())
        {
            StartCoroutine(ReturnRoomCardToHand(room));
        }
    }

    private void RefreshDrawButtonState()
    {
        if (drawButton == null || handManager == null)
            return;

        bool canDraw = !isBusy && guestQueue.Count > 0 && handManager.HasSpace;
        drawButton.interactable = canDraw;
    }

    private IEnumerator HandleCardToCardInteraction(GuestCard firstCard, GuestCard secondCard)
    {
        if (firstCard == null || secondCard == null || isBusy)
            yield break;

        isBusy = true;
        RefreshDrawButtonState();

        // Hand <-> Hand: just switch selection to the second card
        if (firstCard.CurrentLocationType == CardLocationType.Hand &&
            secondCard.CurrentLocationType == CardLocationType.Hand)
        {
            SelectCard(secondCard);
            isBusy = false;
            RefreshDrawButtonState();
            yield break;
        }

        // Hand <-> Room
        if (firstCard.CurrentLocationType == CardLocationType.Hand &&
            secondCard.CurrentLocationType == CardLocationType.Room &&
            secondCard.CurrentRoom != null)
        {
            string handCardName = firstCard.DisplayName;
            string roomCardName = secondCard.DisplayName;

            yield return StartCoroutine(SwapHandCardWithRoomCard(firstCard, secondCard.CurrentRoom));
            Log($"Swapped {handCardName} with {roomCardName}");

            DeselectCurrentCard();
            UpdateDeckCountText();
            RefreshDrawButtonState();
            isBusy = false;
            yield break;
        }

        // Room <-> Hand
        if (firstCard.CurrentLocationType == CardLocationType.Room &&
            firstCard.CurrentRoom != null &&
            secondCard.CurrentLocationType == CardLocationType.Hand)
        {
            string roomCardName = firstCard.DisplayName;
            string handCardName = secondCard.DisplayName;

            yield return StartCoroutine(SwapHandCardWithRoomCard(secondCard, firstCard.CurrentRoom));
            Log($"Swapped {handCardName} with {roomCardName}");

            DeselectCurrentCard();
            UpdateDeckCountText();
            RefreshDrawButtonState();
            isBusy = false;
            yield break;
        }

        // Room <-> Room
        if (firstCard.CurrentLocationType == CardLocationType.Room &&
            secondCard.CurrentLocationType == CardLocationType.Room &&
            firstCard.CurrentRoom != null &&
            secondCard.CurrentRoom != null &&
            firstCard.CurrentRoom != secondCard.CurrentRoom)
        {
            string firstName = firstCard.DisplayName;
            string secondName = secondCard.DisplayName;

            yield return StartCoroutine(SwapRoomCards(firstCard.CurrentRoom, secondCard.CurrentRoom));
            Log($"Swapped {firstName} with {secondName}");

            DeselectCurrentCard();
            UpdateDeckCountText();
            RefreshDrawButtonState();
            isBusy = false;
            yield break;
        }

        // Fallback: just select the new card
        SelectCard(secondCard);
        isBusy = false;
        RefreshDrawButtonState();
    }

    private IEnumerator SwapRoomCards(RoomSlot roomA, RoomSlot roomB)
    {
        if (roomA == null || roomB == null || !roomA.HasCard() || !roomB.HasCard())
            yield break;

        GuestCard cardA = roomA.CurrentCard;
        GuestCard cardB = roomB.CurrentCard;

        roomA.ClearCard();
        roomB.ClearCard();

        cardA.SetSelected(false);
        cardB.SetSelected(false);

        yield return StartCoroutine(AnimateCardToTarget(cardA, roomB.GetCardAnchor()));
        yield return StartCoroutine(AnimateCardToTarget(cardB, roomA.GetCardAnchor()));

        roomA.SetCard(cardB);
        cardB.SetInRoom(roomA);

        roomB.SetCard(cardA);
        cardA.SetInRoom(roomB);
    }

    private IEnumerator HandleEmptyRoomInteraction(RoomSlot room)
    {
        if (room == null || selectedCard == null || isBusy)
            yield break;

        isBusy = true;
        RefreshDrawButtonState();

        if (selectedCard.CurrentLocationType == CardLocationType.Hand)
        {
            string cardName = selectedCard.DisplayName;
            yield return StartCoroutine(MoveHandCardToRoom(selectedCard, room));
            Log($"Placed {cardName} into {room.GetHolderName()}");
        }
        else if (selectedCard.CurrentLocationType == CardLocationType.Room && selectedCard.CurrentRoom != null)
        {
            string cardName = selectedCard.DisplayName;
            yield return StartCoroutine(MoveRoomCardToEmptyRoom(selectedCard.CurrentRoom, room));
            Log($"Moved {cardName} to {room.GetHolderName()}");
        }

        DeselectCurrentCard();
        UpdateDeckCountText();
        RefreshDrawButtonState();
        isBusy = false;
    }

    private IEnumerator MoveRoomCardToEmptyRoom(RoomSlot fromRoom, RoomSlot toRoom)
    {
        if (fromRoom == null || toRoom == null || !fromRoom.HasCard() || toRoom.HasCard())
            yield break;

        GuestCard card = fromRoom.CurrentCard;
        fromRoom.ClearCard();

        card.SetSelected(false);

        yield return StartCoroutine(AnimateCardToTarget(card, toRoom.GetCardAnchor()));

        toRoom.SetCard(card);
        card.SetInRoom(toRoom);
    }

    private IEnumerator MoveHandCardToRoom(GuestCard card, RoomSlot targetRoom)
    {
        if (card == null || targetRoom == null)
            yield break;

        handManager.RemoveFromHand(card);
        card.SetSelected(false);

        yield return StartCoroutine(AnimateCardToTarget(card, targetRoom.GetCardAnchor()));

        targetRoom.SetCard(card);
        card.SetInRoom(targetRoom);
    }

    private IEnumerator DrawCardsRoutine(int amount)
    {
        if (guestCardPrefab == null || handManager == null)
            yield break;

        isBusy = true;
        RefreshDrawButtonState();

        int drawsRemaining = amount;

        while (drawsRemaining > 0 && handManager.HasSpace && guestQueue.Count > 0)
        {
            LevelGuestEntry guestData = guestQueue.Dequeue();

            string guestName = guestData.guestName;
            List<RoomTrait> preferredTraitsToUse = guestData.preferredTraits;
            nextCardId++;
            string cardId = $"CARD_{nextCardId:D3}";

            RectTransform spawnParent = animationLayer != null
                ? animationLayer
                : handManager.GetHandContainer() as RectTransform;

            GuestCard newCard = Instantiate(guestCardPrefab, spawnParent);
            RectTransform newCardRect = newCard.transform as RectTransform;

            newCard.Initialize(cardId, guestName, this);
            newCard.SetPreferredTraits(preferredTraitsToUse);

            if (newCardRect != null && drawSpawnPoint != null)
            {
                newCardRect.position = drawSpawnPoint.position;
                newCardRect.localScale = Vector3.one;
                newCardRect.rotation = Quaternion.Euler(0f, 0f, drawStartRotationZ);
            }

            handManager.AddToHand(newCard, false);

            yield return StartCoroutine(AnimateDrawCardToHand(newCard, handManager.GetHandContainer()));

            handManager.RefreshHandLayout();
            drawsRemaining--;

            yield return new WaitForSeconds(drawStaggerDelay);
        }

        UpdateDeckCountText();
        isBusy = false;
        RefreshDrawButtonState();
    }

    private IEnumerator AnimateDrawCardToHand(GuestCard card, Transform targetParent)
    {
        if (card == null || targetParent == null)
            yield break;

        RectTransform cardRect = card.transform as RectTransform;
        RectTransform targetRect = targetParent as RectTransform;

        if (cardRect == null || targetRect == null)
        {
            card.transform.SetParent(targetParent, false);
            card.transform.localPosition = Vector3.zero;
            card.transform.localScale = Vector3.one;
            card.transform.rotation = Quaternion.identity;
            yield break;
        }

        Canvas rootCanvas = card.GetComponentInParent<Canvas>();
        Transform animParent = animationLayer != null ? animationLayer : rootCanvas.transform;

        Vector3 startWorldPos = cardRect.position;
        Vector3 finalWorldPos = targetRect.position;

        // Overshoot slightly past the hand, then settle back.
        Vector3 direction = (finalWorldPos - startWorldPos).normalized;
        Vector3 overshootWorldPos = finalWorldPos + direction * drawOvershootDistance;

        Quaternion startRotation = cardRect.rotation;
        Quaternion midRotation = Quaternion.Euler(0f, 0f, 4f);
        Quaternion endRotation = Quaternion.identity;

        card.transform.SetParent(animParent, true);
        cardRect.position = startWorldPos;

        float elapsed = 0f;

        while (elapsed < cardMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / cardMoveDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);

            cardRect.position = Vector3.Lerp(startWorldPos, overshootWorldPos, eased);
            cardRect.rotation = Quaternion.Lerp(startRotation, midRotation, eased);

            yield return null;
        }

        elapsed = 0f;

        while (elapsed < drawSettleDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / drawSettleDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);

            cardRect.position = Vector3.Lerp(overshootWorldPos, finalWorldPos, eased);
            cardRect.rotation = Quaternion.Lerp(midRotation, endRotation, eased);

            yield return null;
        }

        card.transform.SetParent(targetParent, false);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.localScale = Vector3.one;
        cardRect.localRotation = Quaternion.identity;
    }

    private IEnumerator ReturnRoomCardToHand(RoomSlot room)
    {
        if (room == null || !room.HasCard())
            yield break;

        if (!handManager.HasSpace)
        {
            Log("Hand is full.");
            yield break;
        }

        GuestCard roomCard = room.CurrentCard;
        room.ClearCard();

        roomCard.SetSelected(false);

        yield return StartCoroutine(AnimateCardToTarget(roomCard, handManager.GetHandContainer()));

        handManager.AddToHand(roomCard, false);
        handManager.RefreshHandLayout();

        UpdateDeckCountText();
        RefreshDrawButtonState();
        Log($"Returned {roomCard.DisplayName} to hand.");
    }

    private IEnumerator SwapHandCardWithRoomCard(GuestCard handCard, RoomSlot room)
    {
        if (handCard == null || room == null || room.CurrentCard == null)
            yield break;

        GuestCard roomCard = room.CurrentCard;

        handManager.RemoveFromHand(handCard);
        room.ClearCard();

        handCard.SetSelected(false);
        roomCard.SetSelected(false);

        yield return StartCoroutine(AnimateCardToTarget(handCard, room.GetCardAnchor()));
        AssignCardToRoom(handCard, room);

        yield return StartCoroutine(AnimateCardToTarget(roomCard, handManager.GetHandContainer()));
        handManager.AddToHand(roomCard, false);
        handManager.RefreshHandLayout();
    }

    private void AssignCardToRoom(GuestCard card, RoomSlot room)
    {
        room.SetCard(card);
        card.SetInRoom(room);
    }

    private void SpawnRoomSlot(LevelRoomEntry roomData)
    {
        RoomSlot newRoom = Instantiate(roomSlotPrefab, roomGridParent);
        newRoom.Initialize(this, roomData.roomNumber);
        newRoom.SetTraits(roomData.traits);
        roomSlots.Add(newRoom);
    }

    public void RefillHand()
    {
        DrawCards(drawAmountPerRefill);
    }

    private void DeselectCurrentCard()
    {
        if (selectedCard != null)
            selectedCard.SetSelected(false);

        selectedCard = null;
    }

    private void UpdateDeckCountText()
    {
        if (deckCountText != null && handManager != null)
            deckCountText.text = $"Deck: {guestQueue.Count}   Hand: {handManager.CurrentHandCount}/{handManager.CurrentMaxHandSize}";
    }

    private IEnumerator AnimateCardToTarget(GuestCard card, Transform targetParent)
    {
        if (card == null || targetParent == null)
            yield break;

        RectTransform cardRect = card.transform as RectTransform;
        RectTransform targetRect = targetParent as RectTransform;

        if (cardRect == null || targetRect == null)
        {
            card.transform.SetParent(targetParent, false);
            card.transform.localPosition = Vector3.zero;
            card.transform.localScale = Vector3.one;
            yield break;
        }

        Canvas rootCanvas = card.GetComponentInParent<Canvas>();
        Transform animParent = animationLayer != null ? animationLayer : rootCanvas.transform;

        Vector3 startWorldPos = cardRect.position;
        Vector3 endWorldPos = targetRect.position;

        card.transform.SetParent(animParent, true);
        cardRect.position = startWorldPos;

        float elapsed = 0f;

        while (elapsed < cardMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / cardMoveDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            cardRect.position = Vector3.Lerp(startWorldPos, endWorldPos, t);
            yield return null;
        }

        card.transform.SetParent(targetParent, false);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.localScale = Vector3.one;
    }

    private void Log(string message)
    {
        Debug.Log(message);

        if (debugText != null)
            debugText.text = message;
    }
}