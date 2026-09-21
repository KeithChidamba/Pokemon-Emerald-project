using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public enum PokemonDetailsUsage
{
    ViewData,
    LearnMoves,
    AlterMoves
}
public class PokemonDetailsHandler : MonoBehaviour,IInjectable
{
    //too lazy to change this to camelCase because ot would take a lot of editor work
    [SerializeField]private Text pkm_name,pkm_ablty, pkm_ablty_desc, pkm_lv,pkm_ID,Trainer_Name;
    [SerializeField]private Text pkm_atk, pkm_sp_atk, pkm_def, pkm_sp_def, pkm_speed, pkm_hp;
    [SerializeField]private Text move_Description,pkm_HeldItem,pkm_CurrentExp,pkm_NextLvExp;
    [SerializeField] private TMP_Text pokemonCaptureInfo;
    [FormerlySerializedAs("moves_pp")] [SerializeField]private Text[] movesPpText;
    [FormerlySerializedAs("moves")] public Text[] moveNamesText;
    [SerializeField]private Image pkm_img;
    [SerializeField]private Image pokeballImage;
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
    
    [SerializeField]private int currentPage;
    public Pokemon CurrentPokemon { get; private set; }
    [SerializeField]private int currentPokemonIndex;
    private IReadOnlyList<Pokemon> pokemonToView;
    public event Action<int> OnMoveSelected;
    private bool singlePokemonView;
    private PokemonDetailsUsage currentUsage;
    private Dictionary<int, Action> _pages = new();
    public GameObject moveSelector;
    public GameObject uiParent;
    private Coroutine _animationRoutine;
    
    private InputStateHandler _inputStateHandler;
    private GameLoadingHandler _gameLoadingHandler;
    
    public void Inject(ServiceContainer container)
    {
        _gameLoadingHandler = container.Resolve<GameLoadingHandler>();
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
        CurrentPokemon = null;
        currentPokemonIndex = 0;
        StopCoroutine(_animationRoutine);
    }

    public void SetUsage(PokemonDetailsUsage newUsage)
    {
        currentUsage = newUsage;
    }
    public void DeactivateDetailsUi()
    {
        currentUsage = PokemonDetailsUsage.ViewData;
        OverlayUi.SetActive(false);
        Stats_ui.SetActive(false);
        Moves_ui.SetActive(false);
        Ability_ui.SetActive(false);
    }
    public void NextPage()
    {
        if (currentPage < 3)
        {
            currentPage++;
            LoadPage(currentPage);
        }

    }
    public void PreviousPage()
    {
        if (currentPage > 1)
        {
            currentPage--;
            LoadPage(currentPage);
        }
    }
    
    public void SelectMove(int moveIndex)
    {
        if (currentUsage != PokemonDetailsUsage.ViewData)
        {
            OnMoveSelected?.Invoke(moveIndex);
            return;
        }
        _inputStateHandler.ChangeInputState(new (InputStateName.PokemonDetailsMoveData, InputStateGroup.PokemonDetails
            ,stateDirection:InputDirection.None, onExit:RemoveMoveDescription));

        var selectedMove = CurrentPokemon.moveSet[moveIndex];
        
        move_Description.text = selectedMove.description;
        move_acc.text = "Accuracy: "+ selectedMove.moveAccuracy;
        move_dmg.text = "Damage: " + selectedMove.moveDamage;
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
            if (i >= CurrentPokemon.types.Count) break;
            typeImages[i].sprite = CurrentPokemon.types[i].typeImage;
            typeImages[i].gameObject.SetActive(true);
        }
        pkm_ablty_desc.text = CurrentPokemon.ability.abilityDescription;
        Trainer_Name.text = _gameLoadingHandler.playerData.playerName;
        pkm_ablty.text = NameDB.GetAbility(CurrentPokemon.ability.abilityName).ToUpper();
        
