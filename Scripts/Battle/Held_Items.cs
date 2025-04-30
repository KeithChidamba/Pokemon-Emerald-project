using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Held_Items : MonoBehaviour
{
    public Battle_Participant participant;
    void Start()
    {
        Turn_Based_Combat.instance.OnNewTurn += CheckUsableItem;
    }

    void CheckUsableItem()
    {
        if(!participant.is_active)return;
        if (participant.pokemon.HeldItem == null) return;
        if (!participant.pokemon.HeldItem.CanBeUsedInBattle) return;
        switch (participant.pokemon.HeldItem.Item_type.ToLower())
        {
            case "heal hp":
                CheckHealCondition();
                break;
            case "status":
                CheckStatusCondition();
                break;
            //in the future, if there's need to add special held items like focus sash, create new type of item and add it to this switch
        }
    }

    void CheckHealCondition()
    {
        if(participant.pokemon.HP >= (participant.pokemon.max_HP/2)) return;
        Item_handler.instance.isHeldItem = true;
        Item_handler.instance.selected_party_pkm = participant.pokemon;
        Item_handler.instance.Use_Item(participant.pokemon.HeldItem);
    }

    void CheckStatusCondition()
    {
        if(participant.pokemon.Status_effect == "None") return;
        Item_handler.instance.isHeldItem = true;
        Item_handler.instance.selected_party_pkm = participant.pokemon;
        Item_handler.instance.Use_Item(participant.pokemon.HeldItem);
    }
}
