using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Battle_Participant : MonoBehaviour
{
    public Abilities ability_h;
    public Pokemon pokemon;
    public Pokemon[] Current_Enemies = {null,null};//will only ever be 2 because double battles
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
    private void Start()
    {
       Turn_Based_Combat.instance.OnNewTurn += Check_Status;
    }
    private void Update()
    {
        if (!is_active) return;
        update_ui();
    }

    void Check_Status()
    {
        if (!is_active) return;
        if (pokemon.HP <= 0)
        {
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
        foreach (Pokemon p in Pokemon_party.instance.party)
            if (p.HP <= 0)
                faint_count++;
        if (faint_count == 0) return;
        if (faint_count == Pokemon_party.instance.num_members)
        {
            Battle_handler.instance.End_Battle(false);
            //send player to pkm center before end battle ui
        }
        else
        {//select next pokemon to switch in
            Pokemon_party.instance.Swapping_in = true;
            Game_ui_manager.instance.View_pkm_Party();
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
        if (pokemon.Status_effect == "None")
            status_img.gameObject.SetActive(false);
        else
        {
            status_img.gameObject.SetActive(true);
            status_img.sprite = Resources.Load<Sprite>("Pokemon_project_assets/Pokemon_obj/Status/" + pokemon.Status_effect.ToLower());
        }
        if (isPlayer)
        {
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
    }
}
