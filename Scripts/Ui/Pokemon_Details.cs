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
        player_exp.value = ((currentPokemon.currentExpAmount/currentPokemon.nextLevelExpAmount)*100);
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
        move_Description.text = currentPokemon.moveSet[moveIndex - 1].description;
        move_acc.text = "Accuracy: "+currentPokemon.moveSet[moveIndex - 1].moveAccuracy;
        move_dmg.text = "Damage: " + currentPokemon.moveSet[moveIndex - 1].moveDamage;
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
            typeImages[i].sprite = currentPokemon.types[i].typeImage;
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
        pkm_atk.text = currentPokemon.attack.ToString();
        pkm_hp.text = currentPokemon.hp+"/"+ currentPokemon.maxHp;
        pkm_def.text = currentPokemon.defense.ToString();
        pkm_sp_atk.text = currentPokemon.specialAttack.ToString();
        pkm_speed.text = currentPokemon.speed.ToString();
        pkm_sp_def.text = currentPokemon.specialDefense.ToString();
        pkm_CurrentExp.text = currentPokemon.currentExpAmount.ToString();
        pkm_NextLvExp.text = currentPokemon.nextLevelExpAmount.ToString();
        pkm_HeldItem.text = (currentPokemon.hasItem)? currentPokemon.heldItem.itemName: "NONE";
        Stats_ui.SetActive(true);
    }     
    private void LoadMovesUiPage()
    {
        Ability_ui.SetActive(false);
        Stats_ui.SetActive(false);
        move_details.SetActive(false);
        move_Description.text = string.Empty;
        for (var j = 0; j < currentPokemon.moveSet.Count; j++)
        {
            moves[j].text = currentPokemon.moveSet[j].moveName;
            Move_type[j].sprite = currentPokemon.moveSet[j].type.typeImage;
            Move_type[j].gameObject.SetActive(true);
            moves_pp[j].text = "pp " + currentPokemon.moveSet[j].powerpoints + "/" + currentPokemon.moveSet[j].maxPowerpoints;
            moves_btns[j].SetActive(true);
        }
        for (var i = currentPokemon.moveSet.Count; i < 4; i++)
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
        pkm_name.text = currentPokemon.pokemonName;
        pkm_ID.text = "ID: "+currentPokemon.pokemonID;
        pkm_lv.text = "Lv "+currentPokemon.currentLevel;
        pkm_img.sprite = currentPokemon.frontPicture;
        gender_img.gameObject.SetActive(true);
        if(currentPokemon.hasGender)
            gender_img.sprite = Resources.Load<Sprite>("Pokemon_project_assets/ui/"+currentPokemon.gender.ToLower());
        else
            gender_img.gameObject.SetActive(false);
        _currentPage = (learningMove || changingMoveData) ? 3 : 1;
        LoadPage(_currentPage);
    }
}
