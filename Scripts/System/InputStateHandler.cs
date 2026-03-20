using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public enum InputDirection { None, Horizontal, Vertical, Grid}
public enum InputStateGroup {None,Bag,PokemonParty,PokemonDetails,PokemonStorage,PokemonBattle,PokeMart }
public enum InputStateName 
{
    PlaceHolder,DialoguePlaceHolder,Empty,DialogueOptions,PokemonBattleMoveSelection,PokemonBattleEnemySelection,PokemonBattleOptions,
    PokemonStorageBoxChange,PokemonStorageExit ,PokemonStorageBoxOptions,PokemonStorageBoxNavigation,PokemonStoragePartyNavigation,
    PokemonStorageUsage,ItemStorageUsage,PokemonStoragePartyOptions,PokemonStorageDepositSelection,
    PokemonDetails, PokemonDetailsMoveSelection ,PokemonDetailsMoveData,
    PlayerBagItemSell,PlayerBagNavigation,
    PokemonPartyOptions,PokemonPartyItemUsage,PokemonPartyNavigation,
    MartItemPurchase,MartItemNavigation,
    PlayerMenu,PlayerProfile,KeyBinds
}
public class InputStateHandler : MonoBehaviour,IInjectable
{
    public InputState CurrentState { get; private set; }
    private InputState _emptyState;
    private int[] directionSelection = { 0, 0, 0, 0 };

    private event Action OnInputUp;
    private event Action OnInputDown; 
    private event Action OnInputRight; 
    private event Action OnInputLeft;
    public event Action<InputState> OnStateRemoved;
    public event Action<InputState> OnStateChanged;
    public event Action<int> OnSelectionIndexChanged;
    
    private bool _readingInputs;
    private bool _currentStateLoaded;
    private bool _handlingState;
    public List<InputState> stateLayers;

    public int[] boxCoordinates={0,0};
    private int _currentBoxCapacity;
    private int _numBoxRows;
    private int _numBoxColumns;
    private int _currentNumBoxElements;
    public int rowRemainder;
    public GameObject emptyPlaceHolder;
    
    private Dialogue_handler _dialogueHandler;
    private Battle_handler _battleHandler;
    private Options_manager _dialogueOptionsHandler;
    private Bag _playerBagHandler;
    private Pokemon_Details _pokemonDetailsHandler;
    private Game_ui_manager _gameUIHandler;
    private Poke_Mart _pokeMartHandler;
    private Pokemon_party _pokemonPartyHandler;
    private pokemon_storage _pokemonStorageHandler;
    private ItemStorageHandler _itemStorageHandler;
    private Game_Load _gameLoadingHandler;
    
    public void Inject(Container container)
    {
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _battleHandler = container.Resolve<Battle_handler>();
        _dialogueOptionsHandler = container.Resolve<Options_manager>();
        _gameUIHandler = container.Resolve<Game_ui_manager>();
        _playerBagHandler = container.Resolve<Bag>();
        _pokeMartHandler = container.Resolve<Poke_Mart>();
        _pokemonPartyHandler = container.Resolve<Pokemon_party>();
        _pokemonStorageHandler = container.Resolve<pokemon_storage>();
        _itemStorageHandler = container.Resolve<ItemStorageHandler>();
        _pokemonDetailsHandler = container.Resolve<Pokemon_Details>();
        _gameLoadingHandler = container.Resolve<Game_Load>();
        gameObject.SetActive(true);
        OnInject();
    }
    
    private void OnInject()
    {
        _gameLoadingHandler.OnGameStarted += () => _readingInputs = true;
        _emptyState = new InputState(InputStateName.Empty,new[]{InputStateGroup.None}, canExit: false);
        CurrentState = _emptyState;
        _currentStateLoaded = false;
    }

