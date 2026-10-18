using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public enum ItemType
{
    Special,Repel,HealHp,Status,PowerPointModifier,Herb,Revive,HeldItem,Vitamin,
    Berry,Pokeball,EvolutionStone,RareCandy,XItem,GainMoney,Overworld,LearnableMove,
    SpecificLogic
}
public class ItemHandler : MonoBehaviour,IInjectable
{
    /// <summary>
    /// Notify item usage success/failure
    /// </summary>
    public event Action<Item,bool> OnItemUsed;
    private Action scheduledInputStateRemoval;
    
    private PokemonDetailsHandler _pokemonDetailsHandler;
    private MoveSequenceHandler _moveUsageHandler;
    private DialogueHandler _dialogueHandler;
    private InputStateHandler _inputStateHandler;
    private BattleHandler _battleHandler;

    private PlayerBagHandler _playerBagHandler;
    private OverworldActionsHandler _overworldActions;
    private PokemonPartyHandler _pokemonPartyHandler;
    private PokemonOperations _pokemonOperationsHandler;
    private PokemonStorageHandler _pokemonStorageHandler;
    private TurnBasedCombatHandler _turnBasedCombatHandler;
    private GameUiHandler _gameUIHandler;
    private PlayerTileHandler _playerTileHandler;
    
    public void Inject(ServiceContainer container)
    {
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _dialogueHandler = container.Resolve<DialogueHandler>();
        _battleHandler = container.Resolve<BattleHandler>();
        _turnBasedCombatHandler = container.Resolve<TurnBasedCombatHandler>();
        _moveUsageHandler = container.Resolve<MoveSequenceHandler>();
        _playerBagHandler = container.Resolve<PlayerBagHandler>();
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
        _pokemonOperationsHandler = container.Resolve<PokemonOperations>();
        _pokemonStorageHandler = container.Resolve<PokemonStorageHandler>();
        _pokemonDetailsHandler = container.Resolve<PokemonDetailsHandler>();
        _overworldActions = container.Resolve<OverworldActionsHandler>();
        _gameUIHandler = container.Resolve<GameUiHandler>();
        _playerTileHandler = container.Resolve<PlayerTileHandler>();
        
        gameObject.SetActive(true);
    }
    public void OnInject() { }

