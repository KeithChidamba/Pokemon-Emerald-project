using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Mathematics;
using TMPro;

public class Dialogue_handler : MonoBehaviour
{
    public Interaction Current_interaction;
    public Text Dialouge_txt;
    [SerializeField] GameObject elipsis_txt;
    [SerializeField] bool text_finished = false;
    public bool can_exit = true;
    public bool displaying = false;
    [SerializeField] string current_line = "";
    [SerializeField] int Max_length = 90;
    [SerializeField] float num_lines = 0;
    [SerializeField] int current_line_num = 0;
    [SerializeField] GameObject dialogue_box;
    [SerializeField] GameObject battle_box;
    [SerializeField] GameObject dialogue_next;
    [SerializeField] GameObject dialogue_exit;
    [SerializeField] GameObject[] option_btns;
    [SerializeField] Text[] option_btns_txt;
    public bool messagesLoading = false;
    public List<Interaction> PendingMessages = new();
    public static Dialogue_handler instance;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
    void Update()
    {
        if (Current_interaction != null)
        {
            if (Current_interaction.interactionType == "Options")
            {
                option_btns_txt[0].text =Current_interaction.optionsUiText[0];
                option_btns_txt[1].text =Current_interaction.optionsUiText[1];
            }
        }
        if (displaying && !text_finished)
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                if (current_line_num < num_lines)
                {
                    current_line_num++;
                    current_line = Current_interaction.interactionMessage.Substring((current_line_num - 1) * Max_length, Max_length);    
                    Dialouge_txt.text = current_line;
                    text_finished = false;
                }
                else if (current_line_num == num_lines)
                {
                    dialogue_next.SetActive(false);
                    elipsis_txt.SetActive(false);
                    current_line = Current_interaction.interactionMessage.Substring(current_line_num * Max_length, Current_interaction.interactionMessage.Length - (current_line_num * Max_length));
                    Dialouge_txt.text = current_line;
                    text_finished = true;
                    if (Current_interaction.interactionType == "Options")
                        Display_Options(true);
                }
            }
        }
        if (overworld_actions.Instance !=null)
            if (overworld_actions.Instance.doingAction )
                Remove_Exit();
        if (displaying && Input.GetKeyDown(KeyCode.R) && can_exit)
            Dialouge_off();
    }

    void Remove_Exit()
    {
        dialogue_exit.SetActive(false);
        can_exit = false;
    }
    Interaction new_interaction(string info,string type,string result)
    {
        Interaction inter = ScriptableObject.CreateInstance<Interaction>();
        inter.interactionMessage = info;
        inter.interactionType = type;
        inter.resultMessage = result;
        return inter;
    }
    public void Write_Info(string info,string type,string result,string[] options, string[]options_txt)//list info
    {
        Interaction details = new_interaction(info,type,result);
        foreach (string option in options)
            details.interactionOptions.Add(option);
        foreach (string txt in options_txt)
            details.optionsUiText.Add(txt);
        Current_interaction = details;
        Display(Current_interaction);
    }
    
    public void Write_Info(string info,string type)//display plain text info to player
    {
        if(Options_manager.Instance.playerInBattle){ 
            if (!overworld_actions.Instance.usingUI & type == "Feedback")
            {
                Battle_Info(info);
                return;
            }
        }
        messagesLoading = false;
        Interaction details = new_interaction(info,type,"");
        Current_interaction = details;
        Display(Current_interaction);
    }
    public void Write_Info(string info,string type,float dialogeOff)//display plain text info to player
    {
        Write_Info(info,type);
        if (Options_manager.Instance.playerInBattle)
        {
            if (overworld_actions.Instance.usingUI)
                Dialouge_off(dialogeOff);
        }
        else
            Dialouge_off(dialogeOff);
    }
    public void Battle_Info(string info)//display plain text info to player
    {
        Remove_Exit();
        Battle_handler.Instance.displayingInfo = true;
        Interaction details = new_interaction(info,"Battle Info","");
        PendingMessages.Add(details);
        if(!messagesLoading)
            StartCoroutine(ProccessQeue(details));
    }
    IEnumerator ProccessQeue(Interaction interaction)
    {
        messagesLoading = true;
        Current_interaction = new_interaction(interaction.interactionMessage,"Battle Info","");
        Display(Current_interaction);
        PendingMessages.Remove(interaction);
        yield return new WaitForSeconds(1f);
        reset_message();
    }
    void reset_message()
    {
        if (PendingMessages.Count > 0)
        {
            foreach (Interaction msg in new List<Interaction>(PendingMessages))
                StartCoroutine(ProccessQeue(msg));
        }
        else
        {
            messagesLoading = false;
            Battle_handler.Instance.displayingInfo = false;
        }
    }
    public void Write_Info(string info, string type, string option1, string result, string option2,string opTxt1,string opTxt2)//display a choice, with a result when they choose NO
    {
        Interaction details = new_interaction(info,type,result);
        details.interactionOptions.Add(option1);
        details.interactionOptions.Add(option2);
        details.optionsUiText.Add(opTxt1);
        details.optionsUiText.Add(opTxt2);
        Current_interaction = details;
        Display(Current_interaction);
    }
    public void Display_Options(bool display)
    {
        foreach (GameObject obj in option_btns)
            obj.SetActive(display);
    }
    public void Option_choice(string choice)
    {//only ever be 2 options, unless list type dialogue 
        if (PokemonOperations.LearningNewMove)
            Options_manager.Instance.selectedNewMoveOption = true;
        if (choice == "Option 1")
        {
            Display_Options(false);
            Options_manager.Instance.Complete_Interaction(Current_interaction,0);
        }
        else if (choice == "Option 2")
        {
            if (Current_interaction.interactionOptions[1] == "")//if no option 2,basically the player chose NO
                Dialouge_off();
            else
                Options_manager.Instance.Complete_Interaction(Current_interaction,1);//do second option
        }
    }
    public void Dialouge_off(float delay)
    {
        Invoke(nameof(Dialouge_off), delay);
    }
    public void  Dialouge_off()
    {
        Display_Options(false);
        Current_interaction = null;
        if(!Options_manager.Instance.playerInBattle || overworld_actions.Instance.usingUI)
            dialogue_box.SetActive(false);
        else
            battle_box.SetActive(false);
        current_line = "";
        Dialouge_txt.text = "";
        displaying = false;
        text_finished = false;
        num_lines = 0;
        current_line_num = 0;
        if(Player_movement.instance)
            Player_movement.instance.canmove = true;
        dialogue_next.SetActive(false);
        elipsis_txt.SetActive(false);
        dialogue_exit.SetActive(false);
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
        text_finished = false;
        displaying = true;
        num_lines = math.trunc(interaction.interactionMessage.Length / Max_length);
        if (!Options_manager.Instance.playerInBattle || overworld_actions.Instance.usingUI)
        {
            dialogue_box.SetActive(true);
            Dialouge_txt.color=Color.black;
            battle_box.SetActive(false);
        }
        else if(!overworld_actions.Instance.usingUI && Options_manager.Instance.playerInBattle)
        {
            battle_box.SetActive(true);
            Dialouge_txt.color=Color.white;
            dialogue_box.SetActive(false);
        }
        if(!Options_manager.Instance.playerInBattle)
            dialogue_exit.SetActive(true);
        if (interaction.interactionMessage.Length > Max_length)
        {
            dialogue_next.SetActive(true);
            elipsis_txt.SetActive(true);
            current_line = interaction.interactionMessage.Substring(0,Max_length);
            Dialouge_txt.text = current_line;
            current_line_num++;
        }
        else
        {
            if (interaction.interactionType == "Options")
                Display_Options(true);
            else
                Display_Options(false);
            if (interaction.interactionType == "Event")
                Options_manager.Instance.Complete_Interaction(Current_interaction,0);
            dialogue_next.SetActive(false);
            num_lines = 1;
            current_line_num = 1;
            Dialouge_txt.text = interaction.interactionMessage;
            text_finished = true;
        }

    }
}
