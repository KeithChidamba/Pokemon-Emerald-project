using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Serialization;

public class pokemon_storage : MonoBehaviour
{
    public List<Pokemon> nonPartyPokemon;
    public int totalPokemonCount;
    public int numNonPartyPokemon;
    public int maxPokemonCapacity = 40;
    public int numPartyMembers;
    public Button[] storageOptions;
    public GameObject storageUI;
    public string selectedPokemonID;
    public bool doingStorageOperation;
    public bool usingPC;
    public List<GameObject> nonPartyIcons;
    public GameObject[] partyPokemonIcons;
    public GameObject pokemonIconTemplate;
    public Transform storageIconPositionsParent;
    public bool swapping;
    public bool hasSelectedPokemon;
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
        Game_ui_manager.Instance.ManageScreens(1);
        usingPC = true;
        ActivatePokemonIcons();
        storageUI.SetActive(true);
        Dialogue_handler.Instance.EndDialogue();
        DisableOptions();
    }
    public void ClosePC()
    {
        foreach (var icon in partyPokemonIcons)
            icon.GetComponent<PC_party_pkm>().options.SetActive(false);
        usingPC = false;
        hasSelectedPokemon = false;
        storageUI.SetActive(false);
        RemovePokemonIcons();
        Game_ui_manager.Instance.ManageScreens(-1);
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
                ResetPartyUi();
                partyPokemon.options.SetActive(true);
            }
        }
    }
    void LoadOptions()
    {
        for (var i = 0 ; i<nonPartyPokemon.Count; i++)
            nonPartyIcons[i].GetComponent<PC_pkm>().pokemonSprite.color=Color.HSVToRGB(0,0,100);
        foreach (var btn in storageOptions)
            btn.interactable = true;
    }
    private void DisableOptions()
    {
        foreach (var btn in storageOptions)
            btn.interactable = false;
        ResetPartyUi();
    }
    void ResetPartyUi()
    {
        foreach (var icon in partyPokemonIcons)
            icon.GetComponent<PC_party_pkm>().options.SetActive(false);
    }
    public void RefreshUi()
    {
        DisableOptions();
        RemovePokemonIcons();
        ActivatePokemonIcons();
        hasSelectedPokemon = false;
        swapping = false;
    }
    public void ViewNonPartyPokemonDetails()
    {
        if (hasSelectedPokemon && !swapping)
            Pokemon_Details.Instance.LoadDetails(nonPartyPokemon[SearchForPokemonIndex(selectedPokemonID)]);
    }
    public void SelectNonPartyPokemon(PC_pkm pokemonIcon)
    {
        if (!swapping)
        {
            DisableOptions();
            LoadOptions();
            hasSelectedPokemon = true;
            selectedPokemonID = pokemonIcon.pokemon.pokemonID.ToString();
            pokemonIcon.pokemonSprite.color=Color.HSVToRGB(17,96,54);
        }
    }
    private void RemovePokemonIcons()
    {
        nonPartyIcons.Clear();
        for (var i = 0; i < storageIconPositionsParent.childCount; i++)
        {
            var currentIconPosition = storageIconPositionsParent.GetChild(i);
            if (currentIconPosition.childCount < 1) continue;
            Destroy(currentIconPosition.GetChild(0).gameObject);
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
            var pokemonIcon = Instantiate(pokemonIconTemplate, storageIconPositionsParent.GetChild(i));
            pokemonIcon.SetActive(true);
            pokemonIcon.GetComponent<PC_pkm>().pokemon = nonPartyPokemon[i];
            pokemonIcon.GetComponent<PC_pkm>().LoadImage();
            nonPartyIcons.Add(pokemonIcon);
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
    public void RemoveFromParty(PC_party_pkm partyPokemon)
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
    public void DeletePokemon()
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
    public void SwapWithPartyPokemon()//from pc operations
    {
        DisableOptions();
        storageIconPositionsParent.gameObject.SetActive(false);
        Dialogue_handler.Instance.DisplayInfo("Pick a pokemon in your party to swap with", "Details",1.2f);
        swapping = true;
    }

    public bool MaxPokemonCapacity()
    {
        return totalPokemonCount >= maxPokemonCapacity;
    }
    public void AddPokemonToParty()//from pc operations
    {
        if (Pokemon_party.Instance.numMembers < 6)
        {
            doingStorageOperation = true;
            var pokemonIndex = SearchForPokemonIndex(selectedPokemonID);
            Pokemon_party.Instance.party[Pokemon_party.Instance.numMembers] = CreateAndSetupPokemon(nonPartyPokemon[pokemonIndex]);
            DeleteNonPartyPokemon(pokemonIndex);
            Pokemon_party.Instance.numMembers++;
            numPartyMembers++;
        }
        else
            Dialogue_handler.Instance.DisplayInfo("Party is full, you can still swap out pokemon though", "Details",2f);
        doingStorageOperation = false;
        RefreshUi();
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
