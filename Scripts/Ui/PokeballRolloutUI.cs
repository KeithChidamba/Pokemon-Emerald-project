using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class PokeballRolloutUI : MonoBehaviour,IInjectable
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
    
    private BattleVisuals _battleVisualsHandler;
    private Pokemon_party _pokemonPartyHandler;
    private BattleIntro _battleIntroHandler;
    private Battle_handler _battleHandler;

    public void Inject(ServiceContainer container)
    {
        _battleVisualsHandler = container.Resolve<BattleVisuals>();
        _pokemonPartyHandler = container.Resolve<Pokemon_party>();
        _battleIntroHandler = container.Resolve<BattleIntro>();
        _battleHandler = container.Resolve<Battle_handler>();
    }
    public void OnInject()
    {
        _battleHandler.OnBattleEnd += ResetPokeballs;
        _rectTransform = GetComponent<RectTransform>();
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
        var distance = isPlayerPokeballs ? -_battleVisualsHandler.outOfViewDistance : _battleVisualsHandler.outOfViewDistance;
        var target = new Vector2(_rectTransform.anchoredPosition.x + distance, _rectTransform.anchoredPosition.y);
        yield return _battleVisualsHandler.SlideRect(_rectTransform, _rectTransform.anchoredPosition, target , 600f);
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
            yield return _battleVisualsHandler.SlideRect(pokeballs[i], pokeballs[i].anchoredPosition, pokeballPos,
                pokeballMoveSpeed);
        }

        _finishedDisplaying = true;
        yield return null;
    }

    private void SetPokeballImage(Image pokeballImage, int pokeballIndex)
    {
        if (isPlayerPokeballs)
        {
            if( pokeballIndex < _pokemonPartyHandler.numMembers)
            {
                pokeballImage.sprite = fullPokeballSlot;
                pokeballImage.color =
                    _pokemonPartyHandler.party[pokeballIndex].hp > 0 ? Color.white : new Color32(129, 129, 129,255);
            }
            else
            {
                pokeballImage.sprite = emptyPokeballSlot;
            }
        }
        else
        {
            if (_battleHandler.currentBattleType == BattleType.Double)
            {
                //cross this bridge when we get there
            }
            else
            {
                var partyCount = _battleHandler.battleParticipants[2].pokemonTrainerAI.trainerParty.Count;
                if (pokeballIndex < partyCount)
                {
                    pokeballImage.sprite = fullPokeballSlot;
                    pokeballImage.color = _battleHandler.battleParticipants[2].pokemonTrainerAI.trainerParty[pokeballIndex].hp>0?
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
        _battleIntroHandler.SlideOutOfView(_rectTransform, isPlayerPokeballs ? _battleVisualsHandler.outOfViewDistance : -_battleVisualsHandler.outOfViewDistance);
    }

    private void ResetPokeballs()
    {
        for (var i = 0; i < pokeballs.Count; i++)
        {
            pokeballs[i].anchoredPosition = startPos.anchoredPosition;
        }
        gameObject.SetActive(false);
        _battleIntroHandler.SlideOutOfView(_rectTransform, isPlayerPokeballs ? -_battleVisualsHandler.outOfViewDistance :_battleVisualsHandler.outOfViewDistance);
    }
}
