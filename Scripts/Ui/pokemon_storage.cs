using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;

public class pokemon_storage : MonoBehaviour
{
    public List<Pokemon> nonPartyPokemon;
    public int totalPokemonCount;
    public int numNonPartyPokemon;
    public int maxPokemonCapacity = 21;
    public int numPartyMembers;
    public GameObject[] initialStorageOptions;
    public GameObject[] storageOptions;
    public GameObject storageUI;
    public string selectedPokemonID;
    public bool doingStorageOperation;
    public List<GameObject> nonPartyIcons;
    public GameObject[] partyPokemonIcons;
    public Transform storageIconPositionsParent;
    public bool swapping;
    public bool hasSelectedPokemon;
    public int[] boxCoordinates={0,0};
    public GameObject initialSelector;
    public GameObject boxSelector;
    public GameObject partySelector;
    public GameObject boxOptionsSelector;
    public static pokemon_storage Instance;
    public int rowRemainder;
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
        for (var i = 0; i < storageIconPositionsParent.childCount; i++)
        {
            nonPartyIcons.Add(storageIconPositionsParent.GetChild(i).gameObject);
            if (i == numNonPartyPokemon-1) break;
        }
    }

    public void ResetCoordinates()
    {
        boxCoordinates[0] = 0;
        boxCoordinates[1] = 0;
    }
    void SetRowRemainder()
    {
        var currentRowRemainder = nonPartyPokemon.Count - (boxCoordinates[0]*7);
        rowRemainder =  (currentRowRemainder<7)? currentRowRemainder: 7;
        boxCoordinates[1] = Mathf.Clamp(boxCoordinates[1], 0, rowRemainder-1);
    }
    private int GetCurrentBoxPosition()
    {
        SetRowRemainder();
        var rowCapacity = (boxCoordinates[0]) * 7;
        rowCapacity=Mathf.Clamp(rowCapacity, 0, maxPokemonCapacity);
        return (rowCapacity)+boxCoordinates[1];
    }
    public void MoveCoordinates(string direction, int change)
    {
        SetRowRemainder();
        var coordinateIndex = direction == "Vertical" ? 0 : 1;
        
        var maxIndexForCoordinate  = direction == "Vertical" ? (int)math.ceil(nonPartyPokemon.Count/7) : rowRemainder-1;
        
        boxCoordinates[coordinateIndex] = Mathf.Clamp(boxCoordinates[coordinateIndex] + change, 0, maxIndexForCoordinate);
        
        InputStateHandler.Instance.currentState.currentSelectionIndex 
            = Mathf.Clamp(GetCurrentBoxPosition(),0,numNonPartyPokemon);
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
        hasSelectedPokemon = false;
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
            swapping = false;
            RemovePokemonIcons();
            ActivatePokemonIcons();
            doingStorageOperation = false;
            hasSelectedPokemon = false;
        }
        else
        {
            if (!hasSelectedPokemon)
            {
                ResetPartyUi(partyPokemon);
                partyPokemon.pokemonSprite.color=Color.HSVToRGB(17,96,54);
                var partySelectable = new List<SelectableUI>
                {
                    new(partyPokemon.options,()=>RemoveFromParty(partyPokemon),true)
                };
                
                InputStateHandler.Instance.ChangeInputState(new InputState("Pokemon Storage Party Options",false
                    ,null, null, partySelectable,null,false,
                    false,()=>ResetPartyUi(partyPokemon),()=>ResetPartyUi(partyPokemon),true));
                partyPokemon.options.SetActive(true);
            }
        }
    }
    void LoadOptions()
    {
        for (var i = 0 ; i<nonPartyPokemon.Count; i++)
            nonPartyIcons[i].GetComponent<PC_pkm>().pokemonImage.color=Color.HSVToRGB(0,0,100);
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
       hasSelectedPokemon = false;
       swapping = false;
       InputStateHandler.Instance.RemoveTopInputLayer(true);
    }
    private void ViewNonPartyPokemonDetails()
    {
        if (hasSelectedPokemon && !swapping)
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
                new(storageOptions[3], DeletePokemon,true)
            };

            InputStateHandler.Instance.ChangeInputState(new InputState("Pokemon Storage Box Options",false,null
                , InputStateHandler.Horizontal, boxOptionsSelectables,boxOptionsSelector,true,true
                ,LoadOptions,LoadOptions,true));
            
            LoadOptions();
            hasSelectedPokemon = true;
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
        for (var i = 0;i<nonPartyPokemon.Count;i++)
        {
            if (nonPartyPokemon[i] == null) continue;
            var pokemonIcon = nonPartyIcons[i];
            pokemonIcon.SetActive(true);
            pokemonIcon.GetComponent<PC_pkm>().pokemon = nonPartyPokemon[i];
            pokemonIcon.GetComponent<PC_pkm>().LoadImage();
        }
        for (var i = 0;i < numPartyMembers;i++)
        {
            if (Pokemon_party.Instance.party[i] == null) continue;
            partyPokemonIcons[i].SetActive(true);
            partyPokemonIcons[i].GetComponent<PC_party_pkm>().pokemon = Pokemon_party.Instance.party[i] ;
            partyPokemonIcons[i].GetComponent<PC_party_pkm>().LoadImage();
            doingStorageOperation = true;
        }
        doingStorageOperation = false;
        storageIconPositionsParent.gameObject.SetActive(true);
    }
    private void RemoveFromParty(PC_party_pkm partyPokemon)
    {
        if (numPartyMembers > 1)
        {
            Pokemon_party.Instance.RemoveMember(partyPokemon.partyPosition);
            numPartyMembers--;
            numNonPartyPokemon++;
            RefreshUi();
        }
        else
            Dialogue_handler.Instance.DisplayInfo("There must be at least 1 pokemon in your team","Details",1f);
    }
    private void DeletePokemon()
    {
        var indexToDelete= SearchForPokemonIndex(selectedPokemonID);
        Dialogue_handler.Instance.DisplayInfo("You released "+ nonPartyPokemon[indexToDelete].pokemonName, "Details",1.5f);
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
        storageIconPositionsParent.gameObject.SetActive(false);
        Dialogue_handler.Instance.DisplayInfo("Pick a pokemon in your party to swap with", "Details",1.2f);
        swapping = true;
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
            Pokemon_party.Instance.party[Pokemon_party.Instance.numMembers] = CreateAndSetupPokemon(nonPartyPokemon[pokemonIndex]);
            DeleteNonPartyPokemon(pokemonIndex);
            Pokemon_party.Instance.numMembers++;
            numPartyMembers++;
            RefreshUi();
        }
        else
            Dialogue_handler.Instance.DisplayInfo("Party is full, you can still swap out pokemon though", "Details",2f);
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
