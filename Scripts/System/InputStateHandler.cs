using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class InputStateHandler : MonoBehaviour
{
    public InputState currentState;
    private InputState _emptyState;
    private int[] directionSelection = { 0, 0, 0, 0 };
    public static InputStateHandler Instance;
    private event Action OnInputUp;
    private event Action OnInputDown; 
    private event Action OnInputRight; 
    private event Action OnInputLeft;
    public event Action OnStateRemovalComplete;
    public event Action<InputState> OnStateRemoved;
    public event Action<InputState> OnStateChanged;
    public event Action<int> OnSelectionIndexChanged;
    private bool _readingInputs;
    [SerializeField] private bool _currentStateLoaded;
    private bool _handlingState;
    public List<InputState> stateLayers;
    public enum Directional { None, Horizontal, Vertical, OmniDirection}

    public enum StateGroup {None,Bag,PokemonParty,PokemonDetails,PokemonStorage,PokemonBattle,PokeMart }
    public enum StateName 
    {
        PlaceHolder,DialoguePlaceHolder,Empty,DialogueOptions,PokemonBattleMoveSelection,PokemonBattleEnemySelection,PokemonBattleOptions,
        PokemonStorageBoxChange,PokemonStorageExit ,PokemonStorageBoxOptions,PokemonStorageBoxNavigation,PokemonStoragePartyNavigation,
        PokemonStorageUsage,ItemStorageUsage,PokemonStoragePartyOptions,PokemonStorageDepositSelection,
        PokemonDetails, PokemonDetailsMoveSelection ,PokemonDetailsMoveData,
        PlayerBagItemSell,PlayerBagNavigation,
        PokemonPartyOptions,PokemonPartyItemUsage,PokemonPartyNavigation,
        MartItemPurchase,MartItemNavigation,
        PlayerMenu,PlayerProfile
    }

    public int[] boxCoordinates={0,0};
    private int _currentBoxCapacity;
    private int _numBoxRows;
    private int _numBoxColumns;
    private int _currentNumBoxElements;
    public int rowRemainder;
    public GameObject emptyPlaceHolder;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _readingInputs = false;
    }

    private void Start()
    {
        Game_Load.Instance.OnGameStarted += () => _readingInputs = true;
        _emptyState = new InputState(StateName.Empty,new[]{StateGroup.None}, canExit: false);
        currentState = _emptyState;
        _currentStateLoaded = false;
    }

    public void AddPlaceHolderState()
    {
        ChangeInputState(new InputState(StateName.PlaceHolder,new[]{StateGroup.None}, canExit: false
            , isParent:true,mainView: emptyPlaceHolder));
    }
    public void AddDialoguePlaceHolderState()
    {
        ChangeInputState(new InputState(StateName.DialoguePlaceHolder,new[]{StateGroup.None}, canExit: false
            , isParent:true,mainView: emptyPlaceHolder));
    }
    public void ResetGroupUi(StateGroup group)
    {
        List<InputState> inputStates = new List<InputState>();
        
        inputStates.AddRange(GetRelevantStates(group));
        
        RemoveInputStates(inputStates);
    }
    public void ResetRelevantUi(StateName[] stateNames)
    {
        List<InputState> inputStates = new List<InputState>();
        
        foreach (var stateName in stateNames)
            inputStates.AddRange(GetRelevantStates(stateName));
        
        RemoveInputStates(inputStates);
    }

    private List<InputState> GetRelevantStates(StateGroup group)
    {
        List<InputState> inputStates = new List<InputState>();
        foreach (var state in stateLayers)
             if (state.stateGroups.Contains(group))
                inputStates.Add(state);
        
        return inputStates;
    }
    
    private List<InputState> GetRelevantStates(StateName stateName)
    {
        List<InputState> inputStates = new List<InputState>();
        foreach (var state in stateLayers)
            if (state.stateName == stateName)
                inputStates.Add(state);
        
        return inputStates;
    }
    private void RemoveInputStates(List<InputState> states)
    {
        foreach (var state in states)
            RemoveInputState(state,false);
        StateRemovalCompletion();
    }

    private void RemoveInputState(InputState state,bool manualExit)
    {
        state.selector?.SetActive(false);
        Action method = manualExit ? state.OnExit:state.OnClose;
        method?.Invoke();//note: state must not have onexit/onclose that also starts this coroutine
        ResetInputEvents();
        stateLayers.Remove(state);
        if (!manualExit) return;
        StateRemovalCompletion();
    }

    private void StateRemovalCompletion()
    {
        _currentStateLoaded = false;
        if (stateLayers.Count > 0)
            ChangeInputState(stateLayers.Last());
        else
            currentState =  _emptyState;
        OnStateRemovalComplete?.Invoke();
    }
    public void RemoveTopInputLayer(bool invokeOnExit)
    {
        stateLayers.Last().OnExit = invokeOnExit? stateLayers.Last().OnExit:null;
        RemoveInputState(stateLayers.Last() ,true);
    }

    private void Update()
    {
        if (!_readingInputs) return;
        _handlingState = stateLayers.Count > 0;
        if (!_handlingState) return;
        
        bool viewingExitableDialogue = Dialogue_handler.Instance.canExitDialogue & Dialogue_handler.Instance.displaying; 
        
        if (Input.GetKeyDown(KeyCode.X) && stateLayers.Last().stateName!=StateName.DialogueOptions
                                        && !viewingExitableDialogue && currentState.canManualExit)
            RemoveTopInputLayer(true);
        
        if (currentState.stateName == StateName.Empty) return;

        if (Input.GetKeyDown(KeyCode.Z) && _currentStateLoaded)
        {
            InvokeSelectedEvent();
        }
        
        if (currentState.stateDirectional == Directional.None) return;
        
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            HandleEvents(OnInputLeft, directionSelection[2], Directional.Horizontal);
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            HandleEvents(OnInputRight, directionSelection[3], Directional.Horizontal);
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            HandleEvents(OnInputUp, directionSelection[0], Directional.Vertical);
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            HandleEvents(OnInputDown, directionSelection[1], Directional.Vertical);
        }
    }

    void HandleEvents(Action onInput,int directionIndex,Directional direction)
    {
        onInput?.Invoke();
        
        if (currentState.stateDirectional != Directional.OmniDirection) ChangeSelectionIndex(directionIndex);
        
        if(CanUpdateSelector(direction)) UpdateSelectorUi();
    }
    bool CanUpdateSelector(Directional directional)
    {
        return currentState.displayingSelector &
               currentState.stateDirectional == directional;
    }
    void InvokeSelectedEvent()
    {
        if (currentState.selectableUis == null) return;
        if (currentState.isSelecting)
        {
            if (!currentState.selectableUis[currentState.currentSelectionIndex].canBeSelected) return;
            currentState.selectableUis[currentState.currentSelectionIndex]?.eventForUi?.Invoke();
        }
        else
            currentState.selectableUis[0]?.eventForUi?.Invoke();
    }
    void UpdateSelectorUi()
    {
        if (!currentState.isSelecting) return;
        currentState.selector.transform.position = currentState.selectableUis[currentState.currentSelectionIndex]
            .uiObject.transform.position;
    }
    private void ChangeSelectionIndex(int change)
    {
        currentState.currentSelectionIndex =
            Mathf.Clamp(currentState.currentSelectionIndex+change, 0, currentState.maxSelectionIndex);
        OnSelectionIndexChanged?.Invoke(currentState.currentSelectionIndex);
    }
    public void ChangeInputState(InputState newState)
    {
        OnStateRemoved?.Invoke(currentState);
        ResetCoordinates();
        if (!stateLayers.Any(s => s.stateName == newState.stateName))
            stateLayers.Add(newState);
        ResetInputEvents();
        currentState = stateLayers.Last();
        OnStateChanged?.Invoke(currentState);
        HandleStateExitability();
        SetDirectionals();
        if (currentState.isSelecting) currentState.maxSelectionIndex = currentState.selectableUis.Count-1;
        SetupInputEvents();
        if (currentState.displayingSelector)
        {
            UpdateSelectorUi();
            currentState.selector.SetActive(true);
        }
        _currentStateLoaded = true;
        var parentLayers = stateLayers.Where(s => s.isParentLayer).ToList();
        if (parentLayers.Count==0) return;
        parentLayers.ForEach(l=>l.mainViewUI.SetActive(false));
        parentLayers.Last().mainViewUI.SetActive(true);
    }

    private void HandleStateExitability()
    {
        if (currentState.UpdateExitStatus == null) return;

        currentState.canExit = currentState.UpdateExitStatus.Invoke();
    }
    void SetDirectionals()
    {
        switch (currentState.stateDirectional)
        {
            case Directional.None: 
            case Directional.OmniDirection:
                return;
            case Directional.Horizontal: 
                directionSelection = new[] { 0, 0, -1, 1 };
                break;
            case Directional.Vertical: 
                directionSelection = new[] { -1, 1, 0, 0 };
                break;
        }
    }
    void ResetInputEvents()
    {
        OnInputUp = null; OnInputDown = null; OnInputLeft = null; OnInputRight = null;
        OnSelectionIndexChanged = null;
    }
    private void PlayerBagNavigation()
    {
        if (Bag.Instance.numItems == 0) return;
        
        OnInputLeft += Bag.Instance.ChangeCategoryLeft;
        OnInputRight += Bag.Instance.ChangeCategoryRight;
        
        OnInputUp += Bag.Instance.NavigateUp;
        OnInputDown += Bag.Instance.NavigateDown;
        
        if(Bag.Instance.numItems==Bag.Instance.numItemsForView)
        {
            //prevent selecting null item selectables
            currentState.maxSelectionIndex = Bag.Instance.numItems-1;
            Bag.Instance.OnBagOpened += ()=>
                currentState.maxSelectionIndex = Bag.Instance.numItems-1;
            
            Bag.Instance.OnBagOpened += UpdateSelectorUi;
        }

        switch (Bag.Instance.currentBagUsage)
        {
            case Bag.BagUsage.SellingView:
                currentState.selectableUis.ForEach(s=>s.eventForUi = CreateSellingItemState);
                break;
            case Bag.BagUsage.NormalView:
                currentState.selectableUis.ForEach(s=>s.eventForUi = Bag.Instance.UseItem);
                break;
            case Bag.BagUsage.SelectionOnly:
                currentState.selectableUis.ForEach(s=>s.eventForUi = Bag.Instance.SelectItemForEvent);
                break;
        }
    }

    void CreateSellingItemState()
    {
        var itemSellSelectables = new List<SelectableUI>{new(Bag.Instance.sellingItemUI,Bag.Instance.SellToMarket,true)};
        ChangeInputState(new InputState(StateName.PlayerBagItemSell,
            new[]{StateGroup.Bag}, stateDirectional:Directional.Vertical, selectableUis:itemSellSelectables
            ,selecting:false,onExit:Bag.Instance.ResetItemSellingUi));
        Bag.Instance.ChangeQuantity(0);//initial set for visuals
    }

    private void ItemToSellInputs()
    {
        OnInputUp += ()=>Bag.Instance.ChangeQuantity(1);
        OnInputDown += ()=>Bag.Instance.ChangeQuantity(-1);
    }
    public void UpdateHealthBarColors()
    {
        for (var i = 0;i<Pokemon_party.Instance.numMembers;i++)
        {
            PokemonOperations.UpdateHealthPhase(Pokemon_party.Instance.party[i], 
                    Pokemon_party.Instance.memberCards[i].hpSliderImage);
        }
    }
    public void PokemonPartyOptions()
    {
        var partyOptionsSelectables = new List<SelectableUI>
        {
            new(Pokemon_party.Instance.partyOptions[0]
                , ()=>Game_ui_manager.Instance.ViewPartyPokemonDetails(
                    Pokemon_party.Instance.party[Pokemon_party.Instance.selectedMemberNumber - 1]), true),
            new(Pokemon_party.Instance.partyOptions[1]
                , () => Pokemon_party.Instance.SelectMemberToBeSwapped(Pokemon_party.Instance.selectedMemberNumber)
                , true),
            new(Pokemon_party.Instance.partyOptions[2]
            , Bag.Instance.OpenBagToGiveItem
            ,!Pokemon_party.Instance.party[Pokemon_party.Instance.selectedMemberNumber - 1].hasItem),
            new(Pokemon_party.Instance.partyOptions[3]
                , () => Bag.Instance.TakeItem(Pokemon_party.Instance.selectedMemberNumber)
                ,Pokemon_party.Instance.party[Pokemon_party.Instance.selectedMemberNumber - 1].hasItem)
        };
        partyOptionsSelectables.RemoveAll(s=>!s.canBeSelected);
        ChangeInputState(new InputState(StateName.PokemonPartyOptions,
            new[]{StateGroup.PokemonParty}, stateDirectional:Directional.Vertical, selectableUis:partyOptionsSelectables
            ,selector:Pokemon_party.Instance.optionSelector,selecting:true,display:true
            ,onClose:Pokemon_party.Instance.ClearSelectionUI,onExit:Pokemon_party.Instance.ClearSelectionUI));
        currentState.selector.SetActive(true);
    }

    void SetupPokemonDetails()
    {
        OnInputLeft += Pokemon_Details.Instance.PreviousPage;
        OnInputRight += Pokemon_Details.Instance.NextPage;
        OnInputUp += ()=>Pokemon_Details.Instance.ChangePokemon(-1);
        OnInputDown += ()=>Pokemon_Details.Instance.ChangePokemon(1);
    }

    public void AllowMoveUiNavigation()
    {
        var moveSelectables = new List<SelectableUI>();
        for (var i = 0; i < Pokemon_Details.Instance.currentPokemon.moveSet.Count; i++)
        {
            moveSelectables.Add(new(Pokemon_Details.Instance.moveNamesText[i].gameObject,
                () => Pokemon_Details.Instance.SelectMove(currentState.currentSelectionIndex), true));
        }

        Action onExit = null;
        if (Pokemon_Details.Instance.changingMoveData) 
            onExit = () => Pokemon_Details.Instance.OnMoveSelected?.Invoke(-1);

        if (Pokemon_Details.Instance.learningMove)
            onExit = ()=> OnStateRemovalComplete += RemoveDetailsInputStates;
        
        ChangeInputState(new InputState(StateName.PokemonDetailsMoveSelection,new[]{StateGroup.PokemonDetails},
            stateDirectional:Directional.Vertical,selectableUis:moveSelectables, 
            selector:Pokemon_Details.Instance.moveSelector, selecting:true, display:true,onExit:onExit));
    }
    void RemoveDetailsInputStates()
    {
        OnStateRemovalComplete -= RemoveDetailsInputStates;
        //if started learning but rejected it on move selection screen
        Options_manager.Instance.SkipMove();
        ResetGroupUi(StateGroup.PokemonDetails);
    }
    private void ResetCoordinates()
    {
        boxCoordinates[0] = 0;
        boxCoordinates[1] = 0;
    }
    
    private int GetCurrentFullBoxPosition()
    {
        int row = Mathf.Clamp(boxCoordinates[0], 0, _numBoxRows);
        int col = Mathf.Clamp(boxCoordinates[1], 0, _numBoxColumns)-row;

        int pos = row * _numBoxColumns + col;

        return Mathf.Clamp(pos, 0, _currentNumBoxElements);
    }

    private void MoveCoordinatesFullBox(Directional directional, int change)
    {
        bool vertical = directional == Directional.Vertical;

        if (vertical)
        {
            boxCoordinates[0] = Mathf.Clamp(
                boxCoordinates[0] + change,
                0,
                _numBoxRows
            );
        }
        else
        {
            boxCoordinates[1] = Mathf.Clamp(
                boxCoordinates[1] + change,
                0,
                _numBoxColumns
            );
        }

        int newIndex = GetCurrentFullBoxPosition();
        currentState.currentSelectionIndex =
            Mathf.Clamp(newIndex, 0, currentState.maxSelectionIndex);

        OnSelectionIndexChanged?.Invoke(currentState.currentSelectionIndex);
        UpdateSelectorUi();
    }

    private void SetRowRemainder()
    {
        var currentRowRemainder = _currentNumBoxElements - (boxCoordinates[0] * _numBoxColumns);
        rowRemainder =  (currentRowRemainder < _numBoxColumns)? currentRowRemainder: _numBoxColumns;
        rowRemainder = Mathf.Clamp(rowRemainder, 0, _numBoxColumns);
        boxCoordinates[1] = Mathf.Clamp(boxCoordinates[1], 0, rowRemainder-1);
    }
    private int GetCurrentBoxPositionDynamic()
    {
        SetRowRemainder();
        var currentColumn = boxCoordinates[1];
        var currentRow = boxCoordinates[0];
        var rowCapacity = currentRow * _numBoxColumns;
        rowCapacity = Mathf.Clamp(rowCapacity, 0, _currentBoxCapacity);
        return rowCapacity + Mathf.Clamp(currentColumn, 0, rowRemainder-1);
    }
    private void MoveCoordinatesDynamic(Directional directional, int change)
    {
        SetRowRemainder();
        var coordinateIndex = directional == Directional.Vertical ? 0 : 1;
        
        var maxIndexForCoordinate  = directional == Directional.Vertical ?
            (int)math.ceil((float)_currentNumBoxElements/_numBoxColumns) - 1 : rowRemainder-1;
        
        boxCoordinates[coordinateIndex] = Mathf.Clamp(boxCoordinates[coordinateIndex] + change, 0, maxIndexForCoordinate);
        currentState.currentSelectionIndex = _currentNumBoxElements > currentState.maxSelectionIndex?
            Mathf.Clamp(GetCurrentBoxPositionDynamic(),0,currentState.maxSelectionIndex) 
            :Mathf.Clamp(GetCurrentBoxPositionDynamic(),0,_currentNumBoxElements);
        OnSelectionIndexChanged?.Invoke(currentState.currentSelectionIndex);
        UpdateSelectorUi();
    }
    
    void StorageFullBoxNavigation()
    {
        _currentNumBoxElements = pokemon_storage.BoxCapacity;
        _currentBoxCapacity = pokemon_storage.BoxCapacity;
        _numBoxColumns = pokemon_storage.Instance.boxColumns;
        _numBoxRows = pokemon_storage.BoxCapacity / _numBoxColumns;
        OnInputLeft += ()=> MoveCoordinatesFullBox(Directional.Horizontal,-1);
        OnInputRight += ()=> MoveCoordinatesFullBox(Directional.Horizontal,1);
        OnInputUp += ()=> MoveCoordinatesFullBox(Directional.Vertical,-1);
        OnInputDown += ()=> MoveCoordinatesFullBox(Directional.Vertical,1);
        
        currentState.canExit = false;
        OnSelectionIndexChanged += pokemon_storage.Instance.LoadPokemonData;
        OnSelectionIndexChanged += CheckIfTopRow;
        ChangeSelectionIndex(0);
    }

    public void SetupPokemonStorageState()
    {
        var storageSelectables = new List<SelectableUI>();
        storageSelectables.Add(new(pokemon_storage.Instance.storageBoxExit.gameObject,Game_ui_manager.Instance.ClosePokemonStorage, true));
        
        ChangeInputState(new InputState(StateName.PokemonStorageExit,
            new[] {StateGroup.PokemonStorage }, true,pokemon_storage.Instance.storageUI,
            Directional.Vertical,storageSelectables,pokemon_storage.Instance.initialSelector, true,display:true,canManualExit:false
            ));
        pokemon_storage.Instance.initialSelector.transform.rotation = Quaternion.Euler(0, 180, 180);
        
        OnInputDown += PokemonStorageBoxChange;
    }

    public void PokemonStorageBoxChange()
    {
        var storageSelectables = new List<SelectableUI>();
        for (int i = 0; i < pokemon_storage.NumBoxes; i++)
        {
            storageSelectables.Add(new(pokemon_storage.Instance.boxTopVisualImage.gameObject,null, true));
        }
        pokemon_storage.Instance.initialSelector.transform.rotation = Quaternion.Euler(0, 0, 0);
        ChangeInputState(new InputState(StateName.PokemonStorageBoxChange,
            new[] {StateGroup.PokemonStorage }, true,pokemon_storage.Instance.storageUI,
            Directional.Horizontal,storageSelectables,pokemon_storage.Instance.initialSelector, selecting:true,display:true,canManualExit:false));

        OnInputUp += SwitchToExit;
        OnInputDown += PokemonStorageBoxNavigation;
        OnInputLeft += () => pokemon_storage.Instance.ChangeBox(-1);
        OnInputRight += () => pokemon_storage.Instance.ChangeBox(1);
    }
    public void PokemonStorageBoxNavigation()
    {
        var storageBoxSelectables = new List<SelectableUI>();
        foreach (var icon in pokemon_storage.Instance.nonPartyIcons)
        { 
            var newSelectable = new SelectableUI(icon.gameObject,
                ()=>pokemon_storage.Instance.SelectNonPartyPokemon(icon.GetComponent<PC_pkm>())
                , true);
            storageBoxSelectables.Add(newSelectable);
        }

        ChangeInputState(new InputState(StateName.PokemonStorageBoxNavigation,new[]{StateGroup.PokemonStorage}
            ,stateDirectional:Directional.OmniDirection,selectableUis:storageBoxSelectables,
            selector:pokemon_storage.Instance.initialSelector, selecting:true,display: true,canManualExit:false,canExit:false));
        ChangeSelectionIndex(0);
    }
    private void SwitchToExit()
    {
        RemoveTopInputLayer(false);
        SetupPokemonStorageState();
    }

    private void CheckIfTopRow(int index)
    {
        if (boxCoordinates[0]==0)//if top row
        {
            if (currentState.canExit) return;
            currentState.canExit = true;
            OnInputUp += ExitTopRow;
        }
        else
        {
            OnInputUp -= ExitTopRow;
            currentState.canExit = false;
        }
    }

    private void ExitTopRow()
    {
        RemoveTopInputLayer(false);
        pokemon_storage.Instance.ClearPokemonData();
        PokemonStorageBoxChange();
    }

    private void PokeMartNavigation()
    {
        OnInputUp += Poke_Mart.Instance.NavigateUp;
        OnInputDown += Poke_Mart.Instance.NavigateDown;
        if(Poke_Mart.Instance.numItemsForView==Poke_Mart.Instance.numItems)
        {//prevent selecting null item selectables
            currentState.maxSelectionIndex = Poke_Mart.Instance.numItems-1;
        }
        currentState.selectableUis.ForEach(s=>s.eventForUi = SelectItemToBuy);
    }

    void SelectItemToBuy()
    { 
        Poke_Mart.Instance.quantityUI.SetActive(true);
        var itemQuantitySelectables = new List<SelectableUI>
        {
            new(Poke_Mart.Instance.quantityUI,Poke_Mart.Instance.BuyItem,true)
        };
        ChangeInputState(new InputState(StateName.MartItemPurchase,new[]{StateGroup.PokeMart}
            , stateDirectional:Directional.Vertical, selectableUis:itemQuantitySelectables
            ,selector:Poke_Mart.Instance.quantitySelector,display: true
            ,onExit: ()=>Poke_Mart.Instance.selectedItemQuantity=1));
    }

    void ItemToBuyInputs()
    {
        OnInputUp += ()=>Poke_Mart.Instance.ChangeItemQuantity(1);
        OnInputDown += ()=>Poke_Mart.Instance.ChangeItemQuantity(-1);
    }
    
    void SetupBattleOptions()
    {
        currentState.currentSelectionIndex = 0;
        _currentNumBoxElements = 4;
        _currentBoxCapacity = 4;
        _numBoxColumns = 2;
        SetRowRemainder();
        OnInputLeft += ()=>MoveCoordinatesDynamic(Directional.Horizontal,-1);
        OnInputRight += ()=>MoveCoordinatesDynamic(Directional.Horizontal,1);
        OnInputUp += ()=>MoveCoordinatesDynamic(Directional.Vertical,-2);
        OnInputDown += ()=>MoveCoordinatesDynamic(Directional.Vertical,2);
    }
    
    void SetupMoveSelection()
    {
        Battle_handler.Instance.battleParticipants[Battle_handler.Instance.currentEnemyIndex]
            .pokemonImage.color = Color.HSVToRGB(0,0,100);//reset color if cancelled selection
        currentState.currentSelectionIndex = 0;
        _currentNumBoxElements = currentState.maxSelectionIndex+1;
        _currentBoxCapacity = 4;
        _numBoxColumns = 2;
        SetRowRemainder();
        OnInputLeft += ()=>MoveCoordinatesDynamic(Directional.Horizontal,-1);
        OnInputRight += ()=>MoveCoordinatesDynamic(Directional.Horizontal,1);
        OnInputUp += ()=>MoveCoordinatesDynamic(Directional.Vertical,-2);
        OnInputDown += ()=>MoveCoordinatesDynamic(Directional.Vertical,2);
        
        OnInputLeft += ()=> Battle_handler.Instance.SelectMove(currentState.currentSelectionIndex);
        OnInputRight += () => Battle_handler.Instance.SelectMove(currentState.currentSelectionIndex);
        OnInputUp += () => Battle_handler.Instance.SelectMove(currentState.currentSelectionIndex);
        OnInputDown += () => Battle_handler.Instance.SelectMove(currentState.currentSelectionIndex);
    }

    void SetupEnemySelection()
    {
        Battle_handler.Instance.SelectEnemy(3);
        OnInputLeft += ()=> Battle_handler.Instance.SelectEnemy(-1);
        OnInputRight += () => Battle_handler.Instance.SelectEnemy(1);
    }

    void LoadStoragePokemonData()
    {
        OnSelectionIndexChanged += pokemon_storage.Instance.LoadPokemonData;
    }
    void ShowStorageBoxCapacityData()
    {
        OnSelectionIndexChanged += pokemon_storage.Instance.DisplayBoxCapacity;
    }
    void SetupInputEvents()
    {
        Action stateMethod = currentState.stateName switch
        {
            StateName.PlayerBagNavigation => PlayerBagNavigation,
            StateName.PokemonPartyItemUsage => UpdateHealthBarColors,
            StateName.PokemonPartyNavigation => UpdateHealthBarColors,
            StateName.PokemonDetails => SetupPokemonDetails,
            StateName.PlayerBagItemSell => ItemToSellInputs,
            StateName.PokemonStorageBoxNavigation => StorageFullBoxNavigation,
            StateName.MartItemNavigation => PokeMartNavigation,
            StateName.MartItemPurchase => ItemToBuyInputs,
            StateName.PokemonBattleOptions => SetupBattleOptions,
            StateName.PokemonBattleMoveSelection => SetupMoveSelection,
            StateName.PokemonBattleEnemySelection => SetupEnemySelection,
            StateName.PokemonStoragePartyNavigation=>LoadStoragePokemonData,
            StateName.PokemonStorageDepositSelection=>ShowStorageBoxCapacityData,
            _ => null
        };
        stateMethod?.Invoke();
    }

}