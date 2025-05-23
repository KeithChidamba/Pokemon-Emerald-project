using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Game_ui_manager : MonoBehaviour
{

    public GameObject menuOptions;
    public bool viewingMenu;
    public bool menuOff=true;
    public Player_Info_ui profile;
    public static Game_ui_manager Instance;
    [SerializeField]private int _numUIScreensOpen;
    public bool canExitParty = true;
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
    }

    private void Update()
    {
        if (overworld_actions.Instance == null) return;
        UiInputs();
    }
    public void ManageScreens(int change)
    {
        _numUIScreensOpen += change;
        if (_numUIScreensOpen < 0) _numUIScreensOpen = 0;
        overworld_actions.Instance.usingUI = _numUIScreensOpen>0;
        Player_movement.instance.canmove = _numUIScreensOpen==0;
        if(Options_manager.Instance.playerInBattle) Player_movement.instance.canmove = false;
    }
    private void UiInputs()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !overworld_actions.Instance.usingUI && !overworld_actions.Instance.doingAction &&!viewingMenu)
        {
            ManageScreens(1);
            viewingMenu = true;
            ActivateUiElement(menuOptions);
        }
        if (Input.GetKeyUp(KeyCode.Space) && !overworld_actions.Instance.doingAction && viewingMenu)
            menuOff = false;
        if (Input.GetKeyDown(KeyCode.Space) && viewingMenu && !menuOff)
            CloseMenu();
        
        if (Input.GetKeyDown(KeyCode.Escape) && Pokemon_party.Instance.viewingParty && !Pokemon_party.Instance.viewingDetails)
            if(!Pokemon_party.Instance.swapOutNext & canExitParty)
                CloseParty();
        
        if (Input.GetKeyDown(KeyCode.Escape) && Bag.Instance.viewingBag)
            CloseBag();
        
        if (Input.GetKeyDown(KeyCode.Escape) && profile.viewingProfile)
        {
            CloseProfile();
        }
        if (Input.GetKeyDown(KeyCode.Escape) && Poke_Mart.Instance.viewingStore)
        {
            CloseStore();
            Dialogue_handler.Instance.DisplayList("Would you like anything else?",
                 "", new[]{ "BuyMore","LeaveStore" }, new[]{"Yes", "No"});
        }
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

    public void CloseMenu()
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
        Poke_Mart.Instance.ViewStore();
    }
    public void ViewBag()
    {
        ManageScreens(1);
        Dialogue_handler.Instance.EndDialogue();
        ActivateUiElement(Bag.Instance.bagUI);
        Bag.Instance.ViewBag();
        CloseMenu();
    }
    public void ViewProfile()
    {
        ManageScreens(1);
        ActivateUiElement(profile.gameObject);
        CloseMenu();
        profile.LoadProfile(Game_Load.Instance.playerData);
    }
    public void ViewPokemonParty()
    {
        ManageScreens(1);
        CloseMenu();
        Dialogue_handler.Instance.EndDialogue();
        ActivateUiElement(Pokemon_party.Instance.partyUI);
        Pokemon_party.Instance.ViewParty();
    }
}
