using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;

public class Item_handler : MonoBehaviour,IInjectable
{
    public event Action<Item,bool> OnItemUsed;
    
    private Pokemon_Details _pokemonDetailsHandler;
    private Move_handler _moveUsageHandler;
    private Dialogue_handler _dialogueHandler;
    private InputStateHandler _inputStateHandler;
    private Battle_handler _battleHandler;
    private Area_manager  _areaHandler;
    private Bag _playerBagHandler;
    private overworld_actions _overworldActions;
    private Pokemon_party _pokemonPartyHandler;
    private PokemonOperations _pokemonOperationsHandler;
    private pokemon_storage _pokemonStorageHandler;
    private Turn_Based_Combat _turnBasedCombatHandler;
    private Game_ui_manager _gameUIHandler;
    private PlayerTileHandler _playerTileHandler;
    private BattleOperations _battleOperations;
    
    public void Inject(ServiceContainer container)
    {
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _battleHandler = container.Resolve<Battle_handler>();
        _turnBasedCombatHandler = container.Resolve<Turn_Based_Combat>();
        _moveUsageHandler = container.Resolve<Move_handler>();
        _playerBagHandler = container.Resolve<Bag>();
        _pokemonPartyHandler = container.Resolve<Pokemon_party>();
        _pokemonOperationsHandler = container.Resolve<PokemonOperations>();
        _pokemonStorageHandler = container.Resolve<pokemon_storage>();
        _pokemonDetailsHandler = container.Resolve<Pokemon_Details>();
        _areaHandler = container.Resolve<Area_manager>();
        _overworldActions = container.Resolve<overworld_actions>();
        _gameUIHandler = container.Resolve<Game_ui_manager>();
        _playerTileHandler = container.Resolve<PlayerTileHandler>();
        _battleOperations = container.Resolve<BattleOperations>();
        
        gameObject.SetActive(true);
    }

    public void OnInject()
    {
        
    }

    public void UseItem(Item itemInUse,Pokemon selectedPokemon)
    {
        if (_battleHandler.battleInProgress)
        {
            _inputStateHandler.ResetGroupUi(InputStateGroup.Bag);
        }
        if (itemInUse.itemType == ItemType.Special)
        {
            if (_overworldActions.IsEquipped(item:itemInUse))
            {
                _overworldActions.UnequipItem(itemInUse);
                return;
            }
            _overworldActions.EquipItem(itemInUse);
            return;
        }

        if (itemInUse.forPartyUse)
        {
            OnItemUsed += ResetPartyUI;
        }

        void ResetPartyUI(Item itemUsed,bool successful)
        {
            if(!successful)
            {
                OnItemUsed -= ResetPartyUI;
                _inputStateHandler.ResetGroupUi(InputStateGroup.PokemonParty);
            }
        }
        
        switch (itemInUse.itemType)
        {
            case ItemType.Overworld : UseOverworldItem(itemInUse); break;
            
            case ItemType.Repel: UseRepel(itemInUse); break;
            
            case ItemType.Pokeball: UsePokeball(itemInUse); break;
            
            //party use
            case ItemType.LearnableMove: 
                StartCoroutine(_pokemonOperationsHandler.LearnTmOrHm(itemInUse,selectedPokemon)); 
                break;
            
            case ItemType.PowerPointModifier: ChangePowerpoints(itemInUse,selectedPokemon); break;
            
            case ItemType.Herb: UseHerbs(itemInUse,selectedPokemon); break;
            
            case ItemType.Berry: UseBerries(itemInUse,selectedPokemon); break;
            
            case ItemType.HealHp: RestoreHealth(itemInUse,selectedPokemon); break;
            
            case ItemType.Revive: RevivePokemon(itemInUse,itemInUse.itemType,selectedPokemon); break;
            
            case ItemType.Status: HealStatusEffect(itemInUse,selectedPokemon); break;
            
            case ItemType.Vitamin: GetEVsFromItem(itemInUse,selectedPokemon); break;
            
            case ItemType.EvolutionStone: StartCoroutine(TriggerStoneEvolution(itemInUse,selectedPokemon)); break;
            
            case ItemType.RareCandy: StartCoroutine(LevelUpWithItem(itemInUse,selectedPokemon)); break;
            
            case ItemType.XItem: ItemBuffOrDebuff(itemInUse,selectedPokemon); break;
        }
    }

