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
        }
    }
    IEnumerator LevelUpWithItem()
    {
        Game_ui_manager.Instance.canExitParty = false;
        var exp = PokemonOperations.CalculateExpForNextLevel(selectedPartyPokemon.Current_level, selectedPartyPokemon.EXPGroup);
        Dialogue_handler.Instance.DisplayInfo(selectedPartyPokemon.Pokemon_name+" leveled up!", "Details",1f);
        yield return new WaitForSeconds(1f);
        selectedPartyPokemon.ReceiveExperience((exp-selectedPartyPokemon.CurrentExpAmount)+1);
        DepleteItem();
        ResetItemUsage();
    }

    void TriggerStoneEvolution(string evolutionStoneName)
    {
        if (selectedPartyPokemon.EvolutionStoneName.ToLower() == evolutionStoneName)
        {
            DepleteItem();
            ResetItemUsage();
            selectedPartyPokemon.CheckEvolutionRequirements(0);
        } 
        else
            Dialogue_handler.Instance.DisplayInfo("Cant use that on "+selectedPartyPokemon.Pokemon_name, "Details",1f);
    }
    void ChangeStats(string stat)
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

    private void RevivePokemon(string reviveType)
    {
        if (selectedPartyPokemon.HP > 0) {Dialogue_handler.Instance.DisplayInfo(selectedPartyPokemon.Pokemon_name+" has not fainted!", "Details",1f); return;}
        selectedPartyPokemon.HP = (reviveType=="max revive")? selectedPartyPokemon.max_HP : math.trunc(selectedPartyPokemon.max_HP*0.5f);
        Dialogue_handler.Instance.DisplayInfo(selectedPartyPokemon.Pokemon_name+" has been revived!", "Details",1f);
        DepleteItem();
        Invoke(nameof(SkipTurn),1.2f);
        ResetItemUsage();
    } 
    private void RestorePowerpoints(int moveIndex)
     {
         Pokemon_Details.Instance.OnMoveSelected -= RestorePowerpoints;
         var pointsToAdd = 0;
         var currentMove = selectedPartyPokemon.move_set[moveIndex];
         
         if (_itemInUse.itemName.ToLower() == "ether")
             pointsToAdd = 10;
         
         if (_itemInUse.itemName.ToLower() == "max ether")
             pointsToAdd = currentMove.max_Powerpoints;
         
         var sumPoints = currentMove.Powerpoints + pointsToAdd;
         
         currentMove.Powerpoints = (sumPoints > currentMove.max_Powerpoints) ? currentMove.max_Powerpoints : sumPoints;
 
         Dialogue_handler.Instance.DisplayInfo( currentMove.Move_name+" pp was restored!", "Details",1f);
         DepleteItem();
         ResetItemUsage();
         SkipTurn();
         Pokemon_Details.Instance.ExitDetails();
         Bag.Instance.ViewBag();
     }
    private void IncreasePowerpoints(int moveIndex)
    {
        Pokemon_Details.Instance.OnMoveSelected -= IncreasePowerpoints;
        var currentMove = selectedPartyPokemon.move_set[moveIndex];
        double powerpointRatio = (float) currentMove.max_Powerpoints / currentMove.BasePowerpoints;
        if (Math.Round(powerpointRatio,1) >= 1.6) return;
        
        currentMove.max_Powerpoints += (int)math.floor((0.2*currentMove.BasePowerpoints));
        Dialogue_handler.Instance.DisplayInfo( currentMove.Move_name+"'s pp was increased!", "Details",1f);
        
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
        DepleteItem();
        StartCoroutine(TryToCatchPokemon(pokeball));
    }
    
    IEnumerator TryToCatchPokemon(Item pokeball)
    {
        var isCaught = false;
        var wildPokemon = Wild_pkm.Instance.participant.pokemon;//pokemon only caught in wild
        Dialogue_handler.Instance.DisplayBattleInfo("Trying to catch "+wildPokemon.Pokemon_name+" .....");
        yield return new WaitUntil(()=> !Dialogue_handler.Instance.messagesLoading);
        var ballRate = float.Parse(pokeball.itemEffect);
        var bracket1 = (3 * wildPokemon.max_HP - 2 * wildPokemon.HP) / (3 * wildPokemon.max_HP);
        var catchValue = math.trunc(bracket1 * wildPokemon.CatchRate * ballRate * 
                                      BattleOperations.GetCatchRateBonusFromStatus(wildPokemon.Status_effect));
        
        if (BattleOperations.IsImmediateCatch(catchValue) 
            | BattleOperations.PassedPokeballShakeTest(catchValue))
            isCaught = true;
  
        if (isCaught)
        {
            Dialogue_handler.Instance.DisplayBattleInfo("Well done "+wildPokemon.Pokemon_name+" has been caught");
            Pokemon_party.Instance.AddMember(wildPokemon);
            yield return new WaitUntil(()=> !Dialogue_handler.Instance.messagesLoading);
            Wild_pkm.Instance.participant.EndWildBattle();
        }else
        {
            Dialogue_handler.Instance.DisplayBattleInfo(wildPokemon.Pokemon_name+" escaped the pokeball");
            yield return new WaitUntil(()=> !Dialogue_handler.Instance.messagesLoading);
            SkipTurn();
        }
        ResetItemUsage();
    }
    private void HealStatusEffect(string curableStatus)
    {
        if (selectedPartyPokemon.Status_effect == "None")
        {Dialogue_handler.Instance.DisplayInfo("Pokemon is already healthy","Feedback");return; }
        if (curableStatus == "full heal") 
        {
            selectedPartyPokemon.Status_effect = "None";
            Dialogue_handler.Instance.DisplayInfo(selectedPartyPokemon.Pokemon_name+" has been healed","Feedback");
        }
        else
        {
            if (selectedPartyPokemon.Status_effect.ToLower() == curableStatus)
            {
                selectedPartyPokemon.Status_effect = "None";
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
        if (selectedPartyPokemon.HP <= 0)
        {
            Dialogue_handler.Instance.DisplayInfo( "Pokemon has already fainted","Feedback",1f);
            ResetItemUsage();
            return;
        } 
        if(selectedPartyPokemon.HP>=selectedPartyPokemon.max_HP)
        {
            Dialogue_handler.Instance.DisplayInfo("Pokemon health already is full","Feedback",1f);
            ResetItemUsage();
            return;
        }
        if ((selectedPartyPokemon.HP + healEffect) < selectedPartyPokemon.max_HP)
        {
            selectedPartyPokemon.HP += healEffect;
            Dialogue_handler.Instance.DisplayInfo(selectedPartyPokemon.Pokemon_name+" gained "+healEffect+" health points","Feedback",1f);
        }
        else if ((selectedPartyPokemon.HP + healEffect) >= selectedPartyPokemon.max_HP)
        {
            selectedPartyPokemon.HP = selectedPartyPokemon.max_HP;
            Dialogue_handler.Instance.DisplayInfo(selectedPartyPokemon.Pokemon_name+" gained full health points","Feedback",1f);
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
        //if (!Options_manager.Instance.playerInBattle) 
            //Game_ui_manager.Instance.ViewBag();
    }
    void ResetItemUsage()
    {
        usingItem = false;
        selectedPartyPokemon = null;
    }
}
