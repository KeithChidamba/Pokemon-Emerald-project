using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum DialogType {Details,Options,Event,BattleInfo,BattleDisplayMessage}
public class Dialogue_handler : MonoBehaviour,IInjectable
{
    public Overworld_interactable currentInteractable;
    [SerializeField] private TMP_Text dialougeText;
    public float typingSpeed = 0.04f;
    public bool dialogueFinished;
    public bool canExitDialogue = true;
    public bool displaying;
    [SerializeField] private GameObject infoDialogueBox;
    public GameObject battleDialogueBox;
    [SerializeField] private GameObject dialogueOptionPrefab;
    [SerializeField] private GameObject dialogueOptionBox;
    [SerializeField] private GameObject objectiveDialogueBox;
    [SerializeField] private TMP_Text objectiveDialougeText;
    private DialogueOptionsManager _dialogueOptionsManager;
    [SerializeField] private Transform dialogueUiParent;
    private List<GameObject> _currentDialogueOptions = new();
    public bool messagesLoading;
    public List<Interaction> pendingMessages = new();
    public GameObject optionSelector;
    private Coroutine _typingRoutine;
    
    public event Action<Overworld_interactable> OnOptionsDisplayed;
    
    private Battle_handler _battleHandler;
    private Player_movement _playerMovementHandler;
    private InputStateHandler _inputStateHandler;
    private DialogueOptionsEventHandler _dialogueOptionsHandler;
    private Interaction_handler  _interactionHandler;
    private Game_ui_manager _gameUIManager;
    public void Inject(ServiceContainer container)
    {
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _battleHandler = container.Resolve<Battle_handler>();
        _dialogueOptionsHandler = container.Resolve<DialogueOptionsEventHandler>();
        _gameUIManager = container.Resolve<Game_ui_manager>();
        _interactionHandler = container.Resolve<Interaction_handler>();
        _playerMovementHandler = container.Resolve<Player_movement>();
        gameObject.SetActive(true);
        OnInject();
    }

    private void OnInject()
    {
        _dialogueOptionsManager = dialogueOptionBox.GetComponent<DialogueOptionsManager>();
        _battleHandler.OnBattleEnd += () => messagesLoading = false;
        ResetText();
        dialougeText.overflowMode = TextOverflowModes.Page;
    }

    void Update()
    {
        if (!displaying) return;
        if (dialogueFinished && InputSourceHandler.InputPressed(ControlEvent.Exit) && canExitDialogue)
        {
            EndDialogue();
            if (_gameUIManager.playerInBattle)
            {
                _battleHandler.EnableBattleMessage(_inputStateHandler.currentState);
            }
            else
            {
                StartCoroutine(_playerMovementHandler.AllowPlayerMovement(0.25f));
            }
        }
    }

    public void SetTextSpeed(int settingIndex)
    {
        typingSpeed = settingIndex switch
        {
            0 => 0.06f,//slow
            1 => 0.04f,//normal
            2 => 0.01f,//fast
            _ => 0.04f
        };
    }
    public void  DeletePreviousOptions()
    { 
        if( _dialogueOptionsManager.currentOptions.Count == 0) return;  
        ActivateOptions(false);
        _inputStateHandler.ResetRelevantUi(new []{InputStateName.DialogueOptions,InputStateName.DialoguePlaceHolder});
        _dialogueOptionsManager.currentOptions.Clear();
        foreach (var option in _currentDialogueOptions)
            Destroy(option);
        _currentDialogueOptions.Clear();
    }
    private void CreateDialogueOptions(Interaction currentInteraction)
    {
        OnOptionsDisplayed?.Invoke(currentInteractable);
        dialogueOptionBox.gameObject.SetActive(true);
        var numOptions = currentInteraction.interactionOptions.Count;
        
        for(var i = 0; i < numOptions; i++)
        {
            var newOption = Instantiate(dialogueOptionPrefab, dialogueUiParent);
            var optionScript = newOption.GetComponent<DialogueOption>();
            _currentDialogueOptions.Add(newOption);
            optionScript.SetupOption(i,numOptions,currentInteraction.optionsUiText[i]);
            _dialogueOptionsManager.currentOptions.Add(optionScript);
        }
        StartCoroutine(SetupDialogueOptionsNavigation(currentInteraction));
    }

