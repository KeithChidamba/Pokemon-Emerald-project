using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public enum ItemType
{
    Special,Repel,HealHp,Status,PowerPointModifier,Herb,Revive,HeldItem,Vitamin,
    Berry,Pokeball,EvolutionStone,RareCandy,XItem,GainMoney,Overworld,LearnableMove
}
public class ItemHandler : MonoBehaviour,IInjectable
{
    public event Action<Item,bool> OnItemUsed;
    private Action scheduledInputStateRemoval;
    
    private PokemonDetailsHandler _pokemonDetailsHandler;
    private MoveSequenceHandler _moveUsageHandler;
    private DialogueHandler _dialogueHandler;
    private InputStateHandler _inputStateHandler;
    private BattleHandler _battleHandler;
    private AreaManager  _areaHandler;
    private PlayerBagHandler _playerBagHandler;
    private OverworldActionsHandler _overworldActions;
    private PokemonPartyHandler _pokemonPartyHandler;
    private PokemonOperations _pokemonOperationsHandler;
    private PokemonStorageHandler _pokemonStorageHandler;
    private TurnBasedCombatHandler _turnBasedCombatHandler;
    private GameUiHandler _gameUIHandler;
    private PlayerTileHandler _playerTileHandler;
    private BattleOperations _battleOperations;
    
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
        _areaHandler = container.Resolve<AreaManager>();
        _overworldActions = container.Resolve<OverworldActionsHandler>();
        _gameUIHandler = container.Resolve<GameUiHandler>();
        _playerTileHandler = container.Resolve<PlayerTileHandler>();
        _battleOperations = container.Resolve<BattleOperations>();
        
