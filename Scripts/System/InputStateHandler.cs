using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public interface IInputGroup
{
    public void DetermineOperation();
}
public enum InputDirection { None, Horizontal, Vertical, Grid}
public enum InputStateGroup {None,Bag,PokemonParty,PokemonDetails,PokemonStorage,PokemonBattle,PokeMart, GameSettings,TypingInterface}
public enum InputStateName 
{
    PlaceHolder,DialoguePlaceHolder,Empty,DialogueOptions,
    PokemonBattleMoveSelection,PokemonBattleEnemySelection,PokemonBattleOptions,
    PokemonStorageBoxChange,PokemonStorageExit ,PokemonStorageBoxOptions,PokemonStorageBoxNavigation,PokemonStoragePartyNavigation,
    PokemonStorageUsage,ItemStorageUsage,PokemonStoragePartyOptions,PokemonStorageDepositSelection,
    PokemonDetails, PokemonDetailsMoveSelection ,PokemonDetailsMoveData,
    PlayerBagItemSell,PlayerBagNavigation,
    PokemonPartyOptions,PokemonPartyItemUsage,PokemonPartyNavigation,
    MartItemPurchase,MartItemNavigation,
    PlayerMenu,PlayerProfile,KeyBinds,StartMenu,
    GameSettingsNavigation,GameSettingOptionsNavigation,
    TypingInterfaceNavigation,TypingInterfaceOptions
}
public class InputStateHandler : MonoBehaviour,IInjectable
{
    public InputState currentState;
    private InputState _emptyState;
    private int[] _directionSelection = { 0, 0, 0, 0 };

    public event Action OnInputUp;
    public event Action OnInputDown; 
    public event Action OnInputRight; 
    public event Action OnInputLeft;
    public event Action<InputState> OnStateRemoved;
    public event Action<InputState> OnStateChanged;
    public event Action<int> OnSelectionIndexChanged;
    public event Action<int,bool> OnFullBoxNavigation;
    
    private bool _currentStateLoaded;
    private bool _handlingState;
    [SerializeField]private List<InputState> stateLayers;

    public int[] boxCoordinates={0,0};
    public int currentBoxCapacity;
    public int numBoxRows;
    public int numBoxColumns;
    public int currentNumBoxElements;
    public int rowRemainder;
    public GameObject emptyPlaceHolder;
    
    private Dialogue_handler _dialogueHandler;

    private PlayerBagInputService _playerBagInputService;
    private PokemonBattleInputService _pokemonBattleInputService;
    private PokemartInputService _pokemartInputService;
    private PokemonDetailsInputService _pokemonDetailsInputService;
    private PokemonStorageInputService _pokemonStorageInputService;
    private PokemonPartyInputService _pokemonPartyInputService;
    private GameSettingsInputService _gameSettingsInputService;
    private TypingInterfaceInputService _typingInterfaceInputService;
    private Dictionary<InputStateGroup,IInputGroup> _inputServiceGroups = new ();
    
    public void Inject(ServiceContainer container)
    {
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        
        _playerBagInputService = container.Resolve<PlayerBagInputService>();
        _pokemonPartyInputService = container.Resolve<PokemonPartyInputService>();
        _pokemonBattleInputService = container.Resolve<PokemonBattleInputService>();
        _pokemartInputService = container.Resolve<PokemartInputService>();
        _pokemonDetailsInputService = container.Resolve<PokemonDetailsInputService>();
        _pokemonStorageInputService = container.Resolve<PokemonStorageInputService>();
        _gameSettingsInputService = container.Resolve<GameSettingsInputService>();
        _typingInterfaceInputService = container.Resolve<TypingInterfaceInputService>();
        
        gameObject.SetActive(true);
    }
    
