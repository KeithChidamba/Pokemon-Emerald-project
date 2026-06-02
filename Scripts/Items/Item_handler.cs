using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;

public class Item_handler : MonoBehaviour,IInjectable
{
    private Pokemon _selectedPartyPokemon;
    public bool usingItem;
    public Item itemInUse;
    private Battle_Participant _currentParticipant;
    public event Action<bool> OnItemUsageSuccessful;
    
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
    private PlayerCollisionHandler _playerCollisionHandler;
    
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
        _playerCollisionHandler = container.Resolve<PlayerCollisionHandler>();
        gameObject.SetActive(true);
    }

    public void OnInject()
    {
        
    }
    public void UseItem(Item item,[CanBeNull] Pokemon selectedPokemon)
    {
        if (_battleHandler.battleInProgress)
        {
            _currentParticipant = _battleHandler.GetCurrentParticipant();
            _inputStateHandler.ResetGroupUi(InputStateGroup.Bag);
        }
        
        _selectedPartyPokemon = selectedPokemon;
        itemInUse = item;

        if (itemInUse.itemType == ItemType.Special)
        {
            if (_overworldActions.IsEquipped(item:itemInUse))
            {
                _overworldActions.UnequipItem(itemInUse);
                usingItem = false;
                return;
            }
            _overworldActions.EquipItem(itemInUse);
            return;
        }

        switch (item.itemType)
        {
            case ItemType.LearnableMove: StartCoroutine(_pokemonOperationsHandler.LearnTmOrHm(itemInUse.additionalInfoModule,selectedPokemon)); break;
            
            case ItemType.Overworld : UseOverworldItem(); break;
            
            case ItemType.PowerPointModifier: ChangePowerpoints(); break;
            
            case ItemType.Herb: UseHerbs(); break;
            
            case ItemType.Berry: UseBerries(); break;
            
            case ItemType.HealHp: RestoreHealth(int.Parse(item.itemEffect)); break;
            
            case ItemType.Revive: RevivePokemon(itemInUse.itemType); break;
            
            case ItemType.Status: HealStatusEffect(); break;
            
            case ItemType.Vitamin: GetEVsFromItem(); break;
            
            case ItemType.Pokeball: UsePokeball(item); break;
            
            case ItemType.EvolutionStone: TriggerStoneEvolution(); break;
            
            case ItemType.RareCandy: StartCoroutine(LevelUpWithItem()); break;
            
            case ItemType.XItem: ItemBuffOrDebuff(); break;
            
            case ItemType.Repel: UseRepel(int.Parse(item.itemEffect)); break;
        }
    }

    private void UseRepel(int numSteps)
    {
        _dialogueHandler.DisplayDetails("Repel has been activated");
        _playerCollisionHandler.ActivateRepel(numSteps);
        CompleteItemUsage();
    }
    
    private void UseHerbs()
    {
        OnItemUsageSuccessful += ChangeFriendship;
        var herbInfo = itemInUse.GetModule<HerbInfoModule>(); 
        var usageIndex = herbInfo.GetHerbUsage(itemInUse);
        var herbUsages = new List<Action>
        {
            () => RestoreHealth(int.Parse(itemInUse.itemEffect)),
            () => HealStatusEffect(herbInfo.statusEffect),
            () => RevivePokemon(herbInfo.itemType)
        };
        herbUsages[usageIndex].Invoke();
    }

    private void UseBerries()
    {
        var berryInfo = itemInUse.GetModule<BerryInfoModule>();
        var usageIndex = berryInfo.GetBerryUsage();
        var berryUsages = new List<Action> 
        {
            GetFriendshipFromBerry,
            () => RestoreHealth(int.Parse(itemInUse.itemEffect)),
            () => HealStatusEffect(berryInfo.statusEffect),
            ChangePowerpoints,
            CureConfusion
        };
        berryUsages[usageIndex].Invoke();
    }
    private void UseOverworldItem()
    {
        if (itemInUse.itemName == "Escape Rope")
        {
            if (_areaHandler.currentArea.data.escapable)
            {
                OnItemUsageSuccessful?.Invoke(true);
                CompleteItemUsage();
                _areaHandler.EscapeArea();
                _inputStateHandler.ResetRelevantUi(new[] {InputStateName.PlayerMenu
                        ,InputStateName.PlayerBagNavigation});
            }
            else
            {
                OnItemUsageSuccessful?.Invoke(false);
                _dialogueHandler.DisplayDetails("Can't use that here!");
                ResetItemUsage();
            }
        }
    }
    private void ChangeFriendship(bool itemUsed)
    {
        OnItemUsageSuccessful -= ChangeFriendship;
        var herbInfo = itemInUse.GetModule<HerbInfoModule>();
        var friendshipLoss = herbInfo.herbType switch
        {
            Herb.EnergyPowder => -5,
            Herb.EnergyRoot => -10,
            Herb.HealPowder => -5,
            Herb.RevivalHerb => -15,
            _ => 0
        };
        if(itemUsed && friendshipLoss!=0)
            _selectedPartyPokemon.ChangeFriendshipLevel(friendshipLoss);
    }
    IEnumerator LevelUpWithItem()
    {
        if (_selectedPartyPokemon.currentLevel == 100)
        {
            OnItemUsageSuccessful?.Invoke(false);
            _dialogueHandler.DisplayDetails(_selectedPartyPokemon.pokemonName+" is already max level!");
            ResetItemUsage();
        }
        else
        {
            OnItemUsageSuccessful?.Invoke(true);
            var exp = PokemonOperations.CalculateExpForNextLevel(_selectedPartyPokemon.currentLevel, _selectedPartyPokemon.expGroup)+1;
            _dialogueHandler.DisplayDetails(_selectedPartyPokemon.pokemonName+" leveled up!");
            yield return new WaitForSecondsRealtime(1f);
            _selectedPartyPokemon.ReceiveExperience(exp-_selectedPartyPokemon.currentExpAmount);
            StartCoroutine(CompleteItemUsage(0));
        }
    }

    void TriggerStoneEvolution()
    { 
       var stoneInfo =itemInUse.GetModule<EvolutionStoneInfoModule>();
       if (_selectedPartyPokemon.evolutionStone == stoneInfo.stoneName)
       {
           OnItemUsageSuccessful?.Invoke(true);
            _selectedPartyPokemon.CheckEvolutionRequirements(0);
            CompleteItemUsage();
       }
       else
       {
           OnItemUsageSuccessful?.Invoke(false);
            _dialogueHandler.DisplayDetails("Cant use that on "+_selectedPartyPokemon.pokemonName);
            ResetItemUsage();
       }
    }

    private void GetFriendshipFromBerry()
    {
        var statToDecrease = itemInUse.GetModule<StatInfoModule>();
        if(_selectedPartyPokemon.friendshipLevel>254)
        {
            OnItemUsageSuccessful?.Invoke(false);
            _dialogueHandler.DisplayDetails(_selectedPartyPokemon.pokemonName+"'s friendship is already maxed out");
            ResetItemUsage();
        }
        else
        {
            OnItemUsageSuccessful?.Invoke(true);
            _dialogueHandler.DisplayDetails(_selectedPartyPokemon.pokemonName+"'s friendship was increased");
            ref float evRef = ref PokemonOperations.GetEvStatRef(statToDecrease.statName, _selectedPartyPokemon);
            if (evRef > 100) evRef = 100;
            else PokemonOperations.CalculateEvForStat(statToDecrease.statName, -10, _selectedPartyPokemon);
            _selectedPartyPokemon.DetermineFriendshipLevelChange(true,FriendshipModifier.Berry);
            CompleteItemUsage();
        }
    }
    private void GetEVsFromItem() 
    {
        var statToIncrease = itemInUse.GetModule<StatInfoModule>();
        PokemonOperations.OnEvChange += CheckEvChange;
        PokemonOperations.CalculateEvForStat(statToIncrease.statName, 10, _selectedPartyPokemon);
    }

    private void CheckEvChange(bool hasChanged)
    {
        PokemonOperations.OnEvChange -= CheckEvChange;
        var statToIncrease = itemInUse.GetModule<StatInfoModule>();
        var message = _selectedPartyPokemon.pokemonName + "'s " + NameDB.GetStatName(statToIncrease.statName);
        
        message += (hasChanged)? " was increased" : " can't get any higher";

        if (hasChanged)
        {
            OnItemUsageSuccessful?.Invoke(true);
            _selectedPartyPokemon.DetermineFriendshipLevelChange(false,
                FriendshipModifier.Vitamin);
            DepleteItem();
        }
        OnItemUsageSuccessful?.Invoke(false);
        _dialogueHandler.DisplayDetails(message);
        ResetItemUsage();
    }
    private void ChangePowerpoints()
    {
        _pokemonDetailsHandler.changingMoveData = true;
        var modifierInfo = itemInUse.GetModule<PowerpointModifeir>();
        
        if (modifierInfo.modiferType == ModiferType.RestorePp)
            _pokemonDetailsHandler.OnMoveSelected += RestorePowerpoints;
        else if (modifierInfo.modiferType == ModiferType.MaximisePp)
            _pokemonDetailsHandler.OnMoveSelected += MaximisePowerpoints;
        else if (modifierInfo.modiferType == ModiferType.IncreasePp)
            _pokemonDetailsHandler.OnMoveSelected += IncreasePowerpoints;
        _gameUIHandler.ViewPartyPokemonDetails(_selectedPartyPokemon);
    }

    private void ItemBuffOrDebuff()
    {
        var statInfo = itemInUse.GetModule<StatInfoModule>();
        if (statInfo.statName == Stat.None)
        {//guard spec doesn't buff stat but remove stat reduction
            if (_currentParticipant.ProtectedFromStatChange(false))
            {
                OnItemUsageSuccessful?.Invoke(false);
                _dialogueHandler.DisplayDetails("Your pokemon are already protected");
                ResetItemUsage();
                return;
            }
            _moveUsageHandler.ApplyStatChangeImmunity(_currentParticipant,
                StatChangeability.ImmuneToDecrease,5);
            
            string pokemonProtected = _selectedPartyPokemon.pokemonName;
            
            if (_battleHandler.isDoubleBattle)
            {
                var partner = _battleHandler.battleParticipants[_currentParticipant.GetPartnerIndex()];
                if(partner.isActive)
                {
                    _moveUsageHandler.ApplyStatChangeImmunity(partner,
                        StatChangeability.ImmuneToDecrease, 5);
                    pokemonProtected = _selectedPartyPokemon.pokemonName + " and " + partner.pokemon.pokemonName;
                }
            }
            OnItemUsageSuccessful?.Invoke(false);
            _dialogueHandler.DisplayBattleInfo("A veil of light covers "+pokemonProtected);
            StartCoroutine(CompleteItemUsage(0));
            return;
        }
       
        var buff = BattleOperations.SearchForBuffOrDebuff(_selectedPartyPokemon, statInfo.statName);
        if (buff is { isAtLimit: true })
        {
            _dialogueHandler.DisplayBattleInfo($"{_selectedPartyPokemon.pokemonName}'s " +
                                                        $"{buff.statName} can't go any higher");
            ResetItemUsage();
            OnItemUsageSuccessful?.Invoke(false);
            return;
        }
        OnItemUsageSuccessful?.Invoke(true);
        var xBuffData = new BuffDebuffData(_currentParticipant, statInfo.statName, true, 1);
        _moveUsageHandler.ExecuteBuffOrDebuff(xBuffData);
        StartCoroutine(CompleteItemUsage(0));
    }
    private void RevivePokemon(ItemType itemType)
    {
        if (_selectedPartyPokemon.hp > 0)
        {
            _dialogueHandler.DisplayDetails(_selectedPartyPokemon.pokemonName+" has not fainted!"); 
            OnItemUsageSuccessful?.Invoke(false);
            ResetItemUsage();
            return;
        }
        OnItemUsageSuccessful?.Invoke(true);
        _selectedPartyPokemon.hp = itemType == ItemType.MaxRevive? 
            _selectedPartyPokemon.maxHp : math.trunc(_selectedPartyPokemon.maxHp*0.5f);
        _dialogueHandler.DisplayDetails(_selectedPartyPokemon.pokemonName+" has been revived!");
        StartCoroutine(CompleteItemUsage(2.2f));
    }

    private bool MoveAlterationCancelled(Action<int> eventToUnsubscribe, int moveIndex)
    {
        if (moveIndex > -1) return false; //-1 means it was canceled
        
        _pokemonDetailsHandler.OnMoveSelected -= eventToUnsubscribe;
        ResetItemUsage();
        OnItemUsageSuccessful?.Invoke(false);
        return true;
    }
    private void RestorePowerpoints(int moveIndex)
     {
         if(MoveAlterationCancelled(RestorePowerpoints,moveIndex))//user exited
         {
             _inputStateHandler.ResetGroupUi(InputStateGroup.PokemonDetails);
             return; 
         }
              
         var currentMove = _selectedPartyPokemon.moveSet[moveIndex];
         if (currentMove.powerpoints == currentMove.maxPowerpoints)
         {
             OnItemUsageSuccessful?.Invoke(false);
             _dialogueHandler.DisplayDetails( currentMove.moveName+" pp is already full");
             return;
         }
         _pokemonDetailsHandler.OnMoveSelected -= RestorePowerpoints;
         var modifierInfo = itemInUse.GetModule<PowerpointModifeir>();
         var pointsToAdd = 0;
         
         if (modifierInfo.itemType == ModiferItemType.Ether)
             pointsToAdd = 10;
         
         if (modifierInfo.itemType == ModiferItemType.MaxEther)
             pointsToAdd = currentMove.maxPowerpoints;
         
         var sumPoints = currentMove.powerpoints + pointsToAdd;
         
         currentMove.powerpoints = (sumPoints > currentMove.maxPowerpoints) ? currentMove.maxPowerpoints : sumPoints;
         
         OnItemUsageSuccessful?.Invoke(true);
         _dialogueHandler.DisplayDetails( currentMove.moveName+" pp was restored!");
         StartCoroutine(CompleteItemUsage(2.2f));
         _inputStateHandler.ResetGroupUi(InputStateGroup.PokemonDetails);
     }
    private void IncreasePowerpoints(int moveIndex)
    {
        if(MoveAlterationCancelled(IncreasePowerpoints,moveIndex))
        {
            _inputStateHandler.ResetGroupUi(InputStateGroup.PokemonDetails);
            return; 
        }
        
        var currentMove = _selectedPartyPokemon.moveSet[moveIndex];
        double powerpointRatio = (float) currentMove.maxPowerpoints / currentMove.basePowerpoints;
        if (Math.Round(powerpointRatio, 1) >= 1.6)
        {
            OnItemUsageSuccessful?.Invoke(false);
            _dialogueHandler.DisplayDetails( currentMove.moveName+" pp is already maxed out");
            return;
        }
        _pokemonDetailsHandler.OnMoveSelected -= IncreasePowerpoints;
        currentMove.maxPowerpoints += (int)math.floor(0.2*currentMove.basePowerpoints);
        _dialogueHandler.DisplayDetails( currentMove.moveName+"'s pp was increased!");
        
        OnItemUsageSuccessful?.Invoke(true);
        StartCoroutine(CompleteItemUsage(2.2f));
        _inputStateHandler.ResetGroupUi(InputStateGroup.PokemonDetails);
    }

    private void MaximisePowerpoints(int moveIndex)
    {
        if (MoveAlterationCancelled(MaximisePowerpoints, moveIndex))         
        {
            _inputStateHandler.ResetGroupUi(InputStateGroup.PokemonDetails);
            return; 
        }

        var currentMove = _selectedPartyPokemon.moveSet[moveIndex];
        if (currentMove.maxPowerpoints >= (currentMove.basePowerpoints * 1.6))
        {
            OnItemUsageSuccessful?.Invoke(false);
            _dialogueHandler.DisplayDetails(currentMove.moveName + " pp is already maxed out");
            return;
        }

        _pokemonDetailsHandler.OnMoveSelected -= MaximisePowerpoints;
        currentMove.maxPowerpoints = (int)math.floor(currentMove.basePowerpoints * 1.6);

        OnItemUsageSuccessful?.Invoke(true);
        _dialogueHandler.DisplayDetails(currentMove.moveName + "'s pp was maxed out!");
        StartCoroutine(CompleteItemUsage(2.2f));
        _inputStateHandler.ResetGroupUi(InputStateGroup.PokemonDetails);
}
    private void UsePokeball(Item pokeball)
    {
        if (!CanUsePokeball()) 
        {
            OnItemUsageSuccessful?.Invoke(false);
            ResetItemUsage();
            return;
        }
        DepleteItem();

        _pokemonOperationsHandler.OnPokeballUsed += PokemonCaughtCheck;
        StartCoroutine(_pokemonOperationsHandler.TryToCatchPokemon(pokeball));
        
        void PokemonCaughtCheck(Pokemon pokemon,bool isCaught)
        {
            _pokemonOperationsHandler.OnPokeballUsed -= PokemonCaughtCheck;
            if (!isCaught)
            {
                SkipTurn();
            }
            OnItemUsageSuccessful?.Invoke(true);
            ResetItemUsage();
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


    

    private void CureConfusion()
    {
        if (!_battleHandler.battleInProgress)
        {
            OnItemUsageSuccessful?.Invoke(false);
            _dialogueHandler.DisplayDetails("cant use that outside battle!");
            return;
        }
        if(!_currentParticipant.isConfused)
            _dialogueHandler.DisplayDetails("Pokemon is already healthy");
        else
            _currentParticipant.isConfused = false;
        OnItemUsageSuccessful?.Invoke(true);
    }
    private bool IsValidStatusHeal(StatusEffect curableStatus)
    {
        if (_selectedPartyPokemon.statusEffect == StatusEffect.None)
        {
            if (_battleHandler.battleInProgress)
            {
                if (!_currentParticipant.isConfused)
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
            _selectedPartyPokemon.statusEffect == StatusEffect.BadlyPoison)
        {
            curableStatus = StatusEffect.BadlyPoison;
        }
        
        bool isValidHeal = _selectedPartyPokemon.statusEffect == curableStatus
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
            _currentParticipant.statusHandler.RemoveStatusEffect(healAll);
            _battleHandler.RefreshStatusEffectUI();
        }
        else
            _selectedPartyPokemon.statusEffect = StatusEffect.None;
        
        _dialogueHandler.DisplayDetails(_selectedPartyPokemon.pokemonName+" has been healed");
        OnItemUsageSuccessful?.Invoke(true);
        return true;
    }
    private void HealStatusEffect(StatusEffect curableStatus = StatusEffect.None)
    {
        if (curableStatus == StatusEffect.None)
        {
            var statusInfo = itemInUse.GetModule<StatusHealInfoModule>();
            curableStatus = statusInfo.statusEffect;
        }
        if (!IsValidStatusHeal(curableStatus))
        {
             OnItemUsageSuccessful?.Invoke(false);
             ResetItemUsage();
             return;
        }
        StartCoroutine(CompleteItemUsage(2f));
        _pokemonPartyHandler.RefreshMemberCards();
        _dialogueHandler.EndDialogue(1f);
    }
    private void RestoreHealth(int healEffect)
    {
        if (_selectedPartyPokemon.hp <= 0)
        {
            _dialogueHandler.DisplayDetails( "Pokemon has already fainted");
            ResetItemUsage();
            OnItemUsageSuccessful?.Invoke(false);
            return;
        } 
        if(_selectedPartyPokemon.hp>=_selectedPartyPokemon.maxHp)
        {
            _dialogueHandler.DisplayDetails("Pokemon health already is full");
            ResetItemUsage();
            OnItemUsageSuccessful?.Invoke(false);
            return;
        }
        OnItemUsageSuccessful?.Invoke(true);
        _moveUsageHandler.HealthGainDisplay(healEffect,affectedPokemon:_selectedPartyPokemon);
        _dialogueHandler.DisplayDetails(_selectedPartyPokemon.pokemonName+" gained "+healEffect+" health points");
        StartCoroutine(CompleteItemUsage(2f));
    }
    private void CompleteItemUsage()//only call for items used outside of battle
    {
        _battleHandler.usedTurnForItem = _battleHandler.battleInProgress;
        DepleteItem();
        ResetItemUsage();
     }
    private IEnumerator CompleteItemUsage(float skipDelay)
    {
        CompleteItemUsage();
        yield return new WaitForSecondsRealtime(skipDelay);
        if(itemInUse.forPartyUse) _inputStateHandler.ResetGroupUi(InputStateGroup.PokemonParty);
        SkipTurn();
    }
    private void SkipTurn()
    {
        if (!_battleHandler.battleInProgress) return;
        _turnBasedCombatHandler.NextTurn();
    }

    void DepleteItem()
    {
        _playerBagHandler.DepleteItem(itemInUse);
    }
    void ResetItemUsage()
    {
        usingItem = false;
        _selectedPartyPokemon = null;
    }
}

public enum ItemType
{
    Special,GainExp,HealHp,Status,PowerPointModifier,Herb,Revive,MaxRevive,Vitamin,
    Berry,Pokeball,EvolutionStone,RareCandy,XItem,GainMoney,Overworld,LearnableMove,None,
    Repel
}
