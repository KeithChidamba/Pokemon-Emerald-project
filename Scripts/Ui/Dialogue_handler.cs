using System;
using System.Collections;
using System.Collections.Generic;
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
    private bool _overworldInteraction = false;
    public bool displaying;
    [SerializeField] private string currentLineContent = "";
   [SerializeField] private int maxCharacterLength = 90;
    [SerializeField] private int dialogueLength;
    [SerializeField] private int dialogueProgress;
    [SerializeField] private GameObject infoDialogueBox;
    [SerializeField] private GameObject battleDialogueBox;
    [SerializeField] private GameObject clickNextIndicator;
    [SerializeField] private GameObject dialogueExitIndicator;
    [SerializeField] private GameObject dialogueOptionPrefab;
    [SerializeField] private GameObject dialogueOptionBox;
    private DialogueOptionsManager _dialogueOptionsManager;
    [SerializeField] private Transform dialogueUiParent;
    private List<GameObject> _currentDialogueOptions = new();
    public bool messagesLoading = false;
    public List<Interaction> pendingMessages = new();
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
    }

    void Update()
    {
        if (displaying && !dialogueFinished)
        {
            if (Input.GetKeyDown(KeyCode.F))
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
                    if (currentInteraction.interactionType == "Options")
                        CreateDialogueOptions();
                }
            }
        }
        if (overworld_actions.Instance != null)
            if (overworld_actions.Instance.doingAction )
                DisableDialogueExit();
        if (displaying && Input.GetKeyDown(KeyCode.R) && canExitDialogue)
            EndDialogue();
    }

    private void DeletePreviousOptions()
    {
        _dialogueOptionsManager.currentOptions.Clear();
        foreach (var option in _currentDialogueOptions)
            Destroy(option);
        _currentDialogueOptions.Clear();
    }
    private void CreateDialogueOptions()
    {
        dialogueOptionBox.gameObject.SetActive(true);
        DeletePreviousOptions();
        var numOptions = currentInteraction.interactionOptions.Count;
        for(var i = 0; i < numOptions; i++)
        {
            var newOption = Instantiate(dialogueOptionPrefab, dialogueUiParent);
            var optionScript = newOption.GetComponent<DialogueOption>();
            _currentDialogueOptions.Add(newOption);
            optionScript.SetupOption(i,numOptions,currentInteraction.optionsUiText[i]);
            _dialogueOptionsManager.currentOptions.Add(optionScript);
        }
        _dialogueOptionsManager.LoadUiSize();
        ActivateOptions(true);
    } 
    public void SelectOption(int optionIndex)
    {
        ActivateOptions(false);
        if(_overworldInteraction)
            Options_manager.Instance.CompleteInteraction(currentInteractionObject,optionIndex);
        else
            Options_manager.Instance.CompleteInteraction(currentInteraction,optionIndex);
        _overworldInteraction = false;
        DeletePreviousOptions();
    }
    private void ActivateOptions(bool display)
     {
         dialogueOptionBox.gameObject.SetActive(display);
         foreach (var obj in _dialogueOptionsManager.currentOptions)
             obj.gameObject.SetActive(display);
     }
    private void DisableDialogueExit()
    {
        dialogueExitIndicator.SetActive(false);
        canExitDialogue = false;
    }
    Interaction NewInteraction(string info,string type,string result)
    {
        var newInteraction = ScriptableObject.CreateInstance<Interaction>();
        newInteraction.interactionMessage = info;
        newInteraction.interactionType = type;
        newInteraction.resultMessage = result;
        return newInteraction;
    }
    public void DisplayList(string info,string result,string[] options, string[]optionsText)//list info
    {
        DisableDialogueExit();
        var newInteraction = NewInteraction(info,"Options",result);
        foreach (string option in options)
            newInteraction.interactionOptions.Add(option);
        foreach (string txt in optionsText)
            newInteraction.optionsUiText.Add(txt);
        currentInteraction = newInteraction;
        HandleInteraction();
    }
    
    public void DisplayInfo(string info,string type)
    {
        if(Options_manager.Instance.playerInBattle){ 
            if (!overworld_actions.Instance.usingUI && type == "Feedback")
            {
                DisplayBattleInfo(info);
                return;
            }
        }
        messagesLoading = false;
        var newInteraction = NewInteraction(info,type,"");
        currentInteraction = newInteraction;
        HandleInteraction();
    }
    public void DisplayInfo(string info,string type,float dialogueDuration)
    {
        DisplayInfo(info,type);
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
            DisableDialogueExit();
        DisplayBattleInfo(info);
    }
    public void DisplayBattleInfo(string info)//display plain text info to player
    {
        if (!Options_manager.Instance.playerInBattle)
        {//fail-safe
            DisplayInfo(info,"Details");
            return;
        }
        Battle_handler.Instance.displayingInfo = true;
        DisableDialogueExit();
        var newInteraction = NewInteraction(info,"Battle Info","");
        pendingMessages.Add(newInteraction);
        if(!messagesLoading) StartCoroutine(ProcessQueue(newInteraction));
    }
    private IEnumerator ProcessQueue(Interaction interaction)
    {
        messagesLoading = true;
        //create a duplicate to avoid linking to currentInteraction, because it could delete interaction scrip-object later when nullified
        currentInteraction = NewInteraction(interaction.interactionMessage,"Battle Info","");
        HandleInteraction();
        pendingMessages.Remove(interaction);
        yield return new WaitForSeconds(2f);
        ContinueMessageQueue();
    }
    void ContinueMessageQueue()
    {
        if (pendingMessages.Count > 0)
        {
            foreach (var message in new List<Interaction>(pendingMessages))
                StartCoroutine(ProcessQueue(message));
        }
        else
        {
            messagesLoading = false;
            Battle_handler.Instance.displayingInfo = false;
        }
    }

    public void EndDialogue(float delay)
    {
        Invoke(nameof(EndDialogue), delay);
    }
    public void EndDialogue()
    {
        canExitDialogue=true;
        ActivateOptions(false);
        currentInteraction = null;
        infoDialogueBox.SetActive(false);
        battleDialogueBox.SetActive(false);
        currentLineContent = "";
        dialougeText.text = "";
        displaying = false;
        dialogueFinished = false;
        dialogueLength = 0;
        dialogueProgress = 0;
        if(Player_movement.Instance) Player_movement.Instance.canMove = true;
        clickNextIndicator.SetActive(false);
        elipsisSymbol.SetActive(false);
        dialogueExitIndicator.SetActive(false);
        StopAllCoroutines();
        Battle_handler.Instance.displayingInfo = false;
    }

    public void StartInteraction(Overworld_interactable interactable)
    {
        currentInteractionObject = interactable;
        currentInteraction = interactable.interaction;
        _overworldInteraction = true;
        HandleInteraction();
    }
    private void HandleInteraction()
    {
        if(Player_movement.Instance)
        {
            Player_movement.Instance.canMove = false;
            Player_movement.Instance.movingOnFoot = false;
        }
        dialogueFinished = false;
        displaying = true;  
        var numDialoguePages = (float)currentInteraction.interactionMessage.Length / maxCharacterLength;
        var remainder = math.frac(numDialoguePages); 
        dialogueLength = (remainder>0)? (int)math.ceil(numDialoguePages) : (int)numDialoguePages;
        if (!Options_manager.Instance.playerInBattle || currentInteraction.interactionType != "Battle Info")
        {
            infoDialogueBox.SetActive(true);
            dialougeText.color=Color.black;
            battleDialogueBox.SetActive(false);
        }
        if( (currentInteraction.interactionType == "Battle Info" && Options_manager.Instance.playerInBattle)
            || currentInteraction.interactionType == "Battle Display Message")
        {
            battleDialogueBox.SetActive(true);
            dialougeText.color=Color.white;
            infoDialogueBox.SetActive(false);
        }
        if(!Options_manager.Instance.playerInBattle) dialogueExitIndicator.SetActive(canExitDialogue);
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
            if (currentInteraction.interactionType == "Options")
                CreateDialogueOptions();
            if (currentInteraction.interactionType == "Event")
                Options_manager.Instance.CompleteInteraction(currentInteraction,0);
            clickNextIndicator.SetActive(false);
            dialogueLength = 1;
            dialogueProgress = 1;
            dialougeText.text = currentInteraction.interactionMessage;
            dialogueFinished = true;
        }

    }
}
