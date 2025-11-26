using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine.UI;

public class pokemon_storage : MonoBehaviour
{
    public List<Pokemon> nonPartyPokemon = new();
    public int totalPokemonCount;
    public int numNonPartyPokemon;
    public int numPartyMembers;
    public int maxPokemonCapacity = 30*14;
    public GameObject[] storageOptions;
    public GameObject storageOptionsParent;
    public Text storageOptionsText;
    public Image storageBoxExit;
    public GameObject exitParty;
    public Sprite[] storageBoxExitSprites;
    public GameObject storageUI;
    public string selectedPokemonID;

    public List<PC_pkm> nonPartyIcons = new();
    public List<PC_party_pkm> partyPokemonIcons;
    public Transform boxIconsParent;

    public GameObject initialSelector;
    public Image boxSelectorImage;

    public GameObject boxOptionsSelector;
    public const int BoxCapacity = 30;
    public int boxColumns = 6;
    public const int NumBoxes = 14;
    public int currentBoxIndex;
    public List<PokemonStorageBox> storageBoxes = new();
    public Sprite[] boxTopVisualSprites;
    public Sprite[] boxVisualSprites;
    public Sprite[] boxSelectorSprites;
    private int _pkmDataSpriteIndex;
    public Image boxTopVisualImage;
    public Image boxVisualImage;
    public Text boxName;
    public LoopingUiAnimation[] greyArrows;
    public static pokemon_storage Instance;
    private bool _viewingPC;
    
    private enum PCNavState
    {
        ViewingPokemonData,ExitingPC,ViewingBoxChange
    }
    private PCNavState _currentNavState;

    public enum PCUsageState
    {
        Withdraw,Deposit,Move
    };

    public PCUsageState currentUsageState;
    public Text pokemonDataName;
    public Text pokemonLevel;
    public Image genderImage;
    public Image pokemonImage;
    public Image pokemonDataVisual;
    public Sprite[] pokemonDataVisualSprites;

