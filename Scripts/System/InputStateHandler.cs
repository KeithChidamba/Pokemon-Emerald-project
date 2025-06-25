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
    public void ResetRelevantUi(string keyword)
    {
        List<InputState> sellingUIStates = new List<InputState>();
        foreach (var state in stateLayers)
        {
            if (state.stateName.ToLower().Contains(keyword))
                sellingUIStates.Add(state);
        }
        StartCoroutine(RemoveInputStates(sellingUIStates));
    }
    private IEnumerator RemoveInputStates(List<InputState> states)
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

    public void RemoveTopInputLayer(bool invokeOnExit)
    {
        stateLayers.Last().OnExit = invokeOnExit? stateLayers.Last().OnExit:null;
        StartCoroutine(RemoveInputStates(new (){stateLayers.Last()} ));
    }
    private void Update()
    {
        if (!_readingInputs) return;
        _handlingState = stateLayers.Count > 0;
        if (!_handlingState) return;
        
        if (Input.GetKeyDown(KeyCode.R) && !Dialogue_handler.Instance.displaying)
            RemoveTopInputLayer(true);
        
        if (Input.GetKeyDown(KeyCode.F) )
            InvokeSelectedEvent();

        if (_currentState.stateDirectionals == null) return;
        
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
        if (_currentState.selectableUis == null) return;
        if(_currentState.isSelecting)
            _currentState.selectableUis[_currentState.currentSelectionIndex]?.eventForUi?.Invoke();
        else
            _currentState.selectableUis[0]?.eventForUi?.Invoke();
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
        SetupInputEvents();
        if (_currentState.displayingSelector)
        {
            UpdateSelectorUi();
            _currentState.selector.SetActive(true);
        }

        var parentLayers = stateLayers.Where(s => s.isParentLayer).ToList();
        
        foreach (var layer in parentLayers)
        {
            if (layer == parentLayers.Last())
            {
                layer.mainViewUI.SetActive(true);
                break;
            }
            layer.mainViewUI.SetActive(false);
        }
    }

    void SetDirectionals()
    {
        if (_currentState.stateDirectionals == null) return;
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
        ChangeInputState(new InputState("Player Bag Item Sell",false,null, Vertical, itemSellSelectables
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
        ChangeInputState(new InputState("Player Bag Item Usage",false,null, Horizontal, itemUsageSelectables
            ,Bag.Instance.itemUsageSelector,true,true,null));
        _currentState.selector.SetActive(true);
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
        ChangeInputState(new InputState("Party Pokemon Options",false,null, Vertical, partyOptionsSelectables
            ,Pokemon_party.Instance.optionSelector,true,true,Pokemon_party.Instance.ClearSelectionUI));
        _currentState.selector.SetActive(true);
    }

    void SetupPokemonDetails()
    {
        OnInputLeft += Pokemon_Details.Instance.PreviousPage;
        OnInputRight += Pokemon_Details.Instance.NextPage;
    }
    public void AllowMoveUiNavigation()
    {
        var moveSelectables = new List<SelectableUI>();
        for (var i =0; i < Pokemon_Details.Instance.currentPokemon.moveSet.Count;i++)
        {
            moveSelectables.Add(new(Pokemon_Details.Instance.moves[i].gameObject
                ,ShowMoveDescription,true));
        }
        ChangeInputState(new InputState("Pokemon Details Move Selection",false, null,
            Vertical,moveSelectables, Pokemon_Details.Instance.moveSelector, true, true,null));
    }

    void ShowMoveDescription()
    {
        Pokemon_Details.Instance.SelectMove(_currentState.currentSelectionIndex);
        ChangeInputState(new InputState("Pokemon Details Move Data",false, null,
            null,null, null, false, false,Pokemon_Details.Instance.RemoveMoveDescription));
    }
    void SetupInputEvents()
    {
        Action stateMethod = _currentState.stateName switch
        {
            "Player Bag Navigation" => PlayerBagNavigation,
            "Pokemon Details"=>SetupPokemonDetails,
            _ => null
        };
        stateMethod?.Invoke();
    }

}
