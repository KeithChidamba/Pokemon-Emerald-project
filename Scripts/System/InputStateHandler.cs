using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public interface IInputGroup
{
    public void DetermineOperation();
}
[Serializable]
public class RemovalJob
{
    public InputState state;
    public bool manualExit;

    public RemovalJob(InputState state, bool manualExit)
    {
        this.state = state;
        this.manualExit = manualExit;
    }
}
public enum InputDirection { None, Horizontal, Vertical, Grid}
public enum InputStateGroup {None,Bag,PokemonParty,PokemonDetails,PokemonStorage,PokemonBattle,PokeMart, GameSettings,TypingInterface}
public enum InputStateName 
{
    PlaceHolder,DialoguePlaceHolder,Empty,DialogueOptions,BattleDialoguePlaceHolder,
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
    public bool IsEmptyState =>currentState.stateName == InputStateName.Empty;
    private InputState _emptyState;
    private int[] _directionSelection = { 0, 0, 0, 0 };
    
    public event Action OnInputUp;
    public event Action OnInputDown; 
    public event Action OnInputRight; 
    public event Action OnInputLeft;
    public event Action<InputState> OnStateRemoved;
    public event Action<InputState> OnStateChanged;
    public event Action<InputState> OnStateLoaded;
    public event Action<int> OnSelectionIndexChanged;
    public event Action<int,bool> OnFullBoxNavigation;
    
    private bool _currentStateLoaded;
    private bool _handlingState;
    [SerializeField]private List<InputState> stateLayers;
    [SerializeField]private List<RemovalJob> stateRemovalJobs = new();
    private bool _processingStateRemoval;
    [SerializeField]private  int[] boxCoordinates={0,0};
    [SerializeField]private int currentBoxCapacity;
    [SerializeField]private int numBoxRows;
    [SerializeField]private int numBoxColumns;
    [SerializeField]private int currentNumBoxElements;
    [SerializeField]private int rowRemainder;
    public GameObject emptyPlaceHolder;
    
    private Dialogue_handler _dialogueHandler;
    private Game_ui_manager _gameUIHandler;
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
        _gameUIHandler = container.Resolve<Game_ui_manager>();
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
        _emptyState = new InputState(InputStateName.Empty,InputStateGroup.None, canExit: false,isParent:true,mainView:emptyPlaceHolder);
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

        if (InputSourceHandler.InputPressed(ControlEvent.Exit))
        {
            if (!_dialogueHandler.HandlingStateExit(currentState))
            {
                //handle state normally
                if (currentState.canExit)
                {
                    if (currentState.persistOnExit)
                        currentState.onExit.Invoke();

                    else if (currentState.canManualExit)
                        RemoveTopInputLayer(true);
                }
            }
        }
        