    private void CompleteItemUsage(Item itemUsed, bool successful)
    {
        if (successful)
        {
            SoundManager.Play(UiId.UseItem);
        }
        OnItemUsed -= CompleteItemUsage;
        _inputStateHandler.AddPlaceHolderState();
        StartCoroutine(CompletionSequence());
        return;
        IEnumerator CompletionSequence()
        {
            if(_dialogueHandler.Displaying)
            {
                yield return _dialogueHandler.WaitForDialogueCompletion();
                yield return new WaitForSecondsRealtime(0.5f);
            }
            scheduledInputStateRemoval?.Invoke();
            yield return new WaitForSecondsRealtime(0.1f);

            if (_battleHandler.BattleInProgress)
            {
                yield return new WaitForSecondsRealtime(1f);
            }
            if (itemUsed.forPartyUse) 
            { 
                _inputStateHandler.ResetSpecificUi(InputStateName.PokemonPartyItemUsage,true);
            }
            if (!successful)
            {
                if (itemUsed.forPartyUse || itemUsed.canBeUsedInBattle)
                {
                    //necessary to reset internal bag state
                    _playerBagHandler.SetupBagState();
                }
            }

            if (successful)
            {
                _playerBagHandler.DepleteItem(itemUsed,true);
                if (_battleHandler.BattleInProgress)
                {
                    _inputStateHandler.ResetSpecificUi(InputStateName.PlayerBagNavigation,true);
                    _battleHandler.SetPlayerTurnUsage(PlayerTurnUsage.UseItem);
                    _turnBasedCombatHandler.NextTurn();
                }
                else
                {
                    yield return new WaitForSecondsRealtime(0.2f);
                    _inputStateHandler.ResetSpecificUi(InputStateName.PlaceHolder);
                }
            }
            else
            {
                yield return new WaitForSecondsRealtime(0.2f);
                _inputStateHandler.ResetSpecificUi(InputStateName.PlaceHolder);
            }
        }
    }
    private void QuickExit(Item itemInUse)
    {
        OnItemUsed -= CompleteItemUsage;
        _playerBagHandler.DepleteItem(itemInUse,false);
        _battleHandler.SetPlayerTurnUsage(PlayerTurnUsage.UseItem);
        _turnBasedCombatHandler.NextTurn();
    }
    public void UseItem(Item itemInUse,PokemonStateHandler pokemonState)
    {
        var selectedPokemon = pokemonState?.pokemon;
        scheduledInputStateRemoval = null;
        OnItemUsed += CompleteItemUsage;
        switch (itemInUse.itemType)
        {
            case ItemType.Repel: UseRepel(itemInUse); break;
            
            case ItemType.Pokeball: UsePokeball(itemInUse); break;
            
            case ItemType.Special: EquipItem(itemInUse); break;
        //party use
            case ItemType.LearnableMove:
                StartCoroutine(HandleMoveLearning(itemInUse,selectedPokemon)); break;
            
            //abstract
            case ItemType.Herb: UseHerbs(itemInUse,pokemonState); break;
            
            case ItemType.Berry: UseBerries(itemInUse,pokemonState); break;
            
            case ItemType.PowerPointModifier: ChangePowerpoints(itemInUse,selectedPokemon); break;
            
            //core
            case ItemType.HealHp: RestoreHealth(itemInUse,pokemonState); break;
            
            case ItemType.Revive: RevivePokemon(itemInUse,pokemonState); break;
            
            case ItemType.Status: HealMajorStatusEffect(itemInUse,selectedPokemon); break;
            
            case ItemType.Vitamin: GetEVsFromItem(itemInUse,selectedPokemon); break;
            
            case ItemType.XItem: UseStatModifyingBattleItem(itemInUse); break;
            
            case ItemType.EvolutionStone: StartCoroutine(TriggerStoneEvolution(itemInUse, selectedPokemon)); break;

            case ItemType.RareCandy: StartCoroutine(LevelUpWithItem(itemInUse, pokemonState)); break;
            
            case ItemType.SpecificLogic: HandleSpecific(itemInUse,pokemonState); break;
        }
    }

    private void HandleSpecific(Item itemInUse,PokemonStateHandler state)
    {
        switch (NameDB.ParseItemName(itemInUse.itemName))
        {
            case ItemName.FullRestore:
                FullRestore(itemInUse, state);
                break;
            case ItemName.MaxPotion:
                RestoreHealth(state.pokemon.maxHp, itemInUse, state);
                break;
            case ItemName.FullHeal:
                StatusFullHeal(itemInUse, state.pokemon);
                break;
            case ItemName.GuardSpec:
                GuardSpecItem(itemInUse);
                break;
        }
    }
    private IEnumerator HandleMoveLearning(Item itemInUse,Pokemon pokemon)
    {
        var moveInfo = itemInUse.GetDynamicModule<MoveLearningMachineInfo>();
        var moveNameEnum = NameDB.ParseMoveName(moveInfo.move.moveName);
        var pokemonCanLearnMove = pokemon.learnableHms.Contains(moveNameEnum) ||
                                  pokemon.learnableTms.Contains(moveNameEnum);
        bool operationSuccessful = false;
        if (pokemonCanLearnMove)
        {
            _pokemonOperationsHandler.OnMoveLearnOperationComplete += CheckMoveLearnSuccess;
            scheduledInputStateRemoval = () =>
            {
                _inputStateHandler.ResetSpecificUi(InputStateName.PokemonDetailsMoveSelection);
                _inputStateHandler.ResetSpecificUi(InputStateName.PokemonDetails, true);
            };
            yield return _pokemonOperationsHandler.LearnTmOrHm(moveInfo,pokemon);
           
            OnItemUsed?.Invoke(itemInUse,operationSuccessful);
        }
        else
        {
            _dialogueHandler.DisplayDetails(pokemon.pokemonDisplayName + " cant learn that!");
            OnItemUsed?.Invoke(itemInUse,false);
        }
        yield break;
        void CheckMoveLearnSuccess(Pokemon subject,bool success)
        {
            if (subject.pokemonID == pokemon.pokemonID)
            {
                _pokemonOperationsHandler.OnMoveLearnOperationComplete -= CheckMoveLearnSuccess;
                operationSuccessful = success;
            }
        }
    }
    private void EquipItem(Item itemInUse)
    {
        if (_overworldActions.IsEquipped(item:itemInUse))
        {
            _overworldActions.UnequipItem(itemInUse);
            return;
        }
        _overworldActions.EquipItem(itemInUse);
    }
    private void UseRepel(Item itemInUse)
    {
        var repelDuration = (int)itemInUse.GetDynamicModule<EffectValueInfo>().effectValue;
        _dialogueHandler.DisplayDetails("Repel has been activated");
        _playerTileHandler.ActivateRepel(repelDuration);
        OnItemUsed?.Invoke(itemInUse,true);
    }
    
