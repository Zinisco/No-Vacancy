using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
    [SerializeField] private GameUIController gameUIController;

    [Header("Level Data")]
    [SerializeField] private LevelConfig levelConfig;

    [Header("Draw Settings")]
    [SerializeField] private int startingDrawAmount = 5;
    [SerializeField] private int drawAmountPerRefill = 3;
    [SerializeField] private float drawStaggerDelay = 0.03f;

    private int nextCardId = 0;
    private Queue<LevelGuestEntry> guestQueue = new();

    private GuestCard selectedCard;
    private RoomSlot selectedRoomSlot;
    private bool isBusy;

    #region Unity Lifecycle

    private void Start()
    {
        if (!ValidateReferences())
            return;

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
                DeselectCurrentSelection();
            }
        }
    }

    #endregion

    #region Setup

    private bool ValidateReferences()
    {
        if (handManager == null)
        {
            Log("Missing HandManager.");
            return false;
        }

        if (cardAnimationController == null)
        {
            Log("Missing CardAnimationController.");
            return false;
        }

        if (levelConfig == null)
        {
            Log("Missing LevelConfig.");
            return false;
        }

        if (roomSlotPrefab == null || roomGridParent == null)
        {
            Log("Missing RoomSlot prefab or RoomGrid parent.");
            return false;
        }

        return true;
    }

    private void InitializeRooms()
    {
        roomSlots.Clear();

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

        int roomCount = GetGuestRoomSlotCount();
        int clampedHandSize = Mathf.Min(handManager.BaseMaxHandSize, roomCount);

        handManager.SetMaxHandSize(clampedHandSize);

        DrawCards(startingDrawAmount);

        Log($"Game started. Rooms: {roomCount}, Hand Size: {clampedHandSize}");
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

    #endregion

    #region Public Input Entry Points

    public void DrawCards(int amount)
    {
        if (isBusy)
            return;

        StartCoroutine(DrawCardsRoutine(amount));
    }

    public void RefillHand()
    {
        DrawCards(drawAmountPerRefill);
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

        if (selectedCard == null)
        {
            SelectCard(card);
            return;
        }

        if (selectedCard == card)
        {
            DeselectCurrentSelection();
            return;
        }

        StartCoroutine(HandleCardToCardInteraction(selectedCard, card));
    }

    public void OnGuestCardRightClicked(GuestCard card)
    {
        if (card == null || isBusy)
            return;

        if (card.CurrentLocationType == CardLocationType.Room && card.CurrentRoom != null)
        {
            StartCoroutine(ReturnRoomCardToHand(card.CurrentRoom));
        }
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

        if (!room.HasCard())
        {
            if (selectedCard != null)
            {
                StartCoroutine(HandleEmptyRoomInteraction(room));
                return;
            }

            SelectRoomSlot(room);
            return;
        }

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

    #endregion

    #region Selection

    public void SelectCard(GuestCard card)
    {
        if (card == null)
            return;

        selectedRoomSlot = null;

        if (selectedCard == card)
        {
            DeselectCurrentSelection();
            Log($"Deselected {card.DisplayName}");
            return;
        }

        if (selectedCard != null)
            selectedCard.SetSelected(false);

        selectedCard = card;
        selectedCard.SetSelected(true);

        gameUIController?.ShowGuestTooltip(card);

        string location = card.CurrentLocationType == CardLocationType.Room && card.CurrentRoom != null
            ? card.CurrentRoom.GetHolderName()
            : "Hand";

        Log($"Selected {card.DisplayName} from {location}");
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

        if (selectedCard != null)
            selectedCard.SetSelected(false);

        selectedCard = null;
        selectedRoomSlot = room;

        gameUIController?.ShowRoomTooltip(room);
        Log($"Selected {room.GetHolderName()}");
    }

    private void DeselectCurrentSelection()
    {
        if (selectedCard != null)
            selectedCard.SetSelected(false);

        selectedCard = null;
        selectedRoomSlot = null;

        gameUIController?.HideTooltip();
    }

    private void DeselectSelectedRoomSlot()
    {
        selectedRoomSlot = null;
        gameUIController?.HideTooltip();
    }

    #endregion

    #region Draw / Hand Flow

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
            GuestCard newCard = CreateGuestCardFromData(guestData);

            handManager.AddToHand(newCard, false);
            handManager.RefreshHandLayout();
            Canvas.ForceUpdateCanvases();
            newCard.SnapToCurrentHandPose();

            yield return StartCoroutine(cardAnimationController.AnimateDrawCardToHand(newCard));

            drawsRemaining--;

            if (drawsRemaining > 0)
                yield return new WaitForSeconds(drawStaggerDelay);
        }

        UpdateDeckCountText();
        isBusy = false;
        RefreshDrawButtonState();
    }

    private GuestCard CreateGuestCardFromData(LevelGuestEntry guestData)
    {
        string guestName = guestData.guestName;
        string cardId = $"CARD_{++nextCardId:D3}";

        RectTransform handParent = handManager.GetHandContainer() as RectTransform;
        GuestCard newCard = Instantiate(guestCardPrefab, handParent);

        newCard.Initialize(cardId, guestName, this);
        newCard.SetPreferredTraits(guestData.preferredTraits);
        newCard.SetPreferredFloorPreferences(guestData.preferredFloorPreferences);

        return newCard;
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

        yield return StartCoroutine(AddRoomCardBackToHand(roomCard));

        UpdateDeckCountText();
        RefreshDrawButtonState();
        Log($"Returned {roomCard.DisplayName} to hand.");
    }

    private IEnumerator AddRoomCardBackToHand(GuestCard card)
    {
        if (card == null)
            yield break;

        RectTransform handParent = handManager.GetHandContainer() as RectTransform;
        card.transform.SetParent(handParent, true);

        handManager.AddToHand(card, false);
        handManager.RefreshHandLayout();
        Canvas.ForceUpdateCanvases();

        yield return StartCoroutine(cardAnimationController.AnimateCardToHandPose(card));
    }

    #endregion

    #region Card Interactions

    private IEnumerator HandleCardToCardInteraction(GuestCard firstCard, GuestCard secondCard)
    {
        if (firstCard == null || secondCard == null || isBusy)
            yield break;

        isBusy = true;
        RefreshDrawButtonState();

        if (firstCard.CurrentLocationType == CardLocationType.Hand &&
            secondCard.CurrentLocationType == CardLocationType.Hand)
        {
            SelectCard(secondCard);
            isBusy = false;
            RefreshDrawButtonState();
            yield break;
        }

        if (firstCard.CurrentLocationType == CardLocationType.Hand &&
            secondCard.CurrentLocationType == CardLocationType.Room &&
            secondCard.CurrentRoom != null)
        {
            string handCardName = firstCard.DisplayName;
            string roomCardName = secondCard.DisplayName;

            yield return StartCoroutine(SwapHandCardWithRoomCard(firstCard, secondCard.CurrentRoom));
            Log($"Swapped {handCardName} with {roomCardName}");

            FinishInteraction();
            yield break;
        }

        if (firstCard.CurrentLocationType == CardLocationType.Room &&
            firstCard.CurrentRoom != null &&
            secondCard.CurrentLocationType == CardLocationType.Hand)
        {
            string roomCardName = firstCard.DisplayName;
            string handCardName = secondCard.DisplayName;

            yield return StartCoroutine(SwapHandCardWithRoomCard(secondCard, firstCard.CurrentRoom));
            Log($"Swapped {handCardName} with {roomCardName}");

            FinishInteraction();
            yield break;
        }

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

            FinishInteraction();
            yield break;
        }

        SelectCard(secondCard);
        isBusy = false;
        RefreshDrawButtonState();
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

        yield return StartCoroutine(AddRoomCardBackToHand(roomCard));
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

        FinishInteraction();
        CheckWinState();
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

    private void AssignCardToRoom(GuestCard card, RoomSlot room)
    {
        room.SetCard(card);
        card.SetInRoom(room);
    }

    private void FinishInteraction()
    {
        DeselectCurrentSelection();
        UpdateDeckCountText();
        RefreshDrawButtonState();
        isBusy = false;
    }

    #endregion

    #region Puzzle / Validation

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

    #endregion

    #region UI Helpers

    private void RefreshDrawButtonState()
    {
        if (gameUIController == null || handManager == null)
            return;

        bool canDraw = !isBusy && guestQueue.Count > 0 && handManager.HasSpace;
        gameUIController.SetDrawButtonState(canDraw);
    }

    private void UpdateDeckCountText()
    {
        gameUIController?.SetDeckCount(guestQueue.Count);
    }

    private void Log(string message)
    {
        if (gameUIController != null)
            gameUIController.SetDebugMessage(message);
        else
            Debug.Log(message);
    }

    #endregion
}