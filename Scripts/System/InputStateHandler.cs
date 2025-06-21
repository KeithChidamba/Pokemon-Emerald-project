using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;

public class InputStateHandler : MonoBehaviour
{
    [SerializeField] [CanBeNull] private InputState _currentState;
    public static readonly string[] OmniDirection = {"Horizontal","Vertical"}; 
    public static readonly string[] Vertical = {"Vertical"}; 
    public static readonly string[] Horizontal = {"Horizontal"};
    private int[] directionSelection = { 0, 0, 0, 0 };
    public static InputStateHandler Instance;
    private event Action OnInputUp; 
    private event Action OnInputDown; 
    private event Action OnInputRight; 
    private event Action OnInputLeft;
    private bool _readingInputs = false;
    private bool _handlingState = false;
    public List<InputState> stateLayers;
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
        Game_Load.Instance.OnGameStarted += Inizialize;
    }

    void Inizialize()
    {
        _readingInputs = true;
    }

    public IEnumerator RemoveInputStates(List<InputState> states)
    {
        foreach (var state in states)
        {
            state.selector?.SetActive(false);
            state.OnExit?.Invoke();
            ResetInputEvents();
            var numLayers = stateLayers.Count;
            stateLayers.Remove(state);
            yield return new WaitUntil(() => stateLayers.Count == numLayers - 1);
        }
        if (_handlingState)
            ChangeInputState(stateLayers.Last());
        else
            _currentState =  null;
    }
    private void Update()
    {
        if (!_readingInputs) return;
        _handlingState = stateLayers.Count > 0;
        if (!_handlingState) return;
        
        if (_currentState.stateName == "Movement") return;
        if (Input.GetKeyDown(KeyCode.R) && !Dialogue_handler.Instance.displaying)
        {
            StartCoroutine(RemoveInputStates(new (){stateLayers.Last()} ));
        }
        if (Input.GetKeyDown(KeyCode.F) )
        {
            ExecuteSelectedmethod();
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            OnInputLeft?.Invoke();
            ChangeSelection(directionSelection[2]);
            if(CanUpdateSelector("Horizontal")) UpdateSelector();
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            OnInputRight?.Invoke();
            ChangeSelection(directionSelection[3]);
            if(CanUpdateSelector("Horizontal")) UpdateSelector();
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            OnInputUp?.Invoke();
            ChangeSelection(directionSelection[0]);
            if(CanUpdateSelector("Vertical")) UpdateSelector();
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            OnInputDown?.Invoke();
            ChangeSelection(directionSelection[1]);
            if(CanUpdateSelector("Vertical")) UpdateSelector();
        }
    }

    bool CanUpdateSelector(string directional)
    {
        return _currentState.displayingSelector &
               _currentState.stateDirectionals.Contains(directional);
    }
    void ExecuteSelectedmethod()
    {
        if(_currentState.isSelecting)
            _currentState.selectableUis[_currentState.currentSelectionIndex].eventForUi.Invoke();
        else
            _currentState.selectableUis[0].eventForUi.Invoke();
    }
    void UpdateSelector()
    {
        _currentState.selector.transform.position = _currentState.selectableUis[_currentState.currentSelectionIndex]
            .uiObject.transform.position;
    }
    private void ChangeSelection(int change)
    {
        _currentState.currentSelectionIndex =
            Mathf.Clamp(_currentState.currentSelectionIndex+change, 0, _currentState.maxSelectionIndex);
    }
    public void ChangeInputState(InputState newState)
    {
        if (!stateLayers.Any(s => s.stateName == newState.stateName))
        {
            stateLayers.Add(newState);
        }
        ResetInputEvents();
        _currentState = stateLayers.Last();
        if (_currentState.isSelecting) _currentState.maxSelectionIndex = _currentState.selectableUis.Count-1;
        if (_currentState.stateName != "Movement") SetupInputEvents();
        if(_currentState.displayingSelector)UpdateSelector();
    }

    void ResetInputEvents()
    {
        OnInputUp = null; OnInputDown = null; OnInputLeft = null; OnInputRight = null;
    }
    void PlayerMenu()
    {
        _currentState.selector.SetActive(true);
        directionSelection = new[] { -1, 1, 0, 0 };
    }
    private void PlayerBagNavigation()
    {
        directionSelection = new[] { -1, 1, 0, 0 };
        OnInputUp += Bag.Instance.NavigateUp;
        OnInputDown += Bag.Instance.NavigateDown;
        _currentState.selector = Bag.Instance.itemSelector;

        if (Bag.Instance.sellingItems)
            _currentState.selectableUis[0].eventForUi = SellItemSelection;
        else
            _currentState.selectableUis[0].eventForUi = BagItemUsageSelection;
        
        _currentState.selector.SetActive(true);
        _currentState.maxSelectionIndex = 9;
    }

    void SellItemSelection()
    {
        Options_manager.Instance.SellItem();
        var itemSellSelectables = new List<SelectableUI>{new(null,Bag.Instance.SellToMarket,true)};
        ChangeInputState(new InputState("Player Bag Item Sell", Vertical, itemSellSelectables
            ,null,false,false,StopSellingItems));
        directionSelection = new[] { -1, 1, 0, 0 };
        OnInputUp += ()=>Bag.Instance.ChangeQuantity(1);
        OnInputDown += ()=>Bag.Instance.ChangeQuantity(-1);
    }
    private void StopSellingItems()
    {
        Bag.Instance.sellingItemUI.SetActive(false);
        Bag.Instance.sellingItems = false;
        StartCoroutine(RemoveInputStates(new (){stateLayers.Last()} ));
    }
    void BagItemUsageSelection()
    {
        directionSelection = new[] { 0, 0, -1, 1 };
        var itemUsageSelectables = new List<SelectableUI>
        {
            new(Bag.Instance.itemUsageUi[0],Bag.Instance.UseItem,Bag.Instance.itemUsable)
            ,new(Bag.Instance.itemUsageUi[1],Bag.Instance.GiveItem,Bag.Instance.itemGiveable)
            ,new(Bag.Instance.itemUsageUi[2],Bag.Instance.RemoveItem,Bag.Instance.itemDroppable)
        };
        itemUsageSelectables.RemoveAll(s=>!s.canBeSelected);
        ChangeInputState(new InputState("Player Bag Item Usage", Horizontal, itemUsageSelectables
            ,Bag.Instance.itemUsageSelector,true,true,null));
        _currentState.selector.SetActive(true);
    }
    void SetupInputEvents()
    {
        Action stateMethod = _currentState.stateName switch
        {
            "Player Bag Navigation" => PlayerBagNavigation,
            "Player Menu" => PlayerMenu,
            _ => null
        };
        stateMethod?.Invoke();
    }

}
