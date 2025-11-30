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
    public int maxPokemonCapacity;
    public GameObject[] storageOptions;
    public GameObject[] storagePartyOptions;
    public GameObject storagePartyOptionsParent;
    public GameObject storageOptionsParent;
    public Text boxDepositText;
    public GameObject boxDepositUI;
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
    public int currentIndexOfBox;
    public List<PokemonStorageBox> storageBoxes = new();
    public Sprite[] boxTopVisualSprites;
    public Sprite[] boxVisualSprites;
    public Sprite[] boxSelectorSprites;
    private int _pkmDataSpriteIndex;
    public Image boxTopVisualImage;
    public Image boxVisualImage;
    public Text boxName;
    public LoopingUiAnimation[] boxChangeGreyArrows;
    public LoopingUiAnimation[] depositGreyArrows;
    public static pokemon_storage Instance;
    private bool _viewingPC;
    public StorageBoxMovingData movingOperationData;
    public GameObject movePokemonUIOption;
    public Image selectedPokemonImage;
    public bool movingPokemon;
    private enum PCNavState
    {
        ViewingPokemonData,ExitingPC,ViewingBoxChange,SelectingBoxDeposit,ViewingParty
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
        maxPokemonCapacity = BoxCapacity * NumBoxes;
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
                    pokemonID = string.Empty
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
            case InputStateHandler.StateName.PokemonStorageDepositSelection:
                _currentNavState = PCNavState.SelectingBoxDeposit;
                break;
            case InputStateHandler.StateName.PokemonStorageBoxChange:
                _currentNavState = PCNavState.ViewingBoxChange;
                break;
            case InputStateHandler.StateName.PokemonStoragePartyNavigation:
                _currentNavState = PCNavState.ViewingParty;
                break;
        }
        ActivateCloseBoxAnimation();
        ActivatePkmDataAnimation();
        ActivateArrowAnimation();
    }

    public void UpdateBoxPosition(int newIndex)
    {
        currentIndexOfBox = newIndex;
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
        foreach (var arrow in boxChangeGreyArrows)
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
             
             if (movingPokemon)
             {
                 boxSelectorImage.sprite = boxSelectorSprites[2];
             }
             else
             {
                 boxSelectorImage.sprite = boxSelectorSprites[movingToTarget? 0:1];
             }
             
 
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
            if(!storageBoxes[currentBoxIndex].boxPokemon[currentPokemonIndex].containsPokemon)
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
        RemovePokemonIcons(false);
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
        ActivatePokemonIcons(false);
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
        ClearPokemonData();
        currentUsageState = newState;
        if(currentUsageState == PCUsageState.Withdraw)
        {
            if (Pokemon_party.Instance.numMembers==6)
            {
                Dialogue_handler.Instance.DisplayDetails("Party is full",3f);
                Game_ui_manager.Instance.ClosePokemonStorage();
                return;
            }
            InputStateHandler.Instance.SetupPokemonStorageState();
        }
        if(currentUsageState == PCUsageState.Deposit)
        {
            if (Pokemon_party.Instance.numMembers==1)
            {
                Dialogue_handler.Instance.DisplayDetails("There must be at least 1 pokemon in your team",2f);
                Game_ui_manager.Instance.ClosePokemonStorage();
                return;
            }
            initialSelector.transform.rotation = Quaternion.Euler(0, 0, 0);
            partyUI.SetActive(true);
            var partySelectables = new List<SelectableUI>();

            for (var i = 0 ;i < 6;i++)
            {
                var icon = partyPokemonIcons[i];
                partySelectables.Add( new(icon.gameObject,
                    i<Pokemon_party.Instance.numMembers?() => SelectPartyPokemon(icon):null, 
                    i<Pokemon_party.Instance.numMembers));
            }
            partySelectables.Add( new(exitParty,Game_ui_manager.Instance.ClosePokemonStorage,true) );
            
            InputStateHandler.Instance.ChangeInputState(new InputState(InputStateHandler.StateName.PokemonStoragePartyNavigation,
                new[]{InputStateHandler.StateGroup.PokemonStorage}
                , stateDirectional:InputStateHandler.Directional.Vertical, selectableUis:partySelectables
                ,selector: initialSelector
                ,selecting:true,display:true));

            ActivatePokemonIcons(true);
            LoadPokemonData(0);
        }
        if (currentUsageState == PCUsageState.Move)
        {
            InputStateHandler.Instance.SetupPokemonStorageState();
        }
        StartCoroutine(ActivateSelectorAnimation());
        ChangeBox(0);
    }

    public void ClosePC()
    {
        RemovePokemonIcons(true);
        RemovePokemonIcons(false);
        partyUI.SetActive(false);
        InputStateHandler.Instance.ResetGroupUi(InputStateHandler.StateGroup.PokemonStorage);
        StopAllCoroutines();
        currentIndexOfBox = 0;
    }

    void ResetOptions()
    {
        storageOptionsText.transform.parent.gameObject.SetActive(false);
        storageOptionsParent.SetActive(false);
        movePokemonUIOption.SetActive(false);
        storagePartyOptionsParent.SetActive(false);
    }

    private void RefreshStorageUi(bool isPartyIcons)
    { 
       RemovePokemonIcons(isPartyIcons);
       ActivatePokemonIcons(isPartyIcons); 
       InputStateHandler.Instance.RemoveTopInputLayer(true);
    }
    private void RemovePokemonIcons(bool isPartyIcons)
    {
        if (isPartyIcons)
        {
            foreach (var icon in partyPokemonIcons)
            {
                icon.pokemonSpriteBg.gameObject.SetActive(false);
                icon.gameObject.SetActive(false);
                icon.pokemon = null;
            }
            return;
        }

        foreach (var icon in nonPartyIcons)
        {
            icon.pokemon = null;
            icon.pokemonImage.sprite = null;
            icon.gameObject.SetActive(false);
        }
    }
    private void ActivatePokemonIcons(bool isPartyIcons)
    {
        if (isPartyIcons)
        {
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
            return;
        }

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
            Pokemon pokemonForBox = null;
            
            foreach (var pokemon in nonPartyPokemon)
            {
                if (pokemon.pokemonID.ToString() == storageBoxes[currentBoxIndex].boxPokemon[i].pokemonID)
                {
                    pokemonForBox = pokemon;
                    break;
                }
            }
      
            storageBoxes[currentBoxIndex].currentNumPokemon++;
            var pokemonIcon = nonPartyIcons[i];
            pokemonIcon.gameObject.SetActive(true);
            pokemonIcon.pokemon = pokemonForBox;
            pokemonIcon.LoadImage();
        }
    }
    
    private void ViewNonPartyPokemonDetails()
    {
        Game_ui_manager.Instance.ViewOtherPokemonDetails(nonPartyPokemon[SearchForPokemonIndex(selectedPokemonID)],nonPartyPokemon);
    }
    private void SelectPartyPokemon(PC_party_pkm icon)
    {
        //display options state
        var partyOptionsSelectables = new List<SelectableUI>
        {
            new(storagePartyOptions[0], ()=>RemoveFromParty(icon),true),
            new(storagePartyOptions[1], ()=>Game_ui_manager.Instance.ViewPartyPokemonDetails(icon.pokemon), true),
            new(storagePartyOptions[2], ()=>DeletePokemon(true,icon.partyPosition),true)
        };

        InputStateHandler.Instance.ChangeInputState(new InputState(InputStateHandler.StateName.PokemonStoragePartyOptions,
            new[]{InputStateHandler.StateGroup.PokemonStorage}
            , stateDirectional:InputStateHandler.Directional.Vertical,selectableUis: partyOptionsSelectables
            ,selector:boxOptionsSelector,selecting:true,display:true
            ,onClose:ResetOptions,onExit:ResetOptions));
        
        selectedPokemonID = icon.pokemon.pokemonID.ToString();
        storageOptionsText.transform.parent.gameObject.SetActive(true);
        storagePartyOptionsParent.SetActive(true);
        storageOptionsText.text = icon.pokemon.pokemonName + " is selected.";
    }
    
    public void SelectNonPartyPokemon(PC_pkm pokemonIcon)
    {
        if (movingPokemon)
        {
            if (pokemonIcon.isEmpty)
            {
                movingPokemon = false;
                selectedPokemonImage.gameObject.SetActive(false);
                storageBoxes[movingOperationData.previousBoxIndex].boxPokemon[movingOperationData.previousBoxPosition] = new StorageBoxPokemon
                {
                    pokemonID = string.Empty
                    ,containsPokemon = false
                };
                storageBoxes[currentBoxIndex].boxPokemon[currentIndexOfBox] = new StorageBoxPokemon
                {
                    pokemonID = movingOperationData.pokemonID
                    ,containsPokemon = true
                };
                ClearPokemonData();
                RemovePokemonIcons(false);
                ActivatePokemonIcons(false); 
            }
            else
            {
                storageBoxes[currentBoxIndex].boxPokemon[currentIndexOfBox] = new StorageBoxPokemon
                {
                    pokemonID = movingOperationData.pokemonID
                    ,containsPokemon = true
                };
                
                selectedPokemonID = pokemonIcon.pokemon.pokemonID.ToString();
                movingOperationData = new StorageBoxMovingData();
                movingOperationData.pokemonID = selectedPokemonID;
                movingOperationData.previousBoxIndex = currentBoxIndex;
                movingOperationData.previousBoxPosition = currentIndexOfBox;
                selectedPokemonImage.sprite = pokemonIcon.pokemon.partyFrame1;
                ClearPokemonData();
                RemovePokemonIcons(false);
                ActivatePokemonIcons(false);
            }
        }
        else
        {
            if (pokemonIcon.isEmpty) return;
            var boxOptionsSelectables = new List<SelectableUI>();
            if (currentUsageState == PCUsageState.Move)
            {
                boxOptionsSelectables.Add(new(movePokemonUIOption,StartMovingOperation, true));
                movePokemonUIOption.SetActive(true);
            }
            boxOptionsSelectables.Add(new(storageOptions[0], AddPokemonToParty, true));
            boxOptionsSelectables.Add(new(storageOptions[1], ViewNonPartyPokemonDetails, true));
            boxOptionsSelectables.Add(new(storageOptions[2], () => DeletePokemon(false), true));


            InputStateHandler.Instance.ChangeInputState(new InputState(InputStateHandler.StateName.PokemonStorageBoxOptions,
                new[]{InputStateHandler.StateGroup.PokemonStorage}
                , stateDirectional:InputStateHandler.Directional.Vertical,selectableUis: boxOptionsSelectables
                ,selector:boxOptionsSelector,selecting:true,display:true
                ,onClose:ResetOptions,onExit:ResetOptions));
        
            selectedPokemonID = pokemonIcon.pokemon.pokemonID.ToString();
            storageOptionsText.transform.parent.gameObject.SetActive(true);
            storageOptionsText.text = pokemonIcon.pokemon.pokemonName + " is selected.";
            storageOptionsParent.SetActive(true);
        }
        
    }
    private void RemoveFromParty(PC_party_pkm partyPokemon)
    {
        if (Pokemon_party.Instance.numMembers > 1)
        {
            storagePartyOptionsParent.SetActive(true);
            storageOptionsText.text = "Deposit into which BOX?";
            _currentNavState = PCNavState.SelectingBoxDeposit;
            
            foreach (var arrow in depositGreyArrows)
            {
                arrow.viewingUI = _currentNavState == PCNavState.SelectingBoxDeposit;
            }

            var boxSelection = new List<SelectableUI>();
            for(int i=0;i<NumBoxes;i++)
            {
                var boxNumber = i + 1;
                boxSelection.Add(new(null,()=>SendToPC(boxNumber,partyPokemon),true));
            }
            InputStateHandler.Instance.ChangeInputState(new InputState(InputStateHandler.StateName.PokemonStorageDepositSelection,
                new[]{InputStateHandler.StateGroup.PokemonStorage}
                , stateDirectional:InputStateHandler.Directional.Horizontal,selectableUis: boxSelection,selecting:true,onExit:RemoveDeposit));
            boxDepositUI.SetActive(true);
            DisplayBoxCapacity(0);
        }
        else
            Dialogue_handler.Instance.DisplayDetails("There must be at least 1 pokemon in your team",2f);
    }

    private void RemoveDeposit()
    {
        storageOptionsText.transform.parent.gameObject.SetActive(false);
        boxDepositUI.SetActive(false);
        InputStateHandler.Instance.RemoveTopInputLayer(false);
    }
    public void DisplayBoxCapacity(int boxIndex)
    {
        boxDepositText.text = $"BOX {boxIndex+1}\n"+storageBoxes[boxIndex].currentNumPokemon + "/" + BoxCapacity;
    }
    private void SendToPC(int boxNumber,PC_party_pkm partyPokemon)
    {
        var selectedBox = storageBoxes[boxNumber-1];
        if (selectedBox.currentNumPokemon < BoxCapacity)
        {
            RemoveDeposit();
            storagePartyOptionsParent.SetActive(false);
            nonPartyPokemon.Add(partyPokemon.pokemon);
            Pokemon_party.Instance.RemoveMember(partyPokemon.partyPosition);
            numPartyMembers--;
            numNonPartyPokemon++;
            
            selectedBox.currentNumPokemon++;
            selectedBox.boxPokemon[selectedBox.currentNumPokemon-1] = new StorageBoxPokemon
            {
                pokemonID = partyPokemon.pokemon.pokemonID.ToString()
                ,containsPokemon = true
            };
            RefreshStorageUi(true); 
            LoadPokemonData(partyPokemon.partyPosition-1);
        }
    }
    
    private void StartMovingOperation()
    {
        storageBoxes[currentBoxIndex].boxPokemon[currentIndexOfBox] = new StorageBoxPokemon
        {
            pokemonID = string.Empty
            ,containsPokemon = false
        };
        nonPartyIcons[currentIndexOfBox].pokemonImage.color = new Color(0,0,0,0);
        var pokemonIndex = SearchForPokemonIndex(selectedPokemonID);
        selectedPokemonImage.sprite = nonPartyPokemon[pokemonIndex].partyFrame1;
        selectedPokemonImage.gameObject.SetActive(true);
        movingPokemon = true;
        InputStateHandler.Instance.RemoveTopInputLayer(true);

        movingOperationData = new StorageBoxMovingData();
        movingOperationData.pokemonID = selectedPokemonID;
        movingOperationData.previousBoxIndex = currentBoxIndex;
        movingOperationData.previousBoxPosition = currentIndexOfBox;
    }
    private void DeletePokemon(bool isPartyPokemon,int partyPosition=0)
    {
        ResetOptions();
        if (isPartyPokemon)
        {
            Dialogue_handler.Instance.DisplayDetails("You released "+ Pokemon_party.Instance.party[partyPosition-1].pokemonName,2f);
            Pokemon_party.Instance.RemoveMember(partyPosition);
            numPartyMembers--;
            totalPokemonCount--;
            RefreshStorageUi(true);
            LoadPokemonData(partyPosition-1);
        }
        else
        {
            var indexToDelete= SearchForPokemonIndex(selectedPokemonID);
            Dialogue_handler.Instance.DisplayDetails("You released "+ nonPartyPokemon[indexToDelete].pokemonName,2f);
            DeleteNonPartyPokemon(indexToDelete);
            RefreshStorageUi(false);
            ClearPokemonData();
        }
        
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
    private void AddPokemonToParty()
    {
        if (Pokemon_party.Instance.numMembers < 6)
        {
            var pokemonIndex = SearchForPokemonIndex(selectedPokemonID);
            Pokemon_party.Instance.party[Pokemon_party.Instance.numMembers] = Obj_Instance.CreatePokemon(nonPartyPokemon[pokemonIndex]);
            DeleteNonPartyPokemon(pokemonIndex);
            Pokemon_party.Instance.numMembers++;
            numPartyMembers++;
            storageBoxes[currentBoxIndex].boxPokemon[currentIndexOfBox] = new StorageBoxPokemon
            {
                pokemonID = string.Empty
                ,containsPokemon = false
            };
            
            ClearPokemonData();
            RefreshStorageUi(false);
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
