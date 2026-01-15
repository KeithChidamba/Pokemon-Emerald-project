using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

public class Item_handler : MonoBehaviour
{
    private Pokemon _selectedPartyPokemon;
    public bool usingItem = false;
    public Item itemInUse;
    private Battle_Participant currentParticipant;
    public static Item_handler Instance;
    public event Action<bool> OnItemUsageSuccessful;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    public void UseItem(Item item,[CanBeNull] Pokemon selectedPokemon)
    {
        if (Options_manager.Instance.playerInBattle)
        {
            currentParticipant = Battle_handler.Instance.GetCurrentParticipant();
            InputStateHandler.Instance.ResetGroupUi(InputStateGroup.Bag);
        }
        
        _selectedPartyPokemon = selectedPokemon;
        itemInUse = item;

        if (itemInUse.itemType == ItemType.Special)
        {
            if (overworld_actions.Instance.IsEquipped(item:itemInUse))
            {
                overworld_actions.Instance.UnequipItem(itemInUse);
                usingItem = false;
                return;
            }
            overworld_actions.Instance.EquipItem(itemInUse);
            return;
        }

        switch (item.itemType)
        {
            case ItemType.LearnableMove: StartCoroutine(PokemonOperations.LearnTmOrHm(itemInUse.additionalInfoModule,selectedPokemon)); break;
            
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
        }
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
            if (Area_manager.Instance.currentArea.areaData.escapable)
            {
                OnItemUsageSuccessful?.Invoke(true);
                CompleteItemUsage();
                Area_manager.Instance.EscapeArea();
                InputStateHandler.Instance.ResetRelevantUi(new[] {InputStateName.PlayerMenu
                        ,InputStateName.PlayerBagNavigation});
            }
            else
            {
                OnItemUsageSuccessful?.Invoke(false);
                Dialogue_handler.Instance.DisplayDetails("Can't use that here!");
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
            Dialogue_handler.Instance.DisplayDetails(_selectedPartyPokemon.pokemonName+" is already max level!");
            ResetItemUsage();
        }
        else
        {
            OnItemUsageSuccessful?.Invoke(true);
            var exp = PokemonOperations.CalculateExpForNextLevel(_selectedPartyPokemon.currentLevel, _selectedPartyPokemon.expGroup)+1;
            Dialogue_handler.Instance.DisplayDetails(_selectedPartyPokemon.pokemonName+" leveled up!");
            yield return new WaitForSeconds(1f);
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
            Dialogue_handler.Instance.DisplayDetails("Cant use that on "+_selectedPartyPokemon.pokemonName);
            ResetItemUsage();
       }
    }

