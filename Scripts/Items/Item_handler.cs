using System;
using System.Collections;
using System.Collections.Generic;
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
    public static Item_handler Instance;
    private event Action<bool> OnItemUsageSuccessful;

    public enum ItemType
    {
        Special,GainExp,HealHp,Status,Ether,Herb,Revive,StatIncrease,FriendshipIncrease,Pokeball
        ,EvolutionStone,RareCandy,XItem,GainMoney,Overworld
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
        if(Options_manager.Instance.playerInBattle)
            InputStateHandler.Instance.ResetGroupUi(InputStateHandler.StateGroup.Bag);
        
        _selectedPartyPokemon = selectedPokemon;
        _itemInUse = item;
        if (_itemInUse.itemType == ItemType.Overworld)
        {
            UseOverworldItem();
            return;
        }
        if (_itemInUse.itemType == ItemType.Special)
        {
            if (overworld_actions.Instance.IsEquipped(_itemInUse.itemName))
            {
                Dialogue_handler.Instance.DisplayDetails("Item is already equipped");
                return;
            }
            overworld_actions.Instance.EquipItem(_itemInUse);
            return;
        }
        if (item.itemEffect.ToLower() == "pp")
            ChangePowerpoints();
        else
        {
            switch (item.itemType)
            {
                case ItemType.Herb: UseHerbs(item.itemName); break;
                
                case ItemType.HealHp: RestoreHealth(int.Parse(item.itemEffect)); break;
                
                case ItemType.Revive: RevivePokemon(item.itemEffect.ToLower()); break;
                
                case ItemType.Status: HealStatusEffect(item.itemEffect.ToLower()); break;
                
                case ItemType.StatIncrease: GetEVsFromItem(_itemInUse.itemEffect); break;
                
                case ItemType.FriendshipIncrease: GetFriendshipFromItem(_itemInUse.itemEffect); break;
                
                case ItemType.Pokeball: UsePokeball(item); break;
                
                case ItemType.EvolutionStone: TriggerStoneEvolution(item.itemName.ToLower()); break;
                
                case ItemType.RareCandy: StartCoroutine(LevelUpWithItem()); break;
                
                case ItemType.XItem: ItemBuffOrDebuff(item.itemEffect); break;
            }
        }
        
    }
    private void UseHerbs(string herbType)
    {
        OnItemUsageSuccessful += ChangeFriendship;
        switch (herbType)
        {
            case "Energy Powder":
            case "Energy Root":
                RestoreHealth(int.Parse(_itemInUse.itemEffect));
                break;
            case "Heal Powder":
                HealStatusEffect("full heal");
                break;
            case "Revival Herb":
                RevivePokemon("max revive");
                break;
        }
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
        
        var friendshipLoss = _itemInUse.itemName switch
        {
            "Energy Powder" => -5,
            "Energy Root" => -10,
            "Heal Powder" => -5,
            "Revival Herb" => -15,
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

    void TriggerStoneEvolution(string evolutionStoneName)
    {
        var stone = (NameDB.EvolutionStone)Enum.Parse(typeof(NameDB.EvolutionStone),evolutionStoneName.Replace(" ", ""));
        if (_selectedPartyPokemon.evolutionStone == stone)
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

    private void GetFriendshipFromItem(string statToDecrease)
    {
        if(_selectedPartyPokemon.friendshipLevel>254)
        {
            Dialogue_handler.Instance.DisplayDetails(_selectedPartyPokemon.pokemonName+"'s friendship is already maxed out", 1f);
            ResetItemUsage();
        }
        else
        {
            var evStat = (PokemonOperations.Stat)Enum.Parse(typeof(PokemonOperations.Stat),statToDecrease.Replace(" ", ""));
            ref float evRef = ref PokemonOperations.GetEvStatRef(evStat, _selectedPartyPokemon);
            if (evRef > 100) evRef = 100;
            else PokemonOperations.CalculateEvForStat(evStat, -10, _selectedPartyPokemon);
            _selectedPartyPokemon.ChangeFriendshipLevel(10);
            CompleteItemUsage();
        }
    }
    private void GetEVsFromItem(string evStatName) 
    {
        PokemonOperations.OnEvChange += CheckEvChange;
        var evStat = (PokemonOperations.Stat)Enum.Parse(typeof(PokemonOperations.Stat),evStatName.Replace(" ", ""));
        PokemonOperations.CalculateEvForStat(evStat, 10, _selectedPartyPokemon);
    }

    private void CheckEvChange(bool hasChanged)
    {
        PokemonOperations.OnEvChange -= CheckEvChange;
        var message = _selectedPartyPokemon.pokemonName + "'s " + _itemInUse.itemEffect;
        
        message += (hasChanged)? " was increased" : " can't get any higher";

        if (hasChanged)
        {//CHANGE THIS TO WORK WITH BERRIES ONCE BERRIES ARE DONE
            _selectedPartyPokemon.DetermineFriendshipLevelChange(
                true,PokemonOperations.FriendshipModifier.Vitamin);
            DepleteItem();
        }
        Dialogue_handler.Instance.DisplayDetails(message,  1f);
        ResetItemUsage();
    }
    private void ChangePowerpoints()
    {
        Pokemon_Details.Instance.changingMoveData = true;
        if (_itemInUse.itemType == ItemType.Ether)
            Pokemon_Details.Instance.OnMoveSelected += RestorePowerpoints;
        else if (_itemInUse.itemName.ToLower() == "pp max")
            Pokemon_Details.Instance.OnMoveSelected += MaximisePowerpoints;
        else if (_itemInUse.itemName.ToLower() == "pp up")
            Pokemon_Details.Instance.OnMoveSelected += IncreasePowerpoints;
        Game_ui_manager.Instance.ViewPartyPokemonDetails(_selectedPartyPokemon);
    }

    private void ItemBuffOrDebuff(string statName)
    {
        var currentTurnIndex = Turn_Based_Combat.Instance.currentTurnIndex;
        var currentParticipant = Battle_handler.Instance.battleParticipants[currentTurnIndex];
        _selectedPartyPokemon = currentParticipant.pokemon;
        if (statName == "Stat Reduction")//Guard Spec applies all user's participant
        {
            if (_selectedPartyPokemon.immuneToStatReduction)
            {
                Dialogue_handler.Instance.DisplayDetails("Your pokemon are already protected");
                ResetItemUsage();
                return;
            }
            
            Move_handler.Instance.ApplyStatDropImmunity(currentParticipant,5);
            var pokemonProtected = _selectedPartyPokemon.pokemonName;
            
            if (Battle_handler.Instance.isDoubleBattle)
            {
                var partner = Battle_handler.Instance.battleParticipants[currentParticipant.GetPartnerIndex()];
                if(partner.isActive)
                {
                    Move_handler.Instance.ApplyStatDropImmunity(partner, 5);
                    pokemonProtected = _selectedPartyPokemon.pokemonName + " and " + partner.pokemon.pokemonName;
                }
            }
            
            Dialogue_handler.Instance.DisplayBattleInfo("A veil of light covers "+pokemonProtected);
            StartCoroutine(CompleteItemUsage(0));
            return;
        }
        var stat = (PokemonOperations.Stat)Enum.Parse(typeof(PokemonOperations.Stat),statName.Replace(" ", ""));
        var buff = BattleOperations.SearchForBuffOrDebuff(_selectedPartyPokemon, stat);
        if (buff is { isAtLimit: true })
        {
            Dialogue_handler.Instance.DisplayBattleInfo($"{_selectedPartyPokemon.pokemonName}'s {statName} can't go any higher");
            ResetItemUsage();
            return;
        }
        
        var xBuffData = new BuffDebuffData(currentParticipant, stat, true, 1);
        Move_handler.Instance.SelectRelevantBuffOrDebuff(xBuffData);
        StartCoroutine(CompleteItemUsage(0));
    }
    private void RevivePokemon(string reviveType)
    {
        if (_selectedPartyPokemon.hp > 0)
        {
            Dialogue_handler.Instance.DisplayDetails(_selectedPartyPokemon.pokemonName+" has not fainted!", 1f); 
            OnItemUsageSuccessful?.Invoke(false);
            ResetItemUsage();
            return;
        }
        OnItemUsageSuccessful?.Invoke(true);
        _selectedPartyPokemon.hp = reviveType=="max revive"? _selectedPartyPokemon.maxHp : math.trunc(_selectedPartyPokemon.maxHp*0.5f);
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
         var pointsToAdd = 0;
         
         if (_itemInUse.itemName.ToLower() == "ether")
             pointsToAdd = 10;
         
         if (_itemInUse.itemName.ToLower() == "max ether")
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
            Pokemon_party.Instance.AddMember(wildPokemon);
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

    private bool IsValidStatusHeal(string curableStatus)
    {
        if (_selectedPartyPokemon.statusEffect == PokemonOperations.StatusEffect.None)
        {
            Dialogue_handler.Instance.DisplayDetails("Pokemon is already healthy");
            return false;
        }
        if (curableStatus == "full heal") 
        {
            _selectedPartyPokemon.statusEffect = PokemonOperations.StatusEffect.None;
            _selectedPartyPokemon.isConfused = false;
            Dialogue_handler.Instance.DisplayDetails(_selectedPartyPokemon.pokemonName+" has been healed");
        }
        else
        {
            if (_selectedPartyPokemon.statusEffect.ToString().ToLower() == curableStatus)
            {
                _selectedPartyPokemon.statusEffect = PokemonOperations.StatusEffect.None;
                if (curableStatus == "sleep" || curableStatus == "freeze" || curableStatus == "paralysis")
                    _selectedPartyPokemon.canAttack = true;
                Dialogue_handler.Instance.DisplayDetails("Pokemon has been healed");
                Battle_handler.Instance.RefreshParticipantUI();
            }
            else
            {
                Dialogue_handler.Instance.DisplayDetails("Incorrect heal item");
                return false;
            }
        }
        OnItemUsageSuccessful?.Invoke(true);
        return true;
    }
    private void HealStatusEffect(string curableStatus)
    {
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
