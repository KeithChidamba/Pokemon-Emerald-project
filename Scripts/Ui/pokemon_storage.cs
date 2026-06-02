using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine.UI;
public enum PCUsageState
{
    Withdraw,Deposit,Move
};
public class pokemon_storage : MonoBehaviour,IInjectable
{
    public List<Pokemon> nonPartyPokemon = new();
    public int totalPokemonCount;
    public int numNonPartyPokemon;

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
    public GameObject partyOptionsSelector;
    public const int BoxCapacity = 30;
    public const int BoxColumns = 6;
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
    
    private bool _viewingPC;
    private StorageBoxMovingData movingOperationData;
    public GameObject movePokemonUIOption;
    public Image selectedPokemonImage;
    public bool movingPokemon;
    public event Action<Pokemon> OnPokemonWithdraw;
    public event Action<Pokemon> OnPokemonDeposit;
    private enum PCNavState
    {
        ViewingPokemonData,ExitingPC,ViewingBoxChange,SelectingBoxDeposit,ViewingParty
    }
    private PCNavState _currentNavState;

    public PCUsageState currentUsageState;
    public Text pokemonDataName;
    public Text pokemonLevel;
    public Image genderImage;
    public Image pokemonImage;
    public Image pokemonDataVisual;
    public Sprite[] pokemonDataVisualSprites;
    public GameObject partyUI;
    
    private Game_ui_manager _gameUIHandler;
    private Dialogue_handler _dialogueHandler;
    private InputStateHandler _inputStateHandler;
    private Pokemon_party _pokemonPartyHandler;
    private SaveDataHandler _saveDataHandler;
    private Game_Load _gameLoadingHandler;
    PokemonStorageInputService _pokemonStorageInputService;
    
    public void Inject(ServiceContainer container)
    {
        _pokemonStorageInputService = container.Resolve<PokemonStorageInputService>();
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _gameUIHandler = container.Resolve<Game_ui_manager>();
        _pokemonPartyHandler = container.Resolve<Pokemon_party>();
        _saveDataHandler = container.Resolve<SaveDataHandler>();
        _gameLoadingHandler = container.Resolve<Game_Load>();
        
        gameObject.SetActive(true);
    }

    public void OnInject()
    {
        _gameLoadingHandler.OnGameStarted += LoadPCStorageBoxes;
    }
    
    private void LoadPCStorageBoxes()
    {
        maxPokemonCapacity = BoxCapacity * NumBoxes;
        _inputStateHandler.OnStateChanged += CheckState;
        
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
      
        if (_gameLoadingHandler.LoadedFromSave)
        {
            var savedBoxes = _saveDataHandler.LoadPokemonStorageData();
            foreach (var boxData in savedBoxes)
            {
                storageBoxes[boxData.boxNumber-1].boxPokemon = boxData.boxPokemon;
                storageBoxes[boxData.boxNumber-1].currentNumPokemon = boxData.currentNumPokemon;
            }
        }
            
        nonPartyIcons.Clear();
        for (var i = 0; i < boxIconsParent.childCount; i++)
        {
            var pokemonIcon = boxIconsParent.GetChild(i).gameObject.GetComponent<PC_pkm>();
            pokemonIcon.SetImage();
            nonPartyIcons.Add(pokemonIcon);
        }
        foreach (var arrow in boxChangeGreyArrows)
        {
            arrow.LoadState();
        }
        
    }