    public void OnInject()
    {
        _emptyState = new InputState(InputStateName.Empty,InputStateGroup.None, canExit: false);
        currentState  ??= _emptyState;
        _currentStateLoaded = true;
        _inputServiceGroups.Add(InputStateGroup.Bag,_playerBagInputService);
        _inputServiceGroups.Add(InputStateGroup.PokemonBattle,_pokemonBattleInputService);
        _inputServiceGroups.Add(InputStateGroup.PokemonDetails,_pokemonDetailsInputService);
        _inputServiceGroups.Add(InputStateGroup.PokemonStorage,_pokemonStorageInputService);
        _inputServiceGroups.Add(InputStateGroup.PokemonParty,_pokemonPartyInputService);
        _inputServiceGroups.Add(InputStateGroup.GameSettings,_gameSettingsInputService);
        _inputServiceGroups.Add(InputStateGroup.PokeMart,_pokemartInputService);
        _inputServiceGroups.Add(InputStateGroup.TypingInterface,_typingInterfaceInputService);
    }
    private void Update()
    {
        _handlingState = stateLayers.Count > 0;
        
        if (!_handlingState) return;
        
       
        bool canExitCurrentDialogue = _dialogueHandler.canExitDialogue & _dialogueHandler.displaying;

        if (InputSourceHandler.InputPressed(ControlEvent.Exit))
        {
            if(currentState.stateName != InputStateName.DialogueOptions && !canExitCurrentDialogue)
            {
                if(currentState.canExit)
                {
                    if (currentState.persistOnExit)
                        currentState.OnExit.Invoke();
                    
                    else if(currentState.canManualExit)
                        RemoveTopInputLayer(true);
                }
            }
        }
        
        if (currentState.stateName == InputStateName.Empty) return;
       
        if (InputSourceHandler.InputPressed(ControlEvent.Confirm) && _currentStateLoaded)
        {
            InvokeSelectedEvent();
        }
        
        if (currentState.stateDirection == InputDirection.None) return;
        
        if (InputSourceHandler.InputPressed(ControlEvent.Left))
        {
            HandleEvents(OnInputLeft, _directionSelection[2], InputDirection.Horizontal);
        }
        if (InputSourceHandler.InputPressed(ControlEvent.Right))
        {
            HandleEvents(OnInputRight, _directionSelection[3], InputDirection.Horizontal);
        }
        if (InputSourceHandler.InputPressed(ControlEvent.Up))
        {
            HandleEvents(OnInputUp, _directionSelection[0], InputDirection.Vertical);
        }
        if (InputSourceHandler.InputPressed(ControlEvent.Down))
        {
            HandleEvents(OnInputDown, _directionSelection[1], InputDirection.Vertical);
        }
    }

