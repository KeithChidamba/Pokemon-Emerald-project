using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class PlayerProfileUI
{
    public Text playerName;
    public Text playerMoney;
    public Text trainerID;
    public GameObject parentObject;
    public void LoadProfile(PlayerData player)
    {
        trainerID.text = "ID: "+player.trainerID;
        playerName.text = player.playerName;
        playerMoney.text = player.playerMoney.ToString();
    }
}

public class Game_ui_manager : MonoBehaviour,IInjectable
{
    public GameObject menuOptions;
    [SerializeField]private bool viewingMenu;
    public bool usingUI;
    private bool _canUseUi;
    [SerializeField]private PlayerProfileUI profile;

    [SerializeField]private int numUIScreensOpen;
    [SerializeField]private GameObject exitButton;
    [SerializeField]private List<GameObject> menuUiOptions = new ();
    public GameObject menuSelector;
    public GameObject pcOptionSelector;
    public GameObject pcItemOptionSelector;
    public GameObject[] pcPokemonOptions;
    public GameObject[] pcItemOptions;
    public GameObject pcPokemonOptionsUI;
    public GameObject pcItemOptionsUI;
    public GameObject keyBindsUI;
    public bool usingWebGl;
    [SerializeField]private bool canOpenMenu;
    public Image destinationPointerUI;
    public Image blackFadingScreen;
    
    private Pokemon_Details _pokemonDetailsHandler;
    private Dialogue_handler _dialogueHandler;
    private Player_movement _playerMovementHandler;
    private InputStateHandler _inputStateHandler;
    private PokemonDetailsInputService _pokemonDetailsInputService;
    private DialogueOptionsEventHandler _dialogueOptionsHandler;
    private Game_Load _gameLoadingHandler;
    private SaveDataHandler _saveDataHandler;
    private Bag _playerBagHandler;
    private Poke_Mart _pokeMartHandler;
    private Pokemon_party _pokemonPartyHandler;
    private pokemon_storage _pokemonStorageHandler;
    private ItemStorageHandler _itemStorageHandler;
    private GameSettingsHandler _gameSettingsHandler;
    private TypingInterfaceHandler _typingInterfaceHandler;
    
    public void Inject(ServiceContainer container)
    {
        _pokemonDetailsInputService = container.Resolve<PokemonDetailsInputService>();
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _dialogueOptionsHandler = container.Resolve<DialogueOptionsEventHandler>();
        _playerBagHandler = container.Resolve<Bag>();
        _pokeMartHandler = container.Resolve<Poke_Mart>();
        _pokemonPartyHandler = container.Resolve<Pokemon_party>();
        _pokemonDetailsHandler = container.Resolve<Pokemon_Details>();
        _saveDataHandler = container.Resolve<SaveDataHandler>();
        _playerMovementHandler = container.Resolve<Player_movement>();
        _gameLoadingHandler = container.Resolve<Game_Load>();
        _pokemonStorageHandler = container.Resolve<pokemon_storage>();
        _itemStorageHandler = container.Resolve<ItemStorageHandler>();
        _gameSettingsHandler = container.Resolve<GameSettingsHandler>();
        _typingInterfaceHandler = container.Resolve<TypingInterfaceHandler>();
        
        gameObject.SetActive(true);
    }

    public void OnInject()
    {
        usingWebGl = Application.platform == RuntimePlatform.WebGLPlayer;
        exitButton.SetActive(!usingWebGl);
        if (usingWebGl) menuUiOptions.Remove(menuUiOptions.Last());//remove exit button
        _canUseUi = false;
        canOpenMenu = true;
        _gameLoadingHandler.OnGameStarted += () => _canUseUi = true;
    }
    public IEnumerator FadeInBlackScreen(float duration=0.25f)
    {
        blackFadingScreen.gameObject.SetActive(true);
        yield return Utility.FadeImage(blackFadingScreen,Color.black,duration);
    }
    public void RemoveBlackScreen()
    {
        blackFadingScreen.gameObject.SetActive(false);
    }
    public void SetMenuAccessibility(bool isAccessible)
    {
        canOpenMenu = isAccessible;
    }

