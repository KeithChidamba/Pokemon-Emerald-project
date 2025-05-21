using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

public class Item_handler : MonoBehaviour
{
    public Pokemon selectedPartyPokemon;
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
    public void UseItem(Item item)
    {
        _itemInUse = item;
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
            case "stats":
                ChangeStats(item.itemEffect.ToLower());
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
        var exp = PokemonOperations.CalculateExpForNextLevel(selectedPartyPokemon.currentLevel, selectedPartyPokemon.expGroup);
        Dialogue_handler.Instance.DisplayInfo(selectedPartyPokemon.pokemonName+" leveled up!", "Details",1f);
        yield return new WaitForSeconds(1f);
        selectedPartyPokemon.ReceiveExperience((exp-selectedPartyPokemon.currentExpAmount)+1);
        DepleteItem();
        ResetItemUsage();
    }

    void TriggerStoneEvolution(string evolutionStoneName)
    {
        if (selectedPartyPokemon.evolutionStoneName.ToLower() == evolutionStoneName)
        {
            DepleteItem();
            ResetItemUsage();
            selectedPartyPokemon.CheckEvolutionRequirements(0);
        } 
        else
            Dialogue_handler.Instance.DisplayInfo("Cant use that on "+selectedPartyPokemon.pokemonName, "Details",1f);
    }
    private void ChangeStats(string stat)
    {
        if (stat == "pp")
        {
            Pokemon_Details.Instance.changingMoveData = true;
            if (_itemInUse.itemType.ToLower() == "ether")
                Pokemon_Details.Instance.OnMoveSelected += RestorePowerpoints;
            if (_itemInUse.itemType.ToLower() == "stat increase")
                Pokemon_Details.Instance.OnMoveSelected += IncreasePowerpoints;
            Pokemon_Details.Instance.LoadDetails(selectedPartyPokemon);
        }
        //if i add proteins,calcium etc. Then i can just add them here in a switch based on stat they change
    }