    public IEnumerator SaveStorageData()
    {
        foreach (var box in storageBoxes)
        {
            _saveDataHandler.SaveStorageDataAsJson(box,"Box "+ box.boxNumber);
        }
        yield return null;
    }
    private void CheckState(InputState currentState)
    {
        _viewingPC = currentState.stateGroup==InputStateGroup.PokemonStorage;
        switch (currentState.stateName)
        {
            case InputStateName.PokemonStorageExit:
                _currentNavState = PCNavState.ExitingPC;
                break;
            case InputStateName.PokemonStorageBoxNavigation:
                _currentNavState = PCNavState.ViewingPokemonData;
                break;
            case InputStateName.PokemonStorageDepositSelection:
                _currentNavState = PCNavState.SelectingBoxDeposit;
                break;
            case InputStateName.PokemonStorageBoxChange:
                _currentNavState = PCNavState.ViewingBoxChange;
                break;
            case InputStateName.PokemonStoragePartyNavigation:
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
            yield return new WaitForSecondsRealtime(0.5f);
            storageBoxExit.sprite = storageBoxExitSprites[1];
            yield return new WaitForSecondsRealtime(0.5f);
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
            arrow.ChangeActiveState(_currentNavState == PCNavState.ViewingBoxChange);
        }
    }
    private IEnumerator SwitchPkmDataAnimationSprite()
    {
        while (_currentNavState==PCNavState.ViewingPokemonData)
        {
            pokemonDataVisual.sprite = pokemonDataVisualSprites[_pkmDataSpriteIndex];
            yield return new WaitForSecondsRealtime(0.5f);
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
                 target, 500 * Time.unscaledDeltaTime
             );
             
             if (Vector2.Distance(boxSelectorImage.rectTransform.anchoredPosition, target) < 0.01f)
                 movingToTarget = !movingToTarget;
             
             yield return new WaitForSecondsRealtime(0.25f);
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
            if (currentPokemonIndex == _pokemonPartyHandler.numMembers)
            {
                //the last index is the cancel button
                return;
            }
            pokemon = _pokemonPartyHandler.party[currentPokemonIndex];
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
        
        pokemonDataName.text = pokemon.nickName +"\n /"+pokemon.pokemonName;
        genderImage.gameObject.SetActive(true);
        if(pokemon.hasGender)
            genderImage.sprite = Utility.GetGenderSprite(pokemon.gender);
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

    private int SearchForPokemonIndex(string pokemonID)
    {
        return nonPartyPokemon.FindIndex(p => p.pokemonID.ToString() == pokemonID);
    }

    public void OpenPC(PCUsageState newState)
    {
        ClearPokemonData();
        currentUsageState = newState;
        if(currentUsageState == PCUsageState.Withdraw)
        {
            if (_pokemonPartyHandler.numMembers == _pokemonPartyHandler.maxNumMembers)
            {
                _dialogueHandler.DisplayDetails("Party is full");
                _gameUIHandler.ClosePokemonStorage();
                return;
            }
            _pokemonStorageInputService.SetupPokemonStorageState();
        }
        if(currentUsageState == PCUsageState.Deposit)
        {
            if (_pokemonPartyHandler.numMembers==1)
            {
                _dialogueHandler.DisplayDetails("There must be at least 2 pokemon in your team");
                _gameUIHandler.ClosePokemonStorage();
                return;
            }
            initialSelector.transform.rotation = Quaternion.Euler(0, 0, 0);
            partyUI.SetActive(true);
            var partySelectables = new List<SelectableUI>();

            for (var i = 0 ;i < _pokemonPartyHandler.numMembers; i++)
            {
                var icon = partyPokemonIcons[i];
                partySelectables.Add( new(icon.gameObject, () => SelectPartyPokemon(icon), true)); 
            }
            
            partySelectables.Add( new(exitParty,_gameUIHandler.ClosePokemonStorage,true) );
            
            _inputStateHandler.ChangeInputState(new  (InputStateName.PokemonStoragePartyNavigation, InputStateGroup.PokemonStorage
                , stateDirection:InputDirection.Vertical, selectableUis:partySelectables
                ,selector: initialSelector
                ,selecting:true,display:true, canManualExit:false));

            ActivatePokemonIcons(true);
            LoadPokemonData(0);
        }
        if (currentUsageState == PCUsageState.Move)
        {
            _pokemonStorageInputService.SetupPokemonStorageState();
        }
        StartCoroutine(ActivateSelectorAnimation());
        ChangeBox(0);
    }

    public void ClosePC()
    {
        RemovePokemonIcons(true);
        RemovePokemonIcons(false);
        partyUI.SetActive(false);
        _inputStateHandler.ResetGroupUi(InputStateGroup.PokemonStorage);
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
       _inputStateHandler.RemoveTopInputLayer(true);
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
            for (var i = 0;i < _pokemonPartyHandler.maxNumMembers; i++)
            {
                if (_pokemonPartyHandler.party[i] == null)
                {
                    partyPokemonIcons[i].gameObject.SetActive(false);
                    continue;
                }
                partyPokemonIcons[i].gameObject.SetActive(true);
                partyPokemonIcons[i].pokemon = _pokemonPartyHandler.party[i] ;
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
        _gameUIHandler.ViewPokemonDetails(nonPartyPokemon[SearchForPokemonIndex(selectedPokemonID)],nonPartyPokemon);
    }
    private void SelectPartyPokemon(PC_party_pkm icon)
    {
        if (icon.pokemon == null) return;
        //display options state
        var partyOptionsSelectables = new List<SelectableUI>
        {
            new(storagePartyOptions[0], ()=>RemoveFromParty(icon),true),
            new(storagePartyOptions[1], ()=>_gameUIHandler.ViewPartyPokemonDetails(icon.pokemon), true),
            new(storagePartyOptions[2], ()=>DeletePokemon(true,icon.partyPosition),true)
        };

        _inputStateHandler.ChangeInputState(new  (InputStateName.PokemonStoragePartyOptions, InputStateGroup.PokemonStorage
            , stateDirection:InputDirection.Vertical,selectableUis: partyOptionsSelectables
            ,selector:partyOptionsSelector,selecting:true,display:true
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
                movingOperationData = new StorageBoxMovingData
                {
                    pokemonID = selectedPokemonID,
                    previousBoxIndex = currentBoxIndex,
                    previousBoxPosition = currentIndexOfBox
                };
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


            _inputStateHandler.ChangeInputState(new  (InputStateName.PokemonStorageBoxOptions,
                InputStateGroup.PokemonStorage
                , stateDirection:InputDirection.Vertical,selectableUis: boxOptionsSelectables
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
        if (_pokemonPartyHandler.numMembers > 1)
        {
            storagePartyOptionsParent.SetActive(true);
            storageOptionsText.text = "Deposit into which BOX?";
            _currentNavState = PCNavState.SelectingBoxDeposit;
            
            foreach (var arrow in depositGreyArrows)
            {
                arrow.ChangeActiveState(_currentNavState == PCNavState.SelectingBoxDeposit);
            }

            var boxSelection = new List<SelectableUI>();
            for(int i=0;i<NumBoxes;i++)
            {
                var boxNumber = i + 1;
                boxSelection.Add(new(null,()=>SendToPC(boxNumber,partyPokemon),true));
            }
            _inputStateHandler.ChangeInputState(new  (InputStateName.PokemonStorageDepositSelection, InputStateGroup.PokemonStorage
                , stateDirection:InputDirection.Horizontal,selectableUis: boxSelection,selecting:true,onExit:RemoveDepositUI));
            boxDepositUI.SetActive(true);
            DisplayBoxCapacity(0);
        }
        else
            _dialogueHandler.DisplayDetails("There must be at least 1 pokemon in your team");
    }

    private void RemoveDepositUI()
    {
        storageOptionsText.transform.parent.gameObject.SetActive(false);
        boxDepositUI.SetActive(false);
        _inputStateHandler.RemoveTopInputLayer(false);
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
            OnPokemonDeposit?.Invoke(partyPokemon.pokemon);
            RemoveDepositUI();
            storagePartyOptionsParent.SetActive(false);
            nonPartyPokemon.Add(partyPokemon.pokemon);
            _pokemonPartyHandler.RemoveMember(partyPokemon.partyPosition);
            numNonPartyPokemon++;
            
            selectedBox.currentNumPokemon++;
            selectedBox.boxPokemon[selectedBox.currentNumPokemon-1] = new StorageBoxPokemon
            {
                pokemonID = partyPokemon.pokemon.pokemonID.ToString()
                ,containsPokemon = true
            };
            RemovePokemonIcons(true); 
            OpenPC(PCUsageState.Deposit);
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
        _inputStateHandler.RemoveTopInputLayer(true);

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
            _dialogueHandler.DisplayDetails("You released "+ _pokemonPartyHandler.party[partyPosition-1].pokemonName);
            _pokemonPartyHandler.RemoveMember(partyPosition);
            totalPokemonCount--;
            RefreshStorageUi(true);
        }
        else
        {
            var indexToDelete= SearchForPokemonIndex(selectedPokemonID);
            _dialogueHandler.DisplayDetails("You released "+ nonPartyPokemon[indexToDelete].pokemonName);
            DeleteNonPartyPokemon(indexToDelete);
            RefreshStorageUi(false);
        }
        ClearPokemonData();
    }
    private void DeleteNonPartyPokemon(int index)
    {
        nonPartyPokemon.Remove(nonPartyPokemon[index]);
        storageBoxes[currentBoxIndex].boxPokemon[currentIndexOfBox] = new StorageBoxPokemon
        {
            pokemonID = string.Empty
            ,containsPokemon = false
        };
        numNonPartyPokemon--;
        totalPokemonCount--;
    }
    
    public bool MaxPokemonCapacity()
    {
        return totalPokemonCount >= maxPokemonCapacity;
    }
    private void AddPokemonToParty()
    {
        if (_pokemonPartyHandler.numMembers < _pokemonPartyHandler.maxNumMembers)
        {
            var pokemonIndex = SearchForPokemonIndex(selectedPokemonID);
            OnPokemonWithdraw?.Invoke(nonPartyPokemon[pokemonIndex]);
            _pokemonPartyHandler.party[_pokemonPartyHandler.numMembers] = InstanceFactory.CreatePokemon(nonPartyPokemon[pokemonIndex]);
            DeleteNonPartyPokemon(pokemonIndex);
            _pokemonPartyHandler.numMembers++;
            storageBoxes[currentBoxIndex].boxPokemon[currentIndexOfBox] = new StorageBoxPokemon
            {
                pokemonID = string.Empty
                ,containsPokemon = false
            };
            
            ClearPokemonData();
            RefreshStorageUi(false);
        }
        else
            _dialogueHandler.DisplayDetails("Party is full");
    }

    public void AddPokemonToStorage(Pokemon newPokemon)
    {
       nonPartyPokemon.Add(newPokemon);
       numNonPartyPokemon++;
       totalPokemonCount++;
    }
}


