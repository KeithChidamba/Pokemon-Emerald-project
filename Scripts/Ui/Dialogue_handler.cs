using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Unity.Mathematics;
using TMPro;
using UnityEngine.Serialization;

public class Dialogue_handler : MonoBehaviour
{
    public Interaction currentInteraction;
    public Overworld_interactable currentInteractionObject;
    [SerializeField] private Text dialougeText;
    [SerializeField] GameObject elipsisSymbol;
    [SerializeField] private bool dialogueFinished ;
    public bool canExitDialogue = true;
    [SerializeField]private bool isOverworldOptionsInteraction = false;
    public bool displaying;
    [SerializeField] private string currentLineContent = "";
    [SerializeField] private int maxCharacterLength = 90;
    [SerializeField] private int dialogueLength;
    [SerializeField] private int dialogueProgress;
    [SerializeField] private GameObject infoDialogueBox;
    [SerializeField] private GameObject battleDialogueBox;
    [SerializeField] private GameObject clickNextIndicator;
    [SerializeField] private GameObject dialogueOptionPrefab;
    [SerializeField] private GameObject dialogueOptionBox;
    private DialogueOptionsManager _dialogueOptionsManager;
    [SerializeField] private Transform dialogueUiParent;
    private List<GameObject> _currentDialogueOptions = new();
    public bool messagesLoading;
    public enum DialogType {Details,Options,Event,BattleInfo,BattleDisplayMessage}
    public List<Interaction> pendingMessages = new();
    public GameObject optionSelector;
    public event Action OnMessagedDone;
    public static Dialogue_handler Instance;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        _dialogueOptionsManager = dialogueOptionBox.GetComponent<DialogueOptionsManager>();
        Battle_handler.Instance.OnBattleEnd += () => messagesLoading = false;
    }

    void Update()
    {
        if (displaying && !dialogueFinished)
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                if (dialogueProgress < dialogueLength)
                {
                    dialogueProgress++;
                    currentLineContent = currentInteraction.interactionMessage.Substring((dialogueProgress - 1) * maxCharacterLength, maxCharacterLength);    
                    dialougeText.text = currentLineContent;
                    dialogueFinished = false;
                }
                else if (dialogueProgress == dialogueLength)
                {
                    clickNextIndicator.SetActive(false);
                    elipsisSymbol.SetActive(false);
                    currentLineContent = currentInteraction.interactionMessage.Substring(dialogueProgress * maxCharacterLength, currentInteraction.interactionMessage.Length - (dialogueProgress * maxCharacterLength));
                    dialougeText.text = currentLineContent;
                    dialogueFinished = true;
                    if (currentInteraction.interactionType == DialogType.Options)
                        CreateDialogueOptions();
                }
            }
        }
        if (overworld_actions.Instance != null)
            if (overworld_actions.Instance.doingAction)
                canExitDialogue = false;
        if (displaying && Input.GetKeyDown(KeyCode.X) && canExitDialogue)
        {
            StartCoroutine(Player_movement.Instance.AllowPlayerMovement(0.25f));
            EndDialogue();
        }
    }

    public void  DeletePreviousOptions()
    { 
        if( _dialogueOptionsManager.currentOptions.Count == 0) return;  
        ActivateOptions(false);
        InputStateHandler.Instance.RemoveTopInputLayer(true);
        _dialogueOptionsManager.currentOptions.Clear();
        foreach (var option in _currentDialogueOptions)
            Destroy(option);
        _currentDialogueOptions.Clear();
    }
    private void CreateDialogueOptions()
    {
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
        StartCoroutine(SetupDialogueOptionsNavigation());
    }

    private IEnumerator SetupDialogueOptionsNavigation()
    {
        _dialogueOptionsManager.LoadUiSize();
        canExitDialogue = false;
        yield return new WaitForSeconds(0.5f);
        ActivateOptions(true);
        var optionSelectables = new List<SelectableUI>();
         foreach(var option in _dialogueOptionsManager.currentOptions)
            optionSelectables.Add( new(option.gameObject,()=>SelectOption(option.optionIndex),true) );
        
        InputStateHandler.Instance.ChangeInputState(new InputState(InputStateHandler.StateName.DialogueOptions
            ,new[]{InputStateHandler.StateGroup.None},false,null,
            InputStateHandler.Directional.Vertical,optionSelectables,optionSelector,true,true));
    }
    public void SelectOption(int optionIndex)
    {
        if (isOverworldOptionsInteraction)
        {
            isOverworldOptionsInteraction = false;
            Options_manager.Instance.CompleteInteraction(currentInteractionObject,optionIndex);
        }
        else
            Options_manager.Instance.CompleteInteraction(currentInteraction,optionIndex);
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
        newInteraction.interactionType = type;
        newInteraction.resultMessage = result;
        return newInteraction;
    }
    public void DisplayList(string info,string result,string[] options, string[]optionsText)//list info
    {
        canExitDialogue = false;
        var newInteraction = NewInteraction(info,DialogType.Options,result);
        foreach (string option in options)
            newInteraction.interactionOptions.Add(option);
        foreach (string txt in optionsText)
            newInteraction.optionsUiText.Add(txt);
        currentInteraction = newInteraction;
        HandleInteraction();
    }
    public void DisplaySpecific(string info, DialogType type)
    {
        messagesLoading = false;
        var newInteraction = NewInteraction(info,type,"");
        currentInteraction = newInteraction;
        HandleInteraction();
    }    
    public void DisplayDetails(string info)
    {
        messagesLoading = false;
        var newInteraction = NewInteraction(info,DialogType.Details,"");
        currentInteraction = newInteraction;
        HandleInteraction();
    }
    public void DisplayDetails(string info,float dialogueDuration)
    {
        DisplayDetails(info);
        //dont remove this
        if (Options_manager.Instance.playerInBattle)
        {
            if (overworld_actions.Instance.usingUI)
                EndDialogue(dialogueDuration);
        }
        else
            EndDialogue(dialogueDuration);
    }
    public void DisplayBattleInfo(string info, bool canExit)//display plain text info to player
    {
        if(canExit)
            EndDialogue();
        else
            canExitDialogue = false;
        DisplayBattleInfo(info);
    }
    public void DisplayBattleInfo(string info)//display plain text info to player
    {
        if (!Options_manager.Instance.playerInBattle)
        {//fail-safe
            DisplayDetails(info);
            canExitDialogue = true;
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
        InputStateHandler.Instance.AddDialoguePlaceHolderState();
        while (pendingMessages.Count > 0)
        {
            var interaction = pendingMessages[0];
            currentInteraction = NewInteraction(interaction.interactionMessage, DialogType.BattleInfo, "");
            HandleInteraction();
            pendingMessages.RemoveAt(0);
            yield return new WaitForSeconds(2f);
        }
        messagesLoading = false;
        OnMessagedDone?.Invoke();
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
        Interaction_handler.Instance.AllowInteraction();
        canExitDialogue = true;
        currentInteraction = null;
        currentInteractionObject = null;
        infoDialogueBox.SetActive(false);
        battleDialogueBox.SetActive(false);
        currentLineContent = "";
        dialougeText.text = "";
        displaying = false;
        dialogueFinished = false;
        dialogueLength = 0;
        dialogueProgress = 0;
        Player_movement.Instance.AllowPlayerMovement();
        clickNextIndicator.SetActive(false);
        elipsisSymbol.SetActive(false);
        StopCoroutine(ProcessQueue());
    }
    public void StartInteraction(Overworld_interactable interactable)
    {
        Interaction_handler.Instance.DisableInteraction();
        currentInteractionObject = interactable;
        currentInteraction = interactable.interaction;
        HandleInteraction();
    }
    private void HandleInteraction()
    {
        Player_movement.Instance.RestrictPlayerMovement();
        Player_movement.Instance.movingOnFoot = false;

        dialogueFinished = false;
        displaying = true;  
        var numDialoguePages = (float)currentInteraction.interactionMessage.Length / maxCharacterLength;
        var remainder = math.frac(numDialoguePages); 
        dialogueLength = (remainder>0)? (int)math.ceil(numDialoguePages) : (int)numDialoguePages;
        if (!Options_manager.Instance.playerInBattle || currentInteraction.interactionType != DialogType.BattleInfo)
        {
            infoDialogueBox.SetActive(true);
            dialougeText.color=Color.black;
            battleDialogueBox.SetActive(false);
        }
        if( (currentInteraction.interactionType == DialogType.BattleInfo && Options_manager.Instance.playerInBattle)
            || currentInteraction.interactionType == DialogType.BattleDisplayMessage)
        {
            battleDialogueBox.SetActive(true);
            dialougeText.color=Color.white;
            infoDialogueBox.SetActive(false);
        }
        if (currentInteraction.interactionMessage.Length > maxCharacterLength)
        {
            clickNextIndicator.SetActive(true);
            elipsisSymbol.SetActive(true);
            currentLineContent = currentInteraction.interactionMessage.Substring(0,maxCharacterLength);
            dialougeText.text = currentLineContent;
            dialogueProgress++;
        }
        else
        {
            if (currentInteraction.interactionType == DialogType.Options)
            {
                isOverworldOptionsInteraction = currentInteractionObject!=null;
                CreateDialogueOptions();
            }

            if (currentInteraction.interactionType == DialogType.Event)
            {
                Options_manager.Instance.CompleteInteraction(currentInteraction,0);
                return;//trainer battle logic at the moment
            }
            clickNextIndicator.SetActive(false);
            dialogueLength = 1;
            dialogueProgress = 1;
            dialougeText.text = currentInteraction.interactionMessage;
            dialogueFinished = true;
        }

    }
}
