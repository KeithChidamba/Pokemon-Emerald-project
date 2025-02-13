using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item_handler : MonoBehaviour
{
    public Pokemon selected_party_pkm;
    public bool Using_item = false;
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
        switch (item.Item_type.ToLower())
        {
            case "potion":
                Heal(int.Parse(item.Item_effect));
                break;
            case "status":
                heal_status(item.Item_effect.ToLower());
                break;
            case "pokeball":
                UsePokeball(item);
                break;
        }
    }
    void UsePokeball(Item pokeball)
    {
        if(Options_manager.instance.playerInBattle)
        {
            if (Battle_handler.instance.is_trainer_battle)
                Dialogue_handler.instance.Write_Info("Cant catch someone else's Pokemon!", "Details");
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
        Pokemon WildPokemon = Wild_pkm.instance.pokemon_participant.pokemon;//pokemon only caught in wild
        Dialogue_handler.instance.Battle_Info("Trying to catch "+WildPokemon.Pokemon_name+" .....");
        yield return new WaitUntil(()=> !Dialogue_handler.instance.messagesLoading);
        float BallRate = float.Parse(pokeball.Item_effect);
        float bracket1 = (3 * (WildPokemon.max_HP - 2) * WildPokemon.HP) / (3 * WildPokemon.max_HP);
        float CatchValue = bracket1 * WildPokemon.CatchRate * BallRate * BattleOperations.GetStatusBonus(WildPokemon.Status_effect);
        if(BattleOperations.IsImmediateCatch(CatchValue))
            isCaught = true;
        else
        {
            if (BattleOperations.ShakeCheck(CatchValue))
                isCaught = true;
        }
        if (isCaught)
        {
            Dialogue_handler.instance.Battle_Info("Well done "+WildPokemon.Pokemon_name+" has been caught");
            Pokemon_party.instance.Add_Member(WildPokemon);
            yield return new WaitUntil(()=> !Dialogue_handler.instance.messagesLoading);
            Wild_pkm.instance.pokemon_participant.EndWildBattle();
        }else
        {
            Dialogue_handler.instance.Battle_Info(WildPokemon.Pokemon_name+" escaped the pokeball");
            yield return new WaitUntil(()=> !Dialogue_handler.instance.messagesLoading);
            skipTurn();
        }
        ResetItemUsage();
        StopAllCoroutines();
        yield return null;
    }
    private void heal_status(string status)
    {
        if (selected_party_pkm.Status_effect.ToLower() == status)
        {
            selected_party_pkm.Status_effect = "None";
            if (status == "sleep" | status == "freeze"| status == "paralysis")
                selected_party_pkm.canAttack = true;
            Dialogue_handler.instance.Write_Info("Pokemon has been healed","Details");
            DepleteItem();
            Battle_handler.instance.reload_participant_ui();
        }
        else if (selected_party_pkm.Status_effect == "None")
            Dialogue_handler.instance.Write_Info("Pokemon is already healthy","Details");
        else
            Dialogue_handler.instance.Write_Info("Incorrect heal item","Details");
        Pokemon_party.instance.Refresh_Member_Cards();
        Dialogue_handler.instance.Dialouge_off(1f);
        Invoke(nameof(skipTurn),1.3f);
        ResetItemUsage();
    }
    void skipTurn()
    {
        if (Options_manager.instance.playerInBattle)
        {
            Game_ui_manager.instance.Close_party();
            Turn_Based_Combat.instance.Next_turn();
        }
    }
    private void Heal(int heal_effect)
    {
        if(selected_party_pkm.HP>=selected_party_pkm.max_HP)
        {
            Dialogue_handler.instance.Write_Info("Pokemon health already is full","Details");
            Dialogue_handler.instance.Dialouge_off(1f);
            Invoke(nameof(skipTurn),1.3f);
            ResetItemUsage();
            return;
        }
        if ((selected_party_pkm.HP + heal_effect) < selected_party_pkm.max_HP)
        {
            selected_party_pkm.HP += heal_effect;
            Dialogue_handler.instance.Write_Info(selected_party_pkm.Pokemon_name+" gained "+heal_effect+" health points","Details");
            DepleteItem();
        }
        else if ((selected_party_pkm.HP + heal_effect) >= selected_party_pkm.max_HP)
        {
            selected_party_pkm.HP = selected_party_pkm.max_HP;
            Dialogue_handler.instance.Write_Info(selected_party_pkm.Pokemon_name+" gained "+ (heal_effect-(selected_party_pkm.max_HP - selected_party_pkm.HP))+" health points","Details");
            DepleteItem();
        }
        Dialogue_handler.instance.Dialouge_off(1f);
        Invoke(nameof(skipTurn),1.3f);
        ResetItemUsage();
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