        pokemonCaptureInfo.text = $" <color=red>{CurrentPokemon.nature.natureName.ToUpper()}</color> nature," +
                                   $"\n met at lv{CurrentPokemon.captureInformation.levelCaptured}," +
                                   $"\n <color=red>{CurrentPokemon.captureInformation.areaName.ToUpper()}</color>";
        Ability_ui.SetActive(true);
    }    
    private void LoadStatsUiPage()
    {
        Ability_ui.SetActive(false);
        Moves_ui.SetActive(false);
        pkm_atk.text = CurrentPokemon.attack.ToString();
        pkm_hp.text = CurrentPokemon.hp+"/"+ CurrentPokemon.maxHp;
        pkm_def.text = CurrentPokemon.defense.ToString();
        pkm_sp_atk.text = CurrentPokemon.specialAttack.ToString();
        pkm_speed.text = CurrentPokemon.speed.ToString();
        pkm_sp_def.text = CurrentPokemon.specialDefense.ToString();
        pkm_CurrentExp.text = CurrentPokemon.currentExpAmount.ToString();
        pkm_NextLvExp.text = (CurrentPokemon.nextLevelExpAmount - CurrentPokemon.currentExpAmount).ToString();
        pkm_HeldItem.text = CurrentPokemon.hasItem? CurrentPokemon.heldItem.itemName: "NONE";
        player_exp.maxValue = CurrentPokemon.nextLevelExpAmount;
        player_exp.minValue = CurrentPokemon.currentLevelExpAmount;
        player_exp.value = CurrentPokemon.currentExpAmount;
        Stats_ui.SetActive(true);
    }
    
    private void LoadMovesUiPage()
    {
        if (currentUsage != PokemonDetailsUsage.ViewData)
        {//auto-enter move ui state
            _inputStateHandler.currentState.selectableUis[2]?.eventForUi?.Invoke();
        }

        Ability_ui.SetActive(false);
        Stats_ui.SetActive(false);
        move_details.SetActive(false);
        move_Description.text = string.Empty;
        for (var j = 0; j < CurrentPokemon.moveSet.Count; j++)
        {
            moveNamesText[j].text = CurrentPokemon.moveSet[j].moveName;
            moveTypeImages[j].sprite = CurrentPokemon.moveSet[j].type.typeImage;
            moveTypeImages[j].gameObject.SetActive(true);
            movesPpText[j].text = "pp " + CurrentPokemon.moveSet[j].powerpoints + "/" + CurrentPokemon.moveSet[j].maxPowerpoints;
        }
        for (var i = CurrentPokemon.moveSet.Count; i < 4; i++)
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
        if (singlePokemonView) return;
        var oldIndex = currentPokemonIndex;
        currentPokemonIndex = Mathf.Clamp(currentPokemonIndex + indexChange, 0, pokemonToView.Count - 1);
        if (oldIndex == currentPokemonIndex) return;
        
        CurrentPokemon = pokemonToView[currentPokemonIndex];
        if(_animationRoutine is not null)
        {
            StopCoroutine(_animationRoutine);
        }
        _animationRoutine = StartCoroutine(PokemonAnimation());
        LoadOverlayInfo();
        LoadPage(currentPage);
    }
    private IEnumerator PokemonAnimation()
    {
         pkm_img.sprite = CurrentPokemon.frontPicture;
         yield return new WaitForSecondsRealtime(0.2f);
         pkm_img.sprite = CurrentPokemon.battleIntroFrame;
         yield return new WaitForSecondsRealtime(0.35f);
         pkm_img.sprite = CurrentPokemon.frontPicture;
         yield return new WaitForSecondsRealtime(0.35f);
         pkm_img.sprite = CurrentPokemon.battleIntroFrame;
         yield return new WaitForSecondsRealtime(0.35f);
         pkm_img.sprite = CurrentPokemon.frontPicture;
    }
    void LoadOverlayInfo()
    {
        pkm_name.text = CurrentPokemon.nickName +"\n /"+CurrentPokemon.pokemonName;
        pkm_ID.text = "IDNo"+CurrentPokemon.pokemonID;
        pkm_lv.text = "Lv"+CurrentPokemon.currentLevel;
        pokeballImage.sprite =
            Resources.Load<Sprite>(DirectoryHandler.GetDirectory(AssetDirectory.ItemUI) + CurrentPokemon.pokeballName);
        gender_img.gameObject.SetActive(true);
        if(CurrentPokemon.hasGender)
        {
            gender_img.sprite = Utility.GetGenderSprite(CurrentPokemon.gender);
        }
        else
        {
            gender_img.gameObject.SetActive(false);
        }
    }
    public void LoadDetails(int selectedPokemonIndex,IReadOnlyList<Pokemon> pokemonList)
    {
        singlePokemonView = false;
        OverlayUi.SetActive(true);
        pokemonToView = pokemonList;
        CurrentPokemon = pokemonList[selectedPokemonIndex];
        currentPokemonIndex = selectedPokemonIndex;
        SetDetailsState();
    }
    public void LoadDetails(Pokemon selectedPokemon)
    {
        singlePokemonView = true;
        OverlayUi.SetActive(true);
        CurrentPokemon = selectedPokemon;
        SetDetailsState();
    }
    private void SetDetailsState()
    {
        LoadOverlayInfo();
        currentPage = currentUsage != PokemonDetailsUsage.ViewData ? 3 : 1;
        LoadPage(currentPage);
        _animationRoutine = StartCoroutine(PokemonAnimation());
    }
}