        gameObject.SetActive(true);
    }
    public void OnInject() { }

    public void UseItem(Item itemInUse,Pokemon selectedPokemon)
    {
        scheduledInputStateRemoval = null;
        OnItemUsed += CompleteItemUsage;
        void CompleteItemUsage(Item itemUsed, bool successful)
        {
            OnItemUsed -= CompleteItemUsage;
            _inputStateHandler.AddPlaceHolderState();
            StartCoroutine(CompletionSequence());
            return;
            IEnumerator CompletionSequence()
            {
                if(_dialogueHandler.Displaying)
                {
                    yield return _dialogueHandler.WaitForDialogueCompletion();
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
                    if (!successful)
                    {
                        //necessary to reset internal bag state
                        _playerBagHandler.SetupBagState();
                    }
                }
                if (successful)
                {
                    _playerBagHandler.DepleteItem(itemUsed);
                    if (_battleHandler.BattleInProgress)
                    {
                        _inputStateHandler.ResetSpecificUi(InputStateName.PlayerBagNavigation,true);
                        _battleHandler.SetPlayerTurnUsage(PlayerTurnUsage.UseItem);
                        _turnBasedCombatHandler.NextTurn();
                    }
                }
                yield return new WaitForSecondsRealtime(0.2f);
                if (!_battleHandler.BattleInProgress)
                {
                    _inputStateHandler.ResetSpecificUi(InputStateName.PlaceHolder);
                }
            }
        }
        
        switch (itemInUse.itemType)
        {
            case ItemType.Overworld : UseOverworldItem(itemInUse); break;
            
            case ItemType.Repel: UseRepel(itemInUse); break;
            
            case ItemType.Pokeball: UsePokeball(itemInUse); break;
            
            case ItemType.Special: EquipItem(itemInUse); break;
        //party use
            case ItemType.LearnableMove:
                StartCoroutine(HandleMoveLearning(itemInUse,selectedPokemon)); break;
            
            //abstract
            case ItemType.Herb: UseHerbs(itemInUse,selectedPokemon); break;
            
            case ItemType.Berry: UseBerries(itemInUse,selectedPokemon); break;
            
            case ItemType.PowerPointModifier: ChangePowerpoints(itemInUse,selectedPokemon); break;
            
            //core
            case ItemType.HealHp: RestoreHealth(itemInUse,selectedPokemon); break;
            
            case ItemType.Revive: RevivePokemon(itemInUse,selectedPokemon); break;
            
            case ItemType.Status: HealStatusEffect(itemInUse,selectedPokemon); break;
            
            case ItemType.Vitamin: GetEVsFromItem(itemInUse,selectedPokemon); break;
            
            case ItemType.EvolutionStone: StartCoroutine(TriggerStoneEvolution(itemInUse,selectedPokemon)); break;
            
            case ItemType.RareCandy: StartCoroutine(LevelUpWithItem(itemInUse,selectedPokemon)); break;
            
            case ItemType.XItem: UseStatModifyingBattleItem(itemInUse,selectedPokemon); break;
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
    void EquipItem(Item itemInUse)
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
        var repelDuration = (int)itemInUse.GetDynamicModule<ItemEffectInfo>().effectValue;
        _dialogueHandler.DisplayDetails("Repel has been activated");
        _playerTileHandler.ActivateRepel(repelDuration);
        OnItemUsed?.Invoke(itemInUse,true);
    }
    
    private void UseHerbs(Item itemInUse,Pokemon selectedPartyPokemon)
    {
        var herbInfo = itemInUse.GetModule<HerbInfoModule>();
        OnItemUsed += ChangeFriendship;
        var usageIndex = herbInfo.GetHerbUsage();
        var herbUsages = new List<Action<Item,Pokemon>> 
        {
            RestoreHealth,
            (item,pokemon) => HealStatusEffect(item,pokemon,herbInfo.statusEffect),
            UseReviveHerb
        };
        herbUsages[usageIndex].Invoke(itemInUse,selectedPartyPokemon);
        return;
        
        void UseReviveHerb(Item itemUsed,Pokemon pokemon)
        {
            HandlePokemonRevive(itemInUse,herbInfo.reviveType, pokemon);
        }
        void ChangeFriendship(Item itemUsed,bool successful)
        {
            OnItemUsed -= ChangeFriendship;
            var friendshipLoss = herbInfo.herbType switch
            {
                Herb.EnergyPowder => -5,
                Herb.EnergyRoot => -10,
                Herb.HealPowder => -5,
                Herb.RevivalHerb => -15,
                _ => 0
            };
            if(successful && friendshipLoss!=0)
            {
                selectedPartyPokemon.ChangeFriendshipLevel(friendshipLoss);
            }
        }
    }

    private void UseBerries(Item itemInUse,Pokemon selectedPartyPokemon)
    {
        var berryInfo = itemInUse.GetModule<BerryInfoModule>();
        var usageIndex = berryInfo.GetBerryUsage();
        var berryUsages = new List<Action<Item,Pokemon>> 
        {
            GetFriendshipFromBerry,
            RestoreHealth,
            (item,pokemon) => HealStatusEffect(item,pokemon,berryInfo.statusEffect),
            ChangePowerpoints,
            (item,pokemon) => CureConfusion(item)
        };
        berryUsages[usageIndex].Invoke(itemInUse,selectedPartyPokemon);
    }
    private void UseOverworldItem(Item itemInUse)
    {
        var specialItem = itemInUse.GetDynamicModule<OverworldUsageItem>().specialItem;
        if (specialItem == SpecialOverworldItem.EscapeRope)
        {
            if (_areaHandler.currentArea.data.escapable)
            {
                _areaHandler.EscapeArea();
                _inputStateHandler.ResetRelevantUi(new[] {InputStateName.PlayerMenu
                        ,InputStateName.PlayerBagNavigation});
                OnItemUsed?.Invoke(itemInUse,true);
            }
            else
            {
                _dialogueHandler.DisplayDetails("Can't use that here!");
                OnItemUsed?.Invoke(itemInUse,false);
            }
        }
    }

    IEnumerator LevelUpWithItem(Item itemInUse,Pokemon selectedPartyPokemon)
    {
        if (selectedPartyPokemon.currentLevel == 100)
        {
            _dialogueHandler.DisplayDetails(selectedPartyPokemon.pokemonDisplayName+" is already max level!");
            OnItemUsed?.Invoke(itemInUse,false);
        }
        else
        {
            var expForNextLevel = _pokemonOperationsHandler.CalculateExpForNextLevel(selectedPartyPokemon.currentLevel, selectedPartyPokemon.expGroup);
            var expDifferenceForLevelUp = expForNextLevel - selectedPartyPokemon.currentExpAmount;
            yield return selectedPartyPokemon.ReceiveExperienceOutsideBattle(expDifferenceForLevelUp,true);
            OnItemUsed?.Invoke(itemInUse,true);
        }
    }

    private IEnumerator TriggerStoneEvolution(Item itemInUse,Pokemon selectedPartyPokemon)
    { 
       var stoneInfo = itemInUse.GetModule<EvolutionStoneInfoModule>();
       
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
        var statToDecrease = itemInUse.GetModule<StatInfoModule>();
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
            selectedPartyPokemon.DetermineFriendshipLevelChange(true,FriendshipModifier.Berry);
            OnItemUsed?.Invoke(itemInUse,true);
        }
    }
    private void GetEVsFromItem(Item itemInUse,Pokemon selectedPartyPokemon) 
    {
        var statToIncrease = itemInUse.GetModule<StatInfoModule>();
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
                selectedPartyPokemon.DetermineFriendshipLevelChange(false, FriendshipModifier.Vitamin);
                OnItemUsed?.Invoke(itemInUse,true);
            }
            _dialogueHandler.DisplayDetails(message);
            OnItemUsed?.Invoke(itemInUse,false);
        }
    }

    private void UseStatModifyingBattleItem(Item itemInUse,Pokemon selectedPartyPokemon)
    {
        var statInfo = itemInUse.GetModule<StatInfoModule>();
        var currentParticipant = _battleHandler.GetCurrentParticipant();
        if (statInfo.statName == Stat.None)
        {//guard spec doesn't change stats but prevents stat reduction
            if (currentParticipant.ProtectedFromStatChange(false))
            {
                _dialogueHandler.DisplayDetails("Your pokemon are already protected");
                OnItemUsed?.Invoke(itemInUse,false);
                return;
            }
            _moveUsageHandler.ApplyStatChangeImmunity(currentParticipant,
                StatChangeability.ImmuneToDecrease,5);
            
            string pokemonProtected = selectedPartyPokemon.pokemonDisplayName;
            
            if (_battleHandler.isDoubleBattle)
            {
                var partner = currentParticipant.GetPartner();
                if(partner.isActive)
                {
                    _moveUsageHandler.ApplyStatChangeImmunity(partner,
                        StatChangeability.ImmuneToDecrease, 5);
                    pokemonProtected = selectedPartyPokemon.pokemonDisplayName + " and " + partner.pokemon.pokemonDisplayName;
                }
            }
            _dialogueHandler.DisplayBattleInfo("A veil of light covers "+pokemonProtected);
            OnItemUsed?.Invoke(itemInUse,false);
            return;
        }
       
        var buff = _battleOperations.SearchForStatModifier(selectedPartyPokemon, statInfo.statName);
        if (buff is { isAtLimit: true })
        {
            _dialogueHandler.DisplayBattleInfo($"{selectedPartyPokemon.pokemonDisplayName}'s " +
                                                        $"{buff.statName} can't go any higher");
            OnItemUsed?.Invoke(itemInUse,false);
            return;
        }
       
        var xBuffData = new StatChangeTransitData(currentParticipant, statInfo.statName, true, 1);
        _moveUsageHandler.InitiateStatChange(xBuffData);
        OnItemUsed?.Invoke(itemInUse,true);
    }

    private void RevivePokemon(Item itemInUse,Pokemon pokemon)
    {
        var reviveType = itemInUse.GetModule<RevivalItemInfo>().reviveType;
        HandlePokemonRevive(itemInUse,reviveType, pokemon);
    }
    private void HandlePokemonRevive(Item itemInUse,RevivalItemType reviveType,Pokemon selectedPartyPokemon)
    {
        if (selectedPartyPokemon.hp > 0)
        {
            _dialogueHandler.DisplayDetails(selectedPartyPokemon.pokemonDisplayName+" has not fainted!"); 
            OnItemUsed?.Invoke(itemInUse,false);
        }
        selectedPartyPokemon.hp = reviveType switch
        { 
            RevivalItemType.FullHealth=> selectedPartyPokemon.maxHp , 
            RevivalItemType.HalfHealth=> math.trunc(selectedPartyPokemon.maxHp*0.5f), 
            _=> 0f
        };
        selectedPartyPokemon.NotifyHealthChange();
        _battleHandler.CountValidParticipants();
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
        _gameUIHandler.ViewPartyPokemonDetails(selectedPartyPokemon);
        
        return;
        void MoveOperation(int moveIndex)
        {
            _pokemonDetailsHandler.OnMoveSelected -= MoveOperation;
            _inputStateHandler.OnStateRemoved -= ExitOperation;
            
            var currentMove = selectedPartyPokemon.moveSet[moveIndex];
            var modifierInfo = itemInUse.GetModule<PowerpointModifeir>();
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
         
         var modifierInfo = itemInUse.GetModule<PowerpointModifeir>();
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
        
        void PokemonCaughtCheck(Pokemon pokemon,bool isCaught)
        {
            _pokemonOperationsHandler.OnPokeballUsed -= PokemonCaughtCheck;
            OnItemUsed?.Invoke(itemInUse,true);
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
     
    private void CureConfusion(Item itemInUse)
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
        OnItemUsed?.Invoke(itemInUse,true);
    }
    private bool IsValidStatusHeal(StatusEffect curableStatus,Pokemon selectedPartyPokemon)
    {
        var currentParticipant = _battleHandler.GetCurrentParticipant();
        if (selectedPartyPokemon.statusEffect == StatusEffect.None)
        {
            if (_battleHandler.BattleInProgress)
            {
                if (!currentParticipant.isConfused)
                {
                    _dialogueHandler.DisplayDetails("Pokemon is already healthy");
                    return false;
                }
            }
            else
            {
                _dialogueHandler.DisplayDetails("Pokemon is already healthy");
                return false;
            }
        }
        
        //antidote heals all poison
        if (curableStatus == StatusEffect.Poison &&
            selectedPartyPokemon.statusEffect == StatusEffect.BadlyPoison)
        {
            curableStatus = StatusEffect.BadlyPoison;
        }
        
        bool isValidHeal = selectedPartyPokemon.statusEffect == curableStatus
                                   || curableStatus == StatusEffect.FullHeal;
        
        if (!isValidHeal)
        {
            _dialogueHandler.DisplayDetails("Incorrect heal item");
            return false;
        }
        
        //healing
        if (_battleHandler.BattleInProgress)
        {
            var healAll = curableStatus == StatusEffect.FullHeal;
            currentParticipant.statusHandler.RemoveStatusEffect(healAll);
            currentParticipant.RefreshStatusEffectImage();
        }
        else
            selectedPartyPokemon.statusEffect = StatusEffect.None;
        
        _dialogueHandler.DisplayDetails(selectedPartyPokemon.pokemonDisplayName+" has been healed");
       
        return true;
    }
    private void HealStatusEffect(Item itemInUse,Pokemon selectedPartyPokemon,StatusEffect curableStatus = StatusEffect.None)
    {
        if (curableStatus == StatusEffect.None)
        {
            var statusInfo = itemInUse.GetModule<StatusHealInfoModule>();
            curableStatus = statusInfo.statusEffect;
        }
        var validHeal = IsValidStatusHeal(curableStatus, selectedPartyPokemon);
        if (validHeal)
        {
            _pokemonPartyHandler.RefreshMemberCards();
        }
        OnItemUsed?.Invoke(itemInUse,validHeal);
    }
    private void RestoreHealth(Item itemInUse,Pokemon selectedPartyPokemon)
    {
        var healEffect = itemInUse.GetDynamicModule<ItemEffectInfo>().effectValue;
        if (selectedPartyPokemon.hp <= 0)
        {
            _dialogueHandler.DisplayDetails( selectedPartyPokemon.pokemonDisplayName+" has already fainted");
            OnItemUsed?.Invoke(itemInUse,false);
            return;
        } 
       
        if(selectedPartyPokemon.hp>=selectedPartyPokemon.maxHp)
        {
            _dialogueHandler.DisplayDetails(selectedPartyPokemon.pokemonDisplayName+"'s health already is full");
            OnItemUsed?.Invoke(itemInUse,false);
            return;
        }
        _moveUsageHandler.HealthGainDisplay(healEffect,affectedPokemon:selectedPartyPokemon);
        _dialogueHandler.DisplayDetails(selectedPartyPokemon.pokemonDisplayName+" gained "+healEffect+" health points");
        OnItemUsed?.Invoke(itemInUse,true);
    }
}

