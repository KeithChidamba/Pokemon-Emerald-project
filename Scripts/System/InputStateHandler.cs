using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;

public class InputStateHandler : MonoBehaviour
{
    [SerializeField] private InputState _currentState;
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
        if (stateLayers.Count > 0)
            ChangeInputState(stateLayers.Last());
        else
            _currentState =  null;
    }

    public void RemoveTopInputLayer()
    {
        StartCoroutine(RemoveInputStates(new (){stateLayers.Last()} ));
    }
    private void Update()
    {
        if (!_readingInputs) return;
        _handlingState = stateLayers.Count > 0;
        if (!_handlingState) return;
        
        if (_currentState.stateName == "Movement") return;
        if (Input.GetKeyDown(KeyCode.R) && !Dialogue_handler.Instance.displaying)
            RemoveTopInputLayer();
        
        if (Input.GetKeyDown(KeyCode.F) )
        {
            InvokeSelectedEvent();
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            OnInputLeft?.Invoke();
            ChangeSelectionIndex(directionSelection[2]);
            if(CanUpdateSelector("Horizontal")) UpdateSelectorUi();
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            OnInputRight?.Invoke();
            ChangeSelectionIndex(directionSelection[3]);
            if(CanUpdateSelector("Horizontal")) UpdateSelectorUi();
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            OnInputUp?.Invoke();
            ChangeSelectionIndex(directionSelection[0]);
            if(CanUpdateSelector("Vertical")) UpdateSelectorUi();
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            OnInputDown?.Invoke();
            ChangeSelectionIndex(directionSelection[1]);
            if(CanUpdateSelector("Vertical")) UpdateSelectorUi();
        }
    }

    bool CanUpdateSelector(string directional)
    {
        return _currentState.displayingSelector &
               _currentState.stateDirectionals.Contains(directional);
    }
    void InvokeSelectedEvent()
    {
        if(_currentState.isSelecting)
            _currentState.selectableUis[_currentState.currentSelectionIndex].eventForUi.Invoke();
        else
            _currentState.selectableUis[0].eventForUi.Invoke();
    }
    void UpdateSelectorUi()
    {
        _currentState.selector.transform.position = _currentState.selectableUis[_currentState.currentSelectionIndex]
            .uiObject.transform.position;
    }
    private void ChangeSelectionIndex(int change)
    {
        _currentState.currentSelectionIndex =
            Mathf.Clamp(_currentState.currentSelectionIndex+change, 0, _currentState.maxSelectionIndex);
    }
    public void ChangeInputState(InputState newState)
    {
        if (!stateLayers.Any(s => s.stateName == newState.stateName))
            stateLayers.Add(newState);
        ResetInputEvents();
        _currentState = stateLayers.Last();
        SetDirectionals();
        if (_currentState.isSelecting) _currentState.maxSelectionIndex = _currentState.selectableUis.Count-1;
        if (_currentState.stateName != "Movement") SetupInputEvents();
        if (_currentState.displayingSelector)
        {
            UpdateSelectorUi();
            _currentState.selector.SetActive(true);
        }
    }

    void SetDirectionals()
    {
        if (_currentState.stateDirectionals.Contains("Horizontal"))
            directionSelection = new[] { 0, 0, -1, 1 };
        
        if (_currentState.stateDirectionals.Contains("Vertical"))
            directionSelection = new[] { -1, 1, 0, 0 };
        
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
            _currentState.selectableUis.ForEach(s=>s.eventForUi = SelectItemToSell);
        else
            _currentState.selectableUis.ForEach(s=>s.eventForUi = SelectUsageForItem);
    }

    void SelectItemToSell()
    {
        Bag.Instance.sellingItems = true;
        var itemSellSelectables = new List<SelectableUI>{new(null,Bag.Instance.SellToMarket,true)};
        ChangeInputState(new InputState("Player Bag Item Sell", Vertical, itemSellSelectables
            ,null,false,false,()=>Bag.Instance.sellQuantity=1));
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
        ChangeInputState(new InputState("Player Bag Item Usage", Horizontal, itemUsageSelectables
            ,Bag.Instance.itemUsageSelector,true,true,null));
        _currentState.selector.SetActive(true);
    }

    public void PokemonPartyOptions()
    {
        var partyOptionsSelectables = new List<SelectableUI>
        {
            new(Pokemon_party.Instance.partyOptions[0]
                , Pokemon_party.Instance.ViewPokemonDetails, true),
            new(Pokemon_party.Instance.partyOptions[1]
                , () => Pokemon_party.Instance.SelectMemberToBeSwapped(Pokemon_party.Instance.selectedMemberIndex)
                , true),
            new(Pokemon_party.Instance.partyOptions[2]
                , () => Bag.Instance.TakeItem(Pokemon_party.Instance.selectedMemberIndex)
                ,Pokemon_party.Instance.party[Pokemon_party.Instance.selectedMemberIndex - 1].hasItem)
        };
        partyOptionsSelectables.RemoveAll(s=>!s.canBeSelected);
        ChangeInputState(new InputState("Party Pokemon Options", Vertical, partyOptionsSelectables
            ,Pokemon_party.Instance.optionSelector,true,true,Pokemon_party.Instance.ClearSelectionUI));
        _currentState.selector.SetActive(true);
    }

    void SetupInputEvents()
    {
        Action stateMethod = _currentState.stateName switch
        {
            "Player Bag Navigation" => PlayerBagNavigation,
            _ => null
        };
        stateMethod?.Invoke();
    }

}