    public void AddPlaceHolderState()
    {
        ChangeInputState(new (InputStateName.PlaceHolder,new[]{InputStateGroup.None}, canExit: false
            , isParent:true,mainView: emptyPlaceHolder));
    }
    public void AddDialoguePlaceHolderState()
    {
        ChangeInputState(new (InputStateName.DialoguePlaceHolder,new[]{InputStateGroup.None}, canExit: false
            , isParent:true,mainView: emptyPlaceHolder));
    }
    public void ResetGroupUi(InputStateGroup group)
    {
        List<InputState> inputStates = new List<InputState>();
        
        inputStates.AddRange(GetRelevantStates(group));
        
        RemoveInputStates(inputStates);
    }
    public void ResetRelevantUi(InputStateName[] stateNames)
    {
        List<InputState> inputStates = new List<InputState>();

        foreach (var stateName in stateNames)
            inputStates.AddRange(GetRelevantStates(stateName));
        
        RemoveInputStates(inputStates);
    }
    public void ResetRelevantUi(InputStateName stateName,bool manualExit=false)
    {
        var state = stateLayers.FirstOrDefault(state => state.stateName == stateName);
        if (state == null) return;
        RemoveInputState(state,manualExit);
    }
    private List<InputState> GetRelevantStates(InputStateGroup group)
    {
        List<InputState> inputStates = new List<InputState>();
        foreach (var state in stateLayers)
             if (state.stateGroups.Contains(group))
                inputStates.Add(state);
        
        return inputStates;
    }
    
