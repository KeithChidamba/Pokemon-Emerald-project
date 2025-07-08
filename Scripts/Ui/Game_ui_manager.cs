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
    public event Action OnUiClose;
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

        if (numUIScreensOpen == 0)
        {
            OnUiClose?.Invoke();
            Player_movement.Instance.AllowPlayerMovement();
        }
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
    } 
    private void ActivateMenuSelection()
    {
        var menuOptionsMethods = new List<Action>
        {
            ViewPokemonParty,Save_manager.Instance.SaveAllData, ViewBag, ViewProfile
        };
        
        if (!usingWebGl) menuOptionsMethods.Add(Options_manager.Instance.ExitGame);
        
        var menuSelectables = new List<SelectableUI>();
            
        for (var i =0; i<menuOptionsMethods.Count;i++)
            menuSelectables.Add( new(menuUiOptions[i],menuOptionsMethods[i],true) );
            
        InputStateHandler.Instance.ChangeInputState(new InputState(InputStateHandler.StateName.PlayerMenu,
            new[] { InputStateHandler.StateGroup.None},true,menuOptions,
            InputStateHandler.Directional.Vertical, menuSelectables,menuSelector,true
            , true,CloseMenu,CloseMenu));
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
    private void ClosePokeMart()
    {
        ManageScreens(-1);
        Poke_Mart.Instance.ExitStore();
        ActivateUiElement(Poke_Mart.Instance.storeUI, false);
        Dialogue_handler.Instance.DisplayList("Would you like anything else?", 
            "", new[]{ "BuyMore","LeaveStore" }, new[]{"Yes", "No"});
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

    private void ClosePokemonStorage()
    {
        ManageScreens(-1);
        ActivateUiElement(pokemon_storage.Instance.storageUI,false);
        pokemon_storage.Instance.ClosePC();
    }
    public void ViewBag()
    {
        ManageScreens(1);
        Dialogue_handler.Instance.EndDialogue();
        ActivateUiElement(Bag.Instance.bagUI,true);
        Bag.Instance.ViewBag();

        var bagSelectables = new List<SelectableUI>();
        
        foreach(var item in Bag.Instance.bagItemsUI) bagSelectables.Add( new(item.gameObject,null,true) );
        
        InputStateHandler.Instance.ChangeInputState(new InputState(InputStateHandler.StateName.PlayerBagNavigation,
            new[] { InputStateHandler.StateGroup.Bag},true,
            Bag.Instance.bagUI, InputStateHandler.Directional.Vertical, bagSelectables,
                    Bag.Instance.itemSelector,true,true,CloseBag,CloseBag));
    }
    public void ViewProfile()
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
        ManageScreens(1);
        Dialogue_handler.Instance.EndDialogue();
        Pokemon_party.Instance.ClearSelectionUI();
        ActivateUiElement(Pokemon_party.Instance.partyUI, true);
        Pokemon_party.Instance.RefreshMemberCards();

        var partyUsageState = Item_handler.Instance.usingItem
            ? InputStateHandler.StateName.PokemonPartyItemUsage
            : InputStateHandler.StateName.PokemonPartyNavigation;

        var partySelectables = new List<SelectableUI>();

        for (var i = 0; i < Pokemon_party.Instance.numMembers; i++)
        {
            var memberNumber = i + 1;
            partySelectables.Add(new(Pokemon_party.Instance.memberCards[i].gameObject
                , () => Pokemon_party.Instance.SelectMember(memberNumber), true));
        }

        InputStateHandler.Instance.ChangeInputState(new InputState(partyUsageState,
            new[]{InputStateHandler.StateGroup.PokemonParty }, true,Pokemon_party.Instance.partyUI,
            InputStateHandler.Directional.Vertical, partySelectables, Pokemon_party.Instance.memberSelector
            , true, true,CloseParty,CloseParty));
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
        ViewOtherPokemonDetails(selectedPokemon,Pokemon_party.Instance.party.ToList());
    }
    public void ViewPokemonStorage()
    {
        ManageScreens(1);
        ActivateUiElement(pokemon_storage.Instance.storageUI, true);
        var storageSelectables = new List<SelectableUI>
        {
            new(pokemon_storage.Instance.initialStorageOptions[0],
                InputStateHandler.Instance.PokemonStorageBoxNavigation, true),
            new(pokemon_storage.Instance.initialStorageOptions[1],
                InputStateHandler.Instance.PokemonStoragePartyNavigation, true)
        };
        InputStateHandler.Instance.ChangeInputState(new InputState(InputStateHandler.StateName.PokemonStorage,
            new[] { InputStateHandler.StateGroup.PokemonStorage }, true,pokemon_storage.Instance.storageUI,
            InputStateHandler.Directional.Horizontal,storageSelectables,pokemon_storage.Instance.initialSelector
            , true, true,ClosePokemonStorage,ClosePokemonStorage));
        pokemon_storage.Instance.OpenPC();
    }

    public void ViewPokeMart()
    {
        ManageScreens(1);
        ActivateUiElement(Poke_Mart.Instance.storeUI,true);
        Bag.Instance.ViewBag();
 
        var martSelectables = new List<SelectableUI>();
        
        foreach(var item in Poke_Mart.Instance.storeItemsUI) 
            martSelectables.Add( new(item.gameObject,null,true) );
        
        InputStateHandler.Instance.ChangeInputState(new InputState(InputStateHandler.StateName.MartItemNavigation
            ,new[] { InputStateHandler.StateGroup.PokeMart },true,
            Poke_Mart.Instance.storeUI, InputStateHandler.Directional.Vertical, martSelectables,
            Poke_Mart.Instance.itemSelector,true,true,ClosePokeMart,ClosePokeMart));
    }
}
