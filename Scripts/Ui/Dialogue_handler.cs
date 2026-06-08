using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Mathematics;

public enum DialogType {Details,Options,Event,BattleInfo,BattleDisplayMessage,CustomOptions}
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
    [SerializeField] private LoopingUiAnimation endOfDialoguePointer;
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
    public event Action OnDialogueEnded;
    private Battle_handler _battleHandler;
    private Player_movement _playerMovementHandler;
    private InputStateHandler _inputStateHandler;
    private DialogueOptionsEventHandler _dialogueOptionsHandler;
    private Interaction_handler  _interactionHandler;

    public void Inject(ServiceContainer container)
    {
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _battleHandler = container.Resolve<Battle_handler>();
        _dialogueOptionsHandler = container.Resolve<DialogueOptionsEventHandler>();
        _interactionHandler = container.Resolve<Interaction_handler>();
        _playerMovementHandler = container.Resolve<Player_movement>();
        gameObject.SetActive(true);
    }

    public void OnInject()
    {
        endOfDialoguePointer.LoadState(false);
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
            if (_battleHandler.battleInProgress)
            {
                _battleHandler.EnableBattleMessage(_inputStateHandler.currentState);
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
    public void DisplayCustomOptions(string info, string[]optionsText, Action[] optionEvents,string result="")
    {
        _dialogueOptionsHandler.OnInteractionOptionChosen += InvokeSelectedOption;
        
        canExitDialogue = false;
        var newInteraction = NewInteraction(info,DialogType.CustomOptions,result);
        foreach (string text in optionsText) 
        {
            newInteraction.interactionOptions.Add(InteractionOptions.Custom);
            newInteraction.optionsUiText.Add(text);
        }
        
        HandleInteraction(newInteraction);
        return;
        
        void InvokeSelectedOption(Interaction interaction,int optionIndex)
        {
            DeletePreviousOptions();
            optionEvents[optionIndex]?.Invoke();
            _dialogueOptionsHandler.OnInteractionOptionChosen -= InvokeSelectedOption;
        }
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
        if (!_battleHandler.battleInProgress)
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
            var interaction = pendingMessages[0];
            var currentInteraction = NewInteraction(interaction.interactionMessage, DialogType.BattleInfo, "");
            SetBattleTextBox(currentInteraction);
            _inputStateHandler.AddBattleDialoguePlaceHolderState();
            _typingRoutine = StartCoroutine(TypeText(currentInteraction,false));
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
        DeletePreviousOptions();
        _interactionHandler.AllowInteraction();
        canExitDialogue = true;
        endOfDialoguePointer.ChangeActiveState(false);
        endOfDialoguePointer.gameObject.SetActive(false);
        infoDialogueBox.SetActive(false);
        battleDialogueBox.SetActive(false);
        ResetText();
        displaying = false;
        currentInteractable = null;
        dialogueFinished = false;
        StopCoroutine(ProcessQueue());
        OnDialogueEnded?.Invoke();
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

    public void DisplayObjectiveText(string message)
    {
        objectiveDialogueBox.SetActive(true);
        objectiveDialougeText.text = message;
    }

    public void RemoveObjectiveText()
    {
        objectiveDialogueBox.SetActive(false);
    }
    void ResetText()
    {
         dialougeText.text = string.Empty;
         dialougeText.ForceMeshUpdate();
         dialougeText.maxVisibleCharacters = 0;
    }
    private IEnumerator TypeText(Interaction currentInteraction,bool displayPointer=true)
    {
        ResetText();
        dialogueFinished = false;
        displaying = true; 
        dialougeText.text = currentInteraction.interactionMessage;
        dialougeText.ForceMeshUpdate();
        
        int totalPages = dialougeText.textInfo.pageCount;

        for (int page = 1; page <= totalPages; page++)
        {
            if (displayPointer)
            {
                endOfDialoguePointer.ChangeActiveState(false);
                endOfDialoguePointer.gameObject.SetActive(false);
            }

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
            int visibleChar = lastChar;

            while (visibleChar >= firstChar && !dialougeText.textInfo.characterInfo[visibleChar].isVisible)
            {
                visibleChar--;
            }
            yield return null;

            dialougeText.ForceMeshUpdate();

            var charInfo = dialougeText.textInfo.characterInfo[lastChar];
            
            if (displayPointer)
            {
                var worldPos = dialougeText.rectTransform.TransformPoint(charInfo.bottomRight);
                //fine-tuning visual off-sets
                worldPos = new Vector2(worldPos.x+35f, worldPos.y + math.abs(worldPos.y*.2f));
                
                var parentRect = (RectTransform)dialougeText.rectTransform.parent;

                var parentSpacePos = parentRect.InverseTransformPoint(worldPos);
                
                endOfDialoguePointer.SetStartPosition(parentSpacePos);
                endOfDialoguePointer.gameObject.SetActive(true);
                endOfDialoguePointer.ChangeActiveState(true);
            }
           
            // Wait for input before next page
            if(totalPages > 1 && page < totalPages) yield return new WaitUntil(() =>InputSourceHandler.InputPressed(ControlEvent.Confirm));
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
        _playerMovementHandler.RestrictPlayerMovement(MovementRestrictor.Dialogue);
        SetBattleTextBox(currentInteraction);
        if (typeOut)
        {
            // Stop ONLY the typing coroutine
            if (_typingRoutine != null)
            {
                StopCoroutine(_typingRoutine);
            }
            _inputStateHandler.AddDialoguePlaceHolderState();
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
        if (!_battleHandler.battleInProgress || currentInteraction.dialogueType != DialogType.BattleInfo)
        {
            infoDialogueBox.SetActive(true);
            dialougeText.color=Color.black;
            battleDialogueBox.SetActive(false);
        }
        if( (currentInteraction.dialogueType == DialogType.BattleInfo && _battleHandler.battleInProgress)
            || currentInteraction.dialogueType == DialogType.BattleDisplayMessage)
        {
            battleDialogueBox.SetActive(true);
            dialougeText.color=Color.white;
            infoDialogueBox.SetActive(false);
        }
    }
    private void CompleteDialogueInteraction(Interaction currentInteraction)
    {
        _inputStateHandler.ResetRelevantUi(InputStateName.DialoguePlaceHolder,true);
        if (currentInteraction.dialogueType == DialogType.Options 
            || currentInteraction.dialogueType == DialogType.CustomOptions)
        {
            CreateDialogueOptions(currentInteraction);
        }
    }
}


