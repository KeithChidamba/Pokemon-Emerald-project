using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Pokemon_Details : MonoBehaviour
{
    [SerializeField]private Text pkm_name,pkm_ablty, pkm_ablty_desc, pkm_lv,pkm_ID,pkm_nature,Trainer_Name;
    [SerializeField]private Text pkm_atk, pkm_sp_atk, pkm_def, pkm_sp_def, pkm_speed, pkm_hp;
    [SerializeField]private Text move_Description,pkm_HeldItem,pkm_CurrentExp,pkm_NextLvExp;
    [SerializeField]private GameObject[] moves_btns;
    [SerializeField]private Text[] moves_pp;
    [SerializeField]private Text[] moves;
    [SerializeField]private Image pkm_img;
    [SerializeField]private Image gender_img;
    [SerializeField]private Image type1;
    [SerializeField]private Image type2;
    [SerializeField]private Image[] Move_type;
    [SerializeField]private Slider player_exp;
    
    [SerializeField]private GameObject Ability_ui;
    [SerializeField]private GameObject Stats_ui;
    [SerializeField]private GameObject Moves_ui;
    [SerializeField]private GameObject OverlayUi;
    
    [SerializeField]private GameObject move_details;
    [SerializeField]private Text move_dmg, move_acc;
    
    private int _currentPage;
    public Pokemon currentPokemon;
    public Action<int> OnMoveSelected; 
    public bool learningMove;
    public bool changingMoveData;
    private Dictionary<int, Action> _pages = new();
    public static Pokemon_Details Instance;
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
        _pages.Add(1,LoadAbilityUiPage);
        _pages.Add(2,LoadStatsUiPage);
        _pages.Add(3,LoadMovesUiPage);
    }

    private void Update()
    {
        if(currentPokemon == null)return;
        player_exp.value = ((currentPokemon.CurrentExpAmount/currentPokemon.NextLevelExpAmount)*100);
    }

    public void ExitDetails()
    {
        if (learningMove) return;
        Game_ui_manager.Instance.ManageScreens(-1);
        changingMoveData = false;
        OverlayUi.SetActive(false);
        Stats_ui.SetActive(false);
        Moves_ui.SetActive(false);
        Ability_ui.SetActive(false);
        currentPokemon = null;
        Pokemon_party.Instance.viewingDetails = false;
    }
    public void NextPage()
    {
        if (learningMove || changingMoveData) return;
        if (_currentPage < 3)
            _currentPage++;
        LoadPage(_currentPage);
    }
    public void PreviousPage()
    {
        if (learningMove || changingMoveData) return;
         if (_currentPage > 1)
             _currentPage--;
         LoadPage(_currentPage);
     }
    
    public void DisplayMoveDescription(int moveIndex)
    {
        if (learningMove)
        {
            OnMoveSelected?.Invoke(moveIndex-1);
            return;
        }
        if (changingMoveData) return;
        move_Description.text = currentPokemon.move_set[moveIndex - 1].Description;
        move_acc.text = "Accuracy: "+currentPokemon.move_set[moveIndex - 1].Move_accuracy;
        move_dmg.text = "Damage: " + currentPokemon.move_set[moveIndex - 1].Move_damage;
        move_details.SetActive(true);
    }

    private void LoadPage(int pageNumber)
    {
        if(_pages.TryGetValue(pageNumber,out var openPage))
            openPage();
        else
            Debug.Log($"pokemon details page not found, page number: {pageNumber}");
    }
    private void LoadAbilityUiPage()
    { 
        Stats_ui.SetActive(false);
        Moves_ui.SetActive(false);
        var typeImages = new[]{ type1, type2 };
        for (var i = 0; i < typeImages.Length; i++)
        {
            typeImages[i].gameObject.SetActive(false);
            if (i >= currentPokemon.types.Count) break;
            typeImages[i].sprite = currentPokemon.types[i].type_img;
            typeImages[i].gameObject.SetActive(true);
        }
        pkm_ablty_desc.text = currentPokemon.ability.abilityDescription;
        Trainer_Name.text = Game_Load.Instance.playerData.playerName;
        pkm_ablty.text = currentPokemon.ability.abilityName.ToUpper();
        pkm_nature.text = currentPokemon.nature.natureName.ToUpper();
        Ability_ui.SetActive(true);
    }    
    private void LoadStatsUiPage()
    {
        Ability_ui.SetActive(false);
        Moves_ui.SetActive(false);
        pkm_atk.text = currentPokemon.Attack.ToString();
        pkm_hp.text = currentPokemon.HP+"/"+ currentPokemon.max_HP;
        pkm_def.text = currentPokemon.Defense.ToString();
        pkm_sp_atk.text = currentPokemon.SP_ATK.ToString();
        pkm_speed.text = currentPokemon.speed.ToString();
        pkm_sp_def.text = currentPokemon.SP_DEF.ToString();
        pkm_CurrentExp.text = currentPokemon.CurrentExpAmount.ToString();
        pkm_NextLvExp.text = currentPokemon.NextLevelExpAmount.ToString();
        pkm_HeldItem.text = (currentPokemon.HasItem)? currentPokemon.HeldItem.itemName: "NONE";
        Stats_ui.SetActive(true);
    }     
    private void LoadMovesUiPage()
    {
        Ability_ui.SetActive(false);
        Stats_ui.SetActive(false);
        move_details.SetActive(false);
        move_Description.text = string.Empty;
        for (var j = 0; j < currentPokemon.move_set.Count; j++)
        {
            moves[j].text = currentPokemon.move_set[j].Move_name;
            Move_type[j].sprite = currentPokemon.move_set[j].type.type_img;
            Move_type[j].gameObject.SetActive(true);
            moves_pp[j].text = "pp " + currentPokemon.move_set[j].Powerpoints + "/" + currentPokemon.move_set[j].max_Powerpoints;
            moves_btns[j].SetActive(true);
        }
        for (var i = currentPokemon.move_set.Count; i < 4; i++)
        {
            moves[i].text = string.Empty;
            Move_type[i].gameObject.SetActive(false);
            moves_pp[i].text = string.Empty;
            moves_btns[i].SetActive(false);
        }
        Moves_ui.SetActive(true);
    } 
    public void LoadDetails(Pokemon pokemon)
    {
        Game_ui_manager.Instance.ManageScreens(1);
        OverlayUi.SetActive(true);
        currentPokemon=pokemon;
        pkm_name.text = currentPokemon.Pokemon_name;
        pkm_ID.text = "ID: "+currentPokemon.Pokemon_ID;
        pkm_lv.text = "Lv "+currentPokemon.Current_level;
        pkm_img.sprite = currentPokemon.front_picture;
        gender_img.gameObject.SetActive(true);
        if(currentPokemon.has_gender)
            gender_img.sprite = Resources.Load<Sprite>("Pokemon_project_assets/ui/"+currentPokemon.Gender.ToLower());
        else
            gender_img.gameObject.SetActive(false);
        _currentPage = (learningMove || changingMoveData) ? 3 : 1;
        LoadPage(_currentPage);
    }
}
