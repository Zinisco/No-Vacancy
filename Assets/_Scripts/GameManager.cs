using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    // GameManager is the main brain of the puzzle scene.
    // It does not represent one card or one room.
    // Instead, it coordinates everything:
    //
    // - Creates rooms from LevelConfig
    // - Creates guest cards from LevelConfig
    // - Draws cards into the hand
    // - Handles left/right clicks on cards and rooms
    // - Moves cards between hand and rooms
    // - Swaps cards
    // - Checks if guests are correctly placed
    // - Updates UI/debug text

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


    //Check that all necessary references are assigned, then initialize the rooms and start the game.
    private void Start()
    {
        if (!ValidateReferences())
            return;

        InitializeRooms();
        StartGame();
    }

    // Handle deselection when clicking outside of cards/rooms.
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

    // Validate that all necessary references are assigned in the inspector.
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

    // Clear existing rooms and create new ones based on the LevelConfig.
    // Also applies traits to rooms based on their adjacency to elevators.
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

    // Start the game by filling the guest queue, setting hand size, and drawing starting cards.
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

    // Spawn a RoomSlot or ElevatorSlot based on the provided LevelRoomEntry data.
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

    // Called by UI buttons or other scripts to draw cards into the hand.
    public void DrawCards(int amount) 
    {
        if (isBusy)
            return;

        StartCoroutine(DrawCardsRoutine(amount));
    }

    // Draw cards up to the specified refill amount.
    public void RefillHand() 
    {
        DrawCards(drawAmountPerRefill);
    }

    // Called by the UI when the player clicks the "Draw" button.
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

    // Called by GuestCard when it detects a left-click on itself.
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

    // Called by GuestCard when it detects a right-click on itself.
    public void OnGuestCardRightClicked(GuestCard card) 
    {
        if (card == null || isBusy)
            return;

        if (card.CurrentLocationType == CardLocationType.Room && card.CurrentRoom != null)
        {
            StartCoroutine(ReturnRoomCardToHand(card.CurrentRoom));
        }
    }

    // Called by RoomSlot when it detects a left-click on itself.
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

    // Called by RoomSlot when it detects a right-click on itself.
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

    // Handles selecting a card, showing its tooltip, and deselecting if the same card is clicked again.
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

    // Handles selecting a room slot, showing its tooltip, and deselecting if the same slot is clicked again.
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

    // Deselects any currently selected card or room slot and hides tooltips.
    private void DeselectCurrentSelection()
    {
        if (selectedCard != null)
            selectedCard.SetSelected(false);

        selectedCard = null;
        selectedRoomSlot = null;

        gameUIController?.HideTooltip();
    }

    // Deselects the currently selected room slot and hides tooltips.
    private void DeselectSelectedRoomSlot() 
    {
        selectedRoomSlot = null;
        gameUIController?.HideTooltip();
    }

    #endregion

    #region Draw / Hand Flow

    // Draws cards from the guest queue into the player's hand with animations and staggered timing.
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

    // Instantiates a GuestCard prefab and initializes it with data from the LevelGuestEntry.
    private GuestCard CreateGuestCardFromData(LevelGuestEntry guestData) 
    {
        string guestName = guestData.guestName;
        string cardId = $"CARD_{++nextCardId:D3}";

        RectTransform handParent = handManager.GetHandContainer() as RectTransform;
        GuestCard newCard = Instantiate(guestCardPrefab, handParent);

        newCard.Initialize(cardId, guestName, this);
        newCard.SetPreferredTraits(guestData.preferredTraits);
        newCard.SetPreferredFloorPreferences(guestData.preferredFloorPreferences);
        newCard.SetAdjacencyPreferences(guestData.adjacencyPreferences);
        newCard.SetBehaviorTraits(guestData.behaviorTraits);

        return newCard;
    }

    // Handles returning a card from a room back to the player's hand with animation.
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

    // Animates a card moving back to the hand and adds it to the hand manager.
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

    // Handles interactions when one card is selected and the player clicks on another card,
    // determining if they should swap or just select the new card.
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

    // Handles swapping a card from the hand with a card in a room, including animations and updating references.
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

    // Handles swapping two cards between two room slots, including animations and updating references.
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

    // Handles placing a card from the hand into an empty room or moving a card from one room to an empty room,
    // including animations and updating references.
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

    // Handles moving a card from the hand to an empty room, including animation and updating references.
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

    // Handles moving a card from one room to another empty room, including animation and updating references.
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

    // Helper method to set the card in the room and update the card's reference to its current room.
    private void AssignCardToRoom(GuestCard card, RoomSlot room) 
    {
        room.SetCard(card);
        card.SetInRoom(room);
    }

    // Common cleanup after any card interaction, such as deselecting and refreshing UI states.
    private void FinishInteraction() 
    {
        DeselectCurrentSelection();
        UpdateDeckCountText();
        RefreshDrawButtonState();
        isBusy = false;
    }

    #endregion

    #region Puzzle / Validation

    // After creating all rooms, check which ones are adjacent to elevators
    // and apply the NearElevator trait accordingly.
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

    // Checks if all guests that are currently placed in rooms are in their perfect match rooms,
    // and returns false if any guest is incorrectly placed. Also returns false if no guests are placed.
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

    // Helper method to check if a specific room has a guest
    // and if that guest is a perfect match for the room.
    public bool IsRoomCorrect(RoomSlot room) 
    {
        if (room == null || !room.HasCard() || room.CurrentCard == null)
            return false;

        return room.CurrentCard.IsPerfectMatch(room);
    }

    // Checks if all placed guests are correct and logs the current progress.
    // If all guests are correctly placed, it logs a win message.
    private void CheckWinState() 
    {
        Log($"Correct rooms: {CountCorrectlyAssignedGuests()} / {GetGuestRoomSlotCount()}");

        if (AreAllPlacedGuestsCorrect())
        {
            Log("All guests are in correct rooms!");
        }
    }

    // Counts how many guests are currently placed in rooms that are a perfect match for them.
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

    // Counts how many room slots can accept guests (i.e. are not elevators).
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

    // Helper method to find the highest floor index among all room slots that can accept guests.
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

    public List<RoomSlot> GetNoiseAdjacentRooms(RoomSlot room)
    {
        List<RoomSlot> adjacentRooms = new();

        if (room == null)
            return adjacentRooms;

        for (int i = 0; i < roomSlots.Count; i++)
        {
            RoomSlot other = roomSlots[i];

            if (other == null || other == room)
                continue;

            if (room.IsNoiseAdjacentTo(other))
                adjacentRooms.Add(other);
        }

        return adjacentRooms;
    }

    #endregion

    #region UI Helpers

    // Enables or disables the draw button based on whether the game is busy,
    // if there are cards left in the queue, and if there is space in the hand.
    private void RefreshDrawButtonState() 
    {
        if (gameUIController == null || handManager == null)
            return;

        bool canDraw = !isBusy && guestQueue.Count > 0 && handManager.HasSpace;
        gameUIController.SetDrawButtonState(canDraw);
    }

    // Updates the UI element that shows how many cards are left in the deck/queue.
    private void UpdateDeckCountText() 
    {
        gameUIController?.SetDeckCount(guestQueue.Count);
    }

    // Logs a message to the GameUIController if it exists, otherwise logs to the console.
    private void Log(string message) 
    {
        if (gameUIController != null)
            gameUIController.SetDebugMessage(message);
        else
            Debug.Log(message);
    }

    public List<RoomSlot> GetAdjacentRooms(RoomSlot room)
    {
        List<RoomSlot> adjacentRooms = new();

        if (room == null)
            return adjacentRooms;

        for (int i = 0; i < roomSlots.Count; i++)
        {
            RoomSlot other = roomSlots[i];

            if (other == null || other == room)
                continue;

            if (room.IsAdjacentTo(other))
                adjacentRooms.Add(other);
        }

        return adjacentRooms;
    }

    #endregion
}