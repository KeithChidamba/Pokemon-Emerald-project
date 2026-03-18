using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Game_ui_manager : MonoBehaviour,IInjectable
{

    public GameObject menuOptions;
    public bool viewingMenu;
    public bool canUseUi=true;
    public Player_Info_ui profile;
    public static Game_ui_manager Instance;
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
    [SerializeField]private bool _canOpenMenu;
    public Image destinationPointerUI;
    
    private Item_handler _itemHandler;
    private Pokemon_Details _pokemonDetailsHandler;
    private Dialogue_handler _dialogueHandler;
    private Player_movement _playerMovementHandler;
    private InputStateHandler _inputStateHandler;
    private Options_manager _dialogueOptionsHandler;
    private Game_Load _gameLoadingHandler;
    private overworld_actions _overworldActionsHandler;
    private Save_manager _saveDataHandler;
    private Bag _playerBagHandler;
    private Poke_Mart _pokeMartHandler;
    private Pokemon_party _pokemonPartyHandler;
    private pokemon_storage _pokemonStorageHandler;
    private ItemStorageHandler _itemStorageHandler;
    
    public void Inject(Container container)
    {
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _dialogueOptionsHandler = container.Resolve<Options_manager>();
        _playerBagHandler = container.Resolve<Bag>();
        _pokeMartHandler = container.Resolve<Poke_Mart>();
        _pokemonPartyHandler = container.Resolve<Pokemon_party>();
        _pokemonDetailsHandler = container.Resolve<Pokemon_Details>();
        _saveDataHandler = container.Resolve<Save_manager>();
        _playerMovementHandler = container.Resolve<Player_movement>();
        _gameLoadingHandler = container.Resolve<Game_Load>();
        _overworldActionsHandler = container.Resolve<overworld_actions>();
        _itemHandler = container.Resolve<Item_handler>();
        _pokemonStorageHandler = container.Resolve<pokemon_storage>();
        _itemStorageHandler = container.Resolve<ItemStorageHandler>();
        gameObject.SetActive(true);
    }
    
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
        usingWebGl = Application.platform == RuntimePlatform.WebGLPlayer;
        exitButton.SetActive(!usingWebGl);
        if (usingWebGl) menuUiOptions.Remove(menuUiOptions.Last());//remove exit button
        canUseUi = false;
        _canOpenMenu = true;
        _gameLoadingHandler.OnGameStarted += () => canUseUi = true;
        _inputStateHandler.OnStateChanged += AllowMenuUsage;
    }

    private void AllowMenuUsage( InputState previousState)
    {
        _canOpenMenu = previousState.stateName == InputStateName.Empty;
    }
    private void Update()
    {
        if (_overworldActionsHandler == null) return;
        if (!canUseUi) return;
        if (Input.GetKeyDown(KeyCode.Space) && _canOpenMenu &&!viewingMenu)
        {
            ManageScreens(1);
            viewingMenu = true;
            ActivateUiElement(menuOptions,true);
            ActivateMenuSelection();
        }
    }
    private void ManageScreens(int change)
    {
        numUIScreensOpen += change;
        if (numUIScreensOpen < 0) numUIScreensOpen = 0;
        _overworldActionsHandler.usingUI = numUIScreensOpen>0;

        if (numUIScreensOpen == 0)
        {
            StartCoroutine(_playerMovementHandler.AllowPlayerMovement(0.3f));
        }
        else
            _playerMovementHandler.RestrictPlayerMovement();
        if (_dialogueOptionsHandler.playerInBattle)
        {
            _playerMovementHandler.RestrictPlayerMovement();
        }
    }

    private void ActivateMenuSelection()
    {
        var menuOptionsMethods = new List<Action>
        {
            ViewPokemonParty,()=>StartCoroutine(_saveDataHandler.SaveAllData()), ViewBag, ViewProfile
        };
        
        if (!usingWebGl) menuOptionsMethods.Add(_dialogueOptionsHandler.ExitGame);
        
        var menuSelectables = new List<SelectableUI>();
            
        for (var i =0; i<menuOptionsMethods.Count;i++)
            menuSelectables.Add( new(menuUiOptions[i],menuOptionsMethods[i],true) );
            
        _inputStateHandler.ChangeInputState(new (InputStateName.PlayerMenu,
            new[] { InputStateGroup.None},true,menuOptions,
            InputDirection.Vertical, menuSelectables,menuSelector,true
            , true,CloseMenu,CloseMenu)); }
    private void ActivateUiElement(GameObject ui,bool activated)
    {
        ui.SetActive(activated);
    }
    private void CloseProfile()
    {
        ManageScreens(-1);
        ActivateUiElement(profile.gameObject, false);
        profile.viewingProfile = false;
    }
    private void CloseKeyBinds()
    {
        ManageScreens(-1);
        ActivateUiElement(keyBindsUI, false);
    }
    private void ClosePokeMart()
    {
        ManageScreens(-1);
        ActivateUiElement(_pokeMartHandler.storeUI, false);
        _pokeMartHandler.ExitStore();
        _dialogueHandler.DisplayDetails("Have a great day!");
    }
    private void CloseBag()
    {
        ManageScreens(-1);
        _playerBagHandler.CloseBag();
        ActivateUiElement( _playerBagHandler.bagUI, false);
        ActivateUiElement( _playerBagHandler.bagOverlayUI, false);
    }
    private void CloseParty()
    {
        ManageScreens(-1);
        ActivateUiElement(_pokemonPartyHandler.partyUI.gameObject, false);
        _pokemonPartyHandler.ResetPartyState();
    }

    private void ClosePokemonDetails()
    {
        ManageScreens(-1);
        ActivateUiElement(_pokemonDetailsHandler.uiParent,false);
        _pokemonDetailsHandler.ResetDetailsState();
        _pokemonDetailsHandler.DeactivateDetailsUi();
    }
    private void CloseMenu()
    {
        if (!viewingMenu) return;
        ManageScreens(-1);
        ActivateUiElement(menuOptions, false);
        viewingMenu = false;
    }

    public void ClosePokemonStorage()
    {
        ManageScreens(-1);
        ActivateUiElement(_pokemonStorageHandler.storageUI,false);
        _pokemonStorageHandler.ClosePC();
    }
    public void ViewBag()
    {
        if (_playerBagHandler.currentBagUsage == BagUsage.SellingView)
        {
            var sellableItems = _playerBagHandler.allItems.Count(item => item.canBeSold);
            if (_playerBagHandler.allItems.Count == 0 || sellableItems==0)
            {
                _playerBagHandler.currentBagUsage = BagUsage.NormalView;
                _dialogueHandler.DisplayDetails("You have no items to sell");
                return;
            }
        }
        _dialogueHandler.EndDialogue();
        _playerBagHandler.OnBagOpened += SetBagInputState;
        _playerBagHandler.ViewBag();
    }

    private void SetBagInputState()
    {
        _playerBagHandler.OnBagOpened -= SetBagInputState;
        ActivateUiElement(_playerBagHandler.bagUI,true);
        ManageScreens(1);
        var bagSelectables = new List<SelectableUI>();

        foreach (var item in _playerBagHandler.bagItemsUI)
        {
            bagSelectables.Add( new(item.gameObject,null,true) );
        }
        
        _inputStateHandler.ChangeInputState(new (InputStateName.PlayerBagNavigation,
            new[] { InputStateGroup.Bag},true,
            _playerBagHandler.bagUI, InputDirection.Vertical, bagSelectables,
            _playerBagHandler.itemSelector,true,false,CloseBag,CloseBag));
        
        _playerBagHandler.bagOverlayUI.SetActive(!_playerBagHandler.storageView);
        _playerBagHandler.storageOverlayUI.SetActive(_playerBagHandler.storageView);
    }
    private void ViewProfile()
    {
        ManageScreens(1);
        ActivateUiElement(profile.gameObject,true);
        profile.LoadProfile(_gameLoadingHandler.playerData);
        _inputStateHandler.ChangeInputState(new (InputStateName.PlayerProfile
            ,new[] { InputStateGroup.None},isParent:true
            ,profile.gameObject,onExit:CloseProfile,onClose:CloseProfile));
    }
    public void ViewKeyBinds()
    {
        ManageScreens(1);
        ActivateUiElement(keyBindsUI,true);
        _inputStateHandler.ChangeInputState(new (InputStateName.KeyBinds
            ,new[] { InputStateGroup.None},isParent:true
            ,keyBindsUI,onExit:CloseKeyBinds,onClose:CloseProfile));
    }

    public void ViewPokemonParty()
    {
        if (_pokemonPartyHandler.numMembers < 1)
        {
            _dialogueHandler.DisplayDetails("There a no pokemon in your party");
            return;
        }
        ManageScreens(1);
        _dialogueHandler.EndDialogue();
        _pokemonPartyHandler.ClearSelectionUI();
        ActivateUiElement(_pokemonPartyHandler.partyUI, true);
        
        InputStateName partyUsageState;
        if (_itemHandler.usingItem)
        {
             partyUsageState = InputStateName.PokemonPartyItemUsage;
            _pokemonPartyHandler.UpdatePartyUsageMessage("Use on who?");
        }
        else
        {
            partyUsageState = InputStateName.PokemonPartyNavigation;
            _pokemonPartyHandler.UpdatePartyUsageMessage(_pokemonPartyHandler.swapOutNext?
                "Select a Pokemon to switch"
                :"Choose a pokemon");
        }
       
        var partySelectables = new List<SelectableUI>();

        for (var i = 0; i < _pokemonPartyHandler.numMembers; i++)
        {
            var memberNumber = i + 1;
            partySelectables.Add(new(_pokemonPartyHandler.memberCards[i].gameObject
                , () => _pokemonPartyHandler.SelectMember(memberNumber), true));
        }
        
        //closes the party
        partySelectables.Add(new(_pokemonPartyHandler.cancelButton.gameObject,
            _pokemonPartyHandler.ExitParty
            , true));
        _inputStateHandler.OnStateChanged += _pokemonPartyHandler.CheckStateUpdate;
        _pokemonPartyHandler.memberCards[0].ChangeVisibility(true);//initial visual set
        
        _inputStateHandler.ChangeInputState(new (partyUsageState,
            new[]{InputStateGroup.PokemonParty }, true,_pokemonPartyHandler.partyUI,
            InputDirection.Vertical, partySelectables, _pokemonPartyHandler.memberSelector
            , true, true,CloseParty,CloseParty,canManualExit:false,canExit:true));
        
        _pokemonPartyHandler.RefreshMemberCards();
       
    }
    public void ViewOtherPokemonDetails(Pokemon selectedPokemon,List<Pokemon> pokemonToView)
    { 
        ManageScreens(1);
        ActivateUiElement(_pokemonDetailsHandler.uiParent,true);
        var detailsSelectables = new List<SelectableUI>{
            new(null,null,true)
            ,new(null,null,true)
            ,new(null,_inputStateHandler.AllowMoveUiNavigation,true)
        };
        _inputStateHandler.ChangeInputState(new (InputStateName.PokemonDetails
            ,new[] { InputStateGroup.PokemonDetails}, true,_pokemonDetailsHandler.uiParent,
            InputDirection.Horizontal,detailsSelectables, null
            , true, false,ClosePokemonDetails,ClosePokemonDetails));
        
        _pokemonDetailsHandler.LoadDetails(selectedPokemon,pokemonToView);
    }
    public void ViewPartyPokemonDetails(Pokemon selectedPokemon)
    {
        ViewOtherPokemonDetails(selectedPokemon,_pokemonPartyHandler.GetValidPokemon());
    }

    public void ViewItemStorage()
    {
        _overworldActionsHandler.usingUI = true;
         var pcUsageSelectables = new List<SelectableUI>
        {
            new(pcItemOptions[0], _itemStorageHandler.ViewItemsToWithdraw, true),
            new(pcItemOptions[1], _itemStorageHandler.OpenBagToDepositItem, true),
            new(pcItemOptions[2], _itemStorageHandler.OpenBagToTossItem, true),
            new(pcItemOptions[3], ()=>ClosePCOptions(pcItemOptionsUI), true),
        };
        _inputStateHandler.ChangeInputState(new  (InputStateName.ItemStorageUsage,
            new[] { InputStateGroup.Bag},true,pcItemOptionsUI,
            InputDirection.Vertical, pcUsageSelectables,pcItemOptionSelector,true, true
            ,onExit:ClosePCItemOptions));
        
        pcItemOptionsUI.SetActive(true);
    }

    private void ClosePCItemOptions()
    {
        pcItemOptionsUI.SetActive(false);
        ManageScreens(0);
    }
    public void ViewPokemonStorage()
    {
        _overworldActionsHandler.usingUI = true;
        var pcUsageActions = new List<Action>
        {
            ()=>SetPcUsage(PCUsageState.Withdraw),
            ()=>SetPcUsage(PCUsageState.Deposit),
            ()=>SetPcUsage(PCUsageState.Move),
            ()=>ClosePCOptions(pcPokemonOptionsUI)
        };
        
        var pcUsageSelectables = new List<SelectableUI>();
        
        for (var i =0; i<pcUsageActions.Count;i++)
            pcUsageSelectables.Add( new(pcPokemonOptions[i],pcUsageActions[i],true) );
        
        _inputStateHandler.ChangeInputState(new  (InputStateName.PokemonStorageUsage,
            new[] { InputStateGroup.PokemonStorage},false,pcPokemonOptionsUI,
            InputDirection.Vertical, pcUsageSelectables,pcOptionSelector,true, true,canManualExit:false));
        pcPokemonOptionsUI.SetActive(true);
    }

    private void ClosePCOptions(GameObject ui)
    {
        _inputStateHandler.RemoveTopInputLayer(false);
        ui.SetActive(false);
        ManageScreens(0);
    }
    private void SetPcUsage(PCUsageState currentUsageState)
    {
        pcPokemonOptionsUI.SetActive(false);
        ManageScreens(1);
        ActivateUiElement(_pokemonStorageHandler.storageUI, true);
        _pokemonStorageHandler.OpenPC(currentUsageState);
    }
    public void ViewPokeMart()
    {
        ManageScreens(1);
        ActivateUiElement(_pokeMartHandler.storeUI,true);
        var martSelectables = new List<SelectableUI>();
        foreach(var item in _pokeMartHandler.storeItemsUI) 
            martSelectables.Add( new(item.gameObject,null,true) );
        _inputStateHandler.ChangeInputState(new  (InputStateName.MartItemNavigation
            ,new[] { InputStateGroup.PokeMart },true,
            _pokeMartHandler.storeUI, InputDirection.Vertical, martSelectables,
            _pokeMartHandler.itemSelector,true,true,ClosePokeMart,ClosePokeMart));
    }
}
