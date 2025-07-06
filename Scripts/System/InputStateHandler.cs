using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

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
    private bool _readingInputs;
    [SerializeField] private bool _currentStateLoaded;
    private bool _handlingState;
    public List<InputState> stateLayers;
    public enum Directional { None, Horizontal, Vertical, OmniDirection}

    public enum StateGroup {None,Bag,PokemonParty,PokemonDetails,PokemonStorage,PokemonStorageBox,PokemonStorageParty,PokemonBattle,PokeMart }
    public enum StateName {Empty,DialogueOptions,PokemonBattleMoveSelection,PokemonBattleEnemySelection,PokemonBattleOptions
        ,PokemonStoragePartyOptions ,PokemonStorageBoxOptions , PokemonDetailsMoveData }

    public int[] boxCoordinates={0,0};
    private int _currentBoxCapacity;
    private int _numBoxColumns;
    private int _currentNumBoxElements;
    public int rowRemainder;
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
        _emptyState = new InputState(StateName.Empty,StateGroup.None, false, null, Directional.None, null, null,
            false, false, null, null, true);
        currentState = _emptyState;
        _currentStateLoaded = false;
    }

    public void AddPlaceHolderState()
    {
        ChangeInputState(_emptyState);
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

    private List<InputState> GetRelevantStates(StateGroup keyword)
    {
        List<InputState> inputStates = new List<InputState>();
        foreach (var state in stateLayers)
             if (state.stateGroup == keyword)
                inputStates.Add(state);
        
        return inputStates;
    }
    
    private List<InputState> GetRelevantStates(StateName keyword)
    {
        List<InputState> inputStates = new List<InputState>();
        foreach (var state in stateLayers)
            if (state.stateName == keyword)
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
        method?.Invoke();//note: must not have onexit/onclose that also starts this coroutine
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
        if (Input.GetKeyDown(KeyCode.R) && stateLayers.Last().stateName!=StateName.DialogueOptions
                                        && !viewingExitableDialogue && currentState.canExit)
            RemoveTopInputLayer(true);
        
        if (currentState.stateName == StateName.Empty) return;
        
        if (Input.GetKeyDown(KeyCode.F) && _currentStateLoaded)
            InvokeSelectedEvent();

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
        if(currentState.isSelecting)
            currentState.selectableUis[currentState.currentSelectionIndex]?.eventForUi?.Invoke();
        else
            currentState.selectableUis[0]?.eventForUi?.Invoke();
    }
    void UpdateSelectorUi()
    {
        currentState.selector.transform.position = currentState.selectableUis[currentState.currentSelectionIndex]
            .uiObject.transform.position;
    }
    private void ChangeSelectionIndex(int change)
    {
        currentState.currentSelectionIndex =
            Mathf.Clamp(currentState.currentSelectionIndex+change, 0, currentState.maxSelectionIndex);
    }
    public void ChangeInputState(InputState newState)
    {
        ResetCoordinates();
        if (!stateLayers.Any(s => s.stateName == newState.stateName))
            stateLayers.Add(newState);
        ResetInputEvents();
        currentState = stateLayers.Last();
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
    }
    private void PlayerBagNavigation()
    {
        OnInputUp += Bag.Instance.NavigateUp;
        OnInputDown += Bag.Instance.NavigateDown;
        if (Bag.Instance.sellingItems)
            currentState.selectableUis.ForEach(s=>s.eventForUi = SelectItemToSell);
        else
            currentState.selectableUis.ForEach(s=>s.eventForUi = SelectUsageForItem);
    }

    void SelectItemToSell()
    {
        Bag.Instance.sellingItems = true;
        var itemSellSelectables = new List<SelectableUI>{new(Bag.Instance.sellingItemUI,Bag.Instance.SellToMarket,true)};
        ChangeInputState(new InputState("Player Bag Item Sell",StateGroup.Bag,false,null, 
            Directional.Vertical, itemSellSelectables
            ,Bag.Instance.sellingIndicator,false,
            true,null,()=>Bag.Instance.sellQuantity=1,true));
    }

    void ItemToSellInputs()
    {
        OnInputUp += ()=>Bag.Instance.ChangeQuantity(1);
        OnInputDown += ()=>Bag.Instance.ChangeQuantity(-1);
    }
    void SelectUsageForItem()
    {
        var itemUsageSelectables = new List<SelectableUI>
        {
            new(Bag.Instance.itemUsageUi[0],Bag.Instance.UseItem,Bag.Instance.itemUsable)
            ,new(Bag.Instance.itemUsageUi[1],Bag.Instance.GiveItem,Bag.Instance.itemGiveable)
            ,new(Bag.Instance.itemUsageUi[2],Bag.Instance.RemoveItem,Bag.Instance.itemDroppable)
        };
        itemUsageSelectables.RemoveAll(s=>!s.canBeSelected);
        if (itemUsageSelectables.Count == 0)
        {
            Dialogue_handler.Instance.DisplayInfo("Cant use this item right now","Details",1f);
            return;
        }
        ChangeInputState(new InputState("Player Bag Item Usage",StateGroup.Bag,false,null, 
            Directional.Horizontal, itemUsageSelectables
            ,Bag.Instance.itemUsageSelector,true,true,null,null,true));
        
        currentState.selector.SetActive(true);
    }

    public void PokemonPartyOptions()
    {
        var partyOptionsSelectables = new List<SelectableUI>
        {
            new(Pokemon_party.Instance.partyOptions[0]
                , ()=>Game_ui_manager.Instance.ViewPokemonDetails(
                    Pokemon_party.Instance.party[Pokemon_party.Instance.selectedMemberIndex - 1]), true),
            new(Pokemon_party.Instance.partyOptions[1]
                , () => Pokemon_party.Instance.SelectMemberToBeSwapped(Pokemon_party.Instance.selectedMemberIndex)
                , true),
            new(Pokemon_party.Instance.partyOptions[2]
                , () => Bag.Instance.TakeItem(Pokemon_party.Instance.selectedMemberIndex)
                ,Pokemon_party.Instance.party[Pokemon_party.Instance.selectedMemberIndex - 1].hasItem)
        };
        partyOptionsSelectables.RemoveAll(s=>!s.canBeSelected);
        ChangeInputState(new InputState("Pokemon Party Options",StateGroup.PokemonParty,false,null, Directional.Vertical, partyOptionsSelectables
            ,Pokemon_party.Instance.optionSelector,true,true
            ,Pokemon_party.Instance.ClearSelectionUI,Pokemon_party.Instance.ClearSelectionUI,true));
        currentState.selector.SetActive(true);
    }

    void SetupPokemonDetails()
    {
        OnInputLeft += Pokemon_Details.Instance.PreviousPage;
        OnInputRight += Pokemon_Details.Instance.NextPage;
    }

    public void AllowMoveUiNavigation()
    {
        var moveSelectables = new List<SelectableUI>();
        for (var i = 0; i < Pokemon_Details.Instance.currentPokemon.moveSet.Count; i++)
        {
            moveSelectables.Add(new(Pokemon_Details.Instance.moves[i].gameObject,
                () => Pokemon_Details.Instance.SelectMove(currentState.currentSelectionIndex), true));
        }

        Action onExit = null;
//dont use turnary here,these booleans never are true at same time
        if (Pokemon_Details.Instance.changingMoveData) 
            onExit = () => Pokemon_Details.Instance.OnMoveSelected?.Invoke(-1);

        if (Pokemon_Details.Instance.learningMove)
            onExit = ()=> OnStateRemovalComplete += RemoveDetailsInputStates;
        
        ChangeInputState(new InputState("Pokemon Details Move Selection",StateGroup.PokemonDetails,false, null,
            Directional.Vertical,moveSelectables, Pokemon_Details.Instance.moveSelector, true
            , true,null,onExit,true));
    }
    void RemoveDetailsInputStates()
    {
        OnStateRemovalComplete -= RemoveDetailsInputStates;
        //if started learning but rejected it on move selection screen
        Options_manager.Instance.SkipMove();
        ResetRelevantUi(new [] { StateGroup.PokemonDetails });
    }
    private void ResetCoordinates()
    {
        boxCoordinates[0] = 0;
        boxCoordinates[1] = 0;
    }
    private void SetRowRemainder()
    {
        var currentRowRemainder = _currentNumBoxElements - (boxCoordinates[0] * _numBoxColumns);
        rowRemainder =  (currentRowRemainder < _numBoxColumns)? currentRowRemainder: _numBoxColumns;
        rowRemainder = Mathf.Clamp(rowRemainder, 0, _numBoxColumns);
        boxCoordinates[1] = Mathf.Clamp(boxCoordinates[1], 0, rowRemainder-1);
    }
    private int GetCurrentBoxPosition()
    {
        SetRowRemainder();
        var currentColumn = boxCoordinates[1];
        var currentRow = boxCoordinates[0];
        var rowCapacity = currentRow * _numBoxColumns;
        rowCapacity = Mathf.Clamp(rowCapacity, 0, _currentBoxCapacity);
        return rowCapacity + Mathf.Clamp(currentColumn, 0, rowRemainder-1);
    }
    public void MoveCoordinates(Directional directional, int change)
    {
        SetRowRemainder();
        var coordinateIndex = directional == Directional.Vertical ? 0 : 1;
        
        var maxIndexForCoordinate  = directional == Directional.Vertical ?
            (int)math.ceil((float)_currentNumBoxElements/_numBoxColumns) - 1 : rowRemainder-1;
        
        boxCoordinates[coordinateIndex] = Mathf.Clamp(boxCoordinates[coordinateIndex] + change, 0, maxIndexForCoordinate);
        
        currentState.currentSelectionIndex = Mathf.Clamp(GetCurrentBoxPosition(),0,_currentNumBoxElements);
        UpdateSelectorUi();
    }
    void SetupBoxNavigation()
    {
        if (pokemon_storage.Instance.numNonPartyPokemon == 0)
        {
            RemoveTopInputLayer(true);
            return;
        }
        currentState.currentSelectionIndex = 0;
        _currentNumBoxElements = pokemon_storage.Instance.numNonPartyPokemon;
        _currentBoxCapacity = pokemon_storage.Instance.boxCapacity;
        _numBoxColumns = pokemon_storage.Instance.boxColumns;
        SetRowRemainder();
        OnInputLeft += ()=>MoveCoordinates(Directional.Horizontal,-1);
        OnInputRight += ()=>MoveCoordinates(Directional.Horizontal,1);
        OnInputUp += ()=>MoveCoordinates(Directional.Vertical,-1);
        OnInputDown += ()=>MoveCoordinates(Directional.Vertical,1);
    }
    public void PokemonStorageBoxNavigation()
    {
        if (pokemon_storage.Instance.numNonPartyPokemon == 0)
        {
            Dialogue_handler.Instance.DisplayInfo("There are no pokemon in your storage","Details",1f);
            return;
        }
        var storageBoxSelectables = new List<SelectableUI>();
        foreach (var icon in pokemon_storage.Instance.nonPartyIcons)
        { 
            var newSelectable = new SelectableUI(icon,
                ()=>pokemon_storage.Instance.SelectNonPartyPokemon(icon.GetComponent<PC_pkm>())
                , true);
            storageBoxSelectables.Add(newSelectable);
        }
        
        ChangeInputState(new InputState("Pokemon Storage Box Navigation",StateGroup.PokemonStorage,false,null,Directional.OmniDirection
            ,storageBoxSelectables,pokemon_storage.Instance.boxSelector, true, 
            true,null,null,true));

    }
    public void PokemonStoragePartyNavigation()
    {
        var partySelectables = new List<SelectableUI>();

        for (var i = 0 ;i< Pokemon_party.Instance.numMembers;i++)
        {
            var icon = pokemon_storage.Instance.partyPokemonIcons[i];
            partySelectables.Add( new(icon
                ,() => pokemon_storage.Instance.SelectPartyPokemon(icon.GetComponent<PC_party_pkm>()),true) );
        }
        
        ChangeInputState(new InputState("Pokemon Storage Party Navigation",StateGroup.PokemonStorage,false,null,
            Directional.Vertical, partySelectables, pokemon_storage.Instance.partySelector
            , true, true, ()=>pokemon_storage.Instance.swapping = false
            ,()=>pokemon_storage.Instance.swapping = false,true));
    }
    private void PokeMartNavigation()
    {
        OnInputUp += Poke_Mart.Instance.NavigateUp;
        OnInputDown += Poke_Mart.Instance.NavigateDown;

        currentState.selectableUis.ForEach(s=>s.eventForUi = SelectItemToBuy);
    }

    void SelectItemToBuy()
    { 
        Poke_Mart.Instance.quantityUI.SetActive(true);
        var itemQuantitySelectables = new List<SelectableUI>{new(Poke_Mart.Instance.quantityUI,Poke_Mart.Instance.BuyItem,true)};
        ChangeInputState(new InputState("Mart Item Purchase",StateGroup.PokeMart,false,null, Directional.Vertical, itemQuantitySelectables
            ,Poke_Mart.Instance.quantitySelector,false,
            true,null,()=>Poke_Mart.Instance.selectedItemQuantity=1,true));
    }

    void ItemToBuyInputs()
    {
        OnInputUp += ()=>Poke_Mart.Instance.ChangeItemQuantity(1);
        OnInputDown += ()=>Poke_Mart.Instance.ChangeItemQuantity(-1);
    }
    
    void SetupBattleOptions()
    {
        Battle_handler.Instance.ResetAi();
        Dialogue_handler.Instance.DisplayInfo("What will you do?", "Battle Display Message");
        currentState.currentSelectionIndex = 0;
        _currentNumBoxElements = 4;
        _currentBoxCapacity = 4;
        _numBoxColumns = 2;
        SetRowRemainder();
        OnInputLeft += ()=>MoveCoordinates(Directional.Horizontal,-1);
        OnInputRight += ()=>MoveCoordinates(Directional.Horizontal,1);
        OnInputUp += ()=>MoveCoordinates(Directional.Vertical,-2);
        OnInputDown += ()=>MoveCoordinates(Directional.Vertical,2);
    }
    
    void SetupMoveSelection()
    {
        currentState.currentSelectionIndex = 0;
        _currentNumBoxElements = 4;
        _currentBoxCapacity = 4;
        _numBoxColumns = 2;
        SetRowRemainder();
        OnInputLeft += ()=>MoveCoordinates(Directional.Horizontal,-1);
        OnInputRight += ()=>MoveCoordinates(Directional.Horizontal,1);
        OnInputUp += ()=>MoveCoordinates(Directional.Vertical,-2);
        OnInputDown += ()=>MoveCoordinates(Directional.Vertical,2);
        
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
    void SetupInputEvents()
    {
        Action stateMethod = currentState.stateName switch
        {
            "Player Bag Navigation" => PlayerBagNavigation,
            "Pokemon Details"=>SetupPokemonDetails,
            "Player Bag Item Sell"=>ItemToSellInputs,
            "Pokemon Storage Box Navigation"=>SetupBoxNavigation,
            "Mart Item Navigation"=>PokeMartNavigation,
            "Mart Item Purchase"=>ItemToBuyInputs,
            "Pokemon Battle Options"=>SetupBattleOptions,
            "Pokemon Battle Move Selection"=>SetupMoveSelection,
            "Pokemon Battle Enemy Selection"=>SetupEnemySelection,
            _ => null
        };
        stateMethod?.Invoke();
    }

}