    private IEnumerator SetupDialogueOptionsNavigation(Interaction currentInteraction)
    {
        _dialogueOptionsManager.LoadUiSize();
        ActivateOptions(true);
        var optionSelectables = new List<SelectableUI>();
        
        foreach (var option in _dialogueOptionsManager.currentOptions)
        {
            SelectableUI newOption = new (option.gameObject, () => SelectOption(option.optionIndex,currentInteraction), true);
            optionSelectables.Add( newOption);
        }
        
        _inputStateHandler.ChangeInputState(new (InputStateName.DialogueOptions
            ,InputStateGroup.None,false,null,
            InputDirection.Vertical,optionSelectables,optionSelector,true,true));
        yield return null;
    }
    private void SelectOption(int optionIndex,Interaction currentInteraction)
    {
        if (currentInteraction.isEventTrigger)
        {
            _dialogueOptionsHandler.AlertOverworldInteraction(currentInteractable,optionIndex);
            currentInteractable = null;
        }else
        {
            _dialogueOptionsHandler.CompleteInteraction(currentInteraction,optionIndex);
        }
    }
    private void ActivateOptions(bool display)
     {
         dialogueOptionBox.gameObject.SetActive(display);
         foreach (var obj in _dialogueOptionsManager.currentOptions)
             obj.gameObject.SetActive(display);
     }
    Interaction NewInteraction(string info,DialogType type,string result)
    {
        var newInteraction = ScriptableObject.CreateInstance<Interaction>();
        newInteraction.interactionMessage = info;
        newInteraction.dialogueType = type;
        newInteraction.resultMessage = result;
        return newInteraction;
    }
    public void DisplayList(string info,InteractionOptions[] options
        , string[]optionsText,string result="")//list info
    {
        canExitDialogue = false;
        var newInteraction = NewInteraction(info,DialogType.Options,result);
        foreach (var option in options)
            newInteraction.interactionOptions.Add(option);
        foreach (string txt in optionsText)
            newInteraction.optionsUiText.Add(txt);
        
        HandleInteraction(newInteraction);
    }
    public void DisplaySpecific(string info, DialogType type)
    {
        messagesLoading = false;
        var newInteraction = NewInteraction(info,type,"");
        HandleInteraction(newInteraction,false);
    }    
    public void DisplayDetails(string info,bool canExit=true)
    {
        canExitDialogue = canExit;
        messagesLoading = false;
        var newInteraction = NewInteraction(info,DialogType.Details,"");
        HandleInteraction(newInteraction );
    }

    public void DisplayBattleInfo(string info, bool canExit)
    {
        if(canExit)
            EndDialogue();
        else
            canExitDialogue = false;
        DisplayBattleInfo(info);
    }
    public void DisplayBattleInfo(string info)
    {
        if (!_gameUIManager.playerInBattle)
        {//fail-safe
            DisplayDetails(info);
            return;
        }
        canExitDialogue = false;
        var newInteraction = NewInteraction(info,DialogType.BattleInfo,"");
        pendingMessages.Add(newInteraction);
        if (!messagesLoading) StartCoroutine(ProcessQueue());
    }
    private IEnumerator ProcessQueue()
    {
        messagesLoading = true;
        while (pendingMessages.Count > 0)
        {
            _inputStateHandler.AddDialoguePlaceHolderState();
            var interaction = pendingMessages[0];
            var currentInteraction = NewInteraction(interaction.interactionMessage, DialogType.BattleInfo, "");
            SetBattleTextBox(currentInteraction);
            
            _typingRoutine = StartCoroutine(TypeText(currentInteraction));
            yield return _typingRoutine;
            
            yield return new WaitUntil(()=>dialogueFinished);
            yield return new WaitForSecondsRealtime(1f);
            pendingMessages.RemoveAt(0);
        }
        messagesLoading = false;
        
    }
    public void EndDialogue(float delay)
    {
        Invoke(nameof(EndDialogue), delay);
    }
    public void EndDialogue()
    {
        if (_dialogueOptionsManager.currentOptions.Count > 0)
        {
            DeletePreviousOptions();
        }
        _interactionHandler.AllowInteraction();
        canExitDialogue = true;
        
        infoDialogueBox.SetActive(false);
        battleDialogueBox.SetActive(false);
        ResetText();
        displaying = false;
        currentInteractable = null;
        dialogueFinished = false;
        _playerMovementHandler.AllowPlayerMovement();
        StopCoroutine(ProcessQueue());
    }
    public void StartInteraction(Interaction interaction)
    {
        _interactionHandler.DisableInteraction();
       
        if (interaction.dialogueType == DialogType.Event)
        {
            _dialogueOptionsHandler.CompleteEventInteraction(interaction);
            return;
        }
        HandleInteraction(interaction);
    }
    public void StartInteraction(Overworld_interactable interactable)
    {
        currentInteractable = interactable;
        StartInteraction(interactable.interaction);
    }
    void ResetText()
    {
        dialougeText.text = string.Empty;
        dialougeText.ForceMeshUpdate();
        dialougeText.maxVisibleCharacters = 0;
    }