    private List<InputState> GetRelevantStates(InputStateName stateName)
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
        LoadNextState();
    }

    private void RemoveInputState(InputState state,bool manualExit)
    {
        state.selector?.SetActive(false);
        
        if(state.stateDirection==InputDirection.Grid) ResetCoordinates();
        
        Action method = manualExit ? state.OnExit:state.OnClose;
        method?.Invoke();//note: state must not have onexit/onclose that also starts this coroutine
        stateLayers.Remove(state);
        OnStateRemoved?.Invoke(state);
        if (!manualExit) return;
        LoadNextState();
    }

    private void LoadNextState()
    {
        ChangeInputState(stateLayers.Count > 0? stateLayers.Last() : _emptyState);
    }
    public void RemoveTopInputLayer(bool invokeOnExit)
    {
        CurrentState.OnExit = invokeOnExit? CurrentState.OnExit:null;
        RemoveInputState(stateLayers.Last() ,true);
    }

    private void Update()
    {
        if (!_readingInputs) return;
        _handlingState = stateLayers.Count > 0;
        if (!_handlingState) return;
        
        bool viewingExitableDialogue = _dialogueHandler.canExitDialogue & _dialogueHandler.displaying;

        if (Input.GetKeyDown(KeyCode.X))
        {
            if(CurrentState.stateName != InputStateName.DialogueOptions && !viewingExitableDialogue)
            {
                if(CurrentState.canExit)
                {
                    if (CurrentState.persistOnExit)
                        CurrentState.OnExit.Invoke();
                    
                    else if(CurrentState.canManualExit)
                        RemoveTopInputLayer(true);
                }
            }
        }
        
        if (CurrentState.stateName == InputStateName.Empty) return;

        if (Input.GetKeyDown(KeyCode.Z) && _currentStateLoaded)
        {
            InvokeSelectedEvent();
        }
        
        if (CurrentState.stateDirection == InputDirection.None) return;
        
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            HandleEvents(OnInputLeft, directionSelection[2], InputDirection.Horizontal);
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            HandleEvents(OnInputRight, directionSelection[3], InputDirection.Horizontal);
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            HandleEvents(OnInputUp, directionSelection[0], InputDirection.Vertical);
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            HandleEvents(OnInputDown, directionSelection[1], InputDirection.Vertical);
        }
    }

    void HandleEvents(Action onInput,int directionIndex,InputDirection direction)
    {
        onInput?.Invoke();
        
        if (CurrentState.stateDirection != InputDirection.Grid) ChangeSelectionIndex(directionIndex);
        
        if(CanUpdateSelector(direction)) UpdateSelectorUi();
    }
    bool CanUpdateSelector(InputDirection direction)
    {
        return CurrentState.displayingSelector &
               CurrentState.stateDirection == direction;
    }
    void InvokeSelectedEvent()
    {
        if (CurrentState.selectableUis == null) return;
        if (CurrentState.isSelecting)
        {
            if (!CurrentState.selectableUis[CurrentState.currentSelectionIndex].canBeSelected) return;
            CurrentState.selectableUis[CurrentState.currentSelectionIndex]?.eventForUi?.Invoke();
        }
        else
            CurrentState.selectableUis[0]?.eventForUi?.Invoke();
    }
    void UpdateSelectorUi()
    {
        if (!CurrentState.isSelecting) return;
        CurrentState.selector.transform.position = CurrentState.selectableUis[CurrentState.currentSelectionIndex]
            .uiObject.transform.position;
    }
    private void ChangeSelectionIndex(int change)
    {
        CurrentState.currentSelectionIndex =
            Mathf.Clamp(CurrentState.currentSelectionIndex+change, 0, CurrentState.maxSelectionIndex);
        OnSelectionIndexChanged?.Invoke(CurrentState.currentSelectionIndex);
    }
    public void ChangeInputState(InputState newState)
    {
        if (CurrentState.stateName == newState.stateName) return;
        _currentStateLoaded = false;
        
        stateLayers.RemoveAll(s => s.stateName == newState.stateName);
        stateLayers.Add(newState);
        ResetInputEvents();
        CurrentState = newState;
        OnStateChanged?.Invoke(CurrentState);
        HandleStateExitability();
        SetDirectionals();
        if (CurrentState.isSelecting) CurrentState.maxSelectionIndex = CurrentState.selectableUis.Count-1;
        SetupInputEvents();
        if (CurrentState.displayingSelector)
        {
            UpdateSelectorUi();
            CurrentState.selector.SetActive(true);
        }
        
        _currentStateLoaded = true;
        var parentLayers = stateLayers.Where(s => s.isParentLayer).ToList();
        if (parentLayers.Count==0) return;
        parentLayers.ForEach(l=>l.mainViewUI.SetActive(false));
        parentLayers.Last().mainViewUI.SetActive(true);
    }

    private void HandleStateExitability()
    {
        if (CurrentState.UpdateExitStatus == null) return;
        CurrentState.canExit = CurrentState.UpdateExitStatus.Invoke();
    }
    void SetDirectionals()
    {
        switch (CurrentState.stateDirection)
        {
            case InputDirection.None: 
            case InputDirection.Grid:
                return;
            case InputDirection.Horizontal: 
                directionSelection = new[] { 0, 0, -1, 1 };
                break;
            case InputDirection.Vertical: 
                directionSelection = new[] { -1, 1, 0, 0 };
                break;
        }
    }
    void ResetInputEvents()
    {
        OnInputUp = null; OnInputDown = null; OnInputLeft = null; OnInputRight = null;
        OnSelectionIndexChanged = null;
    }

    public void PlayerBagNavigationRestrictions()
    {
        CurrentState.currentSelectionIndex = 0;
        if(_playerBagHandler.numItems==_playerBagHandler.numItemsForView)
        {
            //prevent selecting null item selectables
            CurrentState.maxSelectionIndex = _playerBagHandler.numItems-1;
            UpdateSelectorUi();
        }
        CurrentState.displayingSelector = _playerBagHandler.numItems > 0;
        _playerBagHandler.itemSelector.SetActive(_playerBagHandler.numItems > 0);
        switch (_playerBagHandler.currentBagUsage)
        {
            case BagUsage.SellingView:
                CurrentState.selectableUis.ForEach(s=>s.eventForUi = CreateSellingItemState);
                break;
            case BagUsage.NormalView:
                CurrentState.selectableUis.ForEach(s=>s.eventForUi = _playerBagHandler.UseItem);
                break;
            case BagUsage.SelectionOnly:
                CurrentState.selectableUis.ForEach(s=>s.eventForUi = _playerBagHandler.SelectItemForEvent);
                break;
        }
    }

    public void PlayerBagNavigation()
    {
        if(_itemStorageHandler.currentUsage != ItemUsage.Deposit)
        {
            OnInputLeft += _playerBagHandler.ChangeCategoryLeft;
            OnInputRight += _playerBagHandler.ChangeCategoryRight;
        }
        OnInputUp += _playerBagHandler.NavigateUp;
        OnInputDown += _playerBagHandler.NavigateDown;
        PlayerBagNavigationRestrictions();
    }

    void CreateSellingItemState()
    {
        var itemSellSelectables = new List<SelectableUI>{new(_playerBagHandler.sellingItemUI,_playerBagHandler.SellToMarket,true)};
        ChangeInputState(new (InputStateName.PlayerBagItemSell,
            new[]{InputStateGroup.Bag}, stateDirection:InputDirection.Vertical, selectableUis:itemSellSelectables
            ,selecting:false,onExit:_playerBagHandler.ResetItemSellingUi,onClose:_playerBagHandler.ResetItemSellingUi));
        _playerBagHandler.ChangeQuantity(0);//initial set for visuals
    }

    private void ItemToSellInputs()
    {
        OnInputUp += ()=>_playerBagHandler.ChangeQuantity(1);
        OnInputDown += ()=>_playerBagHandler.ChangeQuantity(-1);
    }
    public void UpdateHealthBarColors()
    {
        for (var i = 0;i<_pokemonPartyHandler.numMembers;i++)
        {
            PokemonOperations.UpdateHealthPhase(_pokemonPartyHandler.party[i], 
                    _pokemonPartyHandler.memberCards[i].hpSliderImage);
        }
    }
    public void PokemonPartyOptions()
    {
        var partyOptionsSelectables = new List<SelectableUI>
        {
            new(_pokemonPartyHandler.partyOptions[0]
                , ()=>_gameUIHandler.ViewPartyPokemonDetails(
                    _pokemonPartyHandler.party[_pokemonPartyHandler.selectedMemberNumber - 1]), true),
            new(_pokemonPartyHandler.partyOptions[1]
                , () => _pokemonPartyHandler.SelectMemberToBeSwapped(_pokemonPartyHandler.selectedMemberNumber)
                , true),
            new(_pokemonPartyHandler.partyOptions[2]
            , _playerBagHandler.OpenBagToGiveItem
            ,!_pokemonPartyHandler.party[_pokemonPartyHandler.selectedMemberNumber - 1].hasItem),
            new(_pokemonPartyHandler.partyOptions[3]
                , () => _playerBagHandler.TakeItem(_pokemonPartyHandler.selectedMemberNumber)
                ,_pokemonPartyHandler.party[_pokemonPartyHandler.selectedMemberNumber - 1].hasItem)
        };
        partyOptionsSelectables.RemoveAll(s=>!s.canBeSelected);
        ChangeInputState(new (InputStateName.PokemonPartyOptions,
            new[]{InputStateGroup.PokemonParty}, stateDirection:InputDirection.Vertical, selectableUis:partyOptionsSelectables
            ,selector:_pokemonPartyHandler.optionSelector,selecting:true,display:true
            ,onClose:_pokemonPartyHandler.ClearSelectionUI,onExit:_pokemonPartyHandler.ClearSelectionUI));
        CurrentState.selector.SetActive(true);
    }

    void SetupPokemonDetails()
    {
        OnInputLeft += _pokemonDetailsHandler.PreviousPage;
        OnInputRight += _pokemonDetailsHandler.NextPage;
        OnInputUp += ()=>_pokemonDetailsHandler.ChangePokemon(-1);
        OnInputDown += ()=>_pokemonDetailsHandler.ChangePokemon(1);
    }

    public void AllowMoveUiNavigation()
    {
        var moveSelectables = new List<SelectableUI>();
        for (var i = 0; i < _pokemonDetailsHandler.currentPokemon.moveSet.Count; i++)
        {
            moveSelectables.Add(new(_pokemonDetailsHandler.moveNamesText[i].gameObject,
                () => _pokemonDetailsHandler.SelectMove(CurrentState.currentSelectionIndex), true));
        }

        Action onExit = null;
        if (_pokemonDetailsHandler.changingMoveData) 
            onExit = () => _pokemonDetailsHandler.OnMoveSelected?.Invoke(-1);

        if (_pokemonDetailsHandler.learningMove)
            onExit = ()=> OnStateRemoved += RemoveDetailsInputStates;
        
        ChangeInputState(new (InputStateName.PokemonDetailsMoveSelection,new[]{InputStateGroup.PokemonDetails},
            stateDirection:InputDirection.Vertical,selectableUis:moveSelectables, 
            selector:_pokemonDetailsHandler.moveSelector, selecting:true, display:true,onExit:onExit));
    }
    void RemoveDetailsInputStates(InputState state)
    {
        if (state.stateName != InputStateName.PokemonDetailsMoveSelection) return;
        OnStateRemoved -= RemoveDetailsInputStates;
        //if started learning but rejected it on move selection screen
        _dialogueOptionsHandler.SkipMove();
        ResetGroupUi(InputStateGroup.PokemonDetails);
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

    private void MoveCoordinatesFullBox(InputDirection direction, int change)
    {
        bool vertical = direction == InputDirection.Vertical;

        if (boxCoordinates[0]==0 && change<0 && vertical)
        {
            OnSelectionIndexChanged += ExitTopRow;
        }
        
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

        CurrentState.currentSelectionIndex =
            Mathf.Clamp(newIndex, 0, CurrentState.maxSelectionIndex);

        OnSelectionIndexChanged?.Invoke(CurrentState.currentSelectionIndex);
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
    private void MoveCoordinatesDynamic(InputDirection direction, int change)
    {
        SetRowRemainder();
        var coordinateIndex = direction == InputDirection.Vertical ? 0 : 1;
        
        var maxIndexForCoordinate  = direction == InputDirection.Vertical ?
            (int)math.ceil((float)_currentNumBoxElements/_numBoxColumns) - 1 : rowRemainder-1;
        
        boxCoordinates[coordinateIndex] = Mathf.Clamp(boxCoordinates[coordinateIndex] + change, 0, maxIndexForCoordinate);
        CurrentState.currentSelectionIndex = _currentNumBoxElements > CurrentState.maxSelectionIndex?
            Mathf.Clamp(GetCurrentBoxPositionDynamic(),0,CurrentState.maxSelectionIndex) 
            :Mathf.Clamp(GetCurrentBoxPositionDynamic(),0,_currentNumBoxElements);
        OnSelectionIndexChanged?.Invoke(CurrentState.currentSelectionIndex);
        UpdateSelectorUi();
    }
    
    void StorageFullBoxNavigation()
    {
        _currentNumBoxElements = pokemon_storage.BoxCapacity;
        _currentBoxCapacity = pokemon_storage.BoxCapacity;
        _numBoxColumns = _pokemonStorageHandler.boxColumns;
        _numBoxRows = pokemon_storage.BoxCapacity / _numBoxColumns;
        OnInputLeft += ()=> MoveCoordinatesFullBox(InputDirection.Horizontal,-1);
        OnInputRight += ()=> MoveCoordinatesFullBox(InputDirection.Horizontal,1);
        OnInputUp += ()=> MoveCoordinatesFullBox(InputDirection.Vertical,-1);
        OnInputDown += ()=> MoveCoordinatesFullBox(InputDirection.Vertical,1);
        
        CurrentState.canExit = false;
        OnSelectionIndexChanged += _pokemonStorageHandler.LoadPokemonData;
        OnSelectionIndexChanged += _pokemonStorageHandler.UpdateBoxPosition;
    }

    public void SetupPokemonStorageState()
    {
        var storageSelectables = new List<SelectableUI>{
            new(_pokemonStorageHandler.storageBoxExit.gameObject,_gameUIHandler.ClosePokemonStorage, true)
        };
        
        ChangeInputState(new (InputStateName.PokemonStorageExit,
            new[] {InputStateGroup.PokemonStorage }, true,_pokemonStorageHandler.storageUI,
            InputDirection.Vertical,storageSelectables,_pokemonStorageHandler.initialSelector, true,display:true,canManualExit:false
            ));
        _pokemonStorageHandler.initialSelector.transform.rotation = Quaternion.Euler(0, 180, 180);
        
        OnInputDown += PokemonStorageBoxChange;
    }

    private void PokemonStorageBoxChange()
    {
        var storageSelectables = new List<SelectableUI>();
        for (int i = 0; i < pokemon_storage.NumBoxes; i++)
        {
            storageSelectables.Add(new(_pokemonStorageHandler.boxTopVisualImage.gameObject,null, true));
        }
        _pokemonStorageHandler.initialSelector.transform.rotation = Quaternion.Euler(0, 0, 0);
        ChangeInputState(new (InputStateName.PokemonStorageBoxChange,
            new[] {InputStateGroup.PokemonStorage }, true,_pokemonStorageHandler.storageUI,
            InputDirection.Horizontal,storageSelectables,_pokemonStorageHandler.initialSelector, selecting:true,display:true,canManualExit:false));

        OnInputUp += SwitchToExit;
        OnInputDown += PokemonStorageBoxNavigation;
        OnInputLeft += () => _pokemonStorageHandler.ChangeBox(-1);
        OnInputRight += () => _pokemonStorageHandler.ChangeBox(1);
    }
    private void PokemonStorageBoxNavigation()
    {
        var storageBoxSelectables = new List<SelectableUI>();
        foreach (var icon in _pokemonStorageHandler.nonPartyIcons)
        { 
            var newSelectable = new SelectableUI(icon.gameObject,
                ()=>_pokemonStorageHandler.SelectNonPartyPokemon(icon.GetComponent<PC_pkm>())
                , true);
            storageBoxSelectables.Add(newSelectable);
        }

        ChangeInputState(new (InputStateName.PokemonStorageBoxNavigation,new[]{InputStateGroup.PokemonStorage}
            ,stateDirection:InputDirection.Grid,selectableUis:storageBoxSelectables,
            selector:_pokemonStorageHandler.initialSelector, selecting:true,display: true,canManualExit:false,canExit:false));
        ChangeSelectionIndex(0);
    }
    private void SwitchToExit()
    {
        if (_pokemonStorageHandler.movingPokemon) return;
        RemoveTopInputLayer(false);
        SetupPokemonStorageState();
    }
    private void ExitTopRow(int index)
    {
        RemoveTopInputLayer(false);
        _pokemonStorageHandler.ClearPokemonData();
        PokemonStorageBoxChange();
    }

    private void PokeMartNavigation()
    {
        OnInputUp += _pokeMartHandler.NavigateUp;
        OnInputDown += _pokeMartHandler.NavigateDown;
        if(_pokeMartHandler.numItemsForView==_pokeMartHandler.numItems)
        {//prevent selecting null item selectables
            CurrentState.maxSelectionIndex = _pokeMartHandler.numItems-1;
        }
        CurrentState.selectableUis.ForEach(s=>s.eventForUi = SelectItemToBuy);
    }

    void SelectItemToBuy()
    { 
        _pokeMartHandler.quantityUI.SetActive(true);
        var itemQuantitySelectables = new List<SelectableUI>
        {
            new(_pokeMartHandler.quantityUI,_pokeMartHandler.BuyItem,true)
        };
        ChangeInputState(new (InputStateName.MartItemPurchase,new[]{InputStateGroup.PokeMart}
            , stateDirection:InputDirection.Vertical, selectableUis:itemQuantitySelectables
            ,selector:_pokeMartHandler.quantitySelector,display: true
            ,onExit: ()=>_pokeMartHandler.selectedItemQuantity=1));
    }

    void ItemToBuyInputs()
    {
        OnInputUp += ()=>_pokeMartHandler.ChangeItemQuantity(1);
        OnInputDown += ()=>_pokeMartHandler.ChangeItemQuantity(-1);
    }
    
    void SetupBattleOptions()
    {
        CurrentState.persistOnExit = true;
        CurrentState.currentSelectionIndex = 0;
        _currentNumBoxElements = 4;
        _currentBoxCapacity = 4;
        _numBoxColumns = 2;
        SetRowRemainder();
        OnInputLeft += ()=>MoveCoordinatesDynamic(InputDirection.Horizontal,-1);
        OnInputRight += ()=>MoveCoordinatesDynamic(InputDirection.Horizontal,1);
        OnInputUp += ()=>MoveCoordinatesDynamic(InputDirection.Vertical,-2);
        OnInputDown += ()=>MoveCoordinatesDynamic(InputDirection.Vertical,2);
    }
    
    void SetupMoveSelection()
    {
        _battleHandler.battleParticipants[_battleHandler.currentEnemyIndex]
            .pokemonImage.color = Color.HSVToRGB(0,0,100);//reset color if cancelled selection
        CurrentState.currentSelectionIndex = 0;
        _currentNumBoxElements = CurrentState.maxSelectionIndex+1;
        _currentBoxCapacity = 4;
        _numBoxColumns = 2;
        SetRowRemainder();
        OnInputLeft += ()=>MoveCoordinatesDynamic(InputDirection.Horizontal,-1);
        OnInputRight += ()=>MoveCoordinatesDynamic(InputDirection.Horizontal,1);
        OnInputUp += ()=>MoveCoordinatesDynamic(InputDirection.Vertical,-2);
        OnInputDown += ()=>MoveCoordinatesDynamic(InputDirection.Vertical,2);
        
        OnInputLeft += ()=> _battleHandler.SelectMove(CurrentState.currentSelectionIndex);
        OnInputRight += () => _battleHandler.SelectMove(CurrentState.currentSelectionIndex);
        OnInputUp += () => _battleHandler.SelectMove(CurrentState.currentSelectionIndex);
        OnInputDown += () => _battleHandler.SelectMove(CurrentState.currentSelectionIndex);
    }

    void SetupEnemySelection()
    {
        _battleHandler.SelectEnemy(3);
        OnInputLeft += ()=> _battleHandler.SelectEnemy(-1);
        OnInputRight += () => _battleHandler.SelectEnemy(1);
    }

    void LoadStoragePokemonData()
    {
        OnSelectionIndexChanged += _pokemonStorageHandler.LoadPokemonData;
    }
    void ShowStorageBoxCapacityData()
    {
        OnSelectionIndexChanged += _pokemonStorageHandler.DisplayBoxCapacity;
    }
    void SetupInputEvents()
    {
        Action stateMethod = CurrentState.stateName switch
        {
            InputStateName.PlayerBagNavigation => PlayerBagNavigation,
            InputStateName.PokemonPartyItemUsage => UpdateHealthBarColors,
            InputStateName.PokemonPartyNavigation => UpdateHealthBarColors,
            InputStateName.PokemonDetails => SetupPokemonDetails,
            InputStateName.PlayerBagItemSell => ItemToSellInputs,
            InputStateName.PokemonStorageBoxNavigation => StorageFullBoxNavigation,
            InputStateName.MartItemNavigation => PokeMartNavigation,
            InputStateName.MartItemPurchase => ItemToBuyInputs,
            InputStateName.PokemonBattleOptions => SetupBattleOptions,
            InputStateName.PokemonBattleMoveSelection => SetupMoveSelection,
            InputStateName.PokemonBattleEnemySelection => SetupEnemySelection,
            InputStateName.PokemonStoragePartyNavigation=>LoadStoragePokemonData,
            InputStateName.PokemonStorageDepositSelection=>ShowStorageBoxCapacityData,
            _ => null
        };
        stateMethod?.Invoke();
    }

}





