using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Battle_Participant : MonoBehaviour
{
    public Battle_handler battle;
    public Abilities ability_h;
    public Pokemon pokemon;
    public Pokemon[] Current_Enemies = {null,null};//will only ever be 2 because double battles
    public Item_handler item_h;
    public Image pkm_img;
    public Text pkm_name, pkm_HP, pkm_lv;
    public bool isPlayer = false;
    public bool is_active = false;
    public Slider player_hp;
    public Slider player_exp;
    public GameObject[] single_battle_ui;
    public GameObject[] Double_battle_ui;
    public GameObject participant_ui;
    private void Update()
    {
        if (is_active)
        {
            pkm_name.text = pokemon.Pokemon_name;
            pkm_lv.text = "Lv: " + pokemon.Current_level.ToString();
            if (isPlayer)
            {
                pkm_img.sprite = pokemon.back_picture;
                if (!battle.isDouble_battle)
                {
                    pkm_HP.text = pokemon.HP.ToString() + "/" + pokemon.max_HP.ToString();
                    Exp_bar();
                }
            }
            else
            {
                pkm_img.sprite = pokemon.front_picture;
            }
            player_hp.value = pokemon.HP;
            player_hp.maxValue = pokemon.max_HP;
        }
    }

    void UI_visible(GameObject[]arr,bool on)
    {
        foreach (GameObject obj in arr)
        {
            obj.SetActive(on);
        }
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
        if (isPlayer)
        {
            if (battle.isDouble_battle)
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
    }
}
