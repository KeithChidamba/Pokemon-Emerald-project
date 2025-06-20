using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class InputStateHandler : MonoBehaviour
{
    public InputState currentState;
    public static readonly string[] OmniDirection = {"Horizontal","Vertical"}; 
    public static readonly string[] Vertical = {"Vertical"}; 
    public static readonly string[] Horizontal = {"Horizontal"}; 
    public static InputStateHandler Instance;
    private event Action OnInputUp; 
    private event Action OnInputDown; 
    private event Action OnInputRight; 
    private event Action OnInputLeft;
    private bool _readingInputs = false;

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

    public void ResetInputState()
    {
        currentState.selector?.SetActive(false);
        ResetInputEvents();
        currentState = null;
    }
    private void Update()
    {
        if (!_readingInputs) return;
        if (currentState == null) return;
        if (currentState.stateName == "Movement") return;
        
        if (Input.GetKeyDown(KeyCode.F) )
        {
            currentState.selector?.SetActive(false);
            if(currentState.isSelecting)
                currentState.SelectableUI[currentState.currentSelectionIndex].eventForUi.Invoke();
            else
                currentState.SelectableUI[0].eventForUi.Invoke();
        }
        if (currentState.stateDirectionals.Contains("Horizontal"))
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                ChangeSelection(-1);
                OnInputLeft?.Invoke();
            }

            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                ChangeSelection(1);
                OnInputRight?.Invoke();
            }
        }

        if (currentState.stateDirectionals.Contains("Vertical"))
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                ChangeSelection(-1);
                OnInputUp?.Invoke();
            }

            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                ChangeSelection(1);
                OnInputDown?.Invoke();
            }
        }

        if (currentState.displayingSelector)
            currentState.selector.transform.position = currentState.SelectableUI[currentState.currentSelectionIndex]
                .uiObject.transform.position;
        
        if (currentState.isSelecting)
            currentState.maxSelectionIndex = currentState.SelectableUI.Count-1;
    }

    private void ChangeSelection(int change)
    {
        currentState.currentSelectionIndex =
            Mathf.Clamp(currentState.currentSelectionIndex+change, 0, currentState.maxSelectionIndex);
    }
    public void ChangeInputState(InputState newState)
    {
        ResetInputEvents();
        currentState = newState;
        if (currentState.stateName != "Movement") SetupInputEvents();
    }

    void ResetInputEvents()
    {
        OnInputUp = null; OnInputDown = null; OnInputLeft = null; OnInputRight = null;
    }
    void PlayerMenu()
    {
        currentState.maxSelectionIndex = currentState.SelectableUI.Count-1;
        currentState.selector.SetActive(true);
    }
    private void PlayerBagNavigation()
    {
        OnInputUp += Bag.Instance.NavigateUp;
        OnInputDown += Bag.Instance.NavigateDown;
        currentState.selector = Bag.Instance.itemSelector;
        currentState.selector.SetActive(true);
        currentState.SelectableUI[0].eventForUi = BagItemUsageSelection;
        currentState.maxSelectionIndex = 9;
        
    }
    void BagItemUsageSelection()
    {
        ResetInputEvents();
        var itemUsageSelectables = new List<SelectableUI>
        {
            new(Bag.Instance.itemUsageUi[0],Bag.Instance.UseItem,Bag.Instance.itemUsable)
            ,new(Bag.Instance.itemUsageUi[1],Bag.Instance.GiveItem,Bag.Instance.itemGiveable)
            ,new(Bag.Instance.itemUsageUi[2],Bag.Instance.RemoveItem,Bag.Instance.itemDroppable)
        };
        itemUsageSelectables.RemoveAll(s=>!s.canBeSelected);
        currentState = new InputState("Player Bag Item Usage", Horizontal, itemUsageSelectables
            ,Bag.Instance.itemUsageSelector,true,true);
        currentState.selector.SetActive(true);
    }
    void SetupInputEvents()
    {
        Action stateMethod = currentState.stateName switch
        {
            "Player Bag Navigation" => PlayerBagNavigation,
            "Player Menu" => PlayerMenu,
            _ => null
        };
        stateMethod?.Invoke();
    }

}