    private void ItemBuffOrDebuff(string statName)
    {
        var currentTurnIndex = Turn_Based_Combat.Instance.currentTurnIndex;
        selectedPartyPokemon = Battle_handler.Instance.battleParticipants[currentTurnIndex].pokemon;
        if (statName == "Stat Reduction")//Guard Spec applies all user's participant
        {
            if (selectedPartyPokemon.immuneToStatReduction)
            {
                Dialogue_handler.Instance.DisplayInfo("Your pokemon are already protected","Details");
                return;
            }
            var partnerIndex = (currentTurnIndex > 0) ? 0 : 1;
            var player = Battle_handler.Instance.battleParticipants[currentTurnIndex];
            var partner = Battle_handler.Instance.battleParticipants[partnerIndex];
            var pokemonProtected = (partner.isActive) ? 
                selectedPartyPokemon.pokemonName+" and "+ partner.pokemon.pokemonName
                :selectedPartyPokemon.pokemonName;
            
            Move_handler.Instance.ApplyStatDropImmunity(player,5);
            if(Battle_handler.Instance.isDoubleBattle)
                Move_handler.Instance.ApplyStatDropImmunity(partner,5);
            
            Dialogue_handler.Instance.DisplayBattleInfo("A veil of light covers "+pokemonProtected);
            DepleteItem();
            Invoke(nameof(SkipTurn),1f);
            ResetItemUsage();
            return;
        }
        var buff = BattleOperations.SearchForBuffOrDebuff(selectedPartyPokemon, statName);
        if(buff!=null)
            if (buff.Stage > 5)
            {
                Dialogue_handler.Instance.DisplayInfo(selectedPartyPokemon.pokemonName + "'s " + statName
                                                      + " cant go any higher", "Details");
                ResetItemUsage();
                return;
            }
        var xBuffData = new BuffDebuffData(selectedPartyPokemon, statName, true, 1);
        Move_handler.Instance.SelectRelevantBuffOrDebuff(xBuffData);
        DepleteItem();
        Invoke(nameof(SkipTurn),1f);
        ResetItemUsage();
    }
    private void RevivePokemon(string reviveType)
    {
        if (selectedPartyPokemon.hp > 0) {Dialogue_handler.Instance.DisplayInfo(selectedPartyPokemon.pokemonName+" has not fainted!", "Details",1f); return;}
        selectedPartyPokemon.hp = (reviveType=="max revive")? selectedPartyPokemon.maxHp : math.trunc(selectedPartyPokemon.maxHp*0.5f);
        Dialogue_handler.Instance.DisplayInfo(selectedPartyPokemon.pokemonName+" has been revived!", "Details",1f);
        DepleteItem();
        Invoke(nameof(SkipTurn),1.2f);
        ResetItemUsage();
    } 
    private void RestorePowerpoints(int moveIndex)
     {
         Pokemon_Details.Instance.OnMoveSelected -= RestorePowerpoints;
         var pointsToAdd = 0;
         var currentMove = selectedPartyPokemon.moveSet[moveIndex];
         
         if (_itemInUse.itemName.ToLower() == "ether")
             pointsToAdd = 10;
         
         if (_itemInUse.itemName.ToLower() == "max ether")
             pointsToAdd = currentMove.maxPowerpoints;
         
         var sumPoints = currentMove.powerpoints + pointsToAdd;
         
         currentMove.powerpoints = (sumPoints > currentMove.maxPowerpoints) ? currentMove.maxPowerpoints : sumPoints;
 
         Dialogue_handler.Instance.DisplayInfo( currentMove.moveName+" pp was restored!", "Details",1f);
         DepleteItem();
         ResetItemUsage();
         SkipTurn();
         Pokemon_Details.Instance.ExitDetails();
         Bag.Instance.ViewBag();
     }
    private void IncreasePowerpoints(int moveIndex)
    {
        Pokemon_Details.Instance.OnMoveSelected -= IncreasePowerpoints;
        var currentMove = selectedPartyPokemon.moveSet[moveIndex];
        double powerpointRatio = (float) currentMove.maxPowerpoints / currentMove.basePowerpoints;
        if (Math.Round(powerpointRatio,1) >= 1.6) return;
        
        currentMove.maxPowerpoints += (int)math.floor((0.2*currentMove.basePowerpoints));
        Dialogue_handler.Instance.DisplayInfo( currentMove.moveName+"'s pp was increased!", "Details",1f);
        
        DepleteItem();
        ResetItemUsage();
        Pokemon_Details.Instance.ExitDetails();
        Bag.Instance.ViewBag();
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
        if (selectedPartyPokemon.statusEffect == "None")
        {Dialogue_handler.Instance.DisplayInfo("Pokemon is already healthy","Feedback");return; }
        if (curableStatus == "full heal") 
        {
            selectedPartyPokemon.statusEffect = "None";
            Dialogue_handler.Instance.DisplayInfo(selectedPartyPokemon.pokemonName+" has been healed","Feedback");
        }
        else
        {
            if (selectedPartyPokemon.statusEffect.ToLower() == curableStatus)
            {
                selectedPartyPokemon.statusEffect = "None";
                if (curableStatus == "sleep" | curableStatus == "freeze"| curableStatus == "paralysis")
                    selectedPartyPokemon.canAttack = true;
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
        if (selectedPartyPokemon.hp <= 0)
        {
            Dialogue_handler.Instance.DisplayInfo( "Pokemon has already fainted","Feedback",1f);
            ResetItemUsage();
            return;
        } 
        if(selectedPartyPokemon.hp>=selectedPartyPokemon.maxHp)
        {
            Dialogue_handler.Instance.DisplayInfo("Pokemon health already is full","Feedback",1f);
            ResetItemUsage();
            return;
        }
        if ((selectedPartyPokemon.hp + healEffect) < selectedPartyPokemon.maxHp)
        {
            selectedPartyPokemon.hp += healEffect;
            Dialogue_handler.Instance.DisplayInfo(selectedPartyPokemon.pokemonName+" gained "+healEffect+" health points","Feedback",1f);
        }
        else if ((selectedPartyPokemon.hp + healEffect) >= selectedPartyPokemon.maxHp)
        {
            selectedPartyPokemon.hp = selectedPartyPokemon.maxHp;
            Dialogue_handler.Instance.DisplayInfo(selectedPartyPokemon.pokemonName+" gained full health points","Feedback",1f);
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
        selectedPartyPokemon = null;
    }
}
