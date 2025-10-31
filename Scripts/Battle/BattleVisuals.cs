using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleVisuals : MonoBehaviour
{
    public static BattleVisuals Instance;
    public Animator statusEffectAnimator;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public IEnumerator DisplayStatusEffectVisuals(Battle_Participant participant)
    {
        statusEffectAnimator.gameObject.SetActive(true);
        var rect = statusEffectAnimator.GetComponent<RectTransform>();
        rect.anchoredPosition = participant.pokemonImage.rectTransform.anchoredPosition;
        statusEffectAnimator.Play(participant.pokemon.statusEffect.ToString());
        yield return new WaitForSeconds(1.75f);
        statusEffectAnimator.gameObject.SetActive(false);
        yield return null;
    }
    public IEnumerator DisplayDamageTakenVisual(Battle_Participant participant,Move_handler.DamageSource damageSource)
    {
        if(damageSource == Move_handler.DamageSource.Normal)
        {
            for (int i = 0; i < 4; i++)
            {
                yield return new WaitForSeconds(0.2f);
                participant.pokemonImage.color = new Color(0, 0, 0, 0);
                yield return new WaitForSeconds(0.2f);
                participant.pokemonImage.color = Color.white;
            }
        }
        if (damageSource == Move_handler.DamageSource.Poison)
        {
            Color startColor = Color.white;
            Color endColor = new Color(0.29f, 0f, 0.51f);
            float elapsed = 0f;
            var duration = 1f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                participant.pokemonImage.color = Color.Lerp(startColor, endColor, t);
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration / 2f);
                participant.pokemonImage.color = Color.Lerp(endColor, startColor, t);
                yield return null;
            }
            participant.pokemonImage.color = startColor;
            yield return new WaitForSeconds(0.5f);
        }
        if (damageSource == Move_handler.DamageSource.Burn)
        {
            statusEffectAnimator.gameObject.SetActive(true);
            var rect = statusEffectAnimator.GetComponent<RectTransform>();
            var start = new Vector2(participant.pokemonImage.rectTransform.anchoredPosition.x -
                                    (participant.pokemonImage.rectTransform.rect.width*0.5f)
                , participant.pokemonImage.rectTransform.anchoredPosition.y+20f);
            var target = new Vector2(participant.pokemonImage.rectTransform.anchoredPosition.x +
                                     (participant.pokemonImage.rectTransform.rect.width*0.5f)
                , participant.pokemonImage.rectTransform.anchoredPosition.y+20f);
            
            statusEffectAnimator.Play(participant.pokemon.statusEffect.ToString());
            yield return StartCoroutine(SlideRect(rect,start,target,165f));
            statusEffectAnimator.gameObject.SetActive(false);
        }
    }
    public IEnumerator SlideRect(RectTransform rect, Vector2 start, Vector2 target, float speed)
    {
        rect.anchoredPosition = start;
        while (Vector2.Distance(rect.anchoredPosition, target) > 0.5f)
        {
            rect.anchoredPosition = Vector2.MoveTowards(rect.anchoredPosition, target, speed * Time.deltaTime);
            yield return null;
        }
    }
}
