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

    public IEnumerator LoadPokeballs()
    {
        gameObject.SetActive(true);

        _finishedDisplaying = false;
        for (var i = 0; i < pokeballs.Count; i++)
        {
            if (isPlayerPokeballs)
            {
                pokeballs[i].GetComponent<Image>().sprite =
                    i < Pokemon_party.Instance.numMembers ? fullPokeballSlot : emptyPokeballSlot;
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
                    pokeballs[i].GetComponent<Image>().sprite = i < partyCount ? fullPokeballSlot : emptyPokeballSlot;
                }
            }

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
        //hide pokeball display
    }

    public IEnumerator HidePokeballs()
    {
        yield return new WaitUntil(() => _finishedDisplaying);
        BattleIntro.Instance.SlideOutOfView(_rectTransform, isPlayerPokeballs ? 500f : -500f);
        yield return new WaitForSeconds(3f);
        gameObject.SetActive(false);
        BattleIntro.Instance.SlideOutOfView(_rectTransform, isPlayerPokeballs ? -500f : 500f);
        for (var i = 0; i < pokeballs.Count; i++)
        {
            pokeballs[i].anchoredPosition = startPos.anchoredPosition;
        }
    }
}
