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
    public bool menuOff=true;
    public Player_Info_ui profile;
    public static Game_ui_manager Instance;
    [SerializeField]private int numUIScreensOpen;
    [SerializeField]private GameObject exitButton;
    [SerializeField]private List<GameObject> menuUiOptions = new ();
    public GameObject menuSelector;
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
    }

    private void Update()
    {
        if (overworld_actions.Instance == null) return;
        UiInputs();
    }
    private void ManageScreens(int change)
    {
        numUIScreensOpen += change;
        if (numUIScreensOpen < 0) numUIScreensOpen = 0;
        overworld_actions.Instance.usingUI = numUIScreensOpen>0;
        
        if(numUIScreensOpen==0)
            Player_movement.Instance.AllowPlayerMovement();
        else
            Player_movement.Instance.RestrictPlayerMovement();
        
        if(Options_manager.Instance.playerInBattle) Player_movement.Instance.RestrictPlayerMovement();
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
        if (Input.GetKeyUp(KeyCode.Space) && !overworld_actions.Instance.doingAction && viewingMenu)
            menuOff = false;
        // if (Input.GetKeyDown(KeyCode.R) && Poke_Mart.Instance.viewingStore)
        // {
        //     CloseStore();
        //     Dialogue_handler.Instance.DisplayList("Would you like anything else?",
        //          "", new[]{ "BuyMore","LeaveStore" }, new[]{"Yes", "No"});
        // }
    } 
    private void ActivateMenuSelection()
    {
        var menuOptionsMethods = new List<Action>
        {
            ViewPokemonParty,Save_manager.Instance.SaveAllData, ViewBag, ViewProfile,
            Options_manager.Instance.ExitGame
        };
        if (usingWebGl)
        {
            menuOptionsMethods.Remove(menuOptionsMethods.Last());
            menuUiOptions.Remove(menuUiOptions.Last());
        }
        var menuSelectables = new List<SelectableUI>();
            
        for (var i =0; i<menuOptionsMethods.Count;i++)
            menuSelectables.Add( new(menuUiOptions[i],menuOptionsMethods[i],true) );
            
        InputStateHandler.Instance.ChangeInputState(new InputState("Player Menu",true,menuOptions,
            InputStateHandler.Vertical, menuSelectables,menuSelector,true, true,CloseMenu,CloseMenu,true));
    }
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
    private void CloseStore()
    {
        ManageScreens(-1);
        Poke_Mart.Instance.ExitStore();
        ActivateUiElement(Poke_Mart.Instance.storeUI, false);
    }
    private void CloseBag()
    {
        ManageScreens(-1);
        Bag.Instance.CloseBag();
        ActivateUiElement( Bag.Instance.bagUI, false);
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
        menuOff = true;
    }
    public void ViewMarket()
    {
        ManageScreens(1);
        ActivateUiElement(Poke_Mart.Instance.storeUI,true);
    }
    public void ViewBag()
    {
        ManageScreens(1);
        Dialogue_handler.Instance.EndDialogue();
        ActivateUiElement(Bag.Instance.bagUI,true);
        Bag.Instance.ViewBag();

        var bagSelectables = new List<SelectableUI>();
        
        foreach(var item in Bag.Instance.bagItemsUI) bagSelectables.Add( new(item.gameObject,null,true) );
        
        InputStateHandler.Instance.ChangeInputState(new InputState("Player Bag Navigation",true,
            Bag.Instance.bagUI, InputStateHandler.Vertical, bagSelectables,
                    Bag.Instance.itemSelector,true,true,CloseBag,CloseBag,true));
    }
    public void ViewProfile()
    {
        ManageScreens(1);
        ActivateUiElement(profile.gameObject,true);
        profile.LoadProfile(Game_Load.Instance.playerData);
        InputStateHandler.Instance.ChangeInputState(new InputState("Player Profile",true,profile.gameObject
            ,null, null,
            null,false, false,CloseProfile,CloseProfile,true));
    }
    public void ViewPokemonParty()
    {
        ManageScreens(1);
        Dialogue_handler.Instance.EndDialogue();
        Pokemon_party.Instance.ClearSelectionUI();
        ActivateUiElement(Pokemon_party.Instance.partyUI,true);
        Pokemon_party.Instance.RefreshMemberCards();

        var partyUsageState = Item_handler.Instance.usingItem? "Pokemon Party Item Usage" : "Pokemon Party Navigation";
        
        var partySelectables = new List<SelectableUI>();

        for (var i = 0 ;i< Pokemon_party.Instance.numMembers;i++)
        {
            var memberNumber = i + 1;
            partySelectables.Add( new(Pokemon_party.Instance.memberCards[i].gameObject
                ,() => Pokemon_party.Instance.SelectMember(memberNumber),true) );
        }
        
        InputStateHandler.Instance.ChangeInputState(new InputState(partyUsageState,true,Pokemon_party.Instance.partyUI,
            InputStateHandler.Vertical, partySelectables, Pokemon_party.Instance.memberSelector
            , true, true,CloseParty,CloseParty,true));
    }

    public void ViewPokemonDetails(Pokemon pokemonToView)
    { 
        ManageScreens(1);
        ActivateUiElement(Pokemon_Details.Instance.uiParent,true);
        var detailsSelectables = new List<SelectableUI>{
            new(null,null,true)
            ,new(null,null,true)
            ,new(null,InputStateHandler.Instance.AllowMoveUiNavigation,true)
        };
        InputStateHandler.Instance.ChangeInputState(new InputState("Pokemon Details",true,Pokemon_Details.Instance.uiParent,
            InputStateHandler.Horizontal,detailsSelectables, null
            , true, false,ClosePokemonDetails,ClosePokemonDetails,true));
        Pokemon_Details.Instance.LoadDetails(pokemonToView);

    }
}
