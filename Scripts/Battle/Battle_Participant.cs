using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Battle_Participant : MonoBehaviour
{
    public Abilities ability_h;
    public Participant_Status status;
    public Battle_Data data;
    public Pokemon pokemon;
    public List<Battle_Participant> Current_Enemies;
    public Image pkm_img;
    public Image status_img;
    public Text pkm_name, pkm_HP, pkm_lv;
    public bool isPlayer = false;
    public bool is_active = false;
    public Slider player_hp;
    public Slider player_exp;
    public GameObject[] single_battle_ui;
    public GameObject[] Double_battle_ui;
    public GameObject participant_ui;
    public bool Selected_Enemy = false;

    public event Action<Battle_Participant> Onfaint;
    private void Start()
    {
        status = GetComponent<Participant_Status>();
        data = GetComponent<Battle_Data>();
       Turn_Based_Combat.instance.OnNewTurn += Check_Faint;
    }
    private void Update()
    {
        if (!is_active) return;
        update_ui();
    }

    public void Get_exp(Battle_Participant enemy)
    {
        Battle_handler.instance.Distribute_EXP(pokemon.Calc_Exp(enemy.pokemon));
        Current_Enemies.Remove(enemy);
    }
    
    void Check_Faint()
    {
        if (!is_active) return;
        if (pokemon.HP <= 0)
        {
            Onfaint?.Invoke(this);
            if (isPlayer)
                Check_loss();
            else
            {
                if (!Battle_handler.instance.is_trainer_battle)
                {//end battle if wild
                    Wild_pkm.instance.InBattle = false;
                    
                    Battle_handler.instance.End_Battle(true);
                }
                else
                {
                    //swap in new pkm if trainer or end battle
                }
            }
            //play anim
        }
    }
    private void Check_loss()
    {
        int faint_count = 0;
        for(int i=0;i<Pokemon_party.instance.num_members;i++)
            if (Pokemon_party.instance.party[i].HP <= 0)
                faint_count++;
        if (faint_count == Pokemon_party.instance.num_members)
        {
            Battle_handler.instance.End_Battle(false);
            if(!Battle_handler.instance.is_trainer_battle)
                Wild_pkm.instance.InBattle = false;
        }
        else
        {//select next pokemon to switch in
            Pokemon_party.instance.Swapping_in = true;
            Game_ui_manager.instance.View_pkm_Party();
            Dialogue_handler.instance.Write_Info("Select a Pokemon to switch in","Details");
            data.Load_Stats(this);
        }
    }
    private void update_ui()
    {
        pkm_name.text = pokemon.Pokemon_name;
        pkm_lv.text = "Lv: " + pokemon.Current_level;
        if (isPlayer)
        {
            pkm_img.sprite = pokemon.back_picture;
            if (!Battle_handler.instance.isDouble_battle)
            {
                pkm_HP.text = pokemon.HP + "/" + pokemon.max_HP;
                Exp_bar();
            }
        }
        else
            pkm_img.sprite = pokemon.front_picture;
        player_hp.value = pokemon.HP;
        player_hp.maxValue = pokemon.max_HP;
        if(pokemon.HP<=0)
            pokemon.HP = 0;
    }

    public void refresh_statusIMG()
    {
        if (pokemon.Status_effect == "None")
            status_img.gameObject.SetActive(false);
        else
        {
            status_img.gameObject.SetActive(true);
            status_img.sprite = Resources.Load<Sprite>("Pokemon_project_assets/Pokemon_obj/Status/" + pokemon.Status_effect.ToLower());
        }
    }
    void UI_visible(GameObject[]arr,bool on)
    {
        foreach (GameObject obj in arr)
            obj.SetActive(on);
    }
    void Exp_bar()
    {
        player_exp.value = pokemon.level_progress;
        player_exp.maxValue = 100;
        player_exp.minValue = 0;
    }
    public void Load_ui()
    {
        player_hp.minValue = 0;
        is_active = true;
        participant_ui.SetActive(true);
        refresh_statusIMG();
        if (isPlayer)
        {
            foreach (Battle_Participant p in Current_Enemies)
                p.Onfaint+=Get_exp;
            if (Battle_handler.instance.isDouble_battle)
            {
                UI_visible(Double_battle_ui, true);
                UI_visible(single_battle_ui, false);
            }
            else
            {
                UI_visible(Double_battle_ui, false);
                UI_visible(single_battle_ui, true);
            }
        }
    }

    public void Unload_ui()
    {
        participant_ui.SetActive(false);
        is_active = false;
        Current_Enemies.Clear();
    }
}
