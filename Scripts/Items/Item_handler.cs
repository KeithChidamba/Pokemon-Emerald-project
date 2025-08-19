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
    public bool usingHeldItem = false;
    private Item _itemInUse;
    private Battle_Participant currentParticipant;
    public static Item_handler Instance;
    private event Action<bool> OnItemUsageSuccessful;

    public enum ItemType
    {
        Special,GainExp,HealHp,Status,PowerPointModifier,Herb,Revive,MaxRevive,Vitamin,
        Berry,Pokeball,EvolutionStone,RareCandy,XItem,GainMoney,Overworld,LearnableMove
    }
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
            InputStateHandler.Instance.ResetGroupUi(InputStateHandler.StateGroup.Bag);
        }
        
        _selectedPartyPokemon = selectedPokemon;
        _itemInUse = item;

        if (_itemInUse.itemType == ItemType.Special)
        {
            if (overworld_actions.Instance.IsEquipped(item:_itemInUse))
            {
                overworld_actions.Instance.UnequipItem(_itemInUse);
                usingItem = false;
                return;
            }
            overworld_actions.Instance.EquipItem(_itemInUse);
            return;
        }
        
        switch (item.itemType)
        {
            case ItemType.LearnableMove: PokemonOperations.LearnTmOrHm(_itemInUse.additionalItemInfo,selectedPokemon); break;
            
            case ItemType.Overworld : UseOverworldItem(); break;
            
            case ItemType.PowerPointModifier: ChangePowerpoints(); break;
            
            case ItemType.Herb: UseHerbs(); break;
            
            case ItemType.Berry: UseBerries(); break;
            
            case ItemType.HealHp: RestoreHealth(int.Parse(item.itemEffect)); break;
            
            case ItemType.Revive: RevivePokemon(_itemInUse.itemType); break;
            
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
        var herbInfo = _itemInUse.GetModule<HerbInfo>(); 
        var usageIndex = herbInfo.GetHerbUsage(_itemInUse);
        var herbUsages = new List<Action>
        {
            () => RestoreHealth(int.Parse(_itemInUse.itemEffect)),
            () => HealStatusEffect(herbInfo.statusEffect),
            () => RevivePokemon(herbInfo.itemType)
        };
        herbUsages[usageIndex].Invoke();
    }

    private void UseBerries()
    {
        var berryInfo = _itemInUse.GetModule<BerryInfo>();
        var usageIndex = berryInfo.GetBerryUsage();
        var berryUsages = new List<Action> 
        {
            GetFriendshipFromBerry,
            () => RestoreHealth(int.Parse(_itemInUse.itemEffect)),
            () => HealStatusEffect(berryInfo.statusEffect)
        };
        berryUsages[usageIndex].Invoke();
    }
    private void UseOverworldItem()
    {
        if (_itemInUse.itemName == "Escape Rope")
        {
            if (Area_manager.Instance.currentArea.escapable)
            {
                CompleteItemUsage();
                Area_manager.Instance.EscapeArea();
                InputStateHandler.Instance.ResetRelevantUi(new[] {InputStateHandler.StateName.PlayerMenu
                        ,InputStateHandler.StateName.PlayerBagItemUsage,InputStateHandler.StateName.PlayerBagNavigation});
            }
            else
            {
                Dialogue_handler.Instance.DisplayDetails("Can't use that here!", 2f);
                ResetItemUsage();
            }
        }
    }
    private void ChangeFriendship(bool itemUsed)
    {
        OnItemUsageSuccessful -= ChangeFriendship;
        var herbInfo = _itemInUse.GetModule<HerbInfo>();
        var friendshipLoss = herbInfo.herbType switch
        {
            HerbInfo.Herb.EnergyPowder => -5,
            HerbInfo.Herb.EnergyRoot => -10,
            HerbInfo.Herb.HealPowder => -5,
            HerbInfo.Herb.RevivalHerb => -15,
            _ => 0
        };
        if(itemUsed && friendshipLoss!=0)
            _selectedPartyPokemon.ChangeFriendshipLevel(friendshipLoss);
    }
    IEnumerator LevelUpWithItem()
    {
        if (_selectedPartyPokemon.currentLevel == 100)
        {
            Dialogue_handler.Instance.DisplayDetails(_selectedPartyPokemon.pokemonName+" is already max level!", 2f);
            ResetItemUsage();
            yield return null;
        }
        else
        {
            var exp = PokemonOperations.CalculateExpForNextLevel(_selectedPartyPokemon.currentLevel, _selectedPartyPokemon.expGroup);
            Dialogue_handler.Instance.DisplayDetails(_selectedPartyPokemon.pokemonName+" leveled up!", 2f);
            yield return new WaitForSeconds(1f);
            _selectedPartyPokemon.ReceiveExperience((exp-_selectedPartyPokemon.currentExpAmount)+1);
            StartCoroutine(CompleteItemUsage(0));
        }
    }

    void TriggerStoneEvolution()
    { 
       var stoneInfo =_itemInUse.GetModule<EvolutionStoneInfo>();
       if (_selectedPartyPokemon.evolutionStone == stoneInfo.stoneName)
       {
            _selectedPartyPokemon.CheckEvolutionRequirements(0);
            CompleteItemUsage();
       }
       else
       {
            Dialogue_handler.Instance.DisplayDetails("Cant use that on "+_selectedPartyPokemon.pokemonName, 2f);
            ResetItemUsage();
       }
    }

    private void GetFriendshipFromBerry()
    {
        var statToDecrease = _itemInUse.GetModule<StatInfo>();
        if(_selectedPartyPokemon.friendshipLevel>254)
        {
            Dialogue_handler.Instance.DisplayDetails(_selectedPartyPokemon.pokemonName+"'s friendship is already maxed out", 1f);
            ResetItemUsage();
        }
        else
        {
            Dialogue_handler.Instance.DisplayDetails(_selectedPartyPokemon.pokemonName+"'s friendship was increased", 1f);
            ref float evRef = ref PokemonOperations.GetEvStatRef(statToDecrease.statName, _selectedPartyPokemon);
            if (evRef > 100) evRef = 100;
            else PokemonOperations.CalculateEvForStat(statToDecrease.statName, -10, _selectedPartyPokemon);
            _selectedPartyPokemon.DetermineFriendshipLevelChange(true,PokemonOperations.FriendshipModifier.Berry);
            CompleteItemUsage();
        }
    }
    private void GetEVsFromItem() 
    {
        var statToIncrease = _itemInUse.GetModule<StatInfo>();
        PokemonOperations.OnEvChange += CheckEvChange;
        PokemonOperations.CalculateEvForStat(statToIncrease.statName, 10, _selectedPartyPokemon);
    }

    private void CheckEvChange(bool hasChanged)
    {
        PokemonOperations.OnEvChange -= CheckEvChange;
        var statToIncrease = _itemInUse.GetModule<StatInfo>();
        var message = _selectedPartyPokemon.pokemonName + "'s " + NameDB.GetStatName(statToIncrease.statName);
        
        message += (hasChanged)? " was increased" : " can't get any higher";

        if (hasChanged)
        {
            _selectedPartyPokemon.DetermineFriendshipLevelChange(false,
                PokemonOperations.FriendshipModifier.Vitamin);
            DepleteItem();
        }
        Dialogue_handler.Instance.DisplayDetails(message,  1f);
        ResetItemUsage();
    }
    private void ChangePowerpoints()
    {
        Pokemon_Details.Instance.changingMoveData = true;
        var modifierInfo = _itemInUse.GetModule<PowerpointModifeir>();;
        
        if (modifierInfo.modiferType == PowerpointModifeir.ModiferType.RestorePp)
            Pokemon_Details.Instance.OnMoveSelected += RestorePowerpoints;
        else if (modifierInfo.modiferType == PowerpointModifeir.ModiferType.MaximisePp)
            Pokemon_Details.Instance.OnMoveSelected += MaximisePowerpoints;
        else if (modifierInfo.modiferType == PowerpointModifeir.ModiferType.IncreasePp)
            Pokemon_Details.Instance.OnMoveSelected += IncreasePowerpoints;
        Game_ui_manager.Instance.ViewPartyPokemonDetails(_selectedPartyPokemon);
    }

    private void ItemBuffOrDebuff()
    {
        var statInfo = _itemInUse.GetModule<StatInfo>();
        if (statInfo.statName == PokemonOperations.Stat.None)
        {//guard spec doesn't buff stat but remove stat reduction
            if (currentParticipant.ProtectedFromStatChange(false))
            {
                Dialogue_handler.Instance.DisplayDetails("Your pokemon are already protected");
                ResetItemUsage();
                return;
            }
            Move_handler.Instance.ApplyStatChangeImmunity(currentParticipant,
                StatChangeData.StatChangeability.ImmuneToDecrease,5);
            
            string pokemonProtected = _selectedPartyPokemon.pokemonName;
            
            if (Battle_handler.Instance.isDoubleBattle)
            {
                var partner = Battle_handler.Instance.battleParticipants[currentParticipant.GetPartnerIndex()];
                if(partner.isActive)
                {
                    Move_handler.Instance.ApplyStatChangeImmunity(partner,
                        StatChangeData.StatChangeability.ImmuneToDecrease, 5);
                    pokemonProtected = _selectedPartyPokemon.pokemonName + " and " + partner.pokemon.pokemonName;
                }
            }
            
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
            return;
        }
        
        var xBuffData = new BuffDebuffData(currentParticipant, statInfo.statName, true, 1);
        Move_handler.Instance.SelectRelevantBuffOrDebuff(xBuffData);
        StartCoroutine(CompleteItemUsage(0));
    }
    private void RevivePokemon(ItemType itemType)
    {
        if (_selectedPartyPokemon.hp > 0)
        {
            Dialogue_handler.Instance.DisplayDetails(_selectedPartyPokemon.pokemonName+" has not fainted!", 1f); 
            OnItemUsageSuccessful?.Invoke(false);
            ResetItemUsage();
            return;
        }
        OnItemUsageSuccessful?.Invoke(true);
        _selectedPartyPokemon.hp = itemType == ItemType.MaxRevive? 
            _selectedPartyPokemon.maxHp : math.trunc(_selectedPartyPokemon.maxHp*0.5f);
        Dialogue_handler.Instance.DisplayDetails(_selectedPartyPokemon.pokemonName+" has been revived!", 1f);
        StartCoroutine(CompleteItemUsage(2.2f));
    }

    private bool MoveAlterationCancelled(Action<int> eventToUnsubscribe, int moveIndex)
    {
        if (moveIndex > -1) return false; //-1 means it was cancelled
        
        Pokemon_Details.Instance.OnMoveSelected -= eventToUnsubscribe;
        ResetItemUsage();
        return true;
    }
    private void RestorePowerpoints(int moveIndex)
     {
         if(MoveAlterationCancelled(RestorePowerpoints,moveIndex))//user exited
         {
             InputStateHandler.Instance.ResetGroupUi(InputStateHandler.StateGroup.PokemonDetails);
             return; 
         }
              
         var currentMove = _selectedPartyPokemon.moveSet[moveIndex];
         if (currentMove.powerpoints == currentMove.maxPowerpoints)
         {
             Dialogue_handler.Instance.DisplayDetails( currentMove.moveName+" pp is already full", 1f);
             return;
         }
         Pokemon_Details.Instance.OnMoveSelected -= RestorePowerpoints;
         var modifierInfo = _itemInUse.GetModule<PowerpointModifeir>();
         var pointsToAdd = 0;
         
         if (modifierInfo.itemType == PowerpointModifeir.ModiferItemType.Ether)
             pointsToAdd = 10;
         
         if (modifierInfo.itemType == PowerpointModifeir.ModiferItemType.MaxEther)
             pointsToAdd = currentMove.maxPowerpoints;
         
         var sumPoints = currentMove.powerpoints + pointsToAdd;
         
         currentMove.powerpoints = (sumPoints > currentMove.maxPowerpoints) ? currentMove.maxPowerpoints : sumPoints;
 
         Dialogue_handler.Instance.DisplayDetails( currentMove.moveName+" pp was restored!", 1f);
         StartCoroutine(CompleteItemUsage(2.2f));
         InputStateHandler.Instance.ResetGroupUi(InputStateHandler.StateGroup.PokemonDetails);
     }
    private void IncreasePowerpoints(int moveIndex)
    {
        if(MoveAlterationCancelled(IncreasePowerpoints,moveIndex))
        {
            InputStateHandler.Instance.ResetGroupUi(InputStateHandler.StateGroup.PokemonDetails);
            return; 
        }
        
        var currentMove = _selectedPartyPokemon.moveSet[moveIndex];
        double powerpointRatio = (float) currentMove.maxPowerpoints / currentMove.basePowerpoints;
        if (Math.Round(powerpointRatio, 1) >= 1.6)
        {
            Dialogue_handler.Instance.DisplayDetails( currentMove.moveName+" pp is already maxed out", 1f);
            return;
        }
        Pokemon_Details.Instance.OnMoveSelected -= IncreasePowerpoints;
        currentMove.maxPowerpoints += (int)math.floor(0.2*currentMove.basePowerpoints);
        Dialogue_handler.Instance.DisplayDetails( currentMove.moveName+"'s pp was increased!", 1f);
        
        StartCoroutine(CompleteItemUsage(2.2f));
        InputStateHandler.Instance.ResetGroupUi(InputStateHandler.StateGroup.PokemonDetails);
    }

    private void MaximisePowerpoints(int moveIndex)
    {
        if (MoveAlterationCancelled(MaximisePowerpoints, moveIndex))         
        {
            InputStateHandler.Instance.ResetGroupUi(InputStateHandler.StateGroup.PokemonDetails);
            return; 
        }

        var currentMove = _selectedPartyPokemon.moveSet[moveIndex];
        if (currentMove.maxPowerpoints >= (currentMove.basePowerpoints * 1.6))
        {
            Dialogue_handler.Instance.DisplayDetails(currentMove.moveName + " pp is already maxed out",  1f);
            return;
        }

        Pokemon_Details.Instance.OnMoveSelected -= MaximisePowerpoints;
        currentMove.maxPowerpoints = (int)math.floor(currentMove.basePowerpoints * 1.6);

        Dialogue_handler.Instance.DisplayDetails(currentMove.moveName + "'s pp was maxed out!",  1f);
        StartCoroutine(CompleteItemUsage(2.2f));
        InputStateHandler.Instance.ResetGroupUi(InputStateHandler.StateGroup.PokemonDetails);
}
    private void UsePokeball(Item pokeball)
    {
        if (!CanUsePokeball()) 
        {
            ResetItemUsage();
            return;
        }
        DepleteItem();
        StartCoroutine(TryToCatchPokemon(pokeball));
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
            Dialogue_handler.Instance.DisplayDetails("Can't catch someone else's Pokemon!",  1f);
            return false;
        }

        if (pokemon_storage.Instance.MaxPokemonCapacity())
        {
            Dialogue_handler.Instance.DisplayDetails("Can no longer catch more Pokémon, free up space in PC!");
            return false;
        }

        return true;
    }
    
    IEnumerator TryToCatchPokemon(Item pokeball)
    {
        InputStateHandler.Instance.ResetGroupUi(InputStateHandler.StateGroup.Bag);
        var isCaught = false;
        var wildPokemon = Wild_pkm.Instance.participant.pokemon;//pokemon only caught in wild
        Dialogue_handler.Instance.DisplayBattleInfo("Trying to catch "+wildPokemon.pokemonName+" .....");
        yield return new WaitUntil(()=> !Dialogue_handler.Instance.messagesLoading);
        var ballRate = float.Parse(pokeball.itemEffect);
        var bracket1 = (3 * wildPokemon.maxHp - 2 * wildPokemon.hp) / (3 * wildPokemon.maxHp);
        var catchValue = math.trunc(bracket1 * wildPokemon.catchRate * ballRate * 
                                      BattleOperations.GetCatchRateBonusFromStatus(wildPokemon.statusEffect));
        
        if (BattleOperations.IsImmediateCatch(catchValue) 
            || BattleOperations.PassedPokeballShakeTest(catchValue))
            isCaught = true;
  
        if (isCaught)
        {
            Dialogue_handler.Instance.DisplayBattleInfo("Well done "+wildPokemon.pokemonName+" has been caught");
            var rawName = wildPokemon.pokemonName.Replace("Foe ", "");
            wildPokemon.pokemonName = rawName;
            wildPokemon.ChangeFriendshipLevel(70);
            wildPokemon.pokeballName = _itemInUse.itemName;
            Pokemon_party.Instance.AddMember(wildPokemon,_itemInUse.itemName,false);
            yield return new WaitUntil(()=> !Dialogue_handler.Instance.messagesLoading);
            Wild_pkm.Instance.participant.EndWildBattle();
        }else
        {
            Dialogue_handler.Instance.DisplayBattleInfo(wildPokemon.pokemonName+" escaped the pokeball");
            yield return new WaitUntil(()=> !Dialogue_handler.Instance.messagesLoading);
            SkipTurn();
        }
        ResetItemUsage();
    }

    private bool IsValidStatusHeal(PokemonOperations.StatusEffect curableStatus)
    {
        if (_selectedPartyPokemon.statusEffect == PokemonOperations.StatusEffect.None)
        {
            Dialogue_handler.Instance.DisplayDetails("Pokemon is already healthy");
            return false;
        }
        if (curableStatus == PokemonOperations.StatusEffect.Poison &&
            _selectedPartyPokemon.statusEffect == PokemonOperations.StatusEffect.BadlyPoison)
        {//antidote heals all poison
            curableStatus = PokemonOperations.StatusEffect.BadlyPoison;
        }
        
        if (curableStatus != PokemonOperations.StatusEffect.FullHeal &&
            _selectedPartyPokemon.statusEffect != curableStatus) 
        {
            Dialogue_handler.Instance.DisplayDetails("Incorrect heal item");
            return false;
        }
        
        if (curableStatus == PokemonOperations.StatusEffect.FullHeal) //full heal cures confusion
            if(Options_manager.Instance.playerInBattle) currentParticipant.isConfused = false;

        if (Options_manager.Instance.playerInBattle)
        {
            currentParticipant.statusHandler.RemoveStatusEffect();
            Battle_handler.Instance.RefreshStatusEffectUI();
        }
        else
            _selectedPartyPokemon.statusEffect = PokemonOperations.StatusEffect.None;
        
        Dialogue_handler.Instance.DisplayDetails(_selectedPartyPokemon.pokemonName+" has been healed");
        OnItemUsageSuccessful?.Invoke(true);
        return true;
    }
    private void HealStatusEffect(PokemonOperations.StatusEffect curableStatus = PokemonOperations.StatusEffect.None)
    {
        if (curableStatus == PokemonOperations.StatusEffect.None)
        {
            var statusInfo = _itemInUse.GetModule<StatusHealInfo>();
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
            Dialogue_handler.Instance.DisplayDetails( "Pokemon has already fainted",2f);
            ResetItemUsage();
            OnItemUsageSuccessful?.Invoke(false);
            return;
        } 
        if(_selectedPartyPokemon.hp>=_selectedPartyPokemon.maxHp)
        {
            Dialogue_handler.Instance.DisplayDetails("Pokemon health already is full",2f);
            ResetItemUsage();
            OnItemUsageSuccessful?.Invoke(false);
            return;
        }
        OnItemUsageSuccessful?.Invoke(true);
        if ((_selectedPartyPokemon.hp + healEffect) < _selectedPartyPokemon.maxHp)
        {
            _selectedPartyPokemon.hp += healEffect;
            Dialogue_handler.Instance.DisplayDetails(_selectedPartyPokemon.pokemonName+" gained "+healEffect+" health points",2f);
        }
        else if ((_selectedPartyPokemon.hp + healEffect) >= _selectedPartyPokemon.maxHp)
        {
            _selectedPartyPokemon.hp = _selectedPartyPokemon.maxHp;
            Dialogue_handler.Instance.DisplayDetails(_selectedPartyPokemon.pokemonName+" gained full health points",2f);
        }
        StartCoroutine(CompleteItemUsage(3f));
    }
    private void CompleteItemUsage()//only call for items used outside of battle
    {
        Battle_handler.Instance.usedTurnForItem = Options_manager.Instance.playerInBattle;
        if (usingHeldItem)
            DepleteHeldItem(); 
        else
            DepleteItem();
        ResetItemUsage();
     }
    private IEnumerator CompleteItemUsage(float skipDelay)
    {
        CompleteItemUsage();
        yield return new WaitForSeconds(skipDelay);
        if(_itemInUse.forPartyUse) InputStateHandler.Instance.ResetGroupUi(InputStateHandler.StateGroup.PokemonParty);
        SkipTurn();
    }
    private void SkipTurn()
    {
        if (!Options_manager.Instance.playerInBattle) return;
        Turn_Based_Combat.Instance.NextTurn();
    }
    void DepleteHeldItem()
    {
        usingHeldItem = false; 
        _itemInUse.quantity = (_itemInUse.isHeldItem)? 1 : _itemInUse.quantity-1; 
    }
    void DepleteItem()
    {
        _itemInUse.quantity--;
        Bag.Instance.CheckItemQuantity(_itemInUse);
    }
    void ResetItemUsage()
    {
        usingItem = false;
        _selectedPartyPokemon = null;
    }
}
