using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Item_handler : MonoBehaviour
{
    public Pokemon selected_party_pkm;
    public bool Using_item = false;
    public bool isHeldItem = false;
    private Item item_in_use;
    public static Item_handler instance;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
    public void Use_Item(Item item)
    {
        item_in_use = item;
        switch (item.itemType.ToLower())
        {
            case "heal hp":
                heal_health(int.Parse(item.itemEffect));
                break;
            case "revive":
                RevivePokemon(item.itemName.ToLower());
                break;
            case "status":
                heal_status(item.itemEffect.ToLower());
                break;
            case "stats":
                ChangeStats(item.itemEffect.ToLower());
                break;
            case "pokeball":
                UsePokeball(item);
                break;
            case "evolution stone":
                StoneEvolution(item.itemName.ToLower());
                break;
            case "rare candy":
                LevelUp();
                break;
        }
    }
    void LevelUp()
    {
        int exp = PokemonOperations.GetNextLv(selected_party_pkm.Current_level, selected_party_pkm.EXPGroup);
        selected_party_pkm.Recieve_exp(exp-selected_party_pkm.CurrentExpAmount+1);
        Dialogue_handler.instance.Write_Info(selected_party_pkm.Pokemon_name+" leveled up!", "Details",1f);
        DepleteItem();
        ResetItemUsage();
    }

    void StoneEvolution(string EvolutionStoneName)
    {
        if (selected_party_pkm.EvolutionStoneName.ToLower() == EvolutionStoneName)
        {
            DepleteItem();
            ResetItemUsage();
            selected_party_pkm.CheckEvolutionRequirements(0);
        } 
        else
            Dialogue_handler.instance.Write_Info("Cant use that on "+selected_party_pkm.Pokemon_name, "Details",1f);
    }
    void ChangeStats(string Stat)
    {
        if (Stat == "pp")
        {
            Pokemon_Details.instance.ChangingMoveData = true;
            if (item_in_use.itemType.ToLower() == "ether")
                Pokemon_Details.instance.OnMoveSelected += RestorePP;
            else
                Pokemon_Details.instance.OnMoveSelected += IncreasePP;
            Pokemon_Details.instance.Load_Details(selected_party_pkm);
        }
        //if i add proteins,calcium etc. Then i can just add them here in a switch based on stat they change
    }

    void RevivePokemon(string ReviveType)
    {
        if (selected_party_pkm.HP > 0) {Dialogue_handler.instance.Write_Info(selected_party_pkm.Pokemon_name+" has not fainted!", "Details",1f); return;}
        selected_party_pkm.HP = (ReviveType=="revive")? math.trunc(selected_party_pkm.max_HP*0.5f) : selected_party_pkm.max_HP;
        Dialogue_handler.instance.Write_Info(selected_party_pkm.Pokemon_name+" has been revived!", "Details",1f);
        DepleteItem();
        Invoke(nameof(skipTurn),1.2f);
        ResetItemUsage();
    } 
    void RestorePP(int MoveIndex)
     {
         Pokemon_Details.instance.OnMoveSelected -= RestorePP;
         int PointsToAdd = 0;
         Move CurrentMove = selected_party_pkm.move_set[MoveIndex];
         
         if (item_in_use.itemName.ToLower() == "ether")
             PointsToAdd = 10;
         
         if (item_in_use.itemName.ToLower() == "max ether")
             PointsToAdd = CurrentMove.max_Powerpoints;
         
         int SumPoints = CurrentMove.Powerpoints + PointsToAdd;
         
         CurrentMove.Powerpoints = (SumPoints > CurrentMove.max_Powerpoints) ? CurrentMove.max_Powerpoints : SumPoints;
 
         Dialogue_handler.instance.Write_Info( CurrentMove.Move_name+" pp was restored!", "Details",1f);
         DepleteItem();
         ResetItemUsage();
         skipTurn();
         Pokemon_Details.instance.Exit_details();
         Bag.instance.View_bag();
     }
    void IncreasePP(int MoveIndex)
    {
        Pokemon_Details.instance.OnMoveSelected -= IncreasePP;
        double PowerpointRatio = (float)selected_party_pkm.move_set[MoveIndex].max_Powerpoints/
                             selected_party_pkm.move_set[MoveIndex].BasePowerpoints;
        if (Math.Round(PowerpointRatio,1) >= 1.6) return;
        selected_party_pkm.move_set[MoveIndex].max_Powerpoints+=(int)math.floor((0.2*selected_party_pkm.move_set[MoveIndex].BasePowerpoints));
        Dialogue_handler.instance.Write_Info( selected_party_pkm.move_set[MoveIndex].Move_name+" pp was increased!", "Details",1f);
        DepleteItem();
        ResetItemUsage();
        Pokemon_Details.instance.Exit_details();
        Bag.instance.View_bag();
    }
    void UsePokeball(Item pokeball)
    {
        if(Options_manager.instance.playerInBattle)
        {
            if (Battle_handler.Instance.isTrainerBattle)
            {
                Battle_handler.Instance.displayingInfo = true;
                Dialogue_handler.instance.Write_Info("Cant catch someone else's Pokemon!", "Details",1f);
            }
            else
            {
                DepleteItem();
                StartCoroutine(TryCatchPokemon(pokeball));
            }
        }
        else
            Dialogue_handler.instance.Write_Info("Cant use that right now!", "Details");
    }
    
    IEnumerator TryCatchPokemon(Item pokeball)
    {
        bool isCaught = false;
        Pokemon WildPokemon = Wild_pkm.Instance.participant.pokemon;//pokemon only caught in wild
        Dialogue_handler.instance.Battle_Info("Trying to catch "+WildPokemon.Pokemon_name+" .....");
        yield return new WaitUntil(()=> !Dialogue_handler.instance.messagesLoading);
        float BallRate = float.Parse(pokeball.itemEffect);
        float bracket1 = (3 * WildPokemon.max_HP - 2 * WildPokemon.HP) / (3 * WildPokemon.max_HP);
        float CatchValue = math.trunc(bracket1 * WildPokemon.CatchRate * BallRate * 
                                      BattleOperations.GetCatchRateBonusFromStatus(WildPokemon.Status_effect));
        
        if (BattleOperations.IsImmediateCatch(CatchValue) 
            | BattleOperations.PassedPokeballShakeTest(CatchValue))
            isCaught = true;
  
        if (isCaught)
        {
            Dialogue_handler.instance.Battle_Info("Well done "+WildPokemon.Pokemon_name+" has been caught");
            Pokemon_party.instance.Add_Member(WildPokemon);
            yield return new WaitUntil(()=> !Dialogue_handler.instance.messagesLoading);
            Wild_pkm.Instance.participant.EndWildBattle();
        }else
        {
            Dialogue_handler.instance.Battle_Info(WildPokemon.Pokemon_name+" escaped the pokeball");
            yield return new WaitUntil(()=> !Dialogue_handler.instance.messagesLoading);
            skipTurn();
        }
        ResetItemUsage();
    }
    private void heal_status(string StatusToHeal)
    {
        if (selected_party_pkm.Status_effect == "None")
        {Dialogue_handler.instance.Write_Info("Pokemon is already healthy","Feedback");return; }
        if (StatusToHeal == "full heal") {selected_party_pkm.Status_effect = "None";
            Dialogue_handler.instance.Write_Info(selected_party_pkm.Pokemon_name+" has been healed","Feedback");
        }
        else
        {
            if (selected_party_pkm.Status_effect.ToLower() == StatusToHeal)
            {
                selected_party_pkm.Status_effect = "None";
                if (StatusToHeal == "sleep" | StatusToHeal == "freeze"| StatusToHeal == "paralysis")
                    selected_party_pkm.canAttack = true;
                Dialogue_handler.instance.Write_Info("Pokemon has been healed","Feedback");
                Battle_handler.Instance.RefreshParticipantUI();
            }
            else
            {Dialogue_handler.instance.Write_Info("Incorrect heal item","Feedback"); return; }
        }
        ResetItemUsage();
        if (isHeldItem)
        {
            isHeldItem = false; 
            item_in_use.quantity = (item_in_use.isHeldItem)? 1 : item_in_use.quantity-1; 
            return;
        }
        Invoke(nameof(skipTurn),1.3f);
        DepleteItem();
        Pokemon_party.instance.Refresh_Member_Cards();
        Dialogue_handler.instance.Dialouge_off(1f);
        Invoke(nameof(skipTurn),1.3f);
    }
    void skipTurn()
    {
        if (Options_manager.instance.playerInBattle)
        {
            Game_ui_manager.instance.Close_party();
            Turn_Based_Combat.Instance.NextTurn();
        }
    }
    private void heal_health(int heal_effect)
    {
        if(selected_party_pkm.HP<1 | selected_party_pkm.HP>=selected_party_pkm.max_HP)
        {
            string message = (selected_party_pkm.HP<=0)? "Pokemon has already fainted" : "Pokemon health already is full";
            Dialogue_handler.instance.Write_Info(message,"Feedback",1f);
            ResetItemUsage();
            return;
        }
        if ((selected_party_pkm.HP + heal_effect) < selected_party_pkm.max_HP)
        {
            selected_party_pkm.HP += heal_effect;
            Dialogue_handler.instance.Write_Info(selected_party_pkm.Pokemon_name+" gained "+heal_effect+" health points","Feedback",1f);
        }
        else if ((selected_party_pkm.HP + heal_effect) >= selected_party_pkm.max_HP)
        {
            selected_party_pkm.HP = selected_party_pkm.max_HP;
            Dialogue_handler.instance.Write_Info(selected_party_pkm.Pokemon_name+" gained "+ (heal_effect-(selected_party_pkm.max_HP - selected_party_pkm.HP))+" health points","Feedback",1f);
        }
        ResetItemUsage();
        if (isHeldItem)
        {
            isHeldItem = false; 
            item_in_use.quantity = (item_in_use.isHeldItem)? 1 : item_in_use.quantity-1; 
            return;
        }
        DepleteItem();
        Invoke(nameof(skipTurn),1.3f);
    }

    void DepleteItem()
    {
        item_in_use.quantity--;
        Bag.instance.check_Quantity(item_in_use);
    }
    void ResetItemUsage()
    {
        Using_item = false;
        selected_party_pkm = null;
    }
}
