using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputStateLogicHandler : MonoBehaviour
{
    private InputStateHandler _inputStateHandler;
    private Dialogue_handler _dialogueHandler;
    private BattleIntro _battleIntroHandler;
    private Battle_handler _battleHandler;
    private BattleOperations _battleOperationsHandler;
    private BattleVisuals _battleVisualsHandler;
    private Encounter_handler  _encounterHandler;
    private Wild_pkm _wildPokemonHandler;
    private Turn_Based_Combat _turnBasedCombatHandler;
    private Move_handler _moveUsageHandler;
    private MoveLogicHandler _moveLogicHandler;
    private Options_manager _dialogueOptionsHandler;
    private Game_ui_manager _gameUIHandler;
    private Bag _playerBagHandler;
    private Poke_Mart _pokeMartHandler;
    private Pokemon_party _pokemonPartyHandler;
    private PokemonOperations _pokemonOperationsHandler;
    private pokemon_storage _pokemonStorageHandler;
    private ItemStorageHandler _itemStorageHandler;
    private Pokemon_Details _pokemonDetailsHandler;
    private Save_manager _saveDataHandler;
    private Interaction_handler  _interactionHandler;
    private Player_movement _playerMovementHandler;
    private OverworldState _overworldStateHandler;
    private Area_manager  _areaHandler;
    private Game_Load _gameLoadingHandler;
    private overworld_actions _overworldActions;
    private Item_handler _itemHandler;
    private Container container;
    void Skippy()
    {
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _battleVisualsHandler = container.Resolve<BattleVisuals>();
        _battleOperationsHandler = container.Resolve<BattleOperations>();
        _battleIntroHandler = container.Resolve<BattleIntro>();
        _battleHandler = container.Resolve<Battle_handler>();
        _encounterHandler = container.Resolve<Encounter_handler>();
        _wildPokemonHandler = container.Resolve<Wild_pkm>();
        _turnBasedCombatHandler = container.Resolve<Turn_Based_Combat>();
        _moveUsageHandler = container.Resolve<Move_handler>();
        _moveLogicHandler = container.Resolve<MoveLogicHandler>();
        _dialogueOptionsHandler = container.Resolve<Options_manager>();
        _gameUIHandler = container.Resolve<Game_ui_manager>();
        _playerBagHandler = container.Resolve<Bag>();
        _pokeMartHandler = container.Resolve<Poke_Mart>();
        _pokemonPartyHandler = container.Resolve<Pokemon_party>();
        _pokemonOperationsHandler = container.Resolve<PokemonOperations>();
        _pokemonStorageHandler = container.Resolve<pokemon_storage>();
        _itemStorageHandler = container.Resolve<ItemStorageHandler>();
        _pokemonDetailsHandler = container.Resolve<Pokemon_Details>();
        _saveDataHandler = container.Resolve<Save_manager>();
        _interactionHandler = container.Resolve<Interaction_handler>();
        _playerMovementHandler = container.Resolve<Player_movement>();
        _overworldStateHandler = container.Resolve<OverworldState>();
        _areaHandler = container.Resolve<Area_manager>();
        _gameLoadingHandler = container.Resolve<Game_Load>();
        _overworldActions = container.Resolve<overworld_actions>();
        _itemHandler = container.Resolve<Item_handler>();
    }
}
