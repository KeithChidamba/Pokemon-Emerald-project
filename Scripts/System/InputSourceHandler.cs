using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public enum ControlEvent
{
    Up,Down,Left,Right,UseSpecialItem,OpenMenu,Confirm,Exit,OpenSettings,Save
}
[DefaultExecutionOrder(-1000)]
public class InputSourceHandler : MonoBehaviour, IInjectable
{
    private static Dictionary<ControlEvent, bool> _held = new();
    private static Dictionary<ControlEvent, bool> _pressed = new();
    
    public static bool InputPressed(ControlEvent e) => _pressed[e];
    public static bool InputHeld(ControlEvent e) => _held[e];
    public static bool InputRelease(ControlEvent e) => !_held[e];

    //for use by non-mono classes
    public static event Action<ControlEvent> OnInputPressed;
    public static event Action<ControlEvent> OnInputReleased;
    
    private bool _isMobile;
    private bool _gameStarted;
    public GameObject mobileControlsUI;
    
    private Dialogue_handler _dialogueHandler;
    private InputStateHandler _inputStateHandler;
    private Game_ui_manager _gameUIManager;
    private Game_Load _gameLoadingHandler;
    
    public void Inject(ServiceContainer container)
    {
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _gameUIManager = container.Resolve<Game_ui_manager>();
        _gameLoadingHandler = container.Resolve<Game_Load>();
        
        gameObject.SetActive(true);
    }

    public void OnInject()
    {
        _isMobile = false;
        _gameStarted = false;
        _gameLoadingHandler.OnGameStarted += ()=> _gameStarted = true;
        
        foreach (ControlEvent e in Enum.GetValues(typeof(ControlEvent)))
        {
            _held[e] = false;
            _pressed[e] = false;
        }
    }
    public void DisplayMobileControls(bool canDisplay)
    {
        _isMobile = canDisplay;
        mobileControlsUI.SetActive(canDisplay);
    }

    private bool CanUseQuickAction()
    {
        if(!_gameUIManager.usingUI && !_dialogueHandler.displaying && _gameStarted)
        {
            return _inputStateHandler.currentState.stateName == InputStateName.Empty;
        }
        return false;
    }
    private void Update()
    {
        if (InputPressed(ControlEvent.OpenSettings))
        {
            if (CanUseQuickAction())
            {
                _gameUIManager.ViewGameSettings();
            }
        }
        if (InputPressed(ControlEvent.Save))
        {
            if (CanUseQuickAction())
            {
                _gameUIManager.SaveGame();
            }
        }
       
        if (_isMobile) return;
        if (Input.GetKeyDown(KeyCode.LeftArrow)) TriggerPress(ControlEvent.Left);
        if (Input.GetKeyUp(KeyCode.LeftArrow)) TriggerRelease(ControlEvent.Left);

        if (Input.GetKeyDown(KeyCode.RightArrow)) TriggerPress(ControlEvent.Right);
        if (Input.GetKeyUp(KeyCode.RightArrow)) TriggerRelease(ControlEvent.Right);

        if (Input.GetKeyDown(KeyCode.UpArrow)) TriggerPress(ControlEvent.Up);
        if (Input.GetKeyUp(KeyCode.UpArrow)) TriggerRelease(ControlEvent.Up);
        
        if (Input.GetKeyDown(KeyCode.DownArrow)) TriggerPress(ControlEvent.Down);
        if (Input.GetKeyUp(KeyCode.DownArrow)) TriggerRelease(ControlEvent.Down);
        
        if (Input.GetKeyDown(KeyCode.Z)) TriggerPress(ControlEvent.Confirm);
        if (Input.GetKeyUp(KeyCode.Z)) TriggerRelease(ControlEvent.Confirm);
        
        if (Input.GetKeyDown(KeyCode.X)) TriggerPress(ControlEvent.Exit);
        if (Input.GetKeyUp(KeyCode.X)) TriggerRelease(ControlEvent.Exit);
        
        if (Input.GetKeyDown(KeyCode.C)) TriggerPress(ControlEvent.UseSpecialItem);
        if (Input.GetKeyUp(KeyCode.C)) TriggerRelease(ControlEvent.UseSpecialItem);
        
        if (Input.GetKeyDown(KeyCode.Space)) TriggerPress(ControlEvent.OpenMenu);
        if (Input.GetKeyUp(KeyCode.Space)) TriggerRelease(ControlEvent.OpenMenu);
    }
    public static void TriggerPress(ControlEvent e)
    {
        if (!_held[e]) 
        {
            _pressed[e] = true; // only true on first frame
            OnInputPressed?.Invoke(e);
        }
        _held[e] = true;
    }

    public static void TriggerRelease(ControlEvent e)
    {
        _held[e] = false;
        OnInputReleased?.Invoke(e);
    }
    private void LateUpdate()
    {
        foreach (ControlEvent e in _pressed.Keys.ToList())
        {
            _pressed[e] = false;
        }
    }
}