    private void UseHerbs(Item itemInUse,PokemonStateHandler state)
    {
        var selectedPartyPokemon = state.pokemon;
        var herbInfo = itemInUse.GetDynamicModule<HerbInfoModule>();
        OnItemUsed += ChangeFriendship;
        var usageIndex = herbInfo.GetHerbUsage();
        var herbUsages = new List<Action<Item>> 
        {
            (item) => RestoreHealth(item,state),
            (item) => StatusFullHeal(item,selectedPartyPokemon),
            (item) => UseReviveHerb()
        };
        herbUsages[usageIndex].Invoke(itemInUse);
        return;
        void UseReviveHerb()
        {
            HandlePokemonRevive(itemInUse,RevivalItemType.FullHealth, state);
        }
        void ChangeFriendship(Item itemUsed,bool successful)
        {
            OnItemUsed -= ChangeFriendship;
            if(successful)
            {
                selectedPartyPokemon.LoseFriendshipFromHerbs(herbInfo.herbType);
            }
        }
    }

    private void UseBerries(Item itemInUse,PokemonStateHandler state)
    {
        var selectedPartyPokemon = state.pokemon;
        var berryInfo = itemInUse.GetDynamicModule<BerryInfoModule>();
        var usageIndex = (int)berryInfo.berryType;
        var berryUsages = new List<Action<Item,Pokemon>> 
        {
            GetFriendshipFromBerry,
            (item,pokemon) => RestoreHealth(item,state),
            (item,pokemon) =>
            {
                var statusInfo = itemInUse.GetDynamicModule<StatusHealInfoModule>();
                HealMajorStatusEffect(item, pokemon,statusInfo.statusEffect);
            },
            ChangePowerpoints,
            CureConfusion,
            StatusFullHeal
        };
        berryUsages[usageIndex].Invoke(itemInUse,selectedPartyPokemon);
    }
    IEnumerator LevelUpWithItem(Item itemInUse,PokemonStateHandler pokemonState)
    {
        var selectedPartyPokemon = pokemonState.pokemon;
        if (selectedPartyPokemon.currentLevel == 100)
        {
            _dialogueHandler.DisplayDetails(selectedPartyPokemon.pokemonDisplayName+" is already max level!");
            OnItemUsed?.Invoke(itemInUse,false);
        }
        else
        {
            //check if item will cause evolution
            bool awaitEvolution = false;
            int evoIndex = 0;
            if (selectedPartyPokemon.currentEvolutionLineIndex < selectedPartyPokemon.evolutions.Count - 1)
            {//can still evolve
                var levelForNextEvolution =
                    selectedPartyPokemon.evolutionLineLevels[selectedPartyPokemon.currentEvolutionLineIndex];
                if (levelForNextEvolution == selectedPartyPokemon.currentLevel + 1)
                {
                    //this item will trigger evolution
                    pokemonState.OnEvolutionSuccessful += AwaitEvolution;
                    void AwaitEvolution(int evolutionIndex)
                    {
                        pokemonState.OnEvolutionSuccessful -= AwaitEvolution;
                        awaitEvolution = true;
                        evoIndex = evolutionIndex;
                    }
                }
            }
            var expForNextLevel = PokemonOperations.CalculateExpForNextLevel(selectedPartyPokemon.currentLevel, selectedPartyPokemon.expGroup);
            var expDifferenceForLevelUp = expForNextLevel - selectedPartyPokemon.currentExpAmount;
            
            pokemonState.handleEvolutionExternally = true;
            yield return pokemonState.ReceiveExperienceOutsideBattle(expDifferenceForLevelUp,true);
            if(awaitEvolution)
            {
                yield return _pokemonOperationsHandler.HandlePokemonEvolution(selectedPartyPokemon, evoIndex);
            }
            pokemonState.handleEvolutionExternally = false;
            OnItemUsed?.Invoke(itemInUse,true);
        }
    }