        if (IsEmptyState) return;
       
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
            Mathf.Clamp(currentState.currentSelectionIndex+change, 0, currentState.maxSelectableIndex);
        OnSelectionIndexChanged?.Invoke(currentState.currentSelectionIndex);
    }
    public void SetSelectionIndex(int newIndex)
    {
        currentState.currentSelectionIndex = Mathf.Clamp(newIndex, 0, currentState.maxSelectableIndex);
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
            currentState.maxSelectableIndex = currentState.selectableUis.Count>0? currentState.selectableUis.Count - 1 : 0;
        }
        
        if(_inputServiceGroups.TryGetValue(currentState.stateGroup, out var serviceGroup))
        {
            serviceGroup.DetermineOperation();
        }
        
        if (currentState.displayingSelector)
        {
            UpdateSelectorUi();
            currentState.selector.SetActive(true);
        }
        
        if(!currentState.isParentLayer)
        {
            currentState.mainViewUI?.SetActive(true);
        }
        
        _currentStateLoaded = true;
        OnStateLoaded?.Invoke(currentState);
        
        var parentLayers = stateLayers.Where(s => s.isParentLayer).ToList();
        if (parentLayers.Count==0) return;
        
        if (currentState.displayOpenTransition)
        {
            StartCoroutine(PlayTransition(HandleParentDisplay));
        }
        else
        {
            HandleParentDisplay();
        }
        void HandleParentDisplay()
        {
            parentLayers.ForEach(l=>l.mainViewUI.SetActive(false));
            parentLayers.Last().mainViewUI.SetActive(true);
        }
    }

    private void HandleStateExitability()
    {
        if (currentState.updateExitStatus == null) return;
        currentState.canExit = currentState.updateExitStatus.Invoke();
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

    public int GetCoordinate(bool vertical)
    {
        return vertical ? boxCoordinates[0] : boxCoordinates[1];
    }
    private int GetCurrentFullBoxPosition()
    {
        int row = Mathf.Clamp(boxCoordinates[0], 0, numBoxRows);
        int col = Mathf.Clamp(boxCoordinates[1], 0, numBoxColumns);

        int pos = row * numBoxColumns + col;

        return Mathf.Clamp(pos, 0, currentNumBoxElements);
    }

    public void SetupFullBoxNavigation(int numBoxElements,int boxCapacity,int numColumns)
    {
        currentNumBoxElements = numBoxElements;
        currentBoxCapacity = boxCapacity;
        numBoxColumns = numColumns;
        numBoxRows = boxCapacity / numColumns;
        OnInputLeft += ()=> MoveCoordinatesFullBox(InputDirection.Horizontal,-1);
        OnInputRight += ()=> MoveCoordinatesFullBox(InputDirection.Horizontal,1);
        OnInputUp += ()=> MoveCoordinatesFullBox(InputDirection.Vertical,-1);
        OnInputDown += ()=> MoveCoordinatesFullBox(InputDirection.Vertical,1);
    }
    private void MoveCoordinatesFullBox(InputDirection direction, int change)
    {
        bool vertical = direction == InputDirection.Vertical;

        OnFullBoxNavigation?.Invoke(change,vertical);
        
        if (vertical)
        {
            boxCoordinates[0] = Mathf.Clamp(
                boxCoordinates[0] + change,
                0,
                numBoxRows-1
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
            Mathf.Clamp(newIndex, 0, currentState.maxSelectableIndex);

        OnSelectionIndexChanged?.Invoke(currentState.currentSelectionIndex);
        UpdateSelectorUi();
    }
    public void SetupDynamicBoxNavigation(int numBoxElements,int boxCapacity,int numColumns)
    {
        currentNumBoxElements = numBoxElements;
        currentBoxCapacity = boxCapacity;
        numBoxColumns = numColumns;
        SetRowRemainder();
        OnInputLeft += ()=>MoveCoordinatesDynamic(InputDirection.Horizontal,-1);
        OnInputRight += ()=>MoveCoordinatesDynamic(InputDirection.Horizontal,1);
        OnInputUp += ()=>MoveCoordinatesDynamic(InputDirection.Vertical,-1);
        OnInputDown += ()=>MoveCoordinatesDynamic(InputDirection.Vertical,1);
    }
    private int GetCurrentBoxPositionDynamic()
    {
        SetRowRemainder();
        var currentColumn = boxCoordinates[1];
        var currentRow = boxCoordinates[0];
        var rowCapacity = currentRow * numBoxColumns;
        rowCapacity = Mathf.Clamp(rowCapacity, 0, currentBoxCapacity);
        var val = rowCapacity + Mathf.Clamp(currentColumn, 0, rowRemainder-1);
        return val;
    }
    private void MoveCoordinatesDynamic(InputDirection direction, int change)
    {
        SetRowRemainder();
        var coordinateIndex = direction == InputDirection.Vertical ? 0 : 1;
        
        var maxIndexForCoordinate  = direction == InputDirection.Vertical ?
            (int)math.ceil((float)currentNumBoxElements/numBoxColumns) - 1 : rowRemainder-1;
        
        boxCoordinates[coordinateIndex] = Mathf.Clamp(boxCoordinates[coordinateIndex] + change, 0, maxIndexForCoordinate);
        
        currentState.currentSelectionIndex = currentNumBoxElements > currentState.maxSelectableIndex?
            Mathf.Clamp(GetCurrentBoxPositionDynamic(),0,currentState.maxSelectableIndex) 
            :Mathf.Clamp(GetCurrentBoxPositionDynamic(),0,currentNumBoxElements-1);
        
        OnSelectionIndexChanged?.Invoke(currentState.currentSelectionIndex);
        UpdateSelectorUi();
    }
    private void SetRowRemainder()
    {
        var currentRowRemainder = currentNumBoxElements - (boxCoordinates[0] * numBoxColumns);
        rowRemainder =  (currentRowRemainder < numBoxColumns)? currentRowRemainder: numBoxColumns;
        rowRemainder = Mathf.Clamp(rowRemainder, 0, numBoxColumns);
        boxCoordinates[1] = Mathf.Clamp(boxCoordinates[1], 0, rowRemainder-1);
    }
    // Placeholders / Input blockers
    public void AddPlaceHolderState()
    {
        ChangeInputState(new (InputStateName.PlaceHolder,InputStateGroup.None, canExit: false
            , isParent:false,mainView: emptyPlaceHolder,
            displayOpenTransition:false,displayCloseTransition:false));
    }
    public void AddBattleDialoguePlaceHolderState()
    {
        ChangeInputState(new (InputStateName.BattleDialoguePlaceHolder,InputStateGroup.None, canExit: false
            , isParent:false,mainView: emptyPlaceHolder,
            displayOpenTransition:false,displayCloseTransition:false),true);
    }
    public void AddDialoguePlaceHolderState()
    {
        ChangeInputState(new (InputStateName.DialoguePlaceHolder,InputStateGroup.None, canExit: false
            , isParent:false,mainView: emptyPlaceHolder,displayOpenTransition:false
            ,displayCloseTransition:false));
    }
    //utility
    public InputState GetState(InputStateName stateName)
    {
        //use with context
        return stateLayers.FirstOrDefault(state=>state.stateName == stateName);
    }
    public IEnumerator PlayTransition(Action callBack)
    {
        yield return StartCoroutine(_gameUIHandler.FadeInBlackScreen());
        _gameUIHandler.RemoveBlackScreen();
        callBack?.Invoke();
    }
    private List<InputState> GetRelevantStates(InputStateGroup group)
    {
        List<InputState> inputStates = new List<InputState>();
        foreach (var state in stateLayers)
            if (state.stateGroup==group)
                inputStates.Add(state);
        
        return inputStates;
    }
    public void ResetGridUi(InputStateName stateName)
    {
        var state = stateLayers.FirstOrDefault(state => state.stateName == stateName);
        state?.selector?.SetActive(false);
        if(state?.stateDirection==InputDirection.Grid) ResetCoordinates();
    }
    
    //state removal
    public void ResetGroupUi(InputStateGroup group)
    {
        List<InputState> inputStates = new List<InputState>();
        
        inputStates.AddRange(GetRelevantStates(group));
        AddRemovals(inputStates);
    }
    public void ResetRelevantUi(InputStateName[] stateNames)
    {
        var inputStates = new List<InputState>();

        foreach (var stateName in stateNames)
        {
            var state = GetState(stateName);
            if(state != null) inputStates.Add(state);
        }
        AddRemovals(inputStates);
    }
    public void ResetRelevantUi(InputStateName stateName,bool manualExit=false)
    {
        var state = stateLayers.FirstOrDefault(state => state.stateName == stateName);
        if (state == null) return;
        AddRemoval(state,manualExit);
    }
    public void RemoveTopInputLayer(bool invokeOnExit)
    {
        currentState.onExit = invokeOnExit? currentState.onExit:null;
        AddRemoval(stateLayers.Last(),true);
    }
    private void AddRemoval(InputState statesToRemove,bool manualExit=false)
    {
        AddRemovals(new List<InputState>{statesToRemove},manualExit);
    }
    private void AddRemovals(List<InputState> statesToRemove,bool manualExit=false)
    {
        foreach (var stateToRemove in statesToRemove)
        {
            var removalExists = stateRemovalJobs.Any(s => s.state.stateName == stateToRemove.stateName);
            if (!removalExists)
            {
                stateRemovalJobs.Add(new(stateToRemove,manualExit));
            }
        }
        if (!_processingStateRemoval && stateRemovalJobs.Count > 0)
        {
            StartCoroutine(ProcessStateRemoval());
        }
    }
    private IEnumerator ProcessStateRemoval()
    {
        _processingStateRemoval = true;
        var waitTime = 0f;
        var displayingBlackScreen = false;
        
        bool IsParentWithTransition(RemovalJob job)
        {
            return job.manualExit
                   && job.state.isParentLayer
                   && job.state.displayCloseTransition;
        }
        
        while (stateRemovalJobs.Count > 0)
        {
            var currentJob = stateRemovalJobs[0];
            
            //transition
            if (!displayingBlackScreen)
            {
                if(IsParentWithTransition(currentJob))
                {
                    waitTime = 0.2f * stateRemovalJobs.Count;
                    if (waitTime > 1f) waitTime = 1f;
                    displayingBlackScreen = true;
                    yield return StartCoroutine(_gameUIHandler.FadeInBlackScreen());
                }
            }
            
            //removal
            currentJob.state.mainViewUI?.SetActive(false);
            currentJob.state.selector?.SetActive(false);
            
            if(currentJob.state.stateDirection==InputDirection.Grid) ResetCoordinates();
        
            Action method = currentJob.manualExit ? currentJob.state.onExit : currentJob.state.onClose;
            method?.Invoke();
            stateLayers.Remove(currentJob.state);
            OnStateRemoved?.Invoke(currentJob.state);
            stateRemovalJobs.RemoveAt(0);
        }
        
        yield return new WaitForSecondsRealtime(waitTime);
        _gameUIHandler.RemoveBlackScreen();
        _processingStateRemoval = false;

        if (stateLayers.Count > 0)
        {
            var nextLayer = stateLayers.Last();
            nextLayer.displayOpenTransition = false;
            ChangeInputState(nextLayer);
        }
        else
        {
            ChangeInputState(_emptyState);
        }
    }
}





