using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Wild_pokemon_battle : MonoBehaviour
{
    public GameObject Battle_ui;
    public GameObject moves_ui;
    public GameObject options_ui;
    public Options_manager options_manager;
    public Slider player_hp;
    public Slider enemy_hp;
    public Slider player_exp;
    public Pokemon Currrent_pkm;
    public Pokemon Enemy_Currrent_pkm;
    void Update()
    {
        Slider_values(player_hp,Currrent_pkm);
        Exp_bar();
        Slider_values(enemy_hp,Enemy_Currrent_pkm);
    }
    void Slider_values(Slider slider,Pokemon pkm)
    {
        slider.value = pkm.HP;
        slider.maxValue = pkm.max_HP;
        slider.minValue = 0;
    }
    void Exp_bar()
    {
        player_exp.value = Currrent_pkm.level_progress;
        player_exp.maxValue = 100;
        player_exp.minValue = 0;
    }
    public void Start_Battle(Pokemon wild_pkm)
    {
        options_manager.playerInBattle = true;
        Battle_ui.SetActive(true);
    }
}