    private void Update()
    {
        if (!_canUseUi) return;
        if (InputSourceHandler.InputPressed(ControlEvent.OpenMenu) && _inputStateHandler.IsEmptyState && canOpenMenu &&!viewingMenu)
        {
            AddScreen();
            viewingMenu = true;
            ActivateMenuSelection();
        }
    }
    private void AddScreen()
    {
        numUIScreensOpen++;
        usingUI = true;
        _playerMovementHandler.RestrictPlayerMovement(MovementRestrictor.UI);
    }
    private void RemoveScreen()
    {
        numUIScreensOpen--;
        if (numUIScreensOpen < 0)
        {
            Debug.LogWarning("duplicate call, or edge case");
            numUIScreensOpen = 0;
        }
        if (numUIScreensOpen == 0)
        {
            usingUI = false;
            _playerMovementHandler.AllowPlayerMovement(MovementRestrictor.UI,0.25f);
        }
    }
    private void ActivateMenuSelection()
    {
        var menuOptionsMethods = new List<Action>
        {
            ()=>ViewPokemonParty(PartyUsage.General), SaveGame, ValidateBagView, ViewProfile, ViewGameSettings
        };
        
        if (!usingWebGl) menuOptionsMethods.Add(_dialogueOptionsHandler.ExitGame);
        
        var menuSelectables = new List<SelectableUI>();
            
        for (var i =0; i<menuOptionsMethods.Count;i++)
            menuSelectables.Add( new(menuUiOptions[i],menuOptionsMethods[i],true) );
            
        _inputStateHandler.ChangeInputState(new (InputStateName.PlayerMenu,
            InputStateGroup.None,true,menuOptions,
            InputDirection.Vertical, menuSelectables,menuSelector,true
            , true,CloseMenu,CloseMenu)); 
        
        void CloseMenu()
        {
            if (!viewingMenu) return;
            RemoveScreen();
            viewingMenu = false;
        }
    }

    public void SaveGame()
    {
        StartCoroutine(_saveDataHandler.SaveAllData());
    }

    public void ValidateBagView()
    {
        if (_playerBagHandler.allItems.Count == 0)
        {
            _dialogueHandler.DisplayDetails("You have no items");
            _playerBagHandler.CloseBag();
            return;
        }

        if (_playerBagHandler.currentBagUsage == BagUsage.SellingView)
        {
            var sellableItems = _playerBagHandler.allItems.Count(item => item.canBeSold);
            if (sellableItems==0)
            {
                _dialogueHandler.DisplayDetails("You have no items to sell");
                _playerBagHandler.CloseBag();
                return;
            }
        }
        _dialogueHandler.EndDialogue();
        _playerBagHandler.OnBagOpened += ViewBagUI;
        _playerBagHandler.SetupBagState(true);
        void ViewBagUI()
        {
            _playerBagHandler.OnBagOpened -= ViewBagUI;
            AddScreen();
        }
    }
    
    public void SetBagInputState(bool displayTransition)
    {
        var bagSelectables = new List<SelectableUI>();
        for(var i = 0;i < _playerBagHandler.numItems;i++)
        {
            bagSelectables.Add( new(_playerBagHandler.bagItemsUI[i].gameObject,null,true) );
        }
        
        _inputStateHandler.ChangeInputState(new (InputStateName.PlayerBagNavigation,
            InputStateGroup.Bag,true,
            _playerBagHandler.bagUI, InputDirection.Vertical, bagSelectables,
            _playerBagHandler.itemSelector,true,false,CloseBag,CloseBag,
            displayOpenTransition:displayTransition,displayCloseTransition:true),true);
        
        _playerBagHandler.bagOverlayUI.SetActive(!_playerBagHandler.storageView);
        _playerBagHandler.storageOverlayUI.SetActive(_playerBagHandler.storageView);

        void CloseBag()
        {
            RemoveScreen();
            _playerBagHandler.CloseBag();
            _playerBagHandler.bagOverlayUI.SetActive(false);
            _playerBagHandler.storageOverlayUI.SetActive(false);
        }
    }
    
    private void ViewProfile()
    {
        AddScreen();
        profile.LoadProfile(_gameLoadingHandler.playerData);
        _inputStateHandler.ChangeInputState(new (InputStateName.PlayerProfile
            ,InputStateGroup.None,isParent:true
            ,profile.parentObject,onExit:RemoveScreen,onClose:RemoveScreen,displayOpenTransition:true));
    }
    public void ViewKeyBinds()
    {
        AddScreen();
        _inputStateHandler.ChangeInputState(new (InputStateName.KeyBinds,InputStateGroup.None,isParent:true
            ,keyBindsUI,onExit:RemoveScreen,onClose:RemoveScreen));
    }