    private void UseRepel(Item itemInUse)
    {
        var repelDuration = (int)itemInUse.GetDynamicModule<ItemEffectInfo>().effectValue;
        _dialogueHandler.DisplayDetails("Repel has been activated");
        _playerTileHandler.ActivateRepel(repelDuration);
        CompleteItemUsage(itemInUse);
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
            (item,pokemon) => RevivePokemon(item,herbInfo.itemType,pokemon)
        };
        herbUsages[usageIndex].Invoke(itemInUse,selectedPartyPokemon);
        return;
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
                OnItemUsed?.Invoke(itemInUse,true);
                CompleteItemUsage(itemInUse);
                _areaHandler.EscapeArea();
                _inputStateHandler.ResetRelevantUi(new[] {InputStateName.PlayerMenu
                        ,InputStateName.PlayerBagNavigation});
            }
            else
            {
                OnItemUsed?.Invoke(itemInUse,false);
                _dialogueHandler.DisplayDetails("Can't use that here!");
            }
        }
    }

    IEnumerator LevelUpWithItem(Item itemInUse,Pokemon selectedPartyPokemon)
    {
        if (selectedPartyPokemon.currentLevel == 100)
        {
            OnItemUsed?.Invoke(itemInUse,false);
            _dialogueHandler.DisplayDetails(selectedPartyPokemon.pokemonDisplayName+" is already max level!");
            
        }
        else
        {
            OnItemUsed?.Invoke(itemInUse,true);
            var expForNextLevel = _pokemonOperationsHandler.CalculateExpForNextLevel(selectedPartyPokemon.currentLevel, selectedPartyPokemon.expGroup);
            var expDifferenceForLevelUp = expForNextLevel - selectedPartyPokemon.currentExpAmount;
            yield return selectedPartyPokemon.ReceiveExperienceOutsideBattle(expDifferenceForLevelUp,true);
            CompleteItemUsage(itemInUse);
            _inputStateHandler.ResetGroupUi(InputStateGroup.PokemonParty);
        }
    }

    IEnumerator TriggerStoneEvolution(Item itemInUse,Pokemon selectedPartyPokemon)
    { 
       var stoneInfo = itemInUse.GetModule<EvolutionStoneInfoModule>();
       if (selectedPartyPokemon.evolutionStone == stoneInfo.stoneName && selectedPartyPokemon.requiresEvolutionStone)
       {
           yield return _pokemonOperationsHandler.HandlePokemonEvolution(selectedPartyPokemon,0);
           OnItemUsed?.Invoke(itemInUse,true);
           CompleteItemUsage(itemInUse);
       }
       else
       {
           OnItemUsed?.Invoke(itemInUse,false);
            _dialogueHandler.DisplayDetails("Cant use that on "+selectedPartyPokemon.pokemonDisplayName);
            
       }
    }

    private void GetFriendshipFromBerry(Item itemInUse,Pokemon selectedPartyPokemon)
    {
        var statToDecrease = itemInUse.GetModule<StatInfoModule>();
        if(selectedPartyPokemon.friendshipLevel>254)
        {
            OnItemUsed?.Invoke(itemInUse,false);
            _dialogueHandler.DisplayDetails(selectedPartyPokemon.pokemonDisplayName+"'s friendship is already maxed out");
            
        }
        else
        {
            OnItemUsed?.Invoke(itemInUse,true);
            _dialogueHandler.DisplayDetails(selectedPartyPokemon.pokemonDisplayName+"'s friendship was increased");
            ref float evRef = ref _pokemonOperationsHandler.GetEvStatRef(statToDecrease.statName, selectedPartyPokemon);
            if (evRef > 100) evRef = 100;
            else _pokemonOperationsHandler.CalculateEvForStat(statToDecrease.statName, -10, selectedPartyPokemon);
            selectedPartyPokemon.DetermineFriendshipLevelChange(true,FriendshipModifier.Berry);
            CompleteItemUsage(itemInUse);
        }
    }
    private void GetEVsFromItem(Item itemInUse,Pokemon selectedPartyPokemon) 
    {
        var statToIncrease = itemInUse.GetModule<StatInfoModule>();
        _pokemonOperationsHandler.OnEvChange += CheckEvChange;
        _pokemonOperationsHandler.CalculateEvForStat(statToIncrease.statName, 10, selectedPartyPokemon);

        void CheckEvChange(Stat stat,bool hasChanged)
        {
            _pokemonOperationsHandler.OnEvChange -= CheckEvChange;
            var message = selectedPartyPokemon.pokemonDisplayName + "'s " + NameDB.GetStatName(stat);
        
            message += (hasChanged)? " was increased" : " can't get any higher";

            if (hasChanged)
            {
                OnItemUsed?.Invoke(itemInUse,true);
                selectedPartyPokemon.DetermineFriendshipLevelChange(false,
                    FriendshipModifier.Vitamin);
                DepleteItem(itemInUse);
            }
            OnItemUsed?.Invoke(itemInUse,false);
            _dialogueHandler.DisplayDetails(message);
            
        }
    }

    private void ItemBuffOrDebuff(Item itemInUse,Pokemon selectedPartyPokemon)
    {
        var statInfo = itemInUse.GetModule<StatInfoModule>();
        var currentParticipant = _battleHandler.GetCurrentParticipant();
        if (statInfo.statName == Stat.None)
        {//guard spec doesn't buff stat but remove stat reduction
            if (currentParticipant.ProtectedFromStatChange(false))
            {
                OnItemUsed?.Invoke(itemInUse,false);
                _dialogueHandler.DisplayDetails("Your pokemon are already protected");
                
                return;
            }
            _moveUsageHandler.ApplyStatChangeImmunity(currentParticipant,
                StatChangeability.ImmuneToDecrease,5);
            
            string pokemonProtected = selectedPartyPokemon.pokemonDisplayName;
            
            if (_battleHandler.isDoubleBattle)
            {
                var partner = _battleHandler.battleParticipants[currentParticipant.GetPartnerIndex()];
                if(partner.isActive)
                {
                    _moveUsageHandler.ApplyStatChangeImmunity(partner,
                        StatChangeability.ImmuneToDecrease, 5);
                    pokemonProtected = selectedPartyPokemon.pokemonDisplayName + " and " + partner.pokemon.pokemonDisplayName;
                }
            }
            OnItemUsed?.Invoke(itemInUse,false);
            _dialogueHandler.DisplayBattleInfo("A veil of light covers "+pokemonProtected);
            StartCoroutine(CompleteItemUsage(1f,itemInUse));
            return;
        }
       
        var buff = _battleOperations.SearchForBuffOrDebuff(selectedPartyPokemon, statInfo.statName);
        if (buff is { isAtLimit: true })
        {
            _dialogueHandler.DisplayBattleInfo($"{selectedPartyPokemon.pokemonDisplayName}'s " +
                                                        $"{buff.statName} can't go any higher");
            
            OnItemUsed?.Invoke(itemInUse,false);
            return;
        }
        OnItemUsed?.Invoke(itemInUse,true);
        var xBuffData = new BuffDebuffData(currentParticipant, statInfo.statName, true, 1);
        _moveUsageHandler.ExecuteBuffOrDebuff(xBuffData);
        StartCoroutine(CompleteItemUsage(0,itemInUse));
    }
    private void RevivePokemon(Item itemInUse,ItemType reviveType,Pokemon selectedPartyPokemon)
    {
        if (selectedPartyPokemon.hp > 0)
        {
            _dialogueHandler.DisplayDetails(selectedPartyPokemon.pokemonDisplayName+" has not fainted!"); 
            OnItemUsed?.Invoke(itemInUse,false);
            
            return;
        }
        OnItemUsed?.Invoke(itemInUse,true);
        
        selectedPartyPokemon.hp = reviveType == ItemType.MaxRevive? 
            selectedPartyPokemon.maxHp : math.trunc(selectedPartyPokemon.maxHp*0.5f);
        
        _dialogueHandler.DisplayDetails(selectedPartyPokemon.pokemonDisplayName+" has been revived!");
        StartCoroutine(CompleteItemUsage(2.2f,itemInUse));
    }
    private void ChangePowerpoints(Item itemInUse,Pokemon selectedPartyPokemon)
    {
        _pokemonDetailsHandler.changingMoveData = true;
        var modifierInfo = itemInUse.GetModule<PowerpointModifeir>();
        
        _pokemonDetailsHandler.OnMoveSelected += MoveOperation;
        _gameUIHandler.ViewPartyPokemonDetails(selectedPartyPokemon);

        void MoveOperation(int moveIndex)
        {
            Action<int, Item, Move> powerPointOperation = modifierInfo.modiferType switch
            {
                ModiferType.RestorePp => RestorePowerpoints,
                ModiferType.MaximisePp => MaximisePowerpoints,
                _ => IncreasePowerpoints
            };

            var moveAlterationCancelled = moveIndex == -1;
            if (moveAlterationCancelled)
            {
                _inputStateHandler.ResetGroupUi(InputStateGroup.PokemonDetails);
                
                OnItemUsed?.Invoke(itemInUse,false);
                return; 
            }
            OnItemUsed += CheckUsageSuccess;
            
            var currentMove = selectedPartyPokemon.moveSet[moveIndex];
            powerPointOperation.Invoke(moveIndex, itemInUse,currentMove);
            void CheckUsageSuccess(Item itemUsed,bool successful)
            {
                if(successful)
                {
                    OnItemUsed -= CheckUsageSuccess;
                    _pokemonDetailsHandler.OnMoveSelected -= MoveOperation;
                    StartCoroutine(CompleteItemUsage(2.2f,itemInUse));
                    _inputStateHandler.ResetGroupUi(InputStateGroup.PokemonDetails);
                }
            }
        }
    }
    private void RestorePowerpoints(int moveIndex,Item itemInUse,Move currentMove)
     {
         if (currentMove.powerpoints == currentMove.maxPowerpoints)
         {
             OnItemUsed?.Invoke(itemInUse,false);
             _dialogueHandler.DisplayDetails( currentMove.moveName+" pp is already full");
             return;
         }
         
         var modifierInfo = itemInUse.GetModule<PowerpointModifeir>();
         var pointsToAdd = 0;
         
         if (modifierInfo.itemType == ModiferItemType.Ether)
             pointsToAdd = 10;
         
         if (modifierInfo.itemType == ModiferItemType.MaxEther)
             pointsToAdd = currentMove.maxPowerpoints;
         
         var sumPoints = currentMove.powerpoints + pointsToAdd;
         
         currentMove.powerpoints = (sumPoints > currentMove.maxPowerpoints) ? currentMove.maxPowerpoints : sumPoints;
         
         OnItemUsed?.Invoke(itemInUse,true);
         _dialogueHandler.DisplayDetails( currentMove.moveName+" pp was restored!");
     }
    private void IncreasePowerpoints(int moveIndex,Item itemInUse,Move currentMove)
    {
        double powerpointRatio = (float) currentMove.maxPowerpoints / currentMove.basePowerpoints;
        if (Math.Round(powerpointRatio, 1) >= 1.6)
        {
            OnItemUsed?.Invoke(itemInUse,false);
            _dialogueHandler.DisplayDetails( currentMove.moveName+" pp is already maxed out");
            return;
        }
        currentMove.maxPowerpoints += (int)math.floor(0.2*currentMove.basePowerpoints);
        
        _dialogueHandler.DisplayDetails( currentMove.moveName+"'s pp was increased!");
        OnItemUsed?.Invoke(itemInUse,true);
    }

    private void MaximisePowerpoints(int moveIndex,Item itemInUse,Move currentMove)
    {
        if (currentMove.maxPowerpoints >= (currentMove.basePowerpoints * 1.6))
        {
            OnItemUsed?.Invoke(itemInUse,false);
            _dialogueHandler.DisplayDetails(currentMove.moveName + " pp is already maxed out");
            return;
        }
        currentMove.maxPowerpoints = (int)math.floor(currentMove.basePowerpoints * 1.6);

        OnItemUsed?.Invoke(itemInUse,true);
        _dialogueHandler.DisplayDetails(currentMove.moveName + "'s pp was maxed out!");
}
    private void UsePokeball(Item itemInUse)
    {
        if (!CanUsePokeball()) 
        {
            OnItemUsed?.Invoke(itemInUse,false);
            return;
        }
        DepleteItem(itemInUse);

        _pokemonOperationsHandler.OnPokeballUsed += PokemonCaughtCheck;
        StartCoroutine(_pokemonOperationsHandler.TryToCatchPokemon(itemInUse));
        
        void PokemonCaughtCheck(Pokemon pokemon,bool isCaught)
        {
            _pokemonOperationsHandler.OnPokeballUsed -= PokemonCaughtCheck;
            if (!isCaught)
            {
                SkipTurn();
            }
            OnItemUsed?.Invoke(itemInUse,true);
        }
    }

    private bool CanUsePokeball()
    {
        if (!_battleHandler.battleInProgress)
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
    private void CureConfusion(Item itemInUse)
    {
        var currentParticipant = _battleHandler.GetCurrentParticipant();
        if (!_battleHandler.battleInProgress)
        {
            OnItemUsed?.Invoke(itemInUse,false);
            _dialogueHandler.DisplayDetails("cant use that outside battle!");
            return;
        }
        if(!currentParticipant.isConfused)
            _dialogueHandler.DisplayDetails("Pokemon is already healthy");
        else
            currentParticipant.isConfused = false;
        OnItemUsed?.Invoke(itemInUse,true);
    }
    private bool IsValidStatusHeal(Item itemInUse,StatusEffect curableStatus,Pokemon selectedPartyPokemon)
    {
        var currentParticipant = _battleHandler.GetCurrentParticipant();
        if (selectedPartyPokemon.statusEffect == StatusEffect.None)
        {
            if (_battleHandler.battleInProgress)
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
        if (_battleHandler.battleInProgress)
        {
            var healAll = curableStatus == StatusEffect.FullHeal;
            currentParticipant.statusHandler.RemoveStatusEffect(healAll);
            _battleHandler.RefreshStatusEffectUI();
        }
        else
            selectedPartyPokemon.statusEffect = StatusEffect.None;
        
        _dialogueHandler.DisplayDetails(selectedPartyPokemon.pokemonDisplayName+" has been healed");
        OnItemUsed?.Invoke(itemInUse,true);
        return true;
    }
    private void HealStatusEffect(Item itemInUse,Pokemon selectedPartyPokemon,StatusEffect curableStatus = StatusEffect.None)
    {
        if (curableStatus == StatusEffect.None)
        {
            var statusInfo = itemInUse.GetModule<StatusHealInfoModule>();
            curableStatus = statusInfo.statusEffect;
        }
        if (!IsValidStatusHeal(itemInUse,curableStatus,selectedPartyPokemon))
        {
             OnItemUsed?.Invoke(itemInUse,false);
             
             return;
        }
        StartCoroutine(CompleteItemUsage(2f,itemInUse));
        _pokemonPartyHandler.RefreshMemberCards();
        _dialogueHandler.EndDialogue(1f);
    }
    private void RestoreHealth(Item itemInUse,Pokemon selectedPartyPokemon)
    {
        var healEffect = itemInUse.GetDynamicModule<ItemEffectInfo>().effectValue;
        if (selectedPartyPokemon.hp <= 0)
        {
            _dialogueHandler.DisplayDetails( "Pokemon has already fainted");
            
            OnItemUsed?.Invoke(itemInUse,false);
            return;
        } 
        if(selectedPartyPokemon.hp>=selectedPartyPokemon.maxHp)
        {
            _dialogueHandler.DisplayDetails("Pokemon health already is full");
            OnItemUsed?.Invoke(itemInUse,false);
            return;
        }
        OnItemUsed?.Invoke(itemInUse,true);
        _moveUsageHandler.HealthGainDisplay(healEffect,affectedPokemon:selectedPartyPokemon);
        _dialogueHandler.DisplayDetails(selectedPartyPokemon.pokemonDisplayName+" gained "+healEffect+" health points");
        StartCoroutine(CompleteItemUsage(2f,itemInUse));
    }
    private void CompleteItemUsage(Item itemInUse)//only call for items used outside of battle
    {
        if (_battleHandler.battleInProgress)
        {
            _battleHandler.SetPlayerTurnUsage(PlayerTurnUsage.UseItem);
        }
        DepleteItem(itemInUse);
        
     }
    private IEnumerator CompleteItemUsage(float skipDelay,Item itemInUse)
    {
        CompleteItemUsage(itemInUse);
        yield return new WaitForSecondsRealtime(skipDelay);
        if(itemInUse.forPartyUse) _inputStateHandler.ResetGroupUi(InputStateGroup.PokemonParty);
        SkipTurn();
    }
    private void SkipTurn()
    {
        if (!_battleHandler.battleInProgress) return;
        _turnBasedCombatHandler.NextTurn();
    }

    void DepleteItem(Item itemInUse)
    {
        _playerBagHandler.DepleteItem(itemInUse);
    }
}

public enum ItemType
{
    Special,Repel,HealHp,Status,PowerPointModifier,Herb,Revive,MaxRevive,Vitamin,
    Berry,Pokeball,EvolutionStone,RareCandy,XItem,GainMoney,Overworld,LearnableMove,HeldItem
}
