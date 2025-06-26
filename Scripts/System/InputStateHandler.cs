using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;

public class InputStateHandler : MonoBehaviour
{
    [FormerlySerializedAs("_currentState")] public InputState currentState;
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
    public void ResetRelevantUi(string[] keywords)
    {
        List<InputState> inputStates = new List<InputState>();
        
        foreach (var keyword in keywords)
        {
            inputStates.AddRange(GetRelevantStates(keyword));
        }
        StartCoroutine(RemoveInputStates(inputStates));
    }

    private List<InputState> GetRelevantStates(string keyword)
    {
        List<InputState> inputStates = new List<InputState>();
        
        foreach (var state in stateLayers)
            if (state.stateName.ToLower().Contains(keyword.ToLower()))
                inputStates.Add(state);
        
        return inputStates;
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
            currentState =  null;
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
        
        if (Input.GetKeyDown(KeyCode.R) && !Dialogue_handler.Instance.displaying && currentState.canExit)
            RemoveTopInputLayer(true);
        
        if (Input.GetKeyDown(KeyCode.F) )
            InvokeSelectedEvent();

        if (currentState.stateDirectionals == null) return;
        
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
        return currentState.displayingSelector &
               currentState.stateDirectionals.Contains(directional);
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
        if (currentState.stateDirectionals == null) return;
        if (currentState.stateDirectionals.Contains("Horizontal"))
            directionSelection = new[] { 0, 0, -1, 1 };
        
        if (currentState.stateDirectionals.Contains("Vertical"))
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
            currentState.selectableUis.ForEach(s=>s.eventForUi = SelectItemToSell);
        else
            currentState.selectableUis.ForEach(s=>s.eventForUi = SelectUsageForItem);
    }

    void SelectItemToSell()
    {
        Bag.Instance.sellingItems = true;
        var itemSellSelectables = new List<SelectableUI>{new(null,Bag.Instance.SellToMarket,true)};
        ChangeInputState(new InputState("Player Bag Item Sell",false,null, Vertical, itemSellSelectables
            ,null,false,false,()=>Bag.Instance.sellQuantity=1,true));
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
            ,Bag.Instance.itemUsageSelector,true,true,null,true));
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
        ChangeInputState(new InputState("Pokemon Party Options",false,null, Vertical, partyOptionsSelectables
            ,Pokemon_party.Instance.optionSelector,true,true,Pokemon_party.Instance.ClearSelectionUI,true));
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
            onExit = Options_manager.Instance.SkipMove;
        
        ChangeInputState(new InputState("Pokemon Details Move Selection",false, null,
            Vertical,moveSelectables, Pokemon_Details.Instance.moveSelector, true, true,onExit,true));
    }
    void SetupInputEvents()
    {
        Action stateMethod = currentState.stateName switch
        {
            "Player Bag Navigation" => PlayerBagNavigation,
            "Pokemon Details"=>SetupPokemonDetails,
            _ => null
        };
        stateMethod?.Invoke();
    }

}