    public GameObject partyUI;
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
        InputStateHandler.Instance.OnStateChanged += CheckState;
        for (var i = 0;i<NumBoxes;i++)
        {
            var newBox = ScriptableObject.CreateInstance<PokemonStorageBox>();
            newBox.boxNumber = i + 1;
            newBox.boxVisual = boxVisualSprites[i];
            newBox.boxTopVisual = boxTopVisualSprites[i];
            for (var j = 0;j < BoxCapacity;j++)
            {
                var newBoxPokemon = new StorageBoxPokemon
                {
                    pokemonID = 0L
                    ,containsPokemon = false
                };
                newBox.boxPokemon.Add(newBoxPokemon);
            }
            storageBoxes.Add(newBox);
        }
        Save_manager.Instance.LoadPokemonStorageData();
        nonPartyIcons.Clear();
        for (var i = 0; i < boxIconsParent.childCount; i++)
        {
            var pokemonIcon = boxIconsParent.GetChild(i).gameObject.GetComponent<PC_pkm>();
            pokemonIcon.SetImage();
            nonPartyIcons.Add(pokemonIcon);
        }
    }
    public IEnumerator SaveStorageData()
    {
        foreach (var box in storageBoxes)
        {
            Save_manager.Instance.SaveStorageDataAsJson(box,"Box "+ box.boxNumber);
        }
        yield return null;
    }
    private void CheckState(InputState currentState)
    {
        _viewingPC = currentState.stateGroups.Contains(InputStateHandler.StateGroup.PokemonStorage);
        switch (currentState.stateName)
        {
            case InputStateHandler.StateName.PokemonStorageExit:
                _currentNavState = PCNavState.ExitingPC;
                break;
            case InputStateHandler.StateName.PokemonStorageBoxNavigation:
                _currentNavState = PCNavState.ViewingPokemonData;
                break;
            case InputStateHandler.StateName.PokemonStorageBoxChange:
                _currentNavState = PCNavState.ViewingBoxChange;
                break;
        }
        ActivateCloseBoxAnimation();
        ActivatePkmDataAnimation();
        ActivateArrowAnimation();
    }

    private void ActivateCloseBoxAnimation()
    {
        if (!_viewingPC || _currentNavState != PCNavState.ExitingPC)
        {
            return;
        }
        StartCoroutine(SwitchCloseBoxSprite());
    }
    private IEnumerator SwitchCloseBoxSprite()
    {
        while (_currentNavState==PCNavState.ExitingPC)
        {
            storageBoxExit.sprite = storageBoxExitSprites[0];
            yield return new WaitForSeconds(0.5f);
            storageBoxExit.sprite = storageBoxExitSprites[1];
            yield return new WaitForSeconds(0.5f);
        }
        storageBoxExit.sprite = storageBoxExitSprites[0];
    }
    private void ActivatePkmDataAnimation()
    {
        if (!_viewingPC || _currentNavState != PCNavState.ViewingPokemonData)
        {
            return;
        }
        StartCoroutine(SwitchPkmDataAnimationSprite());
    }
    private void ActivateArrowAnimation()
    {
        if (!_viewingPC) return;
        foreach (var arrow in greyArrows)
        {
            arrow.viewingUI = _currentNavState == PCNavState.ViewingBoxChange;
        }
    }
    private IEnumerator SwitchPkmDataAnimationSprite()
    {
        while (_currentNavState==PCNavState.ViewingPokemonData)
        {
            pokemonDataVisual.sprite = pokemonDataVisualSprites[_pkmDataSpriteIndex];
            yield return new WaitForSeconds(0.5f);
            _pkmDataSpriteIndex = _pkmDataSpriteIndex+1>2? 0 : _pkmDataSpriteIndex+1;
        }
        pokemonDataVisual.sprite = pokemonDataVisualSprites[1];
    } 
    private IEnumerator ActivateSelectorAnimation()
    {
         var startPos = boxSelectorImage.rectTransform.anchoredPosition;
         var targetPos = startPos + Vector2.up * 5;
         bool movingToTarget = true;
         while(_viewingPC)
         {
             Vector2 target = movingToTarget ? targetPos : startPos;
             boxSelectorImage.sprite = boxSelectorSprites[movingToTarget? 0:1];
 
             boxSelectorImage.rectTransform.anchoredPosition = Vector2.MoveTowards(
                 boxSelectorImage.rectTransform.anchoredPosition,
                 target, 500 * Time.deltaTime
             );
             
             if (Vector2.Distance(boxSelectorImage.rectTransform.anchoredPosition, target) < 0.01f)
                 movingToTarget = !movingToTarget;
             
             yield return new WaitForSeconds(0.25f);
         }
    }

    public void ClearPokemonData()
    {
        pokemonDataName.text = string.Empty;
        pokemonLevel.text = string.Empty;
        pokemonImage.gameObject.SetActive(false);
        genderImage.gameObject.SetActive(false);
    }
    public void LoadPokemonData(int currentPokemonIndex)
    {
        Pokemon pokemon;
        if(currentUsageState == PCUsageState.Deposit)
        {
            if (currentPokemonIndex + 1 > Pokemon_party.Instance.numMembers)
            {
                ClearPokemonData();
                return;
            }
            pokemon = Pokemon_party.Instance.party[currentPokemonIndex];
        }
        else
        {
            if (currentPokemonIndex + 1 > storageBoxes[currentBoxIndex].currentNumPokemon)
            {
                ClearPokemonData();
                return;
            }
            pokemon = nonPartyIcons[currentPokemonIndex].pokemon;
        }
        
        pokemonDataName.text = pokemon.pokemonName +"\n /"+pokemon.pokemonName;
        genderImage.gameObject.SetActive(true);
        if(pokemon.hasGender)
            genderImage.sprite = Resources.Load<Sprite>(
                Save_manager.GetDirectory(Save_manager.AssetDirectory.UI) 
                + pokemon.gender.ToString().ToLower());
        else
            genderImage.gameObject.SetActive(false);
        pokemonImage.sprite = pokemon.frontPicture;
        pokemonImage.gameObject.SetActive(true);
        pokemonLevel.text = "Lv: "+pokemon.currentLevel;
    }
    public void ChangeBox(int change)
    {
        RemovePokemonIcons();
        currentBoxIndex += change;
        if (currentBoxIndex >= NumBoxes)
        {
            currentBoxIndex = 0;
        }
        if (currentBoxIndex < 0)
        {
            currentBoxIndex = NumBoxes-1;
        }
        boxVisualImage.sprite = storageBoxes[currentBoxIndex].boxVisual;
        boxTopVisualImage.sprite = storageBoxes[currentBoxIndex].boxTopVisual;
        boxName.text = $"Box {currentBoxIndex + 1}";
        ActivatePokemonIcons();
    }

    public void SetBoxData(int indexOfBox,PokemonStorageBox box)
    {
        storageBoxes[indexOfBox].boxPokemon = box.boxPokemon;
        storageBoxes[indexOfBox].currentNumPokemon = box.currentNumPokemon;
    }

    private int SearchForPokemonIndex(string pokemonID)
    {
        return nonPartyPokemon.FindIndex(p => p.pokemonID.ToString() == pokemonID);
    }
    public bool IsPartyPokemon(string pokemonID)
    {
        return Save_manager.Instance.partyIDs.Any(id => id == pokemonID);
    }
    public void OpenPC(PCUsageState newState)
    {
        currentUsageState = newState;
        if(currentUsageState == PCUsageState.Withdraw)
        {
            if (Pokemon_party.Instance.numMembers==6)
            {
                Dialogue_handler.Instance.DisplayDetails("Party is full",3f);
                ClosePC();
                return;
            }
            InputStateHandler.Instance.SetupPokemonStorageState();
        }
        else 
        if(currentUsageState == PCUsageState.Deposit)
        {
            if (Pokemon_party.Instance.numMembers==1)
            {
                Dialogue_handler.Instance.DisplayDetails("There must be at least 1 pokemon in your team",2f);
                ClosePC();
                return;
            }
            partyUI.SetActive(true);
            var partySelectables = new List<SelectableUI>();

            for (var i = 0 ;i < 6;i++)
            {
                var icon = partyPokemonIcons[i];
                partySelectables.Add( new(icon.gameObject,
                    i<Pokemon_party.Instance.numMembers?() => RemoveFromParty(icon):null, 
                    i<Pokemon_party.Instance.numMembers));
            }
            partySelectables.Add( new(exitParty,Game_ui_manager.Instance.ClosePokemonStorage,true) );
            
            InputStateHandler.Instance.ChangeInputState(new InputState(InputStateHandler.StateName.PokemonStoragePartyNavigation,
                new[]{InputStateHandler.StateGroup.PokemonStorage,InputStateHandler.StateGroup.PokemonStorageParty}
                , stateDirectional:InputStateHandler.Directional.Vertical, selectableUis:partySelectables
                ,selector: initialSelector
                ,selecting:true,display:true));
            
            for (var i = 0;i < 6;i++)
            {
                if (Pokemon_party.Instance.party[i] == null)
                {
                    partyPokemonIcons[i].gameObject.SetActive(false);
                    continue;
                }
                partyPokemonIcons[i].gameObject.SetActive(true);
                partyPokemonIcons[i].pokemon = Pokemon_party.Instance.party[i] ;
                partyPokemonIcons[i].LoadImage();
            }
            InputStateHandler.Instance.OnSelectionIndexChanged += LoadPokemonData;
        }
        //add moving around logic
        ClearPokemonData();
        StartCoroutine(ActivateSelectorAnimation());
        ChangeBox(0);
    }

    public void ClosePC()
    {
        RemovePokemonIcons();
        InputStateHandler.Instance.ResetGroupUi(InputStateHandler.StateGroup.PokemonStorage);
        StopAllCoroutines();
        foreach (var icon in partyPokemonIcons)
        {
            icon.pokemonSpriteBg.gameObject.SetActive(false);
            icon.gameObject.SetActive(false);
            icon.pokemon = null;
        }
    }

    void ResetBoxIconSprite(PC_pkm icon)
    {
        storageOptionsParent.SetActive(false);
        icon.pokemonImage.color=Color.HSVToRGB(0,0,100);
    }
    
    private void RefreshUi()
    { 
       RemovePokemonIcons();
       ActivatePokemonIcons(); 
       InputStateHandler.Instance.RemoveTopInputLayer(true);
    }
    private void ViewNonPartyPokemonDetails()
    {
        Game_ui_manager.Instance.ViewOtherPokemonDetails(nonPartyPokemon[SearchForPokemonIndex(selectedPokemonID)],nonPartyPokemon);
    }
    public void SelectNonPartyPokemon(PC_pkm pokemonIcon)
    {
        if (pokemonIcon.isEmpty) return;
       
        var boxOptionsSelectables = new List<SelectableUI>
        {
            new(storageOptions[1], AddPokemonToParty,true),
            new(storageOptions[0], ViewNonPartyPokemonDetails, true),
            new(storageOptions[2], ()=>DeletePokemon(pokemonIcon),true)
        };

        InputStateHandler.Instance.ChangeInputState(new InputState(InputStateHandler.StateName.PokemonStorageBoxOptions,
            new[]{InputStateHandler.StateGroup.PokemonStorage,InputStateHandler.StateGroup.PokemonStorageBox}
            , stateDirectional:InputStateHandler.Directional.Vertical,selectableUis: boxOptionsSelectables
            ,selector:boxOptionsSelector,selecting:true,display:true
            ,onClose:()=>ResetBoxIconSprite(pokemonIcon),onExit:()=>ResetBoxIconSprite(pokemonIcon)));
        
        ResetBoxIconSprite(pokemonIcon);
        selectedPokemonID = pokemonIcon.pokemon.pokemonID.ToString();
        pokemonIcon.pokemonImage.color=Color.HSVToRGB(17,96,54);
        storageOptionsText.text = pokemonIcon.pokemon.pokemonName + " is selected.";
        storageOptionsParent.SetActive(true);
    }
    private void RemovePokemonIcons()
    {
        foreach (var icon in nonPartyIcons)
        {
            icon.pokemon = null;
            icon.pokemonImage.sprite = null;
            icon.gameObject.SetActive(false);
        }
    }
    private void ActivatePokemonIcons()
    {
        storageBoxes[currentBoxIndex].currentNumPokemon = 0;
        for (var i = 0;i<BoxCapacity;i++)
        {
            if (!storageBoxes[currentBoxIndex].boxPokemon[i].containsPokemon)
            {
                nonPartyIcons[i].isEmpty = true;
                continue;
            }
            nonPartyIcons[i].isEmpty = false;
            //this search will always find one
            var pokemonForBox = nonPartyPokemon.First(pokemon =>
                pokemon.pokemonID == storageBoxes[currentBoxIndex].boxPokemon[i].pokemonID);
      
            storageBoxes[currentBoxIndex].currentNumPokemon++;
            var pokemonIcon = nonPartyIcons[i];
            pokemonIcon.gameObject.SetActive(true);
            pokemonIcon.pokemon = pokemonForBox;
            pokemonIcon.LoadImage();
        }
    }
    private void RemoveFromParty(PC_party_pkm partyPokemon)
    {
        if (Pokemon_party.Instance.numMembers > 1)
        {
            //ui state for selecting box
            Pokemon_party.Instance.RemoveMember(partyPokemon.partyPosition);
            Pokemon_party.Instance.numMembers--;
            numNonPartyPokemon++;
            RefreshUi();
            InputStateHandler.Instance.ResetGroupUi(InputStateHandler.StateGroup.PokemonStorageParty);
        }
        else
            Dialogue_handler.Instance.DisplayDetails("There must be at least 1 pokemon in your team",2f);
    }
    private void DeletePokemon(PC_pkm pokemonIcon)
    {
        ResetBoxIconSprite(pokemonIcon);
        var indexToDelete= SearchForPokemonIndex(selectedPokemonID);
        Dialogue_handler.Instance.DisplayDetails("You released "+ nonPartyPokemon[indexToDelete].pokemonName,2f);
        DeleteNonPartyPokemon(indexToDelete);
        RefreshUi();
    }
    private void DeleteNonPartyPokemon(int index)
    {
        nonPartyPokemon.Remove(nonPartyPokemon[index]);
        numNonPartyPokemon--;
        totalPokemonCount--;
    }
    
    public bool MaxPokemonCapacity()
    {
        return totalPokemonCount >= maxPokemonCapacity;
    }
    private void AddPokemonToParty()//from pc operations
    {
        if (Pokemon_party.Instance.numMembers < 6)
        {
            var pokemonIndex = SearchForPokemonIndex(selectedPokemonID);
            ResetBoxIconSprite(nonPartyIcons[pokemonIndex]);
            Pokemon_party.Instance.party[Pokemon_party.Instance.numMembers] = Obj_Instance.CreatePokemon(nonPartyPokemon[pokemonIndex]);
            DeleteNonPartyPokemon(pokemonIndex);
            Pokemon_party.Instance.numMembers++;
            Pokemon_party.Instance.numMembers++;
            RefreshUi();
        }
        else
            Dialogue_handler.Instance.DisplayDetails("Party is full",3f);
    }

    public void AddPokemonToStorage(Pokemon newPokemon)
    {
       nonPartyPokemon.Add(newPokemon);
       numNonPartyPokemon++;
       totalPokemonCount++;
    }
}
