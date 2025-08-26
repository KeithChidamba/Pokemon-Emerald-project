using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

public class Held_Items : MonoBehaviour
{
    private Battle_Participant _participant;
    private Item _heldItem;
    public bool processingItemEffect;
    public event Action<Held_Items> OnHeldItemUsage;
    void Start()
    {
        _participant =  GetComponent<Battle_Participant>();
        Turn_Based_Combat.Instance.OnMoveExecute += CheckForUsableItem;
    }
    void DepleteHeldItem()
    {
        _heldItem.quantity = _heldItem.isHeldItem? 1 : _heldItem.quantity-1; 
    }
    void CheckForUsableItem(Battle_Participant participant)
    {
        if (participant != _participant) return;
        if(!_participant.isActive)return;
        if (!_participant.pokemon.hasItem) return;
        _heldItem = _participant.pokemon.heldItem;
        if (_heldItem.quantity == 0 && !_heldItem.isHeldItem)
        {//remove consumable held items that are depleted, not ones that just have special functionality
            _participant.pokemon.RemoveHeldItem(); return; 
        }
        if (!_heldItem.canBeUsedInBattle) return;
        
        switch (_heldItem.itemType)
        {
            case Item_handler.ItemType.Berry:
                DetermineBerryEffect();
                break;
            case Item_handler.ItemType.HealHp:
                CheckHealCondition();
                break;
            case Item_handler.ItemType.Status:
                CheckStatusCondition();
                break;
            //in the future, if there's need to add special held items like focus sash, create new type of item and add it to this switch
        }
    }
    private void DetermineBerryEffect()
    {
        var berryInfo = _heldItem.GetModule<BerryInfo>();
        switch (berryInfo.berryType)
        {
            case  BerryInfo.Berry.HpHeal:
                CheckHealCondition();
                break;
            case  BerryInfo.Berry.StatusHeal:
                CheckStatusCondition();
                break;
        }
    }
    void CheckHealCondition()
    {
        if(_participant.pokemon.hp >= (_participant.pokemon.maxHp/2)) return;
        processingItemEffect = true;
        OnHeldItemUsage?.Invoke(this);
        DepleteHeldItem();
        StartCoroutine(GetHealing());
    }    
    void CheckStatusCondition()
    {
        if(_participant.pokemon.statusEffect == PokemonOperations.StatusEffect.None) return;
        processingItemEffect = true;
        OnHeldItemUsage?.Invoke(this);
        DepleteHeldItem();
        StartCoroutine(GetStatusHealing());
    }
    private IEnumerator GetHealing()
    { 
        Dialogue_handler.Instance.DisplayBattleInfo(_participant.pokemon.pokemonName+"'s "+_heldItem.itemName +" healed it");
        Move_handler.Instance.HealthGainDisplay(int.Parse(_heldItem.itemEffect),healthGainer:_participant);
        yield return new WaitUntil(() => !Move_handler.Instance.displayingHealthGain);
        yield return new WaitUntil(()=> !Dialogue_handler.Instance.messagesLoading);
        processingItemEffect = false;
    }
    private IEnumerator GetStatusHealing()
    { 
        Debug.Log("triggered status held item");
        var statusInfo = _heldItem.GetModule<StatusHealInfo>();
        var curableStatus = statusInfo.statusEffect;
        
        if (curableStatus == PokemonOperations.StatusEffect.Poison &&
            _participant.pokemon.statusEffect == PokemonOperations.StatusEffect.BadlyPoison)
        {//antidote heals all poison
            curableStatus = PokemonOperations.StatusEffect.BadlyPoison;
        }
        if (curableStatus != PokemonOperations.StatusEffect.FullHeal && 
            _participant.pokemon.statusEffect != curableStatus)
        { 
            processingItemEffect = false;
            yield break;
        }
        _participant.statusHandler.RemoveStatusEffect();
        Battle_handler.Instance.RefreshStatusEffectUI();
        Dialogue_handler.Instance.DisplayBattleInfo(_participant.pokemon.pokemonName+"'s "+_heldItem.itemName +" healed it");
        yield return new WaitUntil(()=> !Dialogue_handler.Instance.messagesLoading);
        processingItemEffect = false;
    }

}
