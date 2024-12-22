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
    [SerializeField] Player_movement movement;
    [SerializeField] GameObject dialogue_box;
    [SerializeField] GameObject battle_box;
    [SerializeField] GameObject dialogue_next;
    [SerializeField] GameObject dialogue_exit;
    public Options_manager options;
    [SerializeField] GameObject[] option_btns;
    [SerializeField] Text[] option_btns_txt;
    public List<Interaction> Message_Qeue;
    public bool messagesLoading = false;
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
        if(Options_manager.instance.playerInBattle)
            Remove_Exit();
        if (Current_interaction != null)
        {
            if (Current_interaction.InterAction_type == "Options")
            {
                option_btns_txt[0].text =Current_interaction.options_txt[0];
                option_btns_txt[1].text =Current_interaction.options_txt[1];
            }
        }
        if (displaying && !text_finished)
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                if (current_line_num < num_lines)
                {
                    current_line_num++;
                    current_line = Current_interaction.InteractionMsg.Substring((current_line_num - 1) * Max_length, Max_length);    
                    Dialouge_txt.text = current_line;
                    text_finished = false;
                }
                else if (current_line_num == num_lines)
                {
                    dialogue_next.SetActive(false);
                    elipsis_txt.SetActive(false);
                    current_line = Current_interaction.InteractionMsg.Substring(current_line_num * Max_length, Current_interaction.InteractionMsg.Length - (current_line_num * Max_length));
                    Dialouge_txt.text = current_line;
                    text_finished = true;
                    if (Current_interaction.InterAction_type == "Options")
                        Display_Options(true);
                }
            }
        }
        if (overworld_actions.instance !=null)
            if (overworld_actions.instance.doing_action )
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
        inter.InteractionMsg = info;
        inter.InterAction_type = type;
        inter.InterAction_result_msg = result;
        return inter;
    }
    public void Write_Info(string info,string type,string result,string[] options, string[]options_txt)//list info
    {
        Interaction details = new_interaction(info,type,result);
        foreach (string option in options)
        {
            details.InterAction_options.Add(option);
        }
        foreach (string txt in options_txt)
        {
            details.options_txt.Add(txt);
        }
        Current_interaction = details;
        Display(Current_interaction);
    }
    public void Write_Info(string info,string type)//display plain text info to player
    {
        Interaction details = new_interaction(info,type,"");
        Current_interaction = details;
        Display(Current_interaction);
    }
    public void Battle_Info(string info)//display plain text info to player
    {
        Remove_Exit();
        Battle_handler.instance.displaying_info = true;
        Interaction details = new_interaction(info,"Battle Info","");
        Message_Qeue.Add(details);
        if(!messagesLoading)
            StartCoroutine(ProccessQeue());
    }
    IEnumerator ProccessQeue()
    {
        foreach (Interaction msg in new List<Interaction>(Message_Qeue))
        {
            messagesLoading = true;
            Current_interaction = new_interaction(msg.InteractionMsg,"Battle Info","");
            Display(Current_interaction);
            Message_Qeue.Remove(msg);
            yield return new WaitForSeconds(1f); //WaitUntil(()=>Message_Qeue.Count==0);
        }
        if (Message_Qeue.Count == 0 )
            messagesLoading = false;
        Battle_handler.instance.displaying_info = false;
        Move_handler.instance.Move_done();
        yield return null;
    }

    public void Write_Info(string info, string type, string option1, string result, string option2,string opTxt1,string opTxt2)//display a choice, with a result when they choose NO
    {
        Interaction details = new_interaction(info,type,result);
        details.InterAction_options.Add(option1);
        details.InterAction_options.Add(option2);
        details.options_txt.Add(opTxt1);
        details.options_txt.Add(opTxt2);
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
        if (choice == "Option 1")
        {
            Display_Options(false);
            options.Complete_Interaction(Current_interaction,0);
        }
        else if (choice == "Option 2")
        {
            if (Current_interaction.InterAction_options[1] == "")//if no option 2,basically the player chose NO
            {
                Dialouge_off();
            }
            else
            {
                options.Complete_Interaction(Current_interaction,1);//do second option
            }
        }
    }
    //public void List choice
    public void Dialouge_off(float delay)
    {
        Invoke(nameof(Dialouge_off), delay);
    }
    /*public void info_off()
    {
        Battle_handler.instance.displaying_info = false;
        messagesLoading = false;
    }
    private void info_off(float delay)
    {
        Invoke(nameof(info_off), delay);
    }*/
    public void  Dialouge_off()
    {
        Display_Options(false);
        Current_interaction = null;
        if(!options.playerInBattle || overworld_actions.instance.using_ui)
            dialogue_box.SetActive(false);
        else
        {
            battle_box.SetActive(false);
        }
        current_line = "";
        Dialouge_txt.text = "";
        displaying = false;
        text_finished = false;
        num_lines = 0;
        current_line_num = 0;
        movement.canmove = true;
        dialogue_next.SetActive(false);
        elipsis_txt.SetActive(false);
        dialogue_exit.SetActive(false);
        Battle_handler.instance.displaying_info = false;
    }
    public void Display(Interaction interaction)
    {
        movement.canmove = false;
        movement.moving = false;
        text_finished = false;
        displaying = true;
        num_lines = math.trunc(interaction.InteractionMsg.Length / Max_length);
        if (!options.playerInBattle || overworld_actions.instance.using_ui)
        {
            dialogue_box.SetActive(true);
            Dialouge_txt.color=Color.black;
            battle_box.SetActive(false);
        }
        else if(!overworld_actions.instance.using_ui && options.playerInBattle)
        {
            battle_box.SetActive(true);
            Dialouge_txt.color=Color.white;
            dialogue_box.SetActive(false);
        }
        dialogue_exit.SetActive(true);
        if (interaction.InteractionMsg.Length > Max_length)
        {
            dialogue_next.SetActive(true);
            elipsis_txt.SetActive(true);
            current_line = interaction.InteractionMsg.Substring(0,Max_length);
            Dialouge_txt.text = current_line;
            current_line_num++;
        }
        else
        {
            if (interaction.InterAction_type == "Options")
            {
                Display_Options(true);
            }
            else
            {          
                Display_Options(false);
            }   
            dialogue_next.SetActive(false);
            num_lines = 1;
            current_line_num = 1;
            Dialouge_txt.text = interaction.InteractionMsg;
            text_finished = true;
        }

    }
}
