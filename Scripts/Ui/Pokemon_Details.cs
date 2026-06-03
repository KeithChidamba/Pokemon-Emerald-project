using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Pokemon_Details : MonoBehaviour,IInjectable
{
    //too lazy to change this to camelCase because ot would take a lot of editor work
    [SerializeField]private Text pkm_name,pkm_ablty, pkm_ablty_desc, pkm_lv,pkm_ID,Trainer_Name;
    [SerializeField]private Text pkm_atk, pkm_sp_atk, pkm_def, pkm_sp_def, pkm_speed, pkm_hp;
    [SerializeField]private Text move_Description,pkm_HeldItem,pkm_CurrentExp,pkm_NextLvExp;
    [SerializeField] private TMP_Text pokemonCaptureInfo;
    [FormerlySerializedAs("moves_pp")] [SerializeField]private Text[] movesPpText;
    [FormerlySerializedAs("moves")] public Text[] moveNamesText;
    [SerializeField]private Image pkm_img;
    [SerializeField]private Image gender_img;
    [SerializeField]private Image type1;
    [SerializeField]private Image type2;
    [FormerlySerializedAs("Move_type")] [SerializeField]private Image[] moveTypeImages;
    [SerializeField]private Slider player_exp;
    
    [SerializeField]private GameObject Ability_ui;
    [SerializeField]private GameObject Stats_ui;
    [SerializeField]private GameObject Moves_ui;
    [SerializeField]private GameObject OverlayUi;
    
    [SerializeField]private GameObject move_details;
    [SerializeField]private Text move_dmg, move_acc;
    
    [SerializeField]private int _currentPage;
    public Pokemon currentPokemon;
    private int _currentPokemonIndex;
    public List<Pokemon> pokemonToView = new();
    public Action<int> OnMoveSelected; 
    public bool learningMove;
    public bool changingMoveData;
    private Dictionary<int, Action> _pages = new();
    public GameObject moveSelector;
    public GameObject uiParent;
    
    private InputStateHandler _inputStateHandler;
    private Game_Load _gameLoadingHandler;
    
    public void Inject(ServiceContainer container)
    {
        _gameLoadingHandler = container.Resolve<Game_Load>();
        _inputStateHandler = container.Resolve<InputStateHandler>();
        gameObject.SetActive(true);
    }

    public void OnInject()
    {
        _pages.Add(1,LoadAbilityUiPage);
        _pages.Add(2,LoadStatsUiPage);
        _pages.Add(3,LoadMovesUiPage);
    }

    public void ResetDetailsState()
    {
        currentPokemon = null;
    }

    public void DeactivateDetailsUi()
    {
        changingMoveData = false;
        OverlayUi.SetActive(false);
        Stats_ui.SetActive(false);
        Moves_ui.SetActive(false);
        Ability_ui.SetActive(false);
    }
    public void NextPage()
    {
        if (_currentPage < 3)
        {
            _currentPage++;
            LoadPage(_currentPage);
        }

    }
    public void PreviousPage()
    {
        if (_currentPage > 1)
        {
            _currentPage--;
            LoadPage(_currentPage);
        }
    }
    
    public void SelectMove(int moveIndex)
    {
        if (learningMove || changingMoveData)
        {
            OnMoveSelected?.Invoke(moveIndex);
            return;
        }
        _inputStateHandler.ChangeInputState(new (InputStateName.PokemonDetailsMoveData, InputStateGroup.PokemonDetails
            ,stateDirection:InputDirection.None, onExit:RemoveMoveDescription));

        var selectedMove = currentPokemon.moveSet[moveIndex];
        
        move_Description.text = selectedMove.description;
        move_acc.text = "Accuracy: "+ selectedMove.moveAccuracy;
        move_dmg.text = "Damage: " + selectedMove.moveDamage;
        move_details.SetActive(true);
    }

    private void  LoadPage(int pageNumber)
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
        Trainer_Name.text = _gameLoadingHandler.playerData.playerName;
        pkm_ablty.text = currentPokemon.ability.abilityName.ToUpper();
        pokemonCaptureInfo.text = $" <color=red>{currentPokemon.nature.natureName.ToUpper()}</color> nature," +
                                   $"\n met at lv{currentPokemon.captureInformation.levelCaptured}," +
                                   $"\n <color=red>{currentPokemon.captureInformation.areaName.ToUpper()}</color>";
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
        pkm_NextLvExp.text = (currentPokemon.nextLevelExpAmount - currentPokemon.currentExpAmount).ToString();
        pkm_HeldItem.text = currentPokemon.hasItem? currentPokemon.heldItem.itemName: "NONE";
        player_exp.maxValue = currentPokemon.nextLevelExpAmount;
        player_exp.minValue = currentPokemon.currentLevelExpAmount;
        player_exp.value = currentPokemon.currentExpAmount;
        Stats_ui.SetActive(true);
    }
    
    private void LoadMovesUiPage()
    {
        if (learningMove || changingMoveData)
        {//simulate F click
            _inputStateHandler.currentState.selectableUis[2]?.eventForUi?.Invoke();
        }

        Ability_ui.SetActive(false);
        Stats_ui.SetActive(false);
        move_details.SetActive(false);
        move_Description.text = string.Empty;
        for (var j = 0; j < currentPokemon.moveSet.Count; j++)
        {
            moveNamesText[j].text = currentPokemon.moveSet[j].moveName;
            moveTypeImages[j].sprite = currentPokemon.moveSet[j].type.typeImage;
            moveTypeImages[j].gameObject.SetActive(true);
            movesPpText[j].text = "pp " + currentPokemon.moveSet[j].powerpoints + "/" + currentPokemon.moveSet[j].maxPowerpoints;
        }
        for (var i = currentPokemon.moveSet.Count; i < 4; i++)
        {
            moveNamesText[i].text = string.Empty;
            moveTypeImages[i].gameObject.SetActive(false);
            movesPpText[i].text = string.Empty;
        }
        Moves_ui.SetActive(true);
    }

    private void RemoveMoveDescription()
    {
        move_details.SetActive(false);
        move_Description.text = string.Empty;
    }

    public void ChangePokemon(int indexChange)
    {
        var oldIndex = _currentPokemonIndex;
        _currentPokemonIndex = Mathf.Clamp(_currentPokemonIndex + indexChange, 0, pokemonToView.Count - 1);
        if (oldIndex == _currentPokemonIndex) return;
        
        currentPokemon = pokemonToView[_currentPokemonIndex];
        StartCoroutine(PokemonAnimation());
        LoadOverlayInfo();
        LoadPage(_currentPage);
    }
    private IEnumerator PokemonAnimation()
    {
         pkm_img.sprite = currentPokemon.frontPicture;
         yield return new WaitForSecondsRealtime(0.2f);
         pkm_img.sprite = currentPokemon.battleIntroFrame;
         yield return new WaitForSecondsRealtime(0.35f);
         pkm_img.sprite = currentPokemon.frontPicture;
         yield return new WaitForSecondsRealtime(0.35f);
         pkm_img.sprite = currentPokemon.battleIntroFrame;
         yield return new WaitForSecondsRealtime(0.35f);
         pkm_img.sprite = currentPokemon.frontPicture;
    }
    void LoadOverlayInfo()
    {
        pkm_name.text = currentPokemon.nickName +"\n /"+currentPokemon.pokemonName;
        pkm_ID.text = "IDNo"+currentPokemon.pokemonID;
        pkm_lv.text = "Lv"+currentPokemon.currentLevel;
        gender_img.gameObject.SetActive(true);
        if(currentPokemon.hasGender)
        {
            gender_img.sprite = Utility.GetGenderSprite(currentPokemon.gender);
        }
        else
        {
            gender_img.gameObject.SetActive(false);
        }
    }
    public void LoadDetails(Pokemon selectedPokemon,List<Pokemon> pokemonList)
    {
        OverlayUi.SetActive(true);
        pokemonToView = pokemonList;
        currentPokemon = selectedPokemon;
        LoadOverlayInfo();
        _currentPage = (learningMove || changingMoveData) ? 3 : 1;
        LoadPage(_currentPage);
        StartCoroutine(PokemonAnimation());
    }

   
}