    private IEnumerator TriggerStoneEvolution(Item itemInUse,Pokemon selectedPartyPokemon)
    { 
       var stoneInfo = itemInUse.GetDynamicModule<EvolutionStoneInfoModule>();
       
       if (!selectedPartyPokemon.requiresEvolutionStone)
       {
           _dialogueHandler.DisplayDetails($"{selectedPartyPokemon.pokemonDisplayName} doesn't evolve like that");
           OnItemUsed?.Invoke(itemInUse,false);
       }
       else if (selectedPartyPokemon.evolutionStone != stoneInfo.stoneName)
       {
          _dialogueHandler.DisplayDetails("Cant use that stone on "+selectedPartyPokemon.pokemonDisplayName);
           OnItemUsed?.Invoke(itemInUse,false);
       }
       else
       {
           yield return _pokemonOperationsHandler.HandlePokemonEvolution(selectedPartyPokemon,0);
           OnItemUsed?.Invoke(itemInUse,true);
       }
    }

    private void GetFriendshipFromBerry(Item itemInUse,Pokemon selectedPartyPokemon)
    {
        var statToDecrease = itemInUse.GetDynamicModule<StatInfoModule>();
        if(selectedPartyPokemon.friendshipLevel>254)
        {
            _dialogueHandler.DisplayDetails(selectedPartyPokemon.pokemonDisplayName+"'s friendship is already maxed out");
            OnItemUsed?.Invoke(itemInUse,false);
        }
        else
        {
            _dialogueHandler.DisplayDetails(selectedPartyPokemon.pokemonDisplayName+"'s friendship was increased");
            ref float evRef = ref _pokemonOperationsHandler.GetEvStatRef(statToDecrease.statName, selectedPartyPokemon);
            if (evRef > 100)
            {
                evRef = 100;
            }
            else
            {
                _pokemonOperationsHandler.CalculateEvForStat(statToDecrease.statName, -10, selectedPartyPokemon);
            }
            selectedPartyPokemon.DetermineFriendshipLevelChange(FriendshipModifier.Berry);
            OnItemUsed?.Invoke(itemInUse,true);
        }
    }
    private void GetEVsFromItem(Item itemInUse,Pokemon selectedPartyPokemon) 
    {
        var statToIncrease = itemInUse.GetDynamicModule<StatInfoModule>();
        _pokemonOperationsHandler.OnEvChange += CheckEvChange;
        _pokemonOperationsHandler.CalculateEvForStat(statToIncrease.statName, 10, selectedPartyPokemon);
        return;
        void CheckEvChange(Stat stat,bool hasChanged)
        {
            _pokemonOperationsHandler.OnEvChange -= CheckEvChange;
            var message = selectedPartyPokemon.pokemonDisplayName + "'s " + NameDB.GetStatName(stat);
        
            message += (hasChanged)? " was increased" : " can't get any higher";

            if (hasChanged)
            {
                selectedPartyPokemon.DetermineFriendshipLevelChange(FriendshipModifier.Vitamin);
                OnItemUsed?.Invoke(itemInUse,true);
            }
            _dialogueHandler.DisplayDetails(message);
            OnItemUsed?.Invoke(itemInUse,false);
        }
    }

