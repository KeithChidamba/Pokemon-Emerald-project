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
    public int maxPokemonCapacity = 30*14;
    public int numPartyMembers;
    public GameObject[] initialStorageOptions;
    public GameObject[] storageOptions;
    public Image storageBoxExit;
    public Sprite[] storageBoxExitSprites;
    public GameObject storageUI;
    public string selectedPokemonID;

    public List<PC_pkm> nonPartyIcons = new();
    public GameObject[] partyPokemonIcons;
    public Transform boxIconsParent;
    public bool swapping;
    public GameObject initialSelector;
    public Image boxSelectorImage;
    public GameObject partySelector;
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
    private PCNavState _currentPCState;
    public Text pokemonDataName;
    public Text pokemonLevel;
    public Image genderImage;
    public Image pokemonImage;
    public Image pokemonDataVisual;
    public Sprite[] pokemonDataVisualSprites;
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
                _currentPCState = PCNavState.ExitingPC;
                break;
            case InputStateHandler.StateName.PokemonStorageBoxNavigation:
                _currentPCState = PCNavState.ViewingPokemonData;
                break;
            case InputStateHandler.StateName.PokemonStorageBoxChange:
                _currentPCState = PCNavState.ViewingBoxChange;
                break;
        }
        ActivateCloseBoxAnimation();
        ActivatePkmDataAnimation();
        ActivateArrowAnimation();
    }

    private void ActivateCloseBoxAnimation()
    {
        if (!_viewingPC || _currentPCState != PCNavState.ExitingPC)
        {
            return;
        }
        StartCoroutine(SwitchCloseBoxSprite());
    }
    private IEnumerator SwitchCloseBoxSprite()
    {
        while (_currentPCState==PCNavState.ExitingPC)
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
        if (!_viewingPC || _currentPCState != PCNavState.ViewingPokemonData)
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
            arrow.viewingUI = _currentPCState == PCNavState.ViewingBoxChange;
        }
    }
    private IEnumerator SwitchPkmDataAnimationSprite()
    {
        while (_currentPCState==PCNavState.ViewingPokemonData)
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
        if (currentPokemonIndex+1 > storageBoxes[currentBoxIndex].currentNumPokemon)
        {
            ClearPokemonData();
            return;
        }
        var pokemon = nonPartyIcons[currentPokemonIndex].pokemon;
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
    public void OpenPC()
    {
        ClearPokemonData();
        StartCoroutine(ActivateSelectorAnimation());
        ChangeBox(0);
    }

    public void ClosePC()
    {
        foreach (var icon in partyPokemonIcons)
            icon.GetComponent<PC_party_pkm>().options.SetActive(false);
        RemovePokemonIcons();
    }
    public void SelectPartyPokemon(PC_party_pkm partyPokemon)
    {
        if (swapping)
        {
            var pokemonIndex = SearchForPokemonIndex(selectedPokemonID);
            var swapStore = Pokemon_party.Instance.party[partyPokemon.partyPosition - 1];
            Pokemon_party.Instance.party[partyPokemon.partyPosition - 1] = Obj_Instance.CreatePokemon(nonPartyPokemon[pokemonIndex]);
            nonPartyPokemon[pokemonIndex] = Obj_Instance.CreatePokemon(swapStore);
            ResetBoxIconSprite(nonPartyIcons[pokemonIndex]);
            RefreshUi();
        }
        else
        {
            ResetPartyUi(partyPokemon);
            partyPokemon.pokemonSprite.color=Color.HSVToRGB(17,96,54);
            var partySelectable = new List<SelectableUI>
            {
                new(partyPokemon.options,()=>RemoveFromParty(partyPokemon),true)
            };
            
            InputStateHandler.Instance.ChangeInputState(new InputState(InputStateHandler.StateName.PokemonStoragePartyOptions
                ,new[]{InputStateHandler.StateGroup.PokemonStorage,InputStateHandler.StateGroup.PokemonStorageParty}
                ,stateDirectional:InputStateHandler.Directional.None, selectableUis:partySelectable,
                onClose:()=>ResetPartyUi(partyPokemon),onExit:()=>ResetPartyUi(partyPokemon)));
            partyPokemon.options.SetActive(true);
        }
    }
    void ResetBoxIconSprite(PC_pkm icon)
    {
        icon.pokemonImage.color=Color.HSVToRGB(0,0,100);
    }

    void ResetPartyUi(PC_party_pkm partyPokemon)
    {
        partyPokemon.pokemonSprite.color=Color.HSVToRGB(0,0,100);
        partyPokemon.options.SetActive(false);
    }
    private void RefreshUi()
    { 
       RemovePokemonIcons();
       ActivatePokemonIcons(); 
       swapping = false;
       InputStateHandler.Instance.RemoveTopInputLayer(true);
    }
    private void ViewNonPartyPokemonDetails()
    {
        Game_ui_manager.Instance.ViewOtherPokemonDetails(nonPartyPokemon[SearchForPokemonIndex(selectedPokemonID)],nonPartyPokemon);
    }
    public void SelectNonPartyPokemon(PC_pkm pokemonIcon)
    {
        if (!swapping)
        {
            var boxOptionsSelectables = new List<SelectableUI>
            {
                new(storageOptions[0], ViewNonPartyPokemonDetails, true),
                new(storageOptions[1], SwapWithPartyPokemon, true),
                new(storageOptions[2], AddPokemonToParty,true),
                new(storageOptions[3], ()=>DeletePokemon(pokemonIcon),true)
            };

            InputStateHandler.Instance.ChangeInputState(new InputState(InputStateHandler.StateName.PokemonStorageBoxOptions,
                new[]{InputStateHandler.StateGroup.PokemonStorage,InputStateHandler.StateGroup.PokemonStorageBox}
                , stateDirectional:InputStateHandler.Directional.Horizontal,selectableUis: boxOptionsSelectables
                ,selector:boxOptionsSelector,selecting:true,display:true
                ,onClose:()=>ResetBoxIconSprite(pokemonIcon),onExit:()=>ResetBoxIconSprite(pokemonIcon)));
            
            ResetBoxIconSprite(pokemonIcon);
            selectedPokemonID = pokemonIcon.pokemon.pokemonID.ToString();
            pokemonIcon.pokemonImage.color=Color.HSVToRGB(17,96,54);
        }
    }
    private void RemovePokemonIcons()
    {
        foreach (var icon in nonPartyIcons)
        {
            icon.pokemon = null;
            icon.pokemonImage.sprite = null;
            icon.gameObject.SetActive(false);
        }
        foreach (var icon in partyPokemonIcons)
        {
            icon.SetActive(false);
            icon.GetComponent<PC_party_pkm>().pokemon = null;
        }
    }
    private void ActivatePokemonIcons()
    {
//         for (var i = 0;i < numPartyMembers;i++)
//         {
//             if (Pokemon_party.Instance.party[i] == null) continue;
//             partyPokemonIcons[i].SetActive(true);
//             partyPokemonIcons[i].GetComponent<PC_party_pkm>().pokemon = Pokemon_party.Instance.party[i] ;
//             partyPokemonIcons[i].GetComponent<PC_party_pkm>().LoadImage();
// ;
//         }

        storageBoxes[currentBoxIndex].currentNumPokemon = 0;
        for (var i = 0;i<BoxCapacity;i++)
        {
            //this search will always find one
            if (!storageBoxes[currentBoxIndex].boxPokemon[i].containsPokemon)
            {
                continue;
            }
            // var newBoxPokemon = new StorageBoxPokemon
            // {
            //     pokemonID = nonPartyPokemon[i].pokemonID
            //     ,containsPokemon = false
            // };
            // storageBoxes[currentBoxIndex].boxPokemon[i] = newBoxPokemon;
            
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
        if (numPartyMembers > 1)
        {
            Pokemon_party.Instance.RemoveMember(partyPokemon.partyPosition);
            ResetPartyUi(partyPokemon);
            numPartyMembers--;
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
    private void SwapWithPartyPokemon()
    {
        InputStateHandler.Instance.ResetGroupUi(InputStateHandler.StateGroup.PokemonStorageBox);
        Dialogue_handler.Instance.DisplayDetails("Pick a pokemon in your party to swap with",2f);
        swapping = true;
        InputStateHandler.Instance.PokemonStoragePartyNavigation();
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
            numPartyMembers++;
            RefreshUi();
        }
        else
            Dialogue_handler.Instance.DisplayDetails("Party is full, you can still swap out pokemon though",3f);
    }

    public void AddPokemonToStorage(Pokemon newPokemon)
    {
       nonPartyPokemon.Add(newPokemon);
       numNonPartyPokemon++;
       totalPokemonCount++;
    }
}