    public void ViewPokemonParty(PartyUsage partyUsage)
    {
        if (_pokemonPartyHandler.numMembers < 1)
        {
            _dialogueHandler.DisplayDetails("There a no pokemon in your party");
            return;
        }
        AddScreen();
        _dialogueHandler.EndDialogue();
        _pokemonPartyHandler.ClearSelectionUI();

        _pokemonPartyHandler.currentUsage = partyUsage;
        
        InputStateName partyUsageState;
        if (partyUsage==PartyUsage.ItemUsage)
        {
             partyUsageState = InputStateName.PokemonPartyItemUsage;
            _pokemonPartyHandler.UpdatePartyUsageMessage("Use on who?");
        }
        else
        {
            partyUsageState = InputStateName.PokemonPartyNavigation;
            _pokemonPartyHandler.UpdatePartyUsageMessage(partyUsage == PartyUsage.SwapOut?
                "Select a Pokemon to switch"
                :"Choose a pokemon");
        }
       
        var partySelectables = new List<SelectableUI>();
        
        _pokemonPartyHandler.RefreshMemberCards();
        
        for (var i = 0; i < _pokemonPartyHandler.numMembers; i++)
        {
            var pokemonIndex = i;
            partySelectables.Add(new(_pokemonPartyHandler.memberCards[i].gameObject
                , () => _pokemonPartyHandler.SelectMember(pokemonIndex), true));
        }
        
        //closes the party
        partySelectables.Add(new(_pokemonPartyHandler.cancelButton.gameObject,
            _pokemonPartyHandler.ValidatePartyExit
            , true));
        
        _inputStateHandler.OnStateChanged += _pokemonPartyHandler.CheckStateUpdate;
        _pokemonPartyHandler.memberCards[0].ChangeVisibility(true);//initial visual set
        
        _inputStateHandler.ChangeInputState(new (partyUsageState,
            InputStateGroup.PokemonParty, true,_pokemonPartyHandler.partyUI,
            InputDirection.Vertical, partySelectables, _pokemonPartyHandler.memberSelector
            , true, true,CloseParty,CloseParty,canManualExit:false,canExit:true
            ,displayOpenTransition:true));
        
        void CloseParty()
        {
            _pokemonPartyHandler.ResetPartyState();
            RemoveScreen();
        }
    }
    public void ViewPokemonDetails(Pokemon initiallySelectedPokemon,List<Pokemon> pokemonToView)
    { 
        AddScreen();
        var detailsSelectables = new List<SelectableUI>{
            new(null,null,true)
            ,new(null,null,true)
            ,new(null,_pokemonDetailsInputService.AllowMoveUiNavigation,true)
        };
        _inputStateHandler.ChangeInputState(new (InputStateName.PokemonDetails
            ,InputStateGroup.PokemonDetails, true,_pokemonDetailsHandler.uiParent,
            InputDirection.Horizontal,detailsSelectables, null
            , true, false,ClosePokemonDetails,ClosePokemonDetails
            ,displayOpenTransition:true));
        
        _pokemonDetailsHandler.LoadDetails(initiallySelectedPokemon,pokemonToView);
        
        void ClosePokemonDetails()
        {
            RemoveScreen();
            _pokemonDetailsHandler.ResetDetailsState();
            _pokemonDetailsHandler.DeactivateDetailsUi();
        }
    }
    public void ViewPartyPokemonDetails(Pokemon selectedPokemon)
    {
        ViewPokemonDetails(selectedPokemon,_pokemonPartyHandler.GetValidPokemon());
    }

    public void ViewItemStorage()
    {
        AddScreen();
         var pcUsageSelectables = new List<SelectableUI>
        {
            new(pcItemOptions[0], _itemStorageHandler.ViewItemsToWithdraw, true),
            new(pcItemOptions[1], _itemStorageHandler.OpenBagToDepositItem, true),
            new(pcItemOptions[2], _itemStorageHandler.OpenBagToTossItem, true),
            new(pcItemOptions[3], ()=>_inputStateHandler.ResetRelevantUi(InputStateName.ItemStorageUsage), true),
        };
        _inputStateHandler.ChangeInputState(new  (InputStateName.ItemStorageUsage,
            InputStateGroup.Bag,true,pcItemOptionsUI,
            InputDirection.Vertical, pcUsageSelectables,pcItemOptionSelector,true, true
            ,onExit:ClosePCItemOptions,onClose:ClosePCItemOptions));
        void ClosePCItemOptions()
        {
            pcItemOptionsUI.SetActive(false);
            RemoveScreen();
        }
    }