    private void GuardSpecItem(Item itemInUse)
    {
        //guard spec prevents stat reduction
        var currentParticipant = _battleHandler.GetCurrentParticipant();
        
        _battleHandler.GetTeam(currentParticipant.participantKey)
            .GetStatChangeImmunity(StatChangeability.ImmuneToDecrease,5);
        
        string pokemonProtected = currentParticipant.Pokemon.pokemonDisplayName;
        if (_battleHandler.isDoubleBattle)
        {
            var partner = currentParticipant.GetPartner();
            partner.Pokemon.DetermineFriendshipLevelChange(FriendshipModifier.XItem);
            pokemonProtected = currentParticipant.Pokemon.pokemonDisplayName 
                               + " and " + partner.Pokemon.pokemonDisplayName;
        }
        currentParticipant.Pokemon.DetermineFriendshipLevelChange(FriendshipModifier.XItem);
        _dialogueHandler.DisplayDetails("A veil of light covers "+pokemonProtected);
        
        OnItemUsed?.Invoke(itemInUse,true);
    }
    private void UseStatModifyingBattleItem(Item itemInUse)
    {
        var statInfo = itemInUse.GetDynamicModule<StatInfoModule>();
        var currentParticipant = _battleHandler.GetCurrentParticipant();
        
        var existingBuff = currentParticipant.Pokemon.statModifiers
            .FirstOrDefault(b=>b.stat == statInfo.statName);
     
        if (existingBuff is { isAtLimit: true })
        {
            _dialogueHandler.DisplayDetails($"{currentParticipant.Pokemon.pokemonDisplayName}'s " +
                                            $"{existingBuff.statName} can't go any higher");
            OnItemUsed?.Invoke(itemInUse,false);
        }
        else
        {
            StartCoroutine(CompleteUsage());
        }
        return;
        IEnumerator CompleteUsage()
        {
            var xBuffData = new StatChangeTransitData(currentParticipant, statInfo.statName, true, 1);
            yield return _moveUsageHandler.ExecuteSequentialStatChange(xBuffData,false);
            
            _dialogueHandler.DisplayDetails($"{currentParticipant.Pokemon.pokemonDisplayName}'s " +
                                            $"{statInfo.statName} Increased");
            currentParticipant.Pokemon.DetermineFriendshipLevelChange(FriendshipModifier.XItem);
            OnItemUsed?.Invoke(itemInUse,true);
        }
    }

