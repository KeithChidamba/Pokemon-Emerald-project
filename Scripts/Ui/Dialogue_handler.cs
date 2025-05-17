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
    [SerializeField] private Text dialougeText;
    [SerializeField] GameObject elipsisSymbol;
    [SerializeField] private bool dialogueFinished ;
    public bool canExitDialogue = true;
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
    [SerializeField] private Transform dialogueOptionParent;
    private DialogueOptionsManager _dialogueOptionsManager;
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
        _dialogueOptionsManager = dialogueOptionParent.GetComponent<DialogueOptionsManager>();
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
        dialogueOptionParent.gameObject.SetActive(true);
        DeletePreviousOptions();
        for(var i = 0; i < currentInteraction.interactionOptions.Count; i++)
        {
            var newOption = Instantiate(dialogueOptionPrefab, dialogueOptionParent.parent);
            var optionScript = newOption.GetComponent<DialogueOption>();
            _currentDialogueOptions.Add(newOption);
            optionScript.SetupOption(i,currentInteraction.interactionOptions.Count,currentInteraction.optionsUiText[i]);
            _dialogueOptionsManager.currentOptions.Add(optionScript);
        }
        _dialogueOptionsManager.LoadUiSize();
        ActivateOptions(true);
    } 
    public void SelectOption(int optionIndex)
    {
        if (PokemonOperations.LearningNewMove)
            Options_manager.Instance.selectedNewMoveOption = true;
        ActivateOptions(false);
        Options_manager.Instance.CompleteInteraction(currentInteraction,optionIndex);
    }
    private void ActivateOptions(bool display)
     {
         dialogueOptionParent.gameObject.SetActive(display);
         foreach (var obj in _dialogueOptionsManager.currentOptions)
             obj.gameObject.SetActive(display);
     }
    public void DisableDialogueExit()
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
        var details = NewInteraction(info,"Options",result);
        foreach (string option in options)
            details.interactionOptions.Add(option);
        foreach (string txt in optionsText)
            details.optionsUiText.Add(txt);
        currentInteraction = details;
        Display(currentInteraction);
    }
    
    public void DisplayInfo(string info,string type)
    {
        if(Options_manager.Instance.playerInBattle){ 
            if (!overworld_actions.Instance.usingUI & type == "Feedback")
            {
                DisplayBattleInfo(info);
                return;
            }
        }
        messagesLoading = false;
        Interaction details = NewInteraction(info,type,"");
        currentInteraction = details;
        Display(currentInteraction);
    }
    public void DisplayInfo(string info,string type,float dialogueDuration)
    {
        DisplayInfo(info,type);
        if (Options_manager.Instance.playerInBattle)
        {
            if (overworld_actions.Instance.usingUI)
                EndDialogue(dialogueDuration);
        }
        else
            EndDialogue(dialogueDuration);
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
        var details = NewInteraction(info,"Battle Info","");
        pendingMessages.Add(details);
        if(!messagesLoading)
            StartCoroutine(ProcessQueue(details));
    }
    private IEnumerator ProcessQueue(Interaction interaction)
    {
        messagesLoading = true;
        currentInteraction = NewInteraction(interaction.interactionMessage,"Battle Info","");
        Display(currentInteraction);
        pendingMessages.Remove(interaction);
        yield return new WaitForSeconds(1f);
        reset_message();
    }
    void reset_message()
    {
        if (pendingMessages.Count > 0)
        {
            foreach (Interaction msg in new List<Interaction>(pendingMessages))
                StartCoroutine(ProcessQueue(msg));
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
        if(!Options_manager.Instance.playerInBattle || overworld_actions.Instance.usingUI)
            infoDialogueBox.SetActive(false);
        else
            battleDialogueBox.SetActive(false);
        currentLineContent = "";
        dialougeText.text = "";
        displaying = false;
        dialogueFinished = false;
        dialogueLength = 0;
        dialogueProgress = 0;
        if(Player_movement.instance) Player_movement.instance.canmove = true;
        clickNextIndicator.SetActive(false);
        elipsisSymbol.SetActive(false);
        dialogueExitIndicator.SetActive(false);
        StopAllCoroutines();
        Battle_handler.Instance.displayingInfo = false;
    }
    public void Display(Interaction interaction)
    {
        if(Player_movement.instance)
        {
            Player_movement.instance.canmove = false;
            Player_movement.instance.moving = false;
        }
        dialogueFinished = false;
        displaying = true;  
        var numDialoguePages = (float)interaction.interactionMessage.Length / maxCharacterLength;
        var remainder = math.frac(numDialoguePages); 
        dialogueLength = (remainder>0)? (int)math.ceil(numDialoguePages) : (int)numDialoguePages;
        if (!Options_manager.Instance.playerInBattle || overworld_actions.Instance.usingUI)
        {
            infoDialogueBox.SetActive(true);
            dialougeText.color=Color.black;
            battleDialogueBox.SetActive(false);
        }
        else if(!overworld_actions.Instance.usingUI && Options_manager.Instance.playerInBattle)
        {
            battleDialogueBox.SetActive(true);
            dialougeText.color=Color.white;
            infoDialogueBox.SetActive(false);
        }
        if(!Options_manager.Instance.playerInBattle) dialogueExitIndicator.SetActive(canExitDialogue);
        if (interaction.interactionMessage.Length > maxCharacterLength)
        {
            clickNextIndicator.SetActive(true);
            elipsisSymbol.SetActive(true);
            currentLineContent = interaction.interactionMessage.Substring(0,maxCharacterLength);
            dialougeText.text = currentLineContent;
            dialogueProgress++;
        }
        else
        {
            if (currentInteraction.interactionType == "Options")
                CreateDialogueOptions();
            if (interaction.interactionType == "Event")
                Options_manager.Instance.CompleteInteraction(currentInteraction,0);
            clickNextIndicator.SetActive(false);
            dialogueLength = 1;
            dialogueProgress = 1;
            dialougeText.text = interaction.interactionMessage;
            dialogueFinished = true;
        }

    }
}
