using System;
using UnityEngine;

public interface IInjectable
{
    public void Inject(ServiceContainer container);
    public void OnInject();
}

public class GameInstaller : MonoBehaviour
{
    private ServiceContainer _container;
    [SerializeField] private InputStateHandler inputStateHandler;
    [SerializeField] private InputSourceHandler inputSourceHandler;
    [SerializeField] private DialogueHandler dialogueHandler;
    [SerializeField] private BattleIntro battleIntroHandler;
    [SerializeField] private BattleOperations battleOperationsHandler;
    [SerializeField] private BattleHandler battleHandler;
    [SerializeField] private BattleVisuals battleVisualsHandler;
    [SerializeField] private EncounterHandler  encounterHandler;
    [SerializeField] private WildPokemonAiHandler wildPokemonHandler;
    [SerializeField] private TurnBasedCombatHandler turnBasedCombatHandler;
    [SerializeField] private MoveSequenceHandler moveUsageHandler;
    [SerializeField] private MoveLogicHandler moveLogicHandler;
    [SerializeField] private MoveLogicDatabase moveLogicDatabase;
    [SerializeField] private DialogueOptionsEventHandler dialogueOptionsHandler;
    [SerializeField] private GameUiHandler gameUIHandler;
    [SerializeField] private Bag playerBagHandler;
    [SerializeField] private PokeMartHandler pokeMartHandler;
    [SerializeField] private PokemonPartyHandler pokemonPartyHandler;
    [SerializeField] private PokemonOperations pokemonOperationsHandler;
    [SerializeField] private PokemonStorageHandler pokemonStorageHandler;
    [SerializeField] private ItemStorageHandler itemStorageHandler;
    [SerializeField] private PokemonDetailsHandler pokemonDetailsHandler;
    [SerializeField] private SaveDataHandler saveDataHandler;
    [SerializeField] private InteractionHandler  interactionHandler;
    [SerializeField] private PlayerMovementHandler playerMovementHandler;
    [SerializeField] private PlayerTileHandler playerTileHandler;
    [SerializeField] private OverworldState overworldStateHandler;
    [SerializeField] private AreaManager  areaHandler;
    [SerializeField] private GameLoadingHandler gameLoadingHandler;
    [SerializeField] private OverworldActionsHandler overworldActionsHandler;
    [SerializeField] private ItemHandler itemHandler;
    [SerializeField] private MoveSequenceHandler moveHandler;
    [SerializeField] private GameSettingsHandler gameSettingsHandler;
    [SerializeField] private TypingInterfaceHandler typingInterfaceHandler;
    [SerializeField] private TestingEnvironmentHandler testingSetupHandler;
    
    private event Action OnServicesInjected;
    
    private void Awake()
    {
        _container = new ServiceContainer();
        //mono-services
        _container.RegisterSingleton(inputStateHandler);
        _container.RegisterSingleton(inputSourceHandler);
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
        _container.RegisterSingleton(playerTileHandler);
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
        _container.RegisterSingleton(typingInterfaceHandler);
        _container.RegisterSingleton(testingSetupHandler);
        
        InstanceFactory.GetContainer(_container);//static class dependency
        
        //Non-Mono services
        var playerBagInputService = new PlayerBagInputService(_container);
        var pokemonBattleInputService = new PokemonBattleInputService(_container);
        var pokemartInputService = new PokemartInputService(_container);
        var pokemonDetailsInputService = new PokemonDetailsInputService(_container);
        var pokemonStorageInputService = new PokemonStorageInputService(_container);
        var pokemonPartyInputService = new PokemonPartyInputService(_container);
        var gameSettingsInputService = new GameSettingsInputService(_container);
        var typingInterfaceInputService = new TypingInterfaceInputService(_container);
        
        _container.RegisterSingleton(playerBagInputService);
        _container.RegisterSingleton(pokemonBattleInputService);
        _container.RegisterSingleton(pokemartInputService);
        _container.RegisterSingleton(pokemonStorageInputService);
        _container.RegisterSingleton(pokemonDetailsInputService);
        _container.RegisterSingleton(pokemonPartyInputService);
        _container.RegisterSingleton(gameSettingsInputService);
        _container.RegisterSingleton(typingInterfaceInputService);
        
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
                OnServicesInjected += () => injectable.OnInject();
            }
        }
        OnServicesInjected?.Invoke();
    }
}