    private void RevivePokemon(Item itemInUse,PokemonStateHandler state)
    {
        var reviveType = itemInUse.GetDynamicModule<RevivalItemInfo>().reviveType;
        HandlePokemonRevive(itemInUse,reviveType, state);
    }
    private void HandlePokemonRevive(Item itemInUse,RevivalItemType reviveType,PokemonStateHandler state)
    {
        var selectedPartyPokemon = state.pokemon;
        if (selectedPartyPokemon.hp > 0)
        {
            _dialogueHandler.DisplayDetails(selectedPartyPokemon.pokemonDisplayName+" has not fainted!"); 
            OnItemUsed?.Invoke(itemInUse,false);
            return;
        }
        selectedPartyPokemon.hp = reviveType switch
        { 
            RevivalItemType.FullHealth=> selectedPartyPokemon.maxHp , 
            RevivalItemType.HalfHealth=> math.trunc(selectedPartyPokemon.maxHp*0.5f), 
            _=> 0f
        };
        state.NotifyHealthChange();
        _pokemonPartyHandler.RefreshMemberCards();
        foreach (var participant in _battleHandler.GetParticipants)
        {
            if (participant.Pokemon is null) continue;
            if (participant.Pokemon.pokemonID == selectedPartyPokemon.pokemonID)
            {
                participant.awaitingRevival = true;
                break;
            }
        }
        _dialogueHandler.DisplayDetails(selectedPartyPokemon.pokemonDisplayName+" has been revived!");
        _turnBasedCombatHandler.OnNewTurn += _battleHandler.ReviveParticipant;
        OnItemUsed?.Invoke(itemInUse,true);
    }
    private void ChangePowerpoints(Item itemInUse,Pokemon selectedPartyPokemon)
    {
        scheduledInputStateRemoval = () =>
        {
            _inputStateHandler.ResetSpecificUi(InputStateName.PokemonDetailsMoveSelection);
            _inputStateHandler.ResetSpecificUi(InputStateName.PokemonDetails, true);
        };
        _pokemonDetailsHandler.SetUsage(PokemonDetailsUsage.AlterMoves);
        _pokemonDetailsHandler.OnMoveSelected += MoveOperation;
        _inputStateHandler.OnStateRemoved += ExitOperation;
        _gameUIHandler.ViewPokemonDetails(selectedPartyPokemon);
        
        return;
        void MoveOperation(int moveIndex)
        {
            _pokemonDetailsHandler.OnMoveSelected -= MoveOperation;
            _inputStateHandler.OnStateRemoved -= ExitOperation;
            
            var currentMove = selectedPartyPokemon.moveSet[moveIndex];
            var modifierInfo = itemInUse.GetDynamicModule<PowerpointModifeir>();
            Action<int, Item, Move> powerPointOperation = modifierInfo.modiferType switch
            {
                PowerpointModifeir.ModiferType.RestorePp => RestorePowerpoints,
                PowerpointModifeir.ModiferType.MaximisePp => MaximisePowerpoints,
                _ => IncreasePowerpoints
            };
            powerPointOperation.Invoke(moveIndex, itemInUse,currentMove);
        }
        void ExitOperation(InputState state)
        {
            if (state.stateName != InputStateName.PokemonDetailsMoveSelection) return;
            _pokemonDetailsHandler.OnMoveSelected -= MoveOperation;
            _inputStateHandler.OnStateRemoved -= ExitOperation;
            OnItemUsed?.Invoke(itemInUse,false);
        }
    }
    private void RestorePowerpoints(int moveIndex,Item itemInUse,Move currentMove)
     {
         if (currentMove.powerpoints == currentMove.maxPowerpoints)
         {
             _dialogueHandler.DisplayDetails( currentMove.moveName+" pp is already full");
             OnItemUsed?.Invoke(itemInUse,false);
             return;
         }
         
         var modifierInfo = itemInUse.GetDynamicModule<PowerpointModifeir>();
         var pointsToAdd = 0;
         
         if (modifierInfo.itemType == PowerpointModifeir.ModiferItemType.Ether)
             pointsToAdd = 10;
         
         if (modifierInfo.itemType == PowerpointModifeir.ModiferItemType.MaxEther)
             pointsToAdd = currentMove.maxPowerpoints;
         
         var sumPoints = currentMove.powerpoints + pointsToAdd;
         
         currentMove.powerpoints = sumPoints > currentMove.maxPowerpoints ? currentMove.maxPowerpoints : sumPoints;

         _dialogueHandler.DisplayDetails( currentMove.moveName+" pp was restored!");
         OnItemUsed?.Invoke(itemInUse,true);
     }
    private void IncreasePowerpoints(int moveIndex,Item itemInUse,Move currentMove)
    {
        double powerpointRatio = (float) currentMove.maxPowerpoints / currentMove.basePowerpoints;
        if (Math.Round(powerpointRatio, 1) >= 1.6)
        {
            _dialogueHandler.DisplayDetails( currentMove.moveName+" pp is already maxed out");
            OnItemUsed?.Invoke(itemInUse,false);
            return;
        }
        currentMove.maxPowerpoints += Mathf.FloorToInt(0.2f*currentMove.basePowerpoints);
        
        _dialogueHandler.DisplayDetails( currentMove.moveName+"'s pp was increased!");
        OnItemUsed?.Invoke(itemInUse,true);
    }