    private void GetFriendshipFromBerry()
    {
        var statToDecrease = itemInUse.GetModule<StatInfoModule>();
        if(_selectedPartyPokemon.friendshipLevel>254)
        {
            OnItemUsageSuccessful?.Invoke(false);
            Dialogue_handler.Instance.DisplayDetails(_selectedPartyPokemon.pokemonName+"'s friendship is already maxed out");
            ResetItemUsage();
        }
        else
        {
            OnItemUsageSuccessful?.Invoke(true);
            Dialogue_handler.Instance.DisplayDetails(_selectedPartyPokemon.pokemonName+"'s friendship was increased");
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
        Dialogue_handler.Instance.DisplayDetails(message);
        ResetItemUsage();
    }
    private void ChangePowerpoints()
    {
        Pokemon_Details.Instance.changingMoveData = true;
        var modifierInfo = itemInUse.GetModule<PowerpointModifeir>();
        
        if (modifierInfo.modiferType == ModiferType.RestorePp)
            Pokemon_Details.Instance.OnMoveSelected += RestorePowerpoints;
        else if (modifierInfo.modiferType == ModiferType.MaximisePp)
            Pokemon_Details.Instance.OnMoveSelected += MaximisePowerpoints;
        else if (modifierInfo.modiferType == ModiferType.IncreasePp)
            Pokemon_Details.Instance.OnMoveSelected += IncreasePowerpoints;
        Game_ui_manager.Instance.ViewPartyPokemonDetails(_selectedPartyPokemon);
    }

    private void ItemBuffOrDebuff()
    {
        var statInfo = itemInUse.GetModule<StatInfoModule>();
        if (statInfo.statName == Stat.None)
        {//guard spec doesn't buff stat but remove stat reduction
            if (currentParticipant.ProtectedFromStatChange(false))
            {
                OnItemUsageSuccessful?.Invoke(false);
                Dialogue_handler.Instance.DisplayDetails("Your pokemon are already protected");
                ResetItemUsage();
                return;
            }
            Move_handler.Instance.ApplyStatChangeImmunity(currentParticipant,
                StatChangeability.ImmuneToDecrease,5);
            
            string pokemonProtected = _selectedPartyPokemon.pokemonName;
            
            if (Battle_handler.Instance.isDoubleBattle)
            {
                var partner = Battle_handler.Instance.battleParticipants[currentParticipant.GetPartnerIndex()];
                if(partner.isActive)
                {
                    Move_handler.Instance.ApplyStatChangeImmunity(partner,
                        StatChangeability.ImmuneToDecrease, 5);
                    pokemonProtected = _selectedPartyPokemon.pokemonName + " and " + partner.pokemon.pokemonName;
                }
            }
            OnItemUsageSuccessful?.Invoke(false);
            Dialogue_handler.Instance.DisplayBattleInfo("A veil of light covers "+pokemonProtected);
            StartCoroutine(CompleteItemUsage(0));
            return;
        }
       
        var buff = BattleOperations.SearchForBuffOrDebuff(_selectedPartyPokemon, statInfo.statName);
        if (buff is { isAtLimit: true })
        {
            Dialogue_handler.Instance.DisplayBattleInfo($"{_selectedPartyPokemon.pokemonName}'s " +
                                                        $"{buff.statName} can't go any higher");
            ResetItemUsage();
            OnItemUsageSuccessful?.Invoke(false);
            return;
        }
        OnItemUsageSuccessful?.Invoke(true);
        var xBuffData = new BuffDebuffData(currentParticipant, statInfo.statName, true, 1);
        Move_handler.Instance.SelectRelevantBuffOrDebuff(xBuffData);
        StartCoroutine(CompleteItemUsage(0));
    }
    private void RevivePokemon(ItemType itemType)
    {
        if (_selectedPartyPokemon.hp > 0)
        {
            Dialogue_handler.Instance.DisplayDetails(_selectedPartyPokemon.pokemonName+" has not fainted!"); 
            OnItemUsageSuccessful?.Invoke(false);
            ResetItemUsage();
            return;
        }
        OnItemUsageSuccessful?.Invoke(true);
        _selectedPartyPokemon.hp = itemType == ItemType.MaxRevive? 
            _selectedPartyPokemon.maxHp : math.trunc(_selectedPartyPokemon.maxHp*0.5f);
        Dialogue_handler.Instance.DisplayDetails(_selectedPartyPokemon.pokemonName+" has been revived!");
        StartCoroutine(CompleteItemUsage(2.2f));
    }

    private bool MoveAlterationCancelled(Action<int> eventToUnsubscribe, int moveIndex)
    {
        if (moveIndex > -1) return false; //-1 means it was canceled
        
        Pokemon_Details.Instance.OnMoveSelected -= eventToUnsubscribe;
        ResetItemUsage();
        OnItemUsageSuccessful?.Invoke(false);
        return true;
    }
    private void RestorePowerpoints(int moveIndex)
     {
         if(MoveAlterationCancelled(RestorePowerpoints,moveIndex))//user exited
         {
             InputStateHandler.Instance.ResetGroupUi(InputStateGroup.PokemonDetails);
             return; 
         }
              
         var currentMove = _selectedPartyPokemon.moveSet[moveIndex];
         if (currentMove.powerpoints == currentMove.maxPowerpoints)
         {
             OnItemUsageSuccessful?.Invoke(false);
             Dialogue_handler.Instance.DisplayDetails( currentMove.moveName+" pp is already full");
             return;
         }
         Pokemon_Details.Instance.OnMoveSelected -= RestorePowerpoints;
         var modifierInfo = itemInUse.GetModule<PowerpointModifeir>();
         var pointsToAdd = 0;
         
         if (modifierInfo.itemType == ModiferItemType.Ether)
             pointsToAdd = 10;
         
         if (modifierInfo.itemType == ModiferItemType.MaxEther)
             pointsToAdd = currentMove.maxPowerpoints;
         
         var sumPoints = currentMove.powerpoints + pointsToAdd;
         
         currentMove.powerpoints = (sumPoints > currentMove.maxPowerpoints) ? currentMove.maxPowerpoints : sumPoints;
         
         OnItemUsageSuccessful?.Invoke(true);
         Dialogue_handler.Instance.DisplayDetails( currentMove.moveName+" pp was restored!");
         StartCoroutine(CompleteItemUsage(2.2f));
         InputStateHandler.Instance.ResetGroupUi(InputStateGroup.PokemonDetails);
     }
    private void IncreasePowerpoints(int moveIndex)
    {
        if(MoveAlterationCancelled(IncreasePowerpoints,moveIndex))
        {
            InputStateHandler.Instance.ResetGroupUi(InputStateGroup.PokemonDetails);
            return; 
        }
        
        var currentMove = _selectedPartyPokemon.moveSet[moveIndex];
        double powerpointRatio = (float) currentMove.maxPowerpoints / currentMove.basePowerpoints;
        if (Math.Round(powerpointRatio, 1) >= 1.6)
        {
            OnItemUsageSuccessful?.Invoke(false);
            Dialogue_handler.Instance.DisplayDetails( currentMove.moveName+" pp is already maxed out");
            return;
        }
        Pokemon_Details.Instance.OnMoveSelected -= IncreasePowerpoints;
        currentMove.maxPowerpoints += (int)math.floor(0.2*currentMove.basePowerpoints);
        Dialogue_handler.Instance.DisplayDetails( currentMove.moveName+"'s pp was increased!");
        
        OnItemUsageSuccessful?.Invoke(true);
        StartCoroutine(CompleteItemUsage(2.2f));
        InputStateHandler.Instance.ResetGroupUi(InputStateGroup.PokemonDetails);
    }

    private void MaximisePowerpoints(int moveIndex)
    {
        if (MoveAlterationCancelled(MaximisePowerpoints, moveIndex))         
        {
            InputStateHandler.Instance.ResetGroupUi(InputStateGroup.PokemonDetails);
            return; 
        }

        var currentMove = _selectedPartyPokemon.moveSet[moveIndex];
        if (currentMove.maxPowerpoints >= (currentMove.basePowerpoints * 1.6))
        {
            OnItemUsageSuccessful?.Invoke(false);
            Dialogue_handler.Instance.DisplayDetails(currentMove.moveName + " pp is already maxed out");
            return;
        }

        Pokemon_Details.Instance.OnMoveSelected -= MaximisePowerpoints;
        currentMove.maxPowerpoints = (int)math.floor(currentMove.basePowerpoints * 1.6);

        OnItemUsageSuccessful?.Invoke(true);
        Dialogue_handler.Instance.DisplayDetails(currentMove.moveName + "'s pp was maxed out!");
        StartCoroutine(CompleteItemUsage(2.2f));
        InputStateHandler.Instance.ResetGroupUi(InputStateGroup.PokemonDetails);
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

        PokemonOperations.Instance.OnPokeballUsed += PokemonCaughtCheck;
        StartCoroutine(PokemonOperations.Instance.TryToCatchPokemon(pokeball));
    }

    private bool CanUsePokeball()
    {
        if (!Options_manager.Instance.playerInBattle)
        {
            Dialogue_handler.Instance.DisplayDetails("Can't use that right now!");
            return false;
        }

        if (Battle_handler.Instance.isTrainerBattle)
        {
            Dialogue_handler.Instance.DisplayDetails("Can't catch someone else's Pokemon!");
            return false;
        }

        if (pokemon_storage.Instance.MaxPokemonCapacity())
        {
            Dialogue_handler.Instance.DisplayDetails("Can no longer catch more Pokémon, free up space in PC!");
            return false;
        }

        return true;
    }

    private void PokemonCaughtCheck(Pokemon pokemon,bool isCaught)
    {
        PokemonOperations.Instance.OnPokeballUsed -= PokemonCaughtCheck;
        if (!isCaught)
        {
            SkipTurn();
        }
        OnItemUsageSuccessful?.Invoke(true);
        ResetItemUsage();
    }
    

    private void CureConfusion()
    {
        if (!Options_manager.Instance.playerInBattle)
        {
            OnItemUsageSuccessful?.Invoke(false);
            Dialogue_handler.Instance.DisplayDetails("cant use that outside battle!");
            return;
        }
        if(!currentParticipant.isConfused)
            Dialogue_handler.Instance.DisplayDetails("Pokemon is already healthy");
        else
            currentParticipant.isConfused = false;
        OnItemUsageSuccessful?.Invoke(true);
    }
    private bool IsValidStatusHeal(StatusEffect curableStatus)
    {
        if (_selectedPartyPokemon.statusEffect == StatusEffect.None)
        {
            if (Options_manager.Instance.playerInBattle)
            {
                if (!currentParticipant.isConfused)
                {
                    Dialogue_handler.Instance.DisplayDetails("Pokemon is already healthy");
                    return false;
                }
            }
            else
            {
                Dialogue_handler.Instance.DisplayDetails("Pokemon is already healthy");
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
            Dialogue_handler.Instance.DisplayDetails("Incorrect heal item");
            return false;
        }
        
        //healing
        if (Options_manager.Instance.playerInBattle)
        {
            var healAll = curableStatus == StatusEffect.FullHeal;
            currentParticipant.statusHandler.RemoveStatusEffect(healAll);
            Battle_handler.Instance.RefreshStatusEffectUI();
        }
        else
            _selectedPartyPokemon.statusEffect = StatusEffect.None;
        
        Dialogue_handler.Instance.DisplayDetails(_selectedPartyPokemon.pokemonName+" has been healed");
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
        StartCoroutine(CompleteItemUsage(3f));
        Pokemon_party.Instance.RefreshMemberCards();
        Dialogue_handler.Instance.EndDialogue(1f);
    }
    private void RestoreHealth(int healEffect)
    {
        if (_selectedPartyPokemon.hp <= 0)
        {
            Dialogue_handler.Instance.DisplayDetails( "Pokemon has already fainted");
            ResetItemUsage();
            OnItemUsageSuccessful?.Invoke(false);
            return;
        } 
        if(_selectedPartyPokemon.hp>=_selectedPartyPokemon.maxHp)
        {
            Dialogue_handler.Instance.DisplayDetails("Pokemon health already is full");
            ResetItemUsage();
            OnItemUsageSuccessful?.Invoke(false);
            return;
        }
        OnItemUsageSuccessful?.Invoke(true);
        Move_handler.Instance.HealthGainDisplay(healEffect,affectedPokemon:_selectedPartyPokemon);
        Dialogue_handler.Instance.DisplayDetails(_selectedPartyPokemon.pokemonName+" gained "+healEffect+" health points");
        StartCoroutine(CompleteItemUsage(3f));
    }
    private void CompleteItemUsage()//only call for items used outside of battle
    {
        Battle_handler.Instance.usedTurnForItem = Options_manager.Instance.playerInBattle;
            DepleteItem();
        ResetItemUsage();
     }
    private IEnumerator CompleteItemUsage(float skipDelay)
    {
        CompleteItemUsage();
        yield return new WaitForSeconds(skipDelay);
        if(itemInUse.forPartyUse) InputStateHandler.Instance.ResetGroupUi(InputStateGroup.PokemonParty);
        SkipTurn();
    }
    private void SkipTurn()
    {
        if (!Options_manager.Instance.playerInBattle) return;
        Turn_Based_Combat.Instance.NextTurn();
    }

    void DepleteItem()
    {
        itemInUse.quantity--;
        Bag.Instance.CheckItemQuantity(itemInUse);
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
    Berry,Pokeball,EvolutionStone,RareCandy,XItem,GainMoney,Overworld,LearnableMove
}
