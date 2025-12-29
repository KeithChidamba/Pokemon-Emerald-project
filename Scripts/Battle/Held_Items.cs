using System.Collections;
using UnityEngine;


public class Held_Items : MonoBehaviour
{
    private Battle_Participant _participant;
    private Item _heldItem;
    void Start()
    {
        _participant =  GetComponent<Battle_Participant>();

    }
    void DepleteHeldItem()
    {
        _heldItem.quantity = _heldItem.isHeldItem? 1 : _heldItem.quantity-1; 
    }
    public IEnumerator CheckForUsableItem()
    {
        if (!_participant.pokemon.hasItem) yield break;
        
        _heldItem = _participant.pokemon.heldItem;
        if (_heldItem.quantity == 0 && !_heldItem.isHeldItem)
        {//remove consumable held items that are depleted, not ones that just have special functionality
            _participant.pokemon.RemoveHeldItem(); yield break; 
        }
        if (!_heldItem.canBeUsedInBattle) yield break;
        
        switch (_heldItem.itemType)
        {
            case ItemType.Berry:
                yield return DetermineBerryEffect();
                break;
            case ItemType.HealHp:
                yield return CheckHealCondition();
                break;
            case ItemType.Status:
                yield return CheckStatusCondition();
                break;
            //in the future, if there's need to add special held items like focus sash, create new type of item and add it to this switch
        }
        yield return new WaitUntil(()=> !Dialogue_handler.Instance.messagesLoading);
    }
    private IEnumerator DetermineBerryEffect()
    {
        var berryInfo = _heldItem.GetModule<BerryInfoModule>();
        switch (berryInfo.berryType)
        {
            case  Berry.HpHeal:
                yield return CheckHealCondition();
                break;
            case  Berry.StatusHeal:
                yield return CheckStatusCondition();
                break;
            case  Berry.ConfusionHeal:
                yield return CheckIfConfused();
                break;
        }
    }
    private IEnumerator CheckHealCondition()
    {
        if(_participant.pokemon.hp >= (_participant.pokemon.maxHp/2)) yield break;
        
        DepleteHeldItem();
        yield return GetHealing();
    }    
    private IEnumerator CheckStatusCondition()
    {
        if(_participant.pokemon.statusEffect == StatusEffect.None) yield break;

        DepleteHeldItem();
       yield return GetStatusHealing();
    }
    private IEnumerator CheckIfConfused()
    {
        if(!_participant.isConfused) yield break;

        Dialogue_handler.Instance.DisplayDetails(_participant.pokemon.pokemonName+"'s Persim berry healed its confusion");
        _participant.isConfused = false;

    }
    private IEnumerator GetHealing()
    { 
        Dialogue_handler.Instance.DisplayBattleInfo(_participant.pokemon.pokemonName+"'s "+_heldItem.itemName +" healed it");
        Move_handler.Instance.HealthGainDisplay(int.Parse(_heldItem.itemEffect),healthGainer:_participant);
        yield return new WaitUntil(() => !Move_handler.Instance.displayingHealthGain);
    }
    private IEnumerator GetStatusHealing()
    { 
        Debug.Log("triggered status held item");
        var statusInfo = _heldItem.GetModule<StatusHealInfoModule>();
        var curableStatus = statusInfo.statusEffect;
        
        if (curableStatus == StatusEffect.Poison &&
            _participant.pokemon.statusEffect == StatusEffect.BadlyPoison)
        {//antidote heals all poison
            curableStatus = StatusEffect.BadlyPoison;
        }
        if (curableStatus != StatusEffect.FullHeal && 
            _participant.pokemon.statusEffect != curableStatus)
        { 
            yield break;
        }
        _participant.statusHandler.RemoveStatusEffect();
        Battle_handler.Instance.RefreshStatusEffectUI();
        Dialogue_handler.Instance.DisplayBattleInfo(_participant.pokemon.pokemonName+"'s "+_heldItem.itemName +" healed it");
    }

}
