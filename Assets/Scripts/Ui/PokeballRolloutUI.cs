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
    public Sprite healthyPokeballSlot;
    public Sprite afflictedPokeballSlot;
    public Sprite faintedPokeballSlot;
    private Vector2 _defaultPosition;
    
    private BattleVisuals _battleVisualsHandler;
    private PokemonPartyHandler _pokemonPartyHandler;
    private BattleIntro _battleIntroHandler;
    private BattleHandler _battleHandler;

    public void Inject(ServiceContainer container)
    {
        _battleVisualsHandler = container.Resolve<BattleVisuals>();
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
        _battleIntroHandler = container.Resolve<BattleIntro>();
        _battleHandler = container.Resolve<BattleHandler>();
    }
    public void OnInject()
    {
        _rectTransform = GetComponent<RectTransform>();
        _defaultPosition = _rectTransform.anchoredPosition;
        _battleHandler.OnBattleEnd += ResetPokeballs;
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
        yield return BattleVisuals.SlideRect(_rectTransform, _rectTransform.anchoredPosition, target , 600f);
    }
    public IEnumerator LoadPokeballs()//initial load when battle starts
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
            yield return BattleVisuals.SlideRect(pokeballs[i], pokeballs[i].anchoredPosition, pokeballPos,
                pokeballMoveSpeed);
        }

        _finishedDisplaying = true;
        yield return null;
    }

    private void SetPokeballImage(Image pokeballImage, int pokeballIndex)
    {
        if (isPlayerPokeballs)
        {
            if (pokeballIndex < _pokemonPartyHandler.Party.Count)
            {
                var pokemon = _pokemonPartyHandler.Party[pokeballIndex];
                pokeballImage.sprite = DeterminePokeballImage(pokemon);
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
                var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
                var partyCount = enemy.pokemonTrainerAI.TrainerParty.Count;
                if (pokeballIndex < partyCount)
                {
                    var enemyPokemon = enemy.pokemonTrainerAI
                        .TrainerParty[pokeballIndex];
                    pokeballImage.sprite = DeterminePokeballImage(enemyPokemon);
                }
                else
                {
                    pokeballImage.sprite = emptyPokeballSlot;
                }
            }
        }
    }
    private Sprite DeterminePokeballImage(Pokemon pokemon)
    {
        if (pokemon.hp == 0)
        {
            return faintedPokeballSlot;
        }
        if (pokemon.statusEffect != StatusEffect.None)
        {
            return afflictedPokeballSlot;
        }
        return healthyPokeballSlot;
    }
    public IEnumerator HidePokeballs()
    {
        yield return new WaitUntil(() => _finishedDisplaying);
        _battleIntroHandler.SlideOutOfView(_rectTransform, isPlayerPokeballs ? _battleVisualsHandler.outOfViewDistance : -_battleVisualsHandler.outOfViewDistance);
    }

    private void ResetPokeballs()
    {
        foreach (var pokeball in  pokeballs)
        {
            pokeball.anchoredPosition = startPos.anchoredPosition;
        }
        gameObject.SetActive(false);
        _rectTransform.anchoredPosition = _defaultPosition;
    }
}
