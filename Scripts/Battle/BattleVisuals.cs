using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleVisuals : MonoBehaviour
{
    public static BattleVisuals Instance;
    public Animator playerBattleAnimator;
    public GameObject pokeballImage;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        Battle_handler.Instance.OnBattleEnd += ResetAfterBattle;
    }

    public IEnumerator DisplayStatusEffectVisuals(Battle_Participant participant)
    {
        participant.statusEffectAnimator.gameObject.SetActive(true);
        var rect = participant.statusEffectAnimator.GetComponent<RectTransform>();
        
        if (participant.pokemon.statusEffect == PokemonOperations.StatusEffect.Sleep)
        {
            var start = new Vector2(participant.pokemonImage.rectTransform.anchoredPosition.x
                , participant.pokemonImage.rectTransform.anchoredPosition.y+80f);
            var target = new Vector2(participant.pokemonImage.rectTransform.anchoredPosition.x -
                                     (participant.pokemonImage.rectTransform.rect.width*0.25f)
                , participant.pokemonImage.rectTransform.anchoredPosition.y+200f);
            
            participant.statusEffectAnimator.Play(participant.pokemon.statusEffect.ToString());
            yield return SlideRect(rect,start,target,165f);
            yield return SlideRect(rect,start,target,165f);
        }
        else 
        if (participant.pokemon.statusEffect == PokemonOperations.StatusEffect.Paralysis)
        {
            var imageRect = participant.pokemonImage.rectTransform;
            rect.anchoredPosition = imageRect.anchoredPosition;
            var movementSpeed = 200f;
            var startPos = imageRect.anchoredPosition;
            var target = new Vector2(imageRect.anchoredPosition.x + 10f, imageRect.anchoredPosition.y);
            
            participant.statusEffectAnimator.Play(participant.pokemon.statusEffect.ToString());
            
            yield return SlideRect(imageRect, startPos, target, movementSpeed);
            target = new Vector2(startPos.x - 20f, imageRect.anchoredPosition.y);
            yield return SlideRect(imageRect, imageRect.anchoredPosition, target, movementSpeed);
            target = new Vector2(startPos.x, imageRect.anchoredPosition.y);
            yield return SlideRect(imageRect, imageRect.anchoredPosition, target, movementSpeed);
            yield return new WaitForSeconds(0.25f);
        }
        else
        {
            rect.anchoredPosition = participant.pokemonImage.rectTransform.anchoredPosition;
            participant.statusEffectAnimator.Play(participant.pokemon.statusEffect.ToString());
            yield return new WaitForSeconds(1.75f);
        }
        participant.statusEffectAnimator.gameObject.SetActive(false);
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
            participant.statusEffectAnimator.gameObject.SetActive(true);
            var rect = participant.statusEffectAnimator.GetComponent<RectTransform>();
            var start = new Vector2(participant.pokemonImage.rectTransform.anchoredPosition.x -
                                    (participant.pokemonImage.rectTransform.rect.width*0.5f)
                , participant.pokemonImage.rectTransform.anchoredPosition.y+20f);
            var target = new Vector2(participant.pokemonImage.rectTransform.anchoredPosition.x +
                                     (participant.pokemonImage.rectTransform.rect.width*0.5f)
                , participant.pokemonImage.rectTransform.anchoredPosition.y+20f);
            
            participant.statusEffectAnimator.Play(participant.pokemon.statusEffect.ToString());
            yield return StartCoroutine(SlideRect(rect,start,target,165f));
            participant.statusEffectAnimator.gameObject.SetActive(false);
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
    public IEnumerator DisplayPokemonThrow()
    {//wild battle is always single
        playerBattleAnimator.gameObject.SetActive(true);
        pokeballImage.SetActive(true);
        var pkmImageRect = Battle_handler.Instance.battleParticipants[0].pokemonImage.rectTransform;
        var target = new Vector2(pkmImageRect.anchoredPosition.x, pkmImageRect.anchoredPosition.y-pkmImageRect.rect.height);
        yield return StartCoroutine(SlideRect(pkmImageRect, pkmImageRect.anchoredPosition, target, 300f));
        playerBattleAnimator.Play("pokemon catch");
        yield return new WaitForSeconds(1.6f);
        Wild_pkm.Instance.participant.pokemonImage.rectTransform.sizeDelta = new Vector2(0,0);
        yield return new WaitForSeconds(0.5f);
    }
    public IEnumerator DisplayPokemonCatch()
    {
        playerBattleAnimator.gameObject.SetActive(true);
        pokeballImage.SetActive(true);
        playerBattleAnimator.Play("pokeball successful");
        yield return new WaitForSeconds(1.2f);
        pokeballImage.SetActive(false);
    }
    public IEnumerator DisplayPokeballEscape()
    {
        playerBattleAnimator.Play("pokeball escape");
        yield return new WaitForSeconds(1f);
        Wild_pkm.Instance.participant.pokemonImage.rectTransform.sizeDelta = Battle_handler.Instance.battleParticipants[0].pokemonImage.rectTransform.sizeDelta;
        var pkmImageRect = Battle_handler.Instance.battleParticipants[0].pokemonImage.rectTransform;
        var target = new Vector2(pkmImageRect.anchoredPosition.x, pkmImageRect.anchoredPosition.y+pkmImageRect.rect.height);
        yield return StartCoroutine(SlideRect(pkmImageRect, pkmImageRect.anchoredPosition, target, 300f));
        pokeballImage.SetActive(false);
    }
    public IEnumerator DisplayPokeballShake()
    {
        playerBattleAnimator.Play("pokeball shake");
        yield return new WaitForSeconds(0.8f);
    }
    public IEnumerator DisplayPokemonRelease()
    {
        pokeballImage.SetActive(true);
        playerBattleAnimator.gameObject.SetActive(true);
        playerBattleAnimator.Play("pokemon release");
        yield return new WaitForSeconds(1.5f);
        pokeballImage.SetActive(false);
    }

    private void ResetAfterBattle()
    {
        pokeballImage.SetActive(false);
        Battle_handler.Instance.battleParticipants[2].pokemonImage.rectTransform.sizeDelta 
            = Battle_handler.Instance.battleParticipants[0].pokemonImage.rectTransform.sizeDelta;//After wild battle, reset image size in-case of pokeball interaction
    }
}
