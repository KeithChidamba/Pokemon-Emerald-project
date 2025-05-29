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
        _selectedPartyPokemon = selectedPokemon;
        _itemInUse = item;

        if (item.itemEffect.ToLower() == "pp")
        {
            ChangePowerpoints();
            return;
        }
        
        switch (item.itemType.ToLower())
        {
            case "heal hp":
                RestoreHealth(int.Parse(item.itemEffect));
                break;
            case "revive":
                RevivePokemon(item.itemEffect.ToLower());
                break;
            case "status":
                HealStatusEffect(item.itemEffect.ToLower());
                break;
            case "stat increase":
                GetEVsFromItem(_itemInUse.itemEffect);
                break;
            case "pokeball":
                UsePokeball(item);
                break;
            case "evolution stone":
                TriggerStoneEvolution(item.itemName.ToLower());
                break;
            case "rare candy":
                StartCoroutine(LevelUpWithItem());
                break;
            case "x item":
                ItemBuffOrDebuff(item.itemEffect);
                break;
        }
    }
    IEnumerator LevelUpWithItem()
    {
        Game_ui_manager.Instance.canExitParty = false;
        var exp = PokemonOperations.CalculateExpForNextLevel(_selectedPartyPokemon.currentLevel, _selectedPartyPokemon.expGroup);
        Dialogue_handler.Instance.DisplayInfo(_selectedPartyPokemon.pokemonName+" leveled up!", "Details",1f);
        yield return new WaitForSeconds(1f);
        _selectedPartyPokemon.ReceiveExperience((exp-_selectedPartyPokemon.currentExpAmount)+1);
        DepleteItem();
        ResetItemUsage();
    }

    void TriggerStoneEvolution(string evolutionStoneName)
    {
        if (_selectedPartyPokemon.evolutionStoneName.ToLower() == evolutionStoneName)
        {
            DepleteItem();
            ResetItemUsage();
            _selectedPartyPokemon.CheckEvolutionRequirements(0);
        } 
        else
            Dialogue_handler.Instance.DisplayInfo("Cant use that on "+_selectedPartyPokemon.pokemonName, "Details",1f);
    }

    private void GetEVsFromItem(string stat)
    {
        PokemonOperations.CalculateEvForStat(stat, 10, _selectedPartyPokemon);
        Dialogue_handler.Instance.DisplayInfo(_selectedPartyPokemon.pokemonName+"'s "+stat+" was increased", "Details",1f);
        DepleteItem();
        ResetItemUsage();
        Pokemon_Details.Instance.ExitDetails();
    }
    private void ChangePowerpoints()
    {
        Pokemon_Details.Instance.changingMoveData = true;
        if (_itemInUse.itemType.ToLower() == "ether")
            Pokemon_Details.Instance.OnMoveSelected += RestorePowerpoints;
        if (_itemInUse.itemName.ToLower() == "pp max")
            Pokemon_Details.Instance.OnMoveSelected += MaximisePowerpoints;
        if (_itemInUse.itemName.ToLower() == "pp up")
            Pokemon_Details.Instance.OnMoveSelected += IncreasePowerpoints;
        Pokemon_Details.Instance.LoadDetails(_selectedPartyPokemon);
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
                Dialogue_handler.Instance.DisplayInfo("Your pokemon are already protected","Details");
                return;
            }
            var partnerIndex = (currentTurnIndex > 0) ? 0 : 1;
            var player = Battle_handler.Instance.battleParticipants[currentTurnIndex];
            var partner = Battle_handler.Instance.battleParticipants[partnerIndex];
            var pokemonProtected = (partner.isActive) ? 
                _selectedPartyPokemon.pokemonName+" and "+ partner.pokemon.pokemonName
                :_selectedPartyPokemon.pokemonName;
            
            Move_handler.Instance.ApplyStatDropImmunity(player,5);
            if(Battle_handler.Instance.isDoubleBattle)
                Move_handler.Instance.ApplyStatDropImmunity(partner,5);
            
            Dialogue_handler.Instance.DisplayBattleInfo("A veil of light covers "+pokemonProtected);
            DepleteItem();
            Invoke(nameof(SkipTurn),1f);
            ResetItemUsage();
            return;
        }
        var buff = BattleOperations.SearchForBuffOrDebuff(_selectedPartyPokemon, statName);
        if(buff!=null)
            if (buff.stage > 5)
            {
                Dialogue_handler.Instance.DisplayInfo(_selectedPartyPokemon.pokemonName + "'s " + statName
                                                      + " cant go any higher", "Details");
                ResetItemUsage();
                return;
            }
        
        var xBuffData = new BuffDebuffData(currentParticipant, statName, true, 1);
        Move_handler.Instance.SelectRelevantBuffOrDebuff(xBuffData);
        DepleteItem();
        Invoke(nameof(SkipTurn),1f);
        ResetItemUsage();
    }
    private void RevivePokemon(string reviveType)
    {
        if (_selectedPartyPokemon.hp > 0) {Dialogue_handler.Instance.DisplayInfo(_selectedPartyPokemon.pokemonName+" has not fainted!", "Details",1f); return;}
        _selectedPartyPokemon.hp = (reviveType=="max revive")? _selectedPartyPokemon.maxHp : math.trunc(_selectedPartyPokemon.maxHp*0.5f);
        Dialogue_handler.Instance.DisplayInfo(_selectedPartyPokemon.pokemonName+" has been revived!", "Details",1f);
        DepleteItem();
        Invoke(nameof(SkipTurn),1.2f);
        ResetItemUsage();
    } 
    private void RestorePowerpoints(int moveIndex)
     {
         Pokemon_Details.Instance.OnMoveSelected -= RestorePowerpoints;
         var pointsToAdd = 0;
         var currentMove = _selectedPartyPokemon.moveSet[moveIndex];
         
         if (_itemInUse.itemName.ToLower() == "ether")
             pointsToAdd = 10;
         
         if (_itemInUse.itemName.ToLower() == "max ether")
             pointsToAdd = currentMove.maxPowerpoints;
         
         var sumPoints = currentMove.powerpoints + pointsToAdd;
         
         currentMove.powerpoints = (sumPoints > currentMove.maxPowerpoints) ? currentMove.maxPowerpoints : sumPoints;
 
         Dialogue_handler.Instance.DisplayInfo( currentMove.moveName+" pp was restored!", "Details",1f);
         DepleteItem();
         ResetItemUsage();
         Pokemon_Details.Instance.ExitDetails();
         SkipTurn();
     }
    private void IncreasePowerpoints(int moveIndex)
    {
        Pokemon_Details.Instance.OnMoveSelected -= IncreasePowerpoints;
        var currentMove = _selectedPartyPokemon.moveSet[moveIndex];
        double powerpointRatio = (float) currentMove.maxPowerpoints / currentMove.basePowerpoints;
        if (Math.Round(powerpointRatio,1) >= 1.6) return;
        
        currentMove.maxPowerpoints += (int)math.floor((0.2*currentMove.basePowerpoints));
        Dialogue_handler.Instance.DisplayInfo( currentMove.moveName+"'s pp was increased!", "Details",1f);
        
        DepleteItem();
        ResetItemUsage();
        Pokemon_Details.Instance.ExitDetails();
    }

    private void MaximisePowerpoints(int moveIndex)
    {
        Pokemon_Details.Instance.OnMoveSelected -= MaximisePowerpoints;
        var currentMove = _selectedPartyPokemon.moveSet[moveIndex];

        if (currentMove.maxPowerpoints >= (currentMove.basePowerpoints * 1.6)) return;
        
        currentMove.maxPowerpoints = (int)math.floor( currentMove.basePowerpoints * 1.6 );
        
        Dialogue_handler.Instance.DisplayInfo( currentMove.moveName+"'s pp was maxed out!", "Details",1f);
        
        DepleteItem();
        ResetItemUsage();
        Pokemon_Details.Instance.ExitDetails();
    }
    private void UsePokeball(Item pokeball)
    {
        if (!Options_manager.Instance.playerInBattle)
        {
            Dialogue_handler.Instance.DisplayInfo("Cant use that right now!", "Details");
            return;
        }
        if (Battle_handler.Instance.isTrainerBattle)
        {
            Battle_handler.Instance.displayingInfo = true;
            Dialogue_handler.Instance.DisplayInfo("Cant catch someone else's Pokemon!", "Details",1f);
            return;
        }
        if(pokemon_storage.Instance.MaxPokemonCapacity())
        {
            Dialogue_handler.Instance.DisplayInfo("Can no longer catch more pokemon, free up space in pc!", "Details");
            return;
        }
        DepleteItem();
        StartCoroutine(TryToCatchPokemon(pokeball));
    }
    
    IEnumerator TryToCatchPokemon(Item pokeball)
    {
        var isCaught = false;
        var wildPokemon = Wild_pkm.Instance.participant.pokemon;//pokemon only caught in wild
        Dialogue_handler.Instance.DisplayBattleInfo("Trying to catch "+wildPokemon.pokemonName+" .....");
        yield return new WaitUntil(()=> !Dialogue_handler.Instance.messagesLoading);
        var ballRate = float.Parse(pokeball.itemEffect);
        var bracket1 = (3 * wildPokemon.maxHp - 2 * wildPokemon.hp) / (3 * wildPokemon.maxHp);
        var catchValue = math.trunc(bracket1 * wildPokemon.catchRate * ballRate * 
                                      BattleOperations.GetCatchRateBonusFromStatus(wildPokemon.statusEffect));
        
        if (BattleOperations.IsImmediateCatch(catchValue) 
            | BattleOperations.PassedPokeballShakeTest(catchValue))
            isCaught = true;
  
        if (isCaught)
        {
            Dialogue_handler.Instance.DisplayBattleInfo("Well done "+wildPokemon.pokemonName+" has been caught");
            var rawName = wildPokemon.pokemonName.Replace("Foe ", "");
            wildPokemon.pokemonName = rawName;
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
    private void HealStatusEffect(string curableStatus)
    {
        if (_selectedPartyPokemon.statusEffect == "None")
        {Dialogue_handler.Instance.DisplayInfo("Pokemon is already healthy","Feedback");return; }
        if (curableStatus == "full heal") 
        {
            _selectedPartyPokemon.statusEffect = "None";
            Dialogue_handler.Instance.DisplayInfo(_selectedPartyPokemon.pokemonName+" has been healed","Feedback");
        }
        else
        {
            if (_selectedPartyPokemon.statusEffect.ToLower() == curableStatus)
            {
                _selectedPartyPokemon.statusEffect = "None";
                if (curableStatus == "sleep" | curableStatus == "freeze"| curableStatus == "paralysis")
                    _selectedPartyPokemon.canAttack = true;
                Dialogue_handler.Instance.DisplayInfo("Pokemon has been healed","Feedback");
                Battle_handler.Instance.RefreshParticipantUI();
            }
            else
            {Dialogue_handler.Instance.DisplayInfo("Incorrect heal item","Feedback"); return; }
        }
        ResetItemUsage();
        if (usingHeldItem) { DepleteHeldItem();return;}
        DepleteItem();
        Pokemon_party.Instance.RefreshMemberCards();
        Dialogue_handler.Instance.EndDialogue(1f);
        Invoke(nameof(SkipTurn),1.3f);
    }
    private void SkipTurn()
    {
        if (!Options_manager.Instance.playerInBattle) return;
        Game_ui_manager.Instance.CloseParty();
        Turn_Based_Combat.Instance.NextTurn();
    }
    private void RestoreHealth(int healEffect)
    {
        if (_selectedPartyPokemon.hp <= 0)
        {
            Dialogue_handler.Instance.DisplayInfo( "Pokemon has already fainted","Feedback",1f);
            ResetItemUsage();
            return;
        } 
        if(_selectedPartyPokemon.hp>=_selectedPartyPokemon.maxHp)
        {
            Dialogue_handler.Instance.DisplayInfo("Pokemon health already is full","Feedback",1f);
            ResetItemUsage();
            return;
        }
        if ((_selectedPartyPokemon.hp + healEffect) < _selectedPartyPokemon.maxHp)
        {
            _selectedPartyPokemon.hp += healEffect;
            Dialogue_handler.Instance.DisplayInfo(_selectedPartyPokemon.pokemonName+" gained "+healEffect+" health points","Feedback",1f);
        }
        else if ((_selectedPartyPokemon.hp + healEffect) >= _selectedPartyPokemon.maxHp)
        {
            _selectedPartyPokemon.hp = _selectedPartyPokemon.maxHp;
            Dialogue_handler.Instance.DisplayInfo(_selectedPartyPokemon.pokemonName+" gained full health points","Feedback",1f);
        }
        ResetItemUsage();
        if (usingHeldItem) { DepleteHeldItem();return;}
        DepleteItem();
        Invoke(nameof(SkipTurn),1.3f);
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
