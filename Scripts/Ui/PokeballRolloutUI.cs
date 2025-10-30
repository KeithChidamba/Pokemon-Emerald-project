using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.UI;
using Vector2 = UnityEngine.Vector2;

public class PokeballRolloutUI : MonoBehaviour
{
    public List<RectTransform> pokeballs;
    public RectTransform startPos;
    public float pokeballDistanceApart;
    public float pokeballMoveSpeed;
    private bool _finishedDisplaying;
    private RectTransform _rectTransform;
    public bool isPlayerPokeballs;
    public Sprite emptyPokeballSlot;
    public Sprite fullPokeballSlot;
    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        Battle_handler.Instance.OnBattleEnd += ResetPokeballs;
    }

    public IEnumerator ShowPokeballs()
    {
        gameObject.SetActive(true);
        for (var i = 0; i < pokeballs.Count; i++)
        {
            var pokeballImage = pokeballs[i].GetComponent<Image>();
            SetPokeballImage(pokeballImage, i);
            yield return null;
        }
        var distance = isPlayerPokeballs ? -500f : 500f;
        var target = new Vector2(_rectTransform.anchoredPosition.x + distance, _rectTransform.anchoredPosition.y);
        yield return BattleIntro.Instance.SlideRect(_rectTransform, _rectTransform.anchoredPosition, target , 600f);
    }
    public IEnumerator LoadPokeballs()
    {
        gameObject.SetActive(true);
        
        _finishedDisplaying = false;
        for (var i = 0; i < pokeballs.Count; i++)
        {
            var pokeballImage = pokeballs[i].GetComponent<Image>();

            SetPokeballImage(pokeballImage, i);
            pokeballs[i].anchoredPosition =
                new Vector2(startPos.anchoredPosition.x + (i * pokeballDistanceApart * 0.75f),
                    startPos.anchoredPosition.y);
            var pokeballPos = new Vector2(startPos.anchoredPosition.x + (i * pokeballDistanceApart),
                startPos.anchoredPosition.y);
            yield return BattleIntro.Instance.SlideRect(pokeballs[i], pokeballs[i].anchoredPosition, pokeballPos,
                pokeballMoveSpeed);
        }

        _finishedDisplaying = true;
        yield return null;
    }

    private void SetPokeballImage(Image pokeballImage, int pokeballIndex)
    {
        if (isPlayerPokeballs)
        {
            if( pokeballIndex < Pokemon_party.Instance.numMembers)
            {
                pokeballImage.sprite = fullPokeballSlot;
                pokeballImage.color =
                    Pokemon_party.Instance.party[pokeballIndex].hp > 0 ? Color.white : new Color32(129, 129, 129,255);
            }
            else
            {
                pokeballImage.sprite = emptyPokeballSlot;
            }
        }
        else
        {
            if (Battle_handler.Instance.currentBattleType == TrainerData.BattleType.Double)
            {
                //cross this bridge when we get there
            }
            else
            {
                var partyCount = Battle_handler.Instance.battleParticipants[2].pokemonTrainerAI.trainerParty.Count;
                if (pokeballIndex < partyCount)
                {
                    pokeballImage.sprite = fullPokeballSlot;
                    pokeballImage.color = Battle_handler.Instance.battleParticipants[2].pokemonTrainerAI.trainerParty[pokeballIndex].hp>0?
                        Color.white: new Color32(129, 129, 129,255);
                }
                else
                {
                    pokeballImage.sprite = emptyPokeballSlot;
                }
            }
        }
    }
    public IEnumerator HidePokeballs()
    {
        yield return new WaitUntil(() => _finishedDisplaying);
        BattleIntro.Instance.SlideOutOfView(_rectTransform, isPlayerPokeballs ? 500f : -500f);
    }

    private void ResetPokeballs()
    {
        for (var i = 0; i < pokeballs.Count; i++)
        {
            pokeballs[i].anchoredPosition = startPos.anchoredPosition;
        }
        gameObject.SetActive(false);
        BattleIntro.Instance.SlideOutOfView(_rectTransform, isPlayerPokeballs ? -500f : 500f);
    }
}
