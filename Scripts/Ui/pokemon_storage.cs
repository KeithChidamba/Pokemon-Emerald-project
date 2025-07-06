using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;

public class pokemon_storage : MonoBehaviour
{
    public List<Pokemon> nonPartyPokemon = new();
    public int totalPokemonCount;
    public int numNonPartyPokemon;
    public int maxPokemonCapacity = 21;
    public int numPartyMembers;
    public GameObject[] initialStorageOptions;
    public GameObject[] storageOptions;
    public GameObject storageUI;
    public string selectedPokemonID;
    public bool doingStorageOperation;
    public List<GameObject> nonPartyIcons = new();
    public GameObject[] partyPokemonIcons;
    public Transform storageIconPositionsParent;
    public bool swapping;
    public GameObject initialSelector;
    public GameObject boxSelector;
    public GameObject partySelector;
    public GameObject boxOptionsSelector;
    public int boxCapacity = 21;
    public int boxColumns = 7;
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
        ActivatePokemonIcons();
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
            doingStorageOperation = true;
            var pokemonIndex = SearchForPokemonIndex(selectedPokemonID);
            var swapStore = Pokemon_party.Instance.party[partyPokemon.partyPosition - 1];
            Pokemon_party.Instance.party[partyPokemon.partyPosition - 1] = CreateAndSetupPokemon(nonPartyPokemon[pokemonIndex]);
            nonPartyPokemon[pokemonIndex] = CreateAndSetupPokemon(swapStore);
            ResetBoxIconSprite(nonPartyIcons[pokemonIndex].GetComponent<PC_pkm>());
            RefreshUi();
            doingStorageOperation = false;
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
                ,false,null, 
                InputStateHandler.Directional.None, partySelectable, null,false,
                false,()=>ResetPartyUi(partyPokemon),()=>ResetPartyUi(partyPokemon),true));
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
        Game_ui_manager.Instance.ViewPokemonDetails(nonPartyPokemon[SearchForPokemonIndex(selectedPokemonID)]);
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
                ,false,null
                , InputStateHandler.Directional.Horizontal, boxOptionsSelectables,boxOptionsSelector,true,true
                ,()=>ResetBoxIconSprite(pokemonIcon),()=>ResetBoxIconSprite(pokemonIcon),true));
            
            ResetBoxIconSprite(pokemonIcon);
            selectedPokemonID = pokemonIcon.pokemon.pokemonID.ToString();
            pokemonIcon.pokemonImage.color=Color.HSVToRGB(17,96,54);
        }
    }
    private void RemovePokemonIcons()
    {
        foreach (var icon in nonPartyIcons)
        {
            var pokemonScript = icon.GetComponent<PC_pkm>();
            pokemonScript.pokemon = null;
            pokemonScript.pokemonImage.sprite = null;
            icon.SetActive(false);
        }
        foreach (var icon in partyPokemonIcons)
        {
            icon.SetActive(false);
            icon.GetComponent<PC_party_pkm>().pokemon = null;
        }
    }
    private void ActivatePokemonIcons()
    {
        for (var i = 0;i < numPartyMembers;i++)
        {
            if (Pokemon_party.Instance.party[i] == null) continue;
            partyPokemonIcons[i].SetActive(true);
            partyPokemonIcons[i].GetComponent<PC_party_pkm>().pokemon = Pokemon_party.Instance.party[i] ;
            partyPokemonIcons[i].GetComponent<PC_party_pkm>().LoadImage();
            doingStorageOperation = true;
        }
        nonPartyIcons.Clear();
        for (var i = 0; i < storageIconPositionsParent.childCount; i++)
        {
            if (i == numNonPartyPokemon) break;
            nonPartyIcons.Add(storageIconPositionsParent.GetChild(i).gameObject);
        }
        if (nonPartyIcons.Count == 0)
        {
            doingStorageOperation = false;
            return;
        }
        for (var i = 0;i<nonPartyPokemon.Count;i++)
        {
            if (nonPartyPokemon[i] == null) continue;
            var pokemonIcon = nonPartyIcons[i];
            pokemonIcon.SetActive(true);
            pokemonIcon.GetComponent<PC_pkm>().pokemon = nonPartyPokemon[i];
            pokemonIcon.GetComponent<PC_pkm>().LoadImage();
        }
        doingStorageOperation = false;
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
            doingStorageOperation = true;
            var pokemonIndex = SearchForPokemonIndex(selectedPokemonID);
            ResetBoxIconSprite(nonPartyIcons[pokemonIndex].GetComponent<PC_pkm>());
            Pokemon_party.Instance.party[Pokemon_party.Instance.numMembers] = CreateAndSetupPokemon(nonPartyPokemon[pokemonIndex]);
            DeleteNonPartyPokemon(pokemonIndex);
            Pokemon_party.Instance.numMembers++;
            numPartyMembers++;
            RefreshUi();
        }
        else
            Dialogue_handler.Instance.DisplayDetails("Party is full, you can still swap out pokemon though",3f);
        doingStorageOperation = false;
    }

    public Pokemon CreateAndSetupPokemon(Pokemon template)
    {
        var newPokemon = Obj_Instance.CreatePokemon(template);
        newPokemon.hasTrainer = true; 
        if (!doingStorageOperation)
        {
            totalPokemonCount++;
            if (numPartyMembers < 6)
                numPartyMembers++;
            PokemonOperations.SetPokemonTraits(newPokemon);
            if (newPokemon.currentLevel == 0)
                newPokemon.LevelUp();
            newPokemon.hp = newPokemon.maxHp;
        }
        return newPokemon;
    }


}
