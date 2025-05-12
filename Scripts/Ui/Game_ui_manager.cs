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
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    private void Update()
    {
        if (overworld_actions.Instance == null) return;
            Ui_inputs();
    }
    private void Ui_inputs()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !overworld_actions.Instance.usingUI && !overworld_actions.Instance.doingAction &&!viewingMenu)
        {
            viewingMenu = true;
            ActivateUiElement(menuOptions);
        }
        if (Input.GetKeyUp(KeyCode.Space) && !overworld_actions.Instance.doingAction && viewingMenu)
            menuOff = false;
        
        if (Input.GetKeyDown(KeyCode.Space) && viewingMenu && !menuOff)
            CloseMenu();
        
        if (Input.GetKeyDown(KeyCode.Escape) && Pokemon_party.Instance.viewingParty && !Pokemon_party.Instance.viewingDetails)
            if(!Pokemon_party.Instance.swapOutNext)
                CloseParty();
        
        if (Input.GetKeyDown(KeyCode.Escape) && Bag.Instance.viewingBag)
            CloseBag();
        
        if (Input.GetKeyDown(KeyCode.Escape) && profile.Viewing_profile)
        {
            profile.gameObject.SetActive(false);
            profile.Viewing_profile = false;
            ResetPlayerMovement();
        }
        if (Input.GetKeyDown(KeyCode.Escape) && Poke_Mart.instance.viewing_store)
        {
            CloseStore();
            Dialogue_handler.instance.Write_Info("Would you like anything else?", "Options", "BuyMore","Sure, what would you like","DontBuyMore","Yes","No");
        }
    } 
    public void ResetPlayerMovement()
    {
        overworld_actions.Instance.usingUI = false;
        Player_movement.instance.canmove = true;
    }
    private void ActivateUiElement(GameObject ui)
    {
        ui.SetActive(true);
        overworld_actions.Instance.usingUI = true;
    }
    public void CloseStore()
    {
        Poke_Mart.instance.Exit_Store();
        Poke_Mart.instance.mart_ui.SetActive(false);
        ResetPlayerMovement();
    }
    public void CloseBag()
    {
        Bag.Instance.CloseBag();
        Bag.Instance.bagUI.SetActive(false);
        if (!Options_manager.Instance.playerInBattle)
            ResetPlayerMovement();
        else
            overworld_actions.Instance.usingUI = false;
    }
    public void CloseParty()
    {
        Pokemon_party.Instance.partyUI.gameObject.SetActive(false);
        Pokemon_party.Instance.ClearSelectionUI();
        if (Options_manager.Instance.playerInBattle)
            overworld_actions.Instance.usingUI = false;
        else
            ResetPlayerMovement();
        Item_handler.Instance.usingItem = false;//in case player closes before using item
        Pokemon_party.Instance.givingItem = false;
    }
    public void CloseMenu()
    {
        ResetMenuState();
        ResetPlayerMovement();
    }
    private void ResetMenuState()
    {
        menuOptions.SetActive(false);
        viewingMenu = false;
        menuOff = true;
    }
    public void ViewMarket()
    {
        ActivateUiElement(Poke_Mart.instance.mart_ui);
        Poke_Mart.instance.View_store();
    }
    public void ViewBag()
    {
        Dialogue_handler.instance.Dialouge_off();
        ActivateUiElement(Bag.Instance.bagUI);
        Bag.Instance.ViewBag();
        ResetMenuState();
    }
    public void ViewProfile()
    {
        ActivateUiElement(profile.gameObject);
        ResetMenuState();
        profile.Load_Profile(Game_Load.Instance.playerData);
    }
    public void ViewPokemonParty()
    {
        ResetMenuState();
        Dialogue_handler.instance.Dialouge_off();
        ActivateUiElement(Pokemon_party.Instance.partyUI);
        Pokemon_party.Instance.ViewParty();
    }
}