    public void DisplayObjectiveText(string message)
    {
        objectiveDialogueBox.SetActive(true);
        objectiveDialougeText.text = message;
    }

    public void RemoveObjectiveText()
    {
        objectiveDialogueBox.SetActive(false);
    }
    private IEnumerator TypeText(Interaction currentInteraction)
    {
        ResetText();
        dialogueFinished = false;
        displaying = true; 
        dialougeText.text = currentInteraction.interactionMessage;
        dialougeText.ForceMeshUpdate();
        
        int totalPages = dialougeText.textInfo.pageCount;

        for (int page = 1; page <= totalPages; page++)
        {
            dialougeText.pageToDisplay = page;
            dialougeText.maxVisibleCharacters = 0;

            TMP_PageInfo pageInfo = dialougeText.textInfo.pageInfo[page - 1];
            int firstChar = pageInfo.firstCharacterIndex;
            int lastChar = pageInfo.lastCharacterIndex;

            for (int i = firstChar; i <= lastChar; i++)
            {
                // Skip typing
                if (InputSourceHandler.InputPressed(ControlEvent.Exit))
                {
                    dialougeText.maxVisibleCharacters = lastChar + 1;
                    var canExit = canExitDialogue;
                    canExitDialogue = false;
                    yield return new WaitForSecondsRealtime(1f);
                    canExitDialogue = canExit;
                    break;
                }

                dialougeText.maxVisibleCharacters = i + 1;
                yield return new WaitForSecondsRealtime(typingSpeed);
            }

            // Wait for input before next page
            if(totalPages>1) yield return new WaitUntil(() =>InputSourceHandler.InputPressed(ControlEvent.Confirm));
        }
        dialogueFinished = true;
        CompleteDialogueInteraction(currentInteraction);
        _typingRoutine = null;
    }

    private void HandleInteraction(Interaction currentInteraction,bool typeOut=true)
    {
        if (currentInteraction.dialogueType == DialogType.Options)
        {
            canExitDialogue = false;
        }
        _playerMovementHandler.RestrictPlayerMovement();
        SetBattleTextBox(currentInteraction);
        if (typeOut)
        {
            // Stop ONLY the typing coroutine
            if (_typingRoutine != null)
            {
                StopCoroutine(_typingRoutine);
            }
            _typingRoutine = StartCoroutine(TypeText(currentInteraction));
        }
        else
        {
            dialougeText.text = currentInteraction.interactionMessage;
            dialougeText.maxVisibleCharacters = dialougeText.text.Length;
        }
    }

    private void SetBattleTextBox(Interaction currentInteraction)
    {
        if (!_gameUIManager.playerInBattle || currentInteraction.dialogueType != DialogType.BattleInfo)
        {
            infoDialogueBox.SetActive(true);
            dialougeText.color=Color.black;
            battleDialogueBox.SetActive(false);
        }
        if( (currentInteraction.dialogueType == DialogType.BattleInfo && _gameUIManager.playerInBattle)
            || currentInteraction.dialogueType == DialogType.BattleDisplayMessage)
        {
            battleDialogueBox.SetActive(true);
            dialougeText.color=Color.white;
            infoDialogueBox.SetActive(false);
        }
    }
    private void CompleteDialogueInteraction(Interaction currentInteraction)
    {
        if (currentInteraction == null) return;
        if (currentInteraction.dialogueType == DialogType.Options)
        {
            CreateDialogueOptions(currentInteraction);
        }
    }
}


