using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Held_Items : MonoBehaviour
{
    private Battle_Participant _participant;
    void Start()
    {
        _participant =  GetComponent<Battle_Participant>();
        Turn_Based_Combat.Instance.OnMoveExecute += CheckForUsableItem;
    }

    void CheckForUsableItem()
    {
        if(!_participant.isActive)return;
        if (!_participant.pokemon.HasItem) return;
        if (_participant.pokemon.HeldItem.quantity == 0 & !_participant.pokemon.HeldItem.isHeldItem)
        {//remove consumable held items that are depleted, not ones that just have special functionality
            _participant.pokemon.RemoveHeldItem(); return; 
        }
        if (!_participant.pokemon.HeldItem.canBeUsedInBattle) return;
        switch (_participant.pokemon.HeldItem.itemType.ToLower())
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
        if(_participant.pokemon.HP >= (_participant.pokemon.max_HP/2)) return;        Debug.Log("triggered heal held item");
        Item_handler.Instance.usingHeldItem = true;
        Item_handler.Instance.selectedPartyPokemon = _participant.pokemon;
        Item_handler.Instance.UseItem(_participant.pokemon.HeldItem);
    }

    void CheckStatusCondition()
    {
        if(_participant.pokemon.Status_effect == "None") return;        Debug.Log("triggered status held item");
        Item_handler.Instance.usingHeldItem = true;
        Item_handler.Instance.selectedPartyPokemon = _participant.pokemon;
        Item_handler.Instance.UseItem(_participant.pokemon.HeldItem);
    }
}
