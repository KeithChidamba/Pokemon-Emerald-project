using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Held_Items : MonoBehaviour
{
    public Battle_Participant participant;
    void Start()
    {
        Turn_Based_Combat.instance.OnMoveExecute += CheckUsableItem;
    }

    void CheckUsableItem()
    {
        if(!participant.is_active)return;
        if (!participant.pokemon.HasItem) return;
        if (participant.pokemon.HeldItem.quantity == 0 & !participant.pokemon.HeldItem.isHeldItem)
        {//remove consumable held items that are depleted, not ones that just have special functionality
            participant.pokemon.RemoveHeldItem(); return; 
        }
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
        if(participant.pokemon.HP >= (participant.pokemon.max_HP/2)) return;        Debug.Log("triggered heal held item");
        Item_handler.instance.isHeldItem = true;
        Item_handler.instance.selected_party_pkm = participant.pokemon;
        Item_handler.instance.Use_Item(participant.pokemon.HeldItem);
    }

    void CheckStatusCondition()
    {
        if(participant.pokemon.Status_effect == "None") return;        Debug.Log("triggered status held item");
        Item_handler.instance.isHeldItem = true;
        Item_handler.instance.selected_party_pkm = participant.pokemon;
        Item_handler.instance.Use_Item(participant.pokemon.HeldItem);
    }
}
