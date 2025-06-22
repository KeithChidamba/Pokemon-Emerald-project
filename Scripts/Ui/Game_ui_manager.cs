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
    public bool canExitParty = true;
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
        canExitParty = true;
        usingWebGl = Application.platform == RuntimePlatform.WebGLPlayer;
        exitButton.SetActive(!usingWebGl);
    }

    private void Update()
    {
        if (overworld_actions.Instance == null) return;
        UiInputs();
    }
    public void ManageScreens(int change)
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
            ActivateUiElement(menuOptions);
            ActivateMenuSelection();
        }
        if (Input.GetKeyUp(KeyCode.Space) && !overworld_actions.Instance.doingAction && viewingMenu)
            menuOff = false;
        // if (Input.GetKeyDown(KeyCode.R) && viewingMenu && !menuOff)
        //     CloseMenu();
        
        // if (Input.GetKeyDown(KeyCode.R) && Pokemon_party.Instance.viewingParty && !Pokemon_party.Instance.viewingDetails)
        //     if(!Pokemon_party.Instance.swapOutNext & canExitParty)
        //         CloseParty();
        
        // if (Input.GetKeyDown(KeyCode.R) && Bag.Instance.viewingBag)
        //     CloseBag();
        //
        // if (Input.GetKeyDown(KeyCode.R) && profile.viewingProfile)
        // {
        //     CloseProfile();
        // }
        // if (Input.GetKeyDown(KeyCode.R) && Poke_Mart.Instance.viewingStore)
        // {
        //     CloseStore();
        //     Dialogue_handler.Instance.DisplayList("Would you like anything else?",
        //          "", new[]{ "BuyMore","LeaveStore" }, new[]{"Yes", "No"});
        // }
    } 
    private void ActivateMenuSelection()
    {
        var menuOptionsMethods = new List<Action>()
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
            
        InputStateHandler.Instance.ChangeInputState(new InputState("Player Menu",InputStateHandler.Vertical, 
            menuSelectables,menuSelector,true, true,CloseMenu));
    }
    private void ActivateUiElement(GameObject ui)
    {
        ui.SetActive(true);
    }
    private void CloseProfile()
    {
        ManageScreens(-1);
        profile.gameObject.SetActive(false);
        profile.viewingProfile = false;
    }
    public void CloseStore()
    {
        ManageScreens(-1);
        Poke_Mart.Instance.ExitStore();
        Poke_Mart.Instance.storeUI.SetActive(false);
    }
    public void CloseBag()
    {
        ManageScreens(-1);
        Bag.Instance.CloseBag();
        Bag.Instance.bagUI.SetActive(false);
    }
    public void CloseParty()
    {
        ManageScreens(-1);
        Pokemon_party.Instance.partyUI.gameObject.SetActive(false);
        Pokemon_party.Instance.ClearSelectionUI();
        Item_handler.Instance.usingItem = false;//in case player closes before using item
        Pokemon_party.Instance.givingItem = false;
    }

    private void CloseMenu()
    {
        if (!viewingMenu) return;
        ManageScreens(-1);
        menuOptions.SetActive(false);
        viewingMenu = false;
        menuOff = true;
    }
    public void ViewMarket()
    {
        ManageScreens(1);
        ActivateUiElement(Poke_Mart.Instance.storeUI);
    }
    public void ViewBag()
    {
        ManageScreens(1);
        Dialogue_handler.Instance.EndDialogue();
        ActivateUiElement(Bag.Instance.bagUI);
        Bag.Instance.ViewBag();

        var bagSelectables = new List<SelectableUI>();
        
        foreach(var item in Bag.Instance.bagItemsUI) bagSelectables.Add( new(item.gameObject,null,true) );
        
        InputStateHandler.Instance.ChangeInputState(new InputState("Player Bag Navigation",
                                InputStateHandler.Vertical, bagSelectables,Bag.Instance.itemSelector,true,true,CloseBag));
    }
    public void ViewProfile()
    {
        ManageScreens(1);
        ActivateUiElement(profile.gameObject);
        profile.LoadProfile(Game_Load.Instance.playerData);
        InputStateHandler.Instance.ChangeInputState(new InputState("Player Profile",null, 
            null,null,false, false,CloseProfile));
    }
    public void ViewPokemonParty()
    {
        ManageScreens(1);
        Dialogue_handler.Instance.EndDialogue();
        ActivateUiElement(Pokemon_party.Instance.partyUI);
        Pokemon_party.Instance.ViewParty();
    }
}