    private void MaximisePowerpoints(int moveIndex,Item itemInUse,Move currentMove)
    {
        if (currentMove.maxPowerpoints >= (currentMove.basePowerpoints * 1.6))
        {
            _dialogueHandler.DisplayDetails(currentMove.moveName + " pp is already maxed out");
            OnItemUsed?.Invoke(itemInUse,false);
            return;
        }
        currentMove.maxPowerpoints = Mathf.FloorToInt(currentMove.basePowerpoints * 1.6f);

        _dialogueHandler.DisplayDetails(currentMove.moveName + "'s pp was maxed out!"); 
        OnItemUsed?.Invoke(itemInUse,true);
    }
    private void UsePokeball(Item itemInUse)
    {
        if (!CanUsePokeball()) 
        {
            OnItemUsed?.Invoke(itemInUse,false);
            return;
        }

        _pokemonOperationsHandler.OnPokeballUsed += PokemonCaughtCheck;
        StartCoroutine(_pokemonOperationsHandler.TryToCatchPokemon(itemInUse));
        return;
        void PokemonCaughtCheck(Pokemon pokemon,bool isCaught)
        {
            _pokemonOperationsHandler.OnPokeballUsed -= PokemonCaughtCheck;
            QuickExit(itemInUse);
        }
        bool CanUsePokeball()
        {
            if (!_battleHandler.BattleInProgress)
            {
                _dialogueHandler.DisplayDetails("Can't use that right now!");
                return false;
            }
            if (_battleHandler.isTrainerBattle)
            {
                _dialogueHandler.DisplayDetails("Can't catch someone else's Pokemon!");
                return false;
            }
            if (_pokemonStorageHandler.MaxPokemonCapacity())
            {
                _dialogueHandler.DisplayDetails("Can no longer catch more Pokémon, free up space in PC!");
                return false;
            }
            return true;
        }
    }
    
    private void CureConfusion(Item itemInUse,Pokemon selectedPartyPokemon)
    {
        var currentParticipant = _battleHandler.GetCurrentParticipant();
        if (!_battleHandler.BattleInProgress)
        {
            _dialogueHandler.DisplayDetails("cant use that outside battle!");
            OnItemUsed?.Invoke(itemInUse,false);
            return;
        }
        if(!currentParticipant.isConfused)
            _dialogueHandler.DisplayDetails("Pokemon is already healthy");
        else
            currentParticipant.isConfused = false;
        
        _dialogueHandler.DisplayDetails($"{selectedPartyPokemon.pokemonDisplayName} was healed from its confusion");
        OnItemUsed?.Invoke(itemInUse,true);
    }
    