    public void ViewPokemonStorage()
    {
        AddScreen();
        var pcUsageActions = new List<Action>
        {
            ()=>SetPokemonPcUsage(PCUsageState.Withdraw),
            ()=>SetPokemonPcUsage(PCUsageState.Deposit),
            ()=>SetPokemonPcUsage(PCUsageState.Move),
            ()=>_inputStateHandler.ResetRelevantUi(InputStateName.PokemonStorageUsage)
        };
        
        var pcUsageSelectables = new List<SelectableUI>();
        
        for (var i =0; i<pcUsageActions.Count;i++)
            pcUsageSelectables.Add( new(pcPokemonOptions[i],pcUsageActions[i],true) );
        
        _inputStateHandler.ChangeInputState(new  (InputStateName.PokemonStorageUsage,
            InputStateGroup.PokemonStorage,true,pcPokemonOptionsUI,
            InputDirection.Vertical, pcUsageSelectables,pcOptionSelector,true, true,
            onClose:ClosePokemonPCOptions,onExit:ClosePokemonPCOptions,displayOpenTransition:true));
        
        void ClosePokemonPCOptions()
        {
            pcPokemonOptionsUI.SetActive(false);
            RemoveScreen();
        }
    }
    private void SetPokemonPcUsage(PCUsageState currentUsageState)
    {
        pcPokemonOptionsUI.SetActive(false);
        _pokemonStorageHandler.OpenPC(currentUsageState);
    }
    public void ClosePokemonStorage()
    {
        RemoveScreen();
    }
    public void ViewPokeMart()
    {
        AddScreen();
        var martSelectables = new List<SelectableUI>();
        foreach(var item in _pokeMartHandler.storeItemsUI)
        {
            martSelectables.Add(new(item.gameObject, null, true));
        }
        _inputStateHandler.ChangeInputState(new  (InputStateName.MartItemNavigation
            ,InputStateGroup.PokeMart,true,
            _pokeMartHandler.storeUI, InputDirection.Vertical, martSelectables,
            _pokeMartHandler.itemSelector,true,true,ClosePokeMart,ClosePokeMart));
        void ClosePokeMart()
        {
            _pokeMartHandler.ExitStore();
            _dialogueHandler.DisplayDetails("Have a great day!");
            RemoveScreen(); 
        }
    }

    public void ViewGameSettings()
    {
        AddScreen();
        var gameSettingsSelectables = new List<SelectableUI>();

        for (int i = 0; i<_gameSettingsHandler.gameSettings.Count; i++)
        {
            gameSettingsSelectables.Add( new(_gameSettingsHandler.gameSettingsHeading[i]
                ,null,true) );
        }
        
        _inputStateHandler.ChangeInputState(new  (InputStateName.GameSettingsNavigation,
            InputStateGroup.GameSettings,true,_gameSettingsHandler.mainUI,
            InputDirection.Vertical, gameSettingsSelectables,_gameSettingsHandler.whiteSelector,true, true
            ,onExit:CloseSettings,onClose:CloseSettings));
        
        void CloseSettings()
        {
            _gameSettingsHandler.SetCurrentSetting(0);
            RemoveScreen();
        }
    }
    public void ViewGameSettingsOptions()
    {
        var gameSettingsSelectables = new List<SelectableUI>();
        foreach (var option in _gameSettingsHandler.currentSetting.settingOptions)
        {
            gameSettingsSelectables.Add(new(option.gameObject, null, true));
        }

        _inputStateHandler.ChangeInputState(new(
            InputStateName.GameSettingOptionsNavigation,
            InputStateGroup.GameSettings,
            stateDirection:InputDirection.Horizontal,
            selectableUis:gameSettingsSelectables,
            selecting:true,
            onExit: CloseSettingsFull));
        
        var savedOptionIndex = _gameSettingsHandler.GetCurrentOptionIndex();
        _inputStateHandler.SetSelectionIndex(savedOptionIndex);
        _gameSettingsHandler.SetOptionTextColor(savedOptionIndex);
        
        void CloseSettingsFull()
        {
            _inputStateHandler.ResetGroupUi(InputStateGroup.GameSettings);
        }
    }

    public void ViewTypingInterface(Action<string> alertTextInputReceiver,int inputLength,TypingInterfaceGraphicData graphicData)
    {
        AddScreen();
        _typingInterfaceHandler.OnInputResolved += alertTextInputReceiver;
        _typingInterfaceHandler.OnInputResolved += (input) => RemoveScreen();
        _typingInterfaceHandler.InitializeState(inputLength,graphicData);
    }

}