    private void HandleEvents(Action onInput,int directionIndex,InputDirection direction)
    {
        onInput?.Invoke();
        
        if (currentState.stateDirection != InputDirection.Grid) ChangeSelectionIndex(directionIndex);
        
        if(CanUpdateSelector(direction)) UpdateSelectorUi();
    }
    private bool CanUpdateSelector(InputDirection direction)
    {
        return currentState.displayingSelector &
               currentState.stateDirection == direction;
    }
    private void InvokeSelectedEvent()
    {
        if (currentState.selectableUis == null) return;
        if (currentState.selectableUis.Count == 0) return;
       
        if (currentState.isSelecting)
        {
            if (!currentState.selectableUis[currentState.currentSelectionIndex].canBeSelected) return;
            currentState.selectableUis[currentState.currentSelectionIndex]?.eventForUi?.Invoke();
        }
        else
        {
            currentState.selectableUis[0]?.eventForUi?.Invoke();
        }
    }
    public void UpdateSelectorUi()
    {
        if (!currentState.isSelecting) return;
        currentState.selector.transform.position = currentState.selectableUis[currentState.currentSelectionIndex]
            .uiObject.transform.position;
    }
    public void ChangeSelectionIndex(int change)
    {
        currentState.currentSelectionIndex =
            Mathf.Clamp(currentState.currentSelectionIndex+change, 0, currentState.maxSelectionIndex);
        OnSelectionIndexChanged?.Invoke(currentState.currentSelectionIndex);
    }
    public void SetSelectionIndex(int newIndex)
    {
        currentState.currentSelectionIndex = Mathf.Clamp(newIndex, 0, currentState.maxSelectionIndex);
        OnSelectionIndexChanged?.Invoke(currentState.currentSelectionIndex);
    }
    public void ChangeInputState(InputState newState,bool forceChange = false)
    {
        if (currentState.stateName == newState.stateName && !forceChange) return;
        _currentStateLoaded = false;
        
        stateLayers.RemoveAll(s => s.stateName == newState.stateName);
        stateLayers.Add(newState);
        ResetInputEvents();
        currentState = newState;
        OnStateChanged?.Invoke(currentState);
        HandleStateExitability();
        SetDirectionals();
        if (currentState.isSelecting)
        {
            currentState.maxSelectionIndex = currentState.selectableUis.Count>0? currentState.selectableUis.Count - 1 : 0;
        }
        SetupInputServices();
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
    private void SetDirectionals()
    {
        switch (currentState.stateDirection)
        {
            case InputDirection.None: 
            case InputDirection.Grid:
                return;
            case InputDirection.Horizontal: 
                _directionSelection = new[] { 0, 0, -1, 1 };
                break;
            case InputDirection.Vertical: 
                _directionSelection = new[] { -1, 1, 0, 0 };
                break;
        }
    }
    private void ResetInputEvents()
    {
        OnInputUp = null; OnInputDown = null; OnInputLeft = null; OnInputRight = null;
        OnSelectionIndexChanged = null;
    }
    
    private void ResetCoordinates()
    {
        boxCoordinates[0] = 0;
        boxCoordinates[1] = 0;
    }
    
    private int GetCurrentFullBoxPosition()
    {
        int row = Mathf.Clamp(boxCoordinates[0], 0, numBoxRows);
        int col = Mathf.Clamp(boxCoordinates[1], 0, numBoxColumns);

        int pos = row * numBoxColumns + col;

        return Mathf.Clamp(pos, 0, currentNumBoxElements);
    }

    public void MoveCoordinatesFullBox(InputDirection direction, int change)
    {
        bool vertical = direction == InputDirection.Vertical;

        OnFullBoxNavigation?.Invoke(change,vertical);
        
        if (vertical)
        {
            boxCoordinates[0] = Mathf.Clamp(
                boxCoordinates[0] + change,
                0,
                numBoxRows
            );
        }
        else
        {
            boxCoordinates[1] = Mathf.Clamp(
                boxCoordinates[1] + change,
                0,
                numBoxColumns
            );
        }
        
        int newIndex = GetCurrentFullBoxPosition();

        currentState.currentSelectionIndex =
            Mathf.Clamp(newIndex, 0, currentState.maxSelectionIndex);

        OnSelectionIndexChanged?.Invoke(currentState.currentSelectionIndex);
        UpdateSelectorUi();
    }

    public void SetRowRemainder()
    {
        var currentRowRemainder = currentNumBoxElements - (boxCoordinates[0] * numBoxColumns);
        rowRemainder =  (currentRowRemainder < numBoxColumns)? currentRowRemainder: numBoxColumns;
        rowRemainder = Mathf.Clamp(rowRemainder, 0, numBoxColumns);
        boxCoordinates[1] = Mathf.Clamp(boxCoordinates[1], 0, rowRemainder-1);
    }
    private int GetCurrentBoxPositionDynamic()
    {
        SetRowRemainder();
        var currentColumn = boxCoordinates[1];
        var currentRow = boxCoordinates[0];
        var rowCapacity = currentRow * numBoxColumns;
        rowCapacity = Mathf.Clamp(rowCapacity, 0, currentBoxCapacity);
        var val = rowCapacity + Mathf.Clamp(currentColumn, 0, rowRemainder-1);;
        return val;
    }
    public void MoveCoordinatesDynamic(InputDirection direction, int change)
    {
        SetRowRemainder();
        var coordinateIndex = direction == InputDirection.Vertical ? 0 : 1;
        
        var maxIndexForCoordinate  = direction == InputDirection.Vertical ?
            (int)math.ceil((float)currentNumBoxElements/numBoxColumns) - 1 : rowRemainder-1;
        
        boxCoordinates[coordinateIndex] = Mathf.Clamp(boxCoordinates[coordinateIndex] + change, 0, maxIndexForCoordinate);
        
        currentState.currentSelectionIndex = currentNumBoxElements > currentState.maxSelectionIndex?
            Mathf.Clamp(GetCurrentBoxPositionDynamic(),0,currentState.maxSelectionIndex) 
            :Mathf.Clamp(GetCurrentBoxPositionDynamic(),0,currentNumBoxElements);
        
        OnSelectionIndexChanged?.Invoke(currentState.currentSelectionIndex);
        UpdateSelectorUi();
    }

    private void SetupInputServices()
    {
        if(_inputServiceGroups.TryGetValue(currentState.stateGroup, out var serviceGroup))
        {
            serviceGroup.DetermineOperation();
        }
    }

    public void AddPlaceHolderState()
    {
        ChangeInputState(new (InputStateName.PlaceHolder,InputStateGroup.None, canExit: false
            , isParent:true,mainView: emptyPlaceHolder));
    }
    public void AddDialoguePlaceHolderState()
    {
        ChangeInputState(new (InputStateName.DialoguePlaceHolder,InputStateGroup.None, canExit: false
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
             if (state.stateGroup==group)
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

    public InputState GetState(InputStateName stateName)
    {
        return stateLayers.Find(state=>state.stateName == stateName);
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
        method?.Invoke();//note: state must not have onexit/onclose that also starts this coroutine,otherwise infinite loop
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
        currentState.OnExit = invokeOnExit? currentState.OnExit:null;
        RemoveInputState(stateLayers.Last() ,true);
    }
}