    private void HealMajorStatusEffect(Item itemInUse,Pokemon selectedPartyPokemon,StatusEffect curableStatus = StatusEffect.None)
    {
        if (curableStatus == StatusEffect.None)
        {
            var statusInfo = itemInUse.GetDynamicModule<StatusHealInfoModule>();
            curableStatus = statusInfo.statusEffect;
        }
        
        if (selectedPartyPokemon.statusEffect == StatusEffect.None)
        {
            _dialogueHandler.DisplayDetails("Pokemon is already healthy");
            OnItemUsed?.Invoke(itemInUse,false);
            return;
        }
        
        //antidote heals all poison
        if (curableStatus == StatusEffect.Poison &&
            selectedPartyPokemon.statusEffect == StatusEffect.BadlyPoison)
        {
            curableStatus = StatusEffect.BadlyPoison;
        }
        if (selectedPartyPokemon.statusEffect != curableStatus)
        {
            _dialogueHandler.DisplayDetails("Incorrect heal item");
            OnItemUsed?.Invoke(itemInUse,false);
            return;
        }
        
        HealStatusEffect(selectedPartyPokemon,false);
        _pokemonPartyHandler.RefreshMemberCards();
        
        OnItemUsed?.Invoke(itemInUse,true);
    }
    private void HealStatusEffect(Pokemon selectedPartyPokemon, bool healConfusion)
    {
        if (_battleHandler.BattleInProgress)
        {
            var currentParticipant = _battleHandler.GetCurrentParticipant();
            currentParticipant.statusHandler.RemoveStatusEffect(healConfusion);
            currentParticipant.RefreshStatusEffectImage();
        }
        else
            selectedPartyPokemon.statusEffect = StatusEffect.None;
        _dialogueHandler.DisplayDetails(selectedPartyPokemon.pokemonDisplayName+" has been healed");
    }
    private void RestoreHealth(Item itemInUse,PokemonStateHandler state)
    {
        var healEffect = itemInUse.GetDynamicModule<EffectValueInfo>().effectValue;
        RestoreHealth(healEffect, itemInUse, state);
    }
    private void RestoreHealth(float healAmount,Item itemInUse,PokemonStateHandler state)
    {
        var selectedPartyPokemon = state.pokemon;
        StartCoroutine(AwaitHealthGain());
        return;
        IEnumerator AwaitHealthGain()
        {
            if (selectedPartyPokemon.hp <= 0)
            {
                _dialogueHandler.DisplayDetails( selectedPartyPokemon.pokemonDisplayName+" has already fainted");
                OnItemUsed?.Invoke(itemInUse,false);
                yield break;
            } 
            if(selectedPartyPokemon.hp>=selectedPartyPokemon.maxHp)
            {
                _dialogueHandler.DisplayDetails(selectedPartyPokemon.pokemonDisplayName+"'s health already is full");
                OnItemUsed?.Invoke(itemInUse,false);
                yield break;
            }
            _moveUsageHandler.HealthGainDisplay(healAmount,state);
            _dialogueHandler.DisplayDetails($"{selectedPartyPokemon.pokemonDisplayName} gained {healAmount} health points");
            yield return _moveUsageHandler.AwaitHealthGainDisplay();
            OnItemUsed?.Invoke(itemInUse,true);
        }
    }
    private void FullRestore(Item itemInUse,PokemonStateHandler state)
    {
        var selectedPartyPokemon = state.pokemon;
        if (selectedPartyPokemon.hp <= 0)
        {
            _dialogueHandler.DisplayDetails( selectedPartyPokemon.pokemonDisplayName+" has already fainted");
            OnItemUsed?.Invoke(itemInUse,false);
            return;
        }
 
        var healEffectTriggered = false;
        
        if (selectedPartyPokemon.statusEffect != StatusEffect.None)
        {
            HealStatusEffect(selectedPartyPokemon,true);
            healEffectTriggered = true;
        }
        else
        {
            //Chek for confusion
            if (_battleHandler.BattleInProgress)
            {
                var currentParticipant = _battleHandler.GetCurrentParticipant();
                if(currentParticipant.isConfused)
                {
                    currentParticipant.isConfused = false;
                    healEffectTriggered = true;
                }
            }
        }
        if(healEffectTriggered)_pokemonPartyHandler.RefreshMemberCards();
        
        var fullHealth = selectedPartyPokemon.maxHp;
        if(selectedPartyPokemon.hp < fullHealth)
        {
            StartCoroutine(AwaitHealthGain());
            return;
            IEnumerator AwaitHealthGain()
            {
                _moveUsageHandler.HealthGainDisplay(fullHealth,state);
                if (!healEffectTriggered)
                {
                    _dialogueHandler.DisplayDetails(selectedPartyPokemon.pokemonDisplayName+" has been healed");
                    healEffectTriggered = true;
                }
                yield return _moveUsageHandler.AwaitHealthGainDisplay();
                
                OnItemUsed?.Invoke(itemInUse,healEffectTriggered);
            }
        }

        OnItemUsed?.Invoke(itemInUse,healEffectTriggered);
    }
    private void StatusFullHeal(Item itemInUse,Pokemon selectedPartyPokemon)
    {
        if (selectedPartyPokemon.statusEffect == StatusEffect.None)
        {
            _dialogueHandler.DisplayDetails("Pokemon is already healthy");
            OnItemUsed?.Invoke(itemInUse,false);
            return;
        }
        HealStatusEffect(selectedPartyPokemon,true);
        _pokemonPartyHandler.RefreshMemberCards();
        OnItemUsed?.Invoke(itemInUse,true);
    }
}

