using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GuestCard guestCardPrefab;

    [Header("Board")]
    [SerializeField] private RoomSlot roomSlotPrefab;
    [SerializeField] private RoomSlot elevatorSlotPrefab;
    [SerializeField] private Transform roomGridParent;
    [SerializeField] private List<RoomSlot> roomSlots = new();

    [Header("Managers")]
    [SerializeField] private HandManager handManager;
    [SerializeField] private CardAnimationController cardAnimationController;

    [Header("Level Data")]
    [SerializeField] private LevelConfig levelConfig;

    [Header("UI")]
    [SerializeField] private TMP_Text debugText;
    [SerializeField] private TMP_Text deckCountText;
    [SerializeField] private Button drawButton;

    [Header("Draw Settings")]
    [SerializeField] private int startingDrawAmount = 5;
    [SerializeField] private int drawAmountPerRefill = 3;

    private int nextCardId = 0;
    private Queue<LevelGuestEntry> guestQueue = new();
    private GuestCard selectedCard;
    private RoomSlot selectedRoomSlot;
    private bool isBusy;

    private void Start()
    {
        InitializeRooms();
        StartGame();
    }

    private void Update()
    {
        if (isBusy)
            return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
            {
                DeselectCurrentCard();
            }
        }
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

        ApplyElevatorAdjacencyTraits();
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

        int roomCount = 0;
        for (int i = 0; i < roomSlots.Count; i++)
        {
            if (roomSlots[i].CanAcceptGuest)
                roomCount++;
        }
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

        selectedRoomSlot = null;

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

        if (TraitTooltipPanel.Instance != null)
            TraitTooltipPanel.Instance.ShowGuest(card);

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

        if (!room.CanAcceptGuest)
        {
            SelectRoomSlot(room);
            Log("You can't place a guest in the elevator.");
            return;
        }

        // Empty room
        if (!room.HasCard())
        {
            // If a card is selected, use normal move/place behavior.
            if (selectedCard != null)
            {
                StartCoroutine(HandleEmptyRoomInteraction(room));
                return;
            }

            // No card selected: toggle room info selection only.
            SelectRoomSlot(room);
            return;
        }

        // Occupied room behaves like clicking the card in that room.
        OnGuestCardLeftClicked(room.CurrentCard);
    }

    public void OnRoomRightClicked(RoomSlot room)
    {
        if (room == null || isBusy)
            return;

        if (!room.CanAcceptGuest)
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

    private void SelectRoomSlot(RoomSlot room)
    {
        if (room == null)
            return;

        if (selectedRoomSlot == room)
        {
            DeselectSelectedRoomSlot();
            return;
        }

        selectedRoomSlot = room;

        if (TraitTooltipPanel.Instance != null)
            TraitTooltipPanel.Instance.ShowRoom(room);

        Log($"Selected {room.GetHolderName()}");
    }

    private void DeselectSelectedRoomSlot()
    {
        selectedRoomSlot = null;

        if (TraitTooltipPanel.Instance != null)
            TraitTooltipPanel.Instance.Hide();
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

        yield return StartCoroutine(cardAnimationController.AnimateCardToTarget(cardA, roomB.GetCardAnchor()));
        yield return StartCoroutine(cardAnimationController.AnimateCardToTarget(cardB, roomA.GetCardAnchor()));

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

        CheckWinState();
    }

    private IEnumerator MoveRoomCardToEmptyRoom(RoomSlot fromRoom, RoomSlot toRoom)
    {
        if (fromRoom == null || toRoom == null || !fromRoom.HasCard() || toRoom.HasCard() || !toRoom.CanAcceptGuest)
            yield break;

        GuestCard card = fromRoom.CurrentCard;
        fromRoom.ClearCard();

        card.SetSelected(false);

        yield return StartCoroutine(cardAnimationController.AnimateCardToTarget(card, toRoom.GetCardAnchor()));

        toRoom.SetCard(card);
        card.SetInRoom(toRoom);
    }

    private IEnumerator MoveHandCardToRoom(GuestCard card, RoomSlot targetRoom)
    {
        if (card == null || targetRoom == null || !targetRoom.CanAcceptGuest)
            yield break;

        handManager.RemoveFromHand(card);
        card.SetSelected(false);

        yield return StartCoroutine(cardAnimationController.AnimateCardToTarget(card, targetRoom.GetCardAnchor()));

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

            RectTransform handParent = handManager.GetHandContainer() as RectTransform;
            GuestCard newCard = Instantiate(guestCardPrefab, handParent);
            RectTransform newCardRect = newCard.transform as RectTransform;

            newCard.Initialize(cardId, guestName, this);
            newCard.SetPreferredTraits(preferredTraitsToUse);
            newCard.SetPreferredFloorPreferences(guestData.preferredFloorPreferences);
            
            handManager.AddToHand(newCard, false);
            handManager.RefreshHandLayout();
            Canvas.ForceUpdateCanvases();
            newCard.SnapToCurrentHandPose();
            yield return StartCoroutine(cardAnimationController.AnimateDrawCardToHand(newCard));

            drawsRemaining--;
        }

        UpdateDeckCountText();
        isBusy = false;
        RefreshDrawButtonState();
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

        RectTransform handParent = handManager.GetHandContainer() as RectTransform;
        roomCard.transform.SetParent(handParent, true);

        handManager.AddToHand(roomCard, false);
        handManager.RefreshHandLayout();
        Canvas.ForceUpdateCanvases();

        yield return StartCoroutine(cardAnimationController.AnimateCardToHandPose(roomCard));

        UpdateDeckCountText();
        RefreshDrawButtonState();
        Log($"Returned {roomCard.DisplayName} to hand.");
    }

    private IEnumerator SwapHandCardWithRoomCard(GuestCard handCard, RoomSlot room)
    {
        if (handCard == null || room == null || room.CurrentCard == null || !room.CanAcceptGuest)
            yield break;

        GuestCard roomCard = room.CurrentCard;

        handManager.RemoveFromHand(handCard);
        room.ClearCard();

        handCard.SetSelected(false);
        roomCard.SetSelected(false);

        yield return StartCoroutine(cardAnimationController.AnimateCardToTarget(handCard, room.GetCardAnchor()));
        AssignCardToRoom(handCard, room);

        RectTransform handParent = handManager.GetHandContainer() as RectTransform;
        roomCard.transform.SetParent(handParent, true);

        handManager.AddToHand(roomCard, false);
        handManager.RefreshHandLayout();
        Canvas.ForceUpdateCanvases();

        yield return StartCoroutine(cardAnimationController.AnimateCardToHandPose(roomCard));
    }

    private void AssignCardToRoom(GuestCard card, RoomSlot room)
    {
        room.SetCard(card);
        card.SetInRoom(room);
    }

    private void SpawnRoomSlot(LevelRoomEntry roomData)
    {
        if (roomData == null)
            return;

        RoomSlot prefabToSpawn = roomData.slotType == SlotType.Elevator
            ? elevatorSlotPrefab
            : roomSlotPrefab;

        if (prefabToSpawn == null)
        {
            Log(roomData.slotType == SlotType.Elevator
                ? "Missing ElevatorSlot prefab."
                : "Missing RoomSlot prefab.");
            return;
        }

        RoomSlot newSlot = Instantiate(prefabToSpawn, roomGridParent);
        newSlot.Initialize(this, roomData);
        roomSlots.Add(newSlot);
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
        selectedRoomSlot = null;

        if (TraitTooltipPanel.Instance != null)
            TraitTooltipPanel.Instance.Hide();
    }

    private void UpdateDeckCountText()
    {
        if (deckCountText != null && handManager != null)
            deckCountText.text = $"{guestQueue.Count}";
    }

    private void ApplyElevatorAdjacencyTraits()
    {
        List<RoomSlot> elevators = new List<RoomSlot>();

        for (int i = 0; i < roomSlots.Count; i++)
        {
            if (roomSlots[i].IsElevator)
                elevators.Add(roomSlots[i]);
        }

        for (int i = 0; i < roomSlots.Count; i++)
        {
            RoomSlot slot = roomSlots[i];

            if (slot.IsElevator)
                continue;

            bool nearElevator = false;

            for (int e = 0; e < elevators.Count; e++)
            {
                if (slot.IsAdjacentTo(elevators[e]))
                {
                    nearElevator = true;
                    break;
                }
            }

            if (nearElevator)
                slot.AddTrait(RoomTrait.NearElevator);
            else
                slot.RemoveTrait(RoomTrait.NearElevator);
        }
    }

    public bool AreAllPlacedGuestsCorrect()
    {
        bool hasAnyPlacedGuest = false;

        for (int i = 0; i < roomSlots.Count; i++)
        {
            RoomSlot room = roomSlots[i];

            if (!room.HasCard())
                continue;

            hasAnyPlacedGuest = true;

            GuestCard guest = room.CurrentCard;
            if (guest == null || !guest.IsPerfectMatch(room))
                return false;
        }

        return hasAnyPlacedGuest;
    }

    public bool IsRoomCorrect(RoomSlot room)
    {
        if (room == null || !room.HasCard() || room.CurrentCard == null)
            return false;

        return room.CurrentCard.IsPerfectMatch(room);
    }

    private void CheckWinState()
    {
        Log($"Correct rooms: {CountCorrectlyAssignedGuests()} / {GetGuestRoomSlotCount()}");

        if (AreAllPlacedGuestsCorrect())
        {
            Log("All guests are in correct rooms!");
        }
    }

    public int CountCorrectlyAssignedGuests()
    {
        int correct = 0;

        for (int i = 0; i < roomSlots.Count; i++)
        {
            RoomSlot room = roomSlots[i];

            if (room.HasCard() && room.CurrentCard != null && room.CurrentCard.IsPerfectMatch(room))
                correct++;
        }

        return correct;
    }

    private int GetGuestRoomSlotCount()
    {
        int count = 0;

        for (int i = 0; i < roomSlots.Count; i++)
        {
            if (roomSlots[i].CanAcceptGuest)
                count++;
        }

        return count;
    }

    public int GetTopFloorIndex()
    {
        int topFloor = 1;

        for (int i = 0; i < roomSlots.Count; i++)
        {
            if (roomSlots[i].CanAcceptGuest && roomSlots[i].FloorIndex > topFloor)
                topFloor = roomSlots[i].FloorIndex;
        }

        return topFloor;
    }

    private void Log(string message)
    {
        Debug.Log(message);

        if (debugText != null)
            debugText.text = message;
    }
}