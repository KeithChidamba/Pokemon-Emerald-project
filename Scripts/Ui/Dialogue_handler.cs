using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Unity.Mathematics;
using TMPro;
using UnityEngine.Serialization;

public enum DialogType {Details,Options,Event,BattleInfo,BattleDisplayMessage}
public class Dialogue_handler : MonoBehaviour
{
    public Interaction currentInteraction;
    public Overworld_interactable currentInteractionObject;
    [SerializeField] private TMP_Text dialougeText;
    public float typingSpeed = 0.04f;
    public bool dialogueFinished;
    public bool canExitDialogue = true;
    [SerializeField]private bool isOverworldOptionsInteraction = false;
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
        ResetText();
        dialougeText.overflowMode = TextOverflowModes.Page;
    }

    void Update()
    {
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
        ActivateOptions(true);
        var optionSelectables = new List<SelectableUI>();
        
        foreach (var option in _dialogueOptionsManager.currentOptions)
        {
            SelectableUI newOption = new (option.gameObject, () => SelectOption(option.optionIndex), true);
            optionSelectables.Add( newOption);
        }
        
        InputStateHandler.Instance.ChangeInputState(new (InputStateName.DialogueOptions
            ,new[]{InputStateGroup.None},false,null,
            InputDirection.Vertical,optionSelectables,optionSelector,true,true));
        yield return null;
    }
    private void SelectOption(int optionIndex)
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
        newInteraction.dialogueType = type;
        newInteraction.resultMessage = result;
        return newInteraction;
    }
    public void DisplayList(string info,string result,InteractionOptions[] options
        , string[]optionsText)//list info
    {
        canExitDialogue = false;
        var newInteraction = NewInteraction(info,DialogType.Options,result);
        foreach (var option in options)
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
        HandleInteraction(false);
    }    
    public void DisplayDetails(string info)
    {
        messagesLoading = false;
        var newInteraction = NewInteraction(info,DialogType.Details,"");
        currentInteraction = newInteraction;
        HandleInteraction();
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
            SetBattleTextBox();
            yield return TypeText(currentInteraction.interactionMessage);
            yield return new WaitForSeconds(1f);
            pendingMessages.RemoveAt(0);
            yield return new WaitUntil(()=>dialogueFinished);
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
        ResetText();
        displaying = false;
        dialogueFinished = false;
        Player_movement.Instance.AllowPlayerMovement();
        StopCoroutine(ProcessQueue());
    }
    public void StartInteraction(Overworld_interactable interactable)
    {
        Interaction_handler.Instance.DisableInteraction();
        currentInteractionObject = interactable;
        currentInteraction = interactable.interaction;
        if (currentInteraction.dialogueType == DialogType.Event)
        {
            Options_manager.Instance.CompleteEventInteraction(currentInteractionObject);
            return;
        }
        HandleInteraction();
    }
    
    void ResetText()
    {
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
    private IEnumerator TypeText(string message)
    {
        dialogueFinished = false;
        displaying = true; 
        dialougeText.maxVisibleCharacters = 0;
        dialougeText.text = message;
        ResetText();

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
                if (Input.GetKeyDown(KeyCode.X))
                {
                    dialougeText.maxVisibleCharacters = lastChar + 1;
                    break;
                }

                dialougeText.maxVisibleCharacters = i + 1;
                yield return new WaitForSeconds(typingSpeed);
            }

            // Wait for input before next page
            if(totalPages>1) yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Z));
        }

        CompleteDialogueInteraction();
    }

    private void HandleInteraction(bool typeOut=true)
    {
        Player_movement.Instance.RestrictPlayerMovement();
        SetBattleTextBox();
        if(typeOut)
        {
            StartCoroutine(TypeText(currentInteraction.interactionMessage));
        }
        else
        {
            dialougeText.text = currentInteraction.interactionMessage;
            dialougeText.maxVisibleCharacters = dialougeText.text.Length;
        }
    }

    private void SetBattleTextBox()
    {
        if (!Options_manager.Instance.playerInBattle || currentInteraction.dialogueType != DialogType.BattleInfo)
        {
            infoDialogueBox.SetActive(true);
            dialougeText.color=Color.black;
            battleDialogueBox.SetActive(false);
        }
        if( (currentInteraction.dialogueType == DialogType.BattleInfo && Options_manager.Instance.playerInBattle)
            || currentInteraction.dialogueType == DialogType.BattleDisplayMessage)
        {
            battleDialogueBox.SetActive(true);
            dialougeText.color=Color.white;
            infoDialogueBox.SetActive(false);
        }
    }
    private void CompleteDialogueInteraction()
    {
        dialogueFinished = true;
        if (currentInteraction.dialogueType == DialogType.Options)
        {
            isOverworldOptionsInteraction = currentInteractionObject != null;
            CreateDialogueOptions();
        }
    }
}


