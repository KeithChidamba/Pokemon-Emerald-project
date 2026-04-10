using UnityEngine;

public interface IInjectable
{
    public void Inject(ServiceContainer container);
}
public class GameInstaller : MonoBehaviour
{
    private ServiceContainer _container;
    [SerializeField] private InputStateHandler inputStateHandler;
    [SerializeField] private Dialogue_handler dialogueHandler;
    [SerializeField] private BattleIntro battleIntroHandler;
    [SerializeField] private BattleOperations battleOperationsHandler;
    [SerializeField] private Battle_handler battleHandler;
    [SerializeField] private BattleVisuals battleVisualsHandler;
    [SerializeField] private Encounter_handler  encounterHandler;
    [SerializeField] private Wild_pkm wildPokemonHandler;
    [SerializeField] private Turn_Based_Combat turnBasedCombatHandler;
    [SerializeField] private Move_handler moveUsageHandler;
    [SerializeField] private MoveLogicHandler moveLogicHandler;
    [SerializeField] private MoveLogicDatabase moveLogicDatabase;
    [SerializeField] private Options_manager dialogueOptionsHandler;
    [SerializeField] private Game_ui_manager gameUIHandler;
    [SerializeField] private Bag playerBagHandler;
    [SerializeField] private Poke_Mart pokeMartHandler;
    [SerializeField] private Pokemon_party pokemonPartyHandler;
    [SerializeField] private PokemonOperations pokemonOperationsHandler;
    [SerializeField] private pokemon_storage pokemonStorageHandler;
    [SerializeField] private ItemStorageHandler itemStorageHandler;
    [SerializeField] private Pokemon_Details pokemonDetailsHandler;
    [SerializeField] private Save_manager saveDataHandler;
    [SerializeField] private Interaction_handler  interactionHandler;
    [SerializeField] private Player_movement playerMovementHandler;
    [SerializeField] private OverworldState overworldStateHandler;
    [SerializeField] private Area_manager  areaHandler;
    [SerializeField] private Game_Load gameLoadingHandler;
    [SerializeField] private overworld_actions overworldActionsHandler;
    [SerializeField] private Item_handler itemHandler;
    [SerializeField] private Move_handler moveHandler;
    [SerializeField] private GameSettingsHandler gameSettingsHandler;

    private void Awake()
    {
        _container = new ServiceContainer();
        //mono-services
        _container.RegisterSingleton(inputStateHandler);
        _container.RegisterSingleton(dialogueHandler);
        _container.RegisterSingleton(battleIntroHandler);
        _container.RegisterSingleton(battleHandler);
        _container.RegisterSingleton(encounterHandler);
        _container.RegisterSingleton(wildPokemonHandler);
        _container.RegisterSingleton(turnBasedCombatHandler);
        _container.RegisterSingleton(moveUsageHandler);
        _container.RegisterSingleton(moveLogicHandler);
        _container.RegisterSingleton(dialogueOptionsHandler);
        _container.RegisterSingleton(gameUIHandler);
        _container.RegisterSingleton(playerBagHandler);
        _container.RegisterSingleton(pokeMartHandler);
        _container.RegisterSingleton(pokemonPartyHandler);
        _container.RegisterSingleton(pokemonOperationsHandler);
        _container.RegisterSingleton(pokemonStorageHandler);
        _container.RegisterSingleton(itemStorageHandler);
        _container.RegisterSingleton(pokemonDetailsHandler);
        _container.RegisterSingleton(saveDataHandler);
        _container.RegisterSingleton(interactionHandler);
        _container.RegisterSingleton(playerMovementHandler);
        _container.RegisterSingleton(overworldStateHandler);
        _container.RegisterSingleton(areaHandler);
        _container.RegisterSingleton(gameLoadingHandler);
        _container.RegisterSingleton(overworldActionsHandler);
        _container.RegisterSingleton(itemHandler);
        _container.RegisterSingleton(battleVisualsHandler);
        _container.RegisterSingleton(moveHandler);
        _container.RegisterSingleton(battleOperationsHandler);
        _container.RegisterSingleton(gameSettingsHandler);
        _container.RegisterSingleton(moveLogicDatabase);
        
        Obj_Instance.GetContainer(_container);//static class dependency
        
        //Non-Mono services
        var playerBagInputService = new PlayerBagInputService(_container);
        var pokemonBattleInputService = new PokemonBattleInputService(_container);
        var pokemartInputService = new PokemartInputService(_container);
        var pokemonDetailsInputService = new PokemonDetailsInputService(_container);
        var pokemonStorageInputService = new PokemonStorageInputService(_container);
        var pokemonPartyInputService = new PokemonPartyInputService(_container);
        var gameSettingsInputService = new GameSettingsInputService(_container);
        
        _container.RegisterSingleton(playerBagInputService);
        _container.RegisterSingleton(pokemonBattleInputService);
        _container.RegisterSingleton(pokemartInputService);
        _container.RegisterSingleton(pokemonStorageInputService);
        _container.RegisterSingleton(pokemonDetailsInputService);
        _container.RegisterSingleton(pokemonPartyInputService);
        _container.RegisterSingleton(gameSettingsInputService);
        
        var injectables = FindObjectsOfType<MonoBehaviour>(true);
        
        foreach (var obj in injectables)
        {
            if (obj.isActiveAndEnabled)
            {
                continue;
            }
            if (obj is IInjectable injectable)
            {
                injectable.Inject(_container);
            }
        }
    }
}
