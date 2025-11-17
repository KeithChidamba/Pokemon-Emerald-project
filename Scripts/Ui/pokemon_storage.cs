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
    public GameObject storageUI;
    public string selectedPokemonID;

    public List<PC_pkm> nonPartyIcons = new();
    public GameObject[] partyPokemonIcons;
    public Transform boxIconsParent;
    public bool swapping;
    public GameObject initialSelector;
    public GameObject boxSelector;
    public GameObject partySelector;
    public GameObject boxOptionsSelector;
    public const int BoxCapacity = 30;
    public int boxColumns = 6;
    public const int NumBoxes = 14;
    public int currentBoxIndex;
    public List<PokemonStorageBox> storageBoxes = new();
    public Sprite[] boxTopVisualSprites;
    public Sprite[] boxVisualSprites;
    public Image boxTopVisualImage;
    public Image boxVisualImage;

    public LoopingUiAnimation[] greyArrows;
    public static pokemon_storage Instance;

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
    public void SwitchPkmDataAnimationSprite()
    {
        //event in animation
    }

    public void ChangeBox(int change)
    {
        currentBoxIndex = Mathf.Clamp(currentBoxIndex + change, 0, NumBoxes);
        boxVisualImage.sprite = storageBoxes[currentBoxIndex].boxVisual;
        boxTopVisualImage.sprite = storageBoxes[currentBoxIndex].boxTopVisual;
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
        ChangeBox(0);
        ActivatePokemonIcons();
        foreach (var arrow in greyArrows)
        {
            arrow.viewingUI = true;
        }
    }
    public void ClosePC()
    {
        foreach (var icon in partyPokemonIcons)
            icon.GetComponent<PC_party_pkm>().options.SetActive(false);
        RemovePokemonIcons();
        foreach (var arrow in greyArrows)
        {
            arrow.viewingUI = false;
        }
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
