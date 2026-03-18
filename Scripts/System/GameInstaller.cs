using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInjectable
{
    public void Inject(Container container);
}
public class GameInstaller : MonoBehaviour
{
    private Container _container;
    [SerializeField] private InputStateHandler inputStateHandler;
    [SerializeField] private Dialogue_handler dialogueHandler;
    [SerializeField] private BattleIntro battleIntroHandler;
    [SerializeField] private Battle_handler battleHandler;
    [SerializeField] private BattleVisuals battleVisualsHandler;
    [SerializeField] private Encounter_handler  encounterHandler;
    [SerializeField] private Wild_pkm wildPokemonHandler;
    [SerializeField] private Turn_Based_Combat turnBasedCombatHandler;
    [SerializeField] private Move_handler moveUsageHandler;
    [SerializeField] private MoveLogicHandler moveLogicHandler;
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
    [SerializeField] private overworld_actions overworldActions;
    [SerializeField] private Item_handler itemHandler;
    [SerializeField] private Move_handler moveHandler;
    
    
    void Awake()
    {
        _container = new Container();
        _container.RegisterSingleton(() => dialogueHandler);
        _container.RegisterSingleton(() => inputStateHandler);
        _container.RegisterSingleton(() => battleIntroHandler);
        _container.RegisterSingleton(() => battleHandler);
        _container.RegisterSingleton(() => encounterHandler);
        _container.RegisterSingleton(() => wildPokemonHandler);
        _container.RegisterSingleton(() => turnBasedCombatHandler);
        _container.RegisterSingleton(() => moveUsageHandler);
        _container.RegisterSingleton(() => moveLogicHandler);
        _container.RegisterSingleton(() => dialogueOptionsHandler);
        _container.RegisterSingleton(() => gameUIHandler);
        _container.RegisterSingleton(() => playerBagHandler);
        _container.RegisterSingleton(() => pokeMartHandler);
        _container.RegisterSingleton(() => pokemonPartyHandler);
        _container.RegisterSingleton(() => pokemonOperationsHandler);
        _container.RegisterSingleton(() => pokemonStorageHandler);
        _container.RegisterSingleton(() => itemStorageHandler);
        _container.RegisterSingleton(() => pokemonDetailsHandler);
        _container.RegisterSingleton(() => saveDataHandler);
        _container.RegisterSingleton(() => interactionHandler);
        _container.RegisterSingleton(() => playerMovementHandler);
        _container.RegisterSingleton(() => overworldStateHandler);
        _container.RegisterSingleton(() => areaHandler);
        _container.RegisterSingleton(() => gameLoadingHandler);
        _container.RegisterSingleton(() => overworldActions);
        _container.RegisterSingleton(() => itemHandler);
        _container.RegisterSingleton(() => battleVisualsHandler);
        _container.RegisterSingleton(() => moveHandler);
        
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
