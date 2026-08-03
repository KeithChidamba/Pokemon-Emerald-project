using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class StatusEffectAnimationHandler
{
    public Animator statusEffectAnimator;
    public Image statusEffectImage;
    public RectTransform GetImageRect => statusEffectAnimator.GetComponent<RectTransform>();
    [HideInInspector]public BattleParticipant participant;
    
    public void PlayStatusEffectAnimation()
    {
        statusEffectAnimator.Play(participant.pokemon.statusEffect.ToString());
    }
    public void PlayConfusionAnimation()
    {
        statusEffectAnimator.Play("Confusion"); 
    }
    public void PlayEmptyAnimation()
    {
        statusEffectAnimator.Play("Empty State");
        statusEffectImage.sprite = null;
        statusEffectImage.color = new Color(255, 255, 255, 0);
    }
}
