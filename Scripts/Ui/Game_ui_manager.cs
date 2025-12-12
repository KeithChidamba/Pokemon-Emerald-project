using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class Game_ui_manager : MonoBehaviour
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
    public bool usingWebGl = false;
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
        Game_Load.Instance.OnGameStarted += () => canUseUi = true;
    }

    private void Update()
    {
        if (overworld_actions.Instance == null) return;
        if (!canUseUi) return;
        UiInputs();
    }
    private void ManageScreens(int change)
    {
        numUIScreensOpen += change;
        if (numUIScreensOpen < 0) numUIScreensOpen = 0;
        overworld_actions.Instance.usingUI = numUIScreensOpen>0;

        if (numUIScreensOpen == 0)
        {
            StartCoroutine(Player_movement.Instance.AllowPlayerMovement(0.3f));
        }
        else
            Player_movement.Instance.RestrictPlayerMovement();
        if (Options_manager.Instance.playerInBattle)
        {
            Player_movement.Instance.RestrictPlayerMovement();
        }
    }
    private void UiInputs()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !overworld_actions.Instance.usingUI && !overworld_actions.Instance.doingAction &&!viewingMenu)
        {
            ManageScreens(1);
            viewingMenu = true;
            ActivateUiElement(menuOptions,true);
            ActivateMenuSelection();
        }
    } 
    private void ActivateMenuSelection()
    {
        var menuOptionsMethods = new List<Action>
        {
            ViewPokemonParty,()=>StartCoroutine(Save_manager.Instance.SaveAllData()), ViewBag, ViewProfile
        };
        
        if (!usingWebGl) menuOptionsMethods.Add(Options_manager.Instance.ExitGame);
        
        var menuSelectables = new List<SelectableUI>();
            
        for (var i =0; i<menuOptionsMethods.Count;i++)
            menuSelectables.Add( new(menuUiOptions[i],menuOptionsMethods[i],true) );
            
        InputStateHandler.Instance.ChangeInputState(new InputState(InputStateHandler.StateName.PlayerMenu,
            new[] { InputStateHandler.StateGroup.None},true,menuOptions,
            InputStateHandler.Directional.Vertical, menuSelectables,menuSelector,true
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
    private void ClosePokeMart()
    {
        ManageScreens(-1);
        ActivateUiElement(Poke_Mart.Instance.storeUI, false);
        Poke_Mart.Instance.ExitStore();
        Dialogue_handler.Instance.DisplayDetails("Have a great day!",1f);
    }
    private void CloseBag()
    {
        ManageScreens(-1);
        Bag.Instance.CloseBag();
        ActivateUiElement( Bag.Instance.bagUI, false);
        ActivateUiElement( Bag.Instance.bagOverlayUI, false);
    }
    private void CloseParty()
    {
        ManageScreens(-1);
        ActivateUiElement(Pokemon_party.Instance.partyUI.gameObject, false);
        Pokemon_party.Instance.ResetPartyState();
    }

    private void ClosePokemonDetails()
    {
        ManageScreens(-1);
        ActivateUiElement(Pokemon_Details.Instance.uiParent,false);
        Pokemon_Details.Instance.ResetDetailsState();
        Pokemon_Details.Instance.DeactivateDetailsUi();
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
        ActivateUiElement(pokemon_storage.Instance.storageUI,false);
        pokemon_storage.Instance.ClosePC();
    }
    public void ViewBag()
    {
        if (Bag.Instance.currentBagUsage == Bag.BagUsage.SellingView)
        {
            var sellableItems = Bag.Instance.allItems.Count(item => item.canBeSold);
            if (Bag.Instance.allItems.Count == 0 || sellableItems==0)
            {
                Bag.Instance.currentBagUsage = Bag.BagUsage.NormalView;
                Dialogue_handler.Instance.DisplayDetails("You have no items to sell", 2f);
                return;
            }
        }
        Dialogue_handler.Instance.EndDialogue();
        Bag.Instance.OnBagOpened += SetBagInputState;
        Bag.Instance.ViewBag();
    }

    private void SetBagInputState()
    {
        Bag.Instance.OnBagOpened -= SetBagInputState;
        ActivateUiElement(Bag.Instance.bagUI,true);
        ManageScreens(1);
        var bagSelectables = new List<SelectableUI>();

        foreach (var item in Bag.Instance.bagItemsUI)
        {
            bagSelectables.Add( new(item.gameObject,null,true) );
        }
        
        InputStateHandler.Instance.ChangeInputState(new InputState(InputStateHandler.StateName.PlayerBagNavigation,
            new[] { InputStateHandler.StateGroup.Bag},true,
            Bag.Instance.bagUI, InputStateHandler.Directional.Vertical, bagSelectables,
            Bag.Instance.itemSelector,true,false,CloseBag,CloseBag));
        
        Bag.Instance.bagOverlayUI.SetActive(!Bag.Instance.storageView);
        Bag.Instance.storageOverlayUI.SetActive(Bag.Instance.storageView);
    }
    private void ViewProfile()
    {
        ManageScreens(1);
        ActivateUiElement(profile.gameObject,true);
        profile.LoadProfile(Game_Load.Instance.playerData);
        InputStateHandler.Instance.ChangeInputState(new InputState(InputStateHandler.StateName.PlayerProfile
            ,new[] { InputStateHandler.StateGroup.None},isParent:true
            ,profile.gameObject,onClose:CloseProfile,onExit:CloseProfile));
    }

    public void ViewPokemonParty()
    {
        if (Pokemon_party.Instance.numMembers < 1)
        {
            Dialogue_handler.Instance.DisplayDetails("There a no pokemon in your party");
            return;
        }
        ManageScreens(1);
        Dialogue_handler.Instance.EndDialogue();
        Pokemon_party.Instance.ClearSelectionUI();
        ActivateUiElement(Pokemon_party.Instance.partyUI, true);
        
        InputStateHandler.StateName partyUsageState;
        if (Item_handler.Instance.usingItem)
        {
             partyUsageState = InputStateHandler.StateName.PokemonPartyItemUsage;
            Pokemon_party.Instance.UpdatePartyUsageMessage("Use on who?");
        }
        else
        {
            partyUsageState = InputStateHandler.StateName.PokemonPartyNavigation;
            Pokemon_party.Instance.UpdatePartyUsageMessage(Pokemon_party.Instance.swapOutNext?
                "Select a Pokemon to switch"
                :"Choose a pokemon");
        }
       
        var partySelectables = new List<SelectableUI>();

        for (var i = 0; i < Pokemon_party.Instance.numMembers; i++)
        {
            var memberNumber = i + 1;
            partySelectables.Add(new(Pokemon_party.Instance.memberCards[i].gameObject
                , () => Pokemon_party.Instance.SelectMember(memberNumber), true));
        }
        
        //closes the party
        partySelectables.Add(new(Pokemon_party.Instance.cancelButton.gameObject,
            Pokemon_party.Instance.ExitParty
            , true));
        InputStateHandler.Instance.OnStateChanged += Pokemon_party.Instance.CheckStateUpdate;
        Pokemon_party.Instance.memberCards[0].ChangeVisibility(true);//initial visual set
        
        InputStateHandler.Instance.ChangeInputState(new InputState(partyUsageState,
            new[]{InputStateHandler.StateGroup.PokemonParty }, true,Pokemon_party.Instance.partyUI,
            InputStateHandler.Directional.Vertical, partySelectables, Pokemon_party.Instance.memberSelector
            , true, true,CloseParty,CloseParty,canManualExit:false,canExit:true));
        
        Pokemon_party.Instance.RefreshMemberCards();
       
    }
    public void ViewOtherPokemonDetails(Pokemon selectedPokemon,List<Pokemon> pokemonToView)
    { 
        ManageScreens(1);
        ActivateUiElement(Pokemon_Details.Instance.uiParent,true);
        var detailsSelectables = new List<SelectableUI>{
            new(null,null,true)
            ,new(null,null,true)
            ,new(null,InputStateHandler.Instance.AllowMoveUiNavigation,true)
        };
        InputStateHandler.Instance.ChangeInputState(new InputState(InputStateHandler.StateName.PokemonDetails
            ,new[] { InputStateHandler.StateGroup.PokemonDetails}, true,Pokemon_Details.Instance.uiParent,
            InputStateHandler.Directional.Horizontal,detailsSelectables, null
            , true, false,ClosePokemonDetails,ClosePokemonDetails));
        
        Pokemon_Details.Instance.LoadDetails(selectedPokemon,pokemonToView);
    }
    public void ViewPartyPokemonDetails(Pokemon selectedPokemon)
    {
        ViewOtherPokemonDetails(selectedPokemon,Pokemon_party.Instance.GetValidPokemon());
    }

    public void ViewItemStorage()
    {
         var pcUsageSelectables = new List<SelectableUI>
        {
            new(pcItemOptions[0], ItemStorageHandler.Instance.ViewItemsToWithdraw, true),
            new(pcItemOptions[1], ItemStorageHandler.Instance.OpenBagToDepositItem, true),
            new(pcItemOptions[2], ItemStorageHandler.Instance.OpenBagToTossItem, true),
            new(pcItemOptions[3], ()=>ClosePCOptions(pcItemOptionsUI), true),
        };
        InputStateHandler.Instance.ChangeInputState(new InputState(InputStateHandler.StateName.ItemStorageUsage,
            new[] { InputStateHandler.StateGroup.Bag},true,pcItemOptionsUI,
            InputStateHandler.Directional.Vertical, pcUsageSelectables,pcItemOptionSelector,true, true
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
        var pcUsageActions = new List<Action>
        {
            ()=>SetPcUsage(pokemon_storage.PCUsageState.Withdraw),
            ()=>SetPcUsage(pokemon_storage.PCUsageState.Deposit),
            ()=>SetPcUsage(pokemon_storage.PCUsageState.Move),
            ()=>ClosePCOptions(pcPokemonOptionsUI)
        };
        
        var pcUsageSelectables = new List<SelectableUI>();
        
        for (var i =0; i<pcUsageActions.Count;i++)
            pcUsageSelectables.Add( new(pcPokemonOptions[i],pcUsageActions[i],true) );
        
        InputStateHandler.Instance.ChangeInputState(new InputState(InputStateHandler.StateName.PokemonStorageUsage,
            new[] { InputStateHandler.StateGroup.PokemonStorage},false,pcPokemonOptionsUI,
            InputStateHandler.Directional.Vertical, pcUsageSelectables,pcOptionSelector,true, true,canManualExit:false));
        pcPokemonOptionsUI.SetActive(true);
    }

    private void ClosePCOptions(GameObject ui)
    {
        InputStateHandler.Instance.RemoveTopInputLayer(false);
        ui.SetActive(false);
        ManageScreens(0);
    }
    private void SetPcUsage(pokemon_storage.PCUsageState currentUsageState)
    {
        pcPokemonOptionsUI.SetActive(false);
        ManageScreens(1);
        ActivateUiElement(pokemon_storage.Instance.storageUI, true);
        pokemon_storage.Instance.OpenPC(currentUsageState);
    }
    public void ViewPokeMart()
    {
        ManageScreens(1);
        ActivateUiElement(Poke_Mart.Instance.storeUI,true);
        var martSelectables = new List<SelectableUI>();
        foreach(var item in Poke_Mart.Instance.storeItemsUI) 
            martSelectables.Add( new(item.gameObject,null,true) );
        InputStateHandler.Instance.ChangeInputState(new InputState(InputStateHandler.StateName.MartItemNavigation
            ,new[] { InputStateHandler.StateGroup.PokeMart },true,
            Poke_Mart.Instance.storeUI, InputStateHandler.Directional.Vertical, martSelectables,
            Poke_Mart.Instance.itemSelector,true,true,ClosePokeMart,ClosePokeMart));
    }
}
