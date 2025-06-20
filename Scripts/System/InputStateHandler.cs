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

    private void Update()
    {
        if (!_readingInputs) return;
        if (currentState == null) return;
        if (currentState.stateName == "Movement") return;
        if (Input.GetKeyDown(KeyCode.F) )
        {
            if(currentState.isSelecting)
                currentState.InputEvents[currentState.currentSelectionIndex].Invoke();
            else
                currentState.InputEvents[0].Invoke();
            
        }
        if (currentState.stateDirectionals.Contains("Horizontal"))
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                currentState.currentSelectionIndex--;
                OnInputLeft?.Invoke();
            }

            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                currentState.currentSelectionIndex++;
                OnInputRight?.Invoke();
            }
        }

        if (currentState.stateDirectionals.Contains("Vertical"))
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                currentState.currentSelectionIndex--;
                OnInputUp?.Invoke();
            }

            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                currentState.currentSelectionIndex++;
                OnInputDown?.Invoke();
            }
        }

        if (currentState.isSelecting)
        {
            currentState.maxSelectionIndex = currentState.InputEvents.Count-1;
        }
        currentState.currentSelectionIndex =
            Mathf.Clamp(currentState.currentSelectionIndex, 0, currentState.maxSelectionIndex);
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
        currentState.maxSelectionIndex = currentState.InputEvents.Count-1;
    }
    private void PlayerBagNavigation()
    {
        OnInputUp += Bag.Instance.NavigateUp;
        OnInputDown += Bag.Instance.NavigateDown;
        currentState.InputEvents = new List<Action>();
        currentState.InputEvents.Add(BagItemUsageSelection);
        currentState.maxSelectionIndex = 9;
    }
    void BagItemUsageSelection()
    {
        ResetInputEvents();
        currentState = new InputState("Player Bag Item Usage", Horizontal,
            new (){Bag.Instance.UseItem,Bag.Instance.GiveItem,Bag.Instance.RemoveItem},true);
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
