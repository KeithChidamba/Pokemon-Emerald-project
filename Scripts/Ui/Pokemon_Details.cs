using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Pokemon_Details : MonoBehaviour
{
    public Text pkm_name,pkm_ablty, pkm_ablty_desc, pkm_lv,pkm_ID,pkm_nature,Trainer_Name;
    public Text pkm_atk, pkm_sp_atk, pkm_def, pkm_sp_def, pkm_speed, pkm_hp;
    public Text move_Description,pkm_HeldItem,pkm_CurrentExp,pkm_NextLvExp;
    public GameObject[] moves_btns;
    public Text[] moves_pp;
    public Text[] moves;
    public Image pkm_img;
    public Image gender_img;
    public Image type1;
    public Image type2;
    public Image[] Move_type;
    public Slider player_exp;
    
    public Pokemon current_pkm;
    public GameObject Ability_ui;
    public GameObject Stats_ui;
    public GameObject Moves_ui;
    public GameObject OverlayUi;
    public GameObject move_details;
    public Text move_dmg, move_acc;
    int current_page = 0;
    public Action<int> OnMoveSelected;
    public bool LearningMove = false;
    public bool ChangingMoveData = false;
    public static Pokemon_Details instance;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
    private void Update()
    {
        if(current_pkm == null)return;
        player_exp.value = ((current_pkm.CurrentExpAmount/current_pkm.NextLevelExpAmount)*100);
    }

    public void Exit_details()
    {
        if (LearningMove) return;
        ChangingMoveData = false;
        OverlayUi.SetActive(false);
        Stats_ui.SetActive(false);
        Moves_ui.SetActive(false);
        Ability_ui.SetActive(false);
        current_pkm = null;
        Pokemon_party.instance.viewing_details = false;
    }
    public void Next()
    {
        if (LearningMove || ChangingMoveData) return;
        if (current_page < 3)
            current_page++;
        Load_ui(current_page);
    }
    public void Prev()//prev page
    {
        if (LearningMove || ChangingMoveData) return;
         if (current_page > 1)
             current_page--;
         Load_ui(current_page);
     }
    //Set ui element values for each page
    public void Move_Discription(int move_num)
    {
        OnMoveSelected?.Invoke(move_num-1);
        if (LearningMove | ChangingMoveData) return;
        move_Description.text=current_pkm.move_set[move_num - 1].Description;
        move_acc.text = "Accuracy: "+current_pkm.move_set[move_num - 1].Move_accuracy;
        move_dmg.text = "Damage: " + current_pkm.move_set[move_num - 1].Move_damage;
        move_details.SetActive(true);
    }

    public void Load_ui(int Page)
    {
        switch (Page)
        {
            case 1:
                load_Ability_ui();
                break;
            case 2:
                load_Stats_ui();
                break;
            case 3:
                load_Moves_ui();
                break;
        }
    }
    void load_Ability_ui()
    {
        Stats_ui.SetActive(false);
        Moves_ui.SetActive(false);
        if (current_pkm.types.Count > 1)
        {
            type1.sprite = current_pkm.types[0].type_img;
            type2.sprite = current_pkm.types[1].type_img;
            type1.gameObject.SetActive(true);
            type2.gameObject.SetActive(true);
        }
        else
        {
            type1.sprite = current_pkm.types[0].type_img;
            type1.gameObject.SetActive(true);
            type2.gameObject.SetActive(false);
        }
        pkm_ablty_desc.text = current_pkm.ability.abilityDescription;
        Trainer_Name.text = Game_Load.Instance.playerData.playerName;
        pkm_ablty.text = current_pkm.ability.abilityName.ToUpper();
        pkm_nature.text = current_pkm.nature.natureName.ToUpper();
        Ability_ui.SetActive(true);
    }    
    void load_Stats_ui()
    {
        Ability_ui.SetActive(false);
        Moves_ui.SetActive(false);
        pkm_atk.text = current_pkm.Attack.ToString();
        pkm_hp.text = current_pkm.HP+"/"+ current_pkm.max_HP;
        pkm_def.text = current_pkm.Defense.ToString();
        pkm_sp_atk.text = current_pkm.SP_ATK.ToString();
        pkm_speed.text = current_pkm.speed.ToString();
        pkm_sp_def.text = current_pkm.SP_DEF.ToString();
        pkm_CurrentExp.text = current_pkm.CurrentExpAmount.ToString();
        pkm_NextLvExp.text = current_pkm.NextLevelExpAmount.ToString();
        pkm_HeldItem.text = (current_pkm.HasItem)? current_pkm.HeldItem.itemName: "NONE";
        Stats_ui.SetActive(true);
    }     
    void load_Moves_ui()
    {
        Ability_ui.SetActive(false);
        Stats_ui.SetActive(false);
        move_details.SetActive(false);
        move_Description.text = "";//not null
        int j = 0;
        foreach(Move m in current_pkm.move_set)
        {
            moves[j].text = current_pkm.move_set[j].Move_name;
            Move_type[j].sprite = current_pkm.move_set[j].type.type_img;
            Move_type[j].gameObject.SetActive(true);
            moves_pp[j].text = "pp " + current_pkm.move_set[j].Powerpoints + "/" + current_pkm.move_set[j].max_Powerpoints;
            moves_btns[j].SetActive(true);
            j++;
        }
        for (int i = j; i < 4; i++)
        {
            moves[i].text = "";
            Move_type[i].gameObject.SetActive(false);
            moves_pp[i].text = "";
            moves_btns[i].SetActive(false);
        }
        Moves_ui.SetActive(true);
    } 
    public void Load_Details(Pokemon pokemon)
    {
        OverlayUi.SetActive(true);
        current_pkm=pokemon;
        pkm_name.text = current_pkm.Pokemon_name;
        pkm_ID.text = "ID: "+current_pkm.Pokemon_ID;
        pkm_lv.text = "Lv "+current_pkm.Current_level;
        pkm_img.sprite = current_pkm.front_picture;
        gender_img.gameObject.SetActive(true);
        if(current_pkm.has_gender)
            gender_img.sprite = Resources.Load<Sprite>("Pokemon_project_assets/ui/"+current_pkm.Gender.ToLower());
        else
            gender_img.gameObject.SetActive(false);
        current_page = (LearningMove || ChangingMoveData) ? 3 : 1;
        Load_ui(current_page);
    }
}
