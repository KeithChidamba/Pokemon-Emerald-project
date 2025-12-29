using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleVisuals : MonoBehaviour
{
    public static BattleVisuals Instance;
    public Animator playerBattleAnimator;
    public Animator swapOutAnimator;
    public GameObject pokeballImage;
    private Vector2 _defaultParticipantImageSize;
    private int[] _enemyPokeballXPositions={155,250,330};
    private List<(Image img,Vector2 pos)> _statChangeImages=new();
    public Sprite[] statChangeSprites;
    private Dictionary<Stat, Sprite> statChangeVisuals = new();
    private string _statChangeMessage;
    public float outOfViewDitance = 400f;
    public event Action OnStatVisualDisplayed;
    private List<Coroutine> _activeSlideCoroutines = new();
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
        _defaultParticipantImageSize =
            Battle_handler.Instance.battleParticipants[0].pokemonImage.rectTransform.sizeDelta;
        statChangeVisuals.Add(Stat.Attack,statChangeSprites[0]);
        statChangeVisuals.Add(Stat.Defense,statChangeSprites[1]);
        statChangeVisuals.Add(Stat.Speed,statChangeSprites[2]);
        statChangeVisuals.Add(Stat.SpecialAttack,statChangeSprites[3]);
        statChangeVisuals.Add(Stat.SpecialDefense,statChangeSprites[4]);
        statChangeVisuals.Add(Stat.Accuracy,statChangeSprites[5]);
        statChangeVisuals.Add(Stat.Evasion,statChangeSprites[6]);
        statChangeVisuals.Add(Stat.Multi,statChangeSprites[7]);
    }

    public void CancelBuffVisual()
    {
        OnStatVisualDisplayed?.Invoke();
    }
    public void SelectStatChangeVisuals(Stat statChanged,Battle_Participant participant,string message)
    {
        _statChangeMessage = message;
        if (statChanged == Stat.Crit)
        {
            CancelBuffVisual();
            return;
        }
        _statChangeImages.Clear();
        for(int i =0; i < 3;i++)
        {
            var visualImages = participant.pokemonImage.transform.GetChild(i).GetComponent<Image>();
            _statChangeImages.Add(new(visualImages,visualImages.rectTransform.anchoredPosition));
            _statChangeImages[i].img.sprite = statChangeVisuals[statChanged];
            _statChangeImages[i].img.gameObject.SetActive(true);
        }
        StartCoroutine(DisplayStatChangeVisuals());
    }
    
    private IEnumerator DisplayStatChangeVisuals()
    {
        float speed = 300f;
        for (int i = 0; i < _statChangeImages.Count; i++)
        {
            RectTransform rect = _statChangeImages[i].img.rectTransform;
            Vector2 basePos = _statChangeImages[i].pos;

            Vector2 topPos = basePos + Vector2.up * (0.5f * rect.rect.height);
            Vector2 target = basePos + Vector2.down * (0.5f * rect.rect.height);
            var c = StartCoroutine(SlideRect(rect, topPos, target, speed));
            _activeSlideCoroutines.Add(c);

            if ((i+1) % 2 == 0) yield return new WaitForSeconds(0.33f);
        }

        yield return new WaitForSeconds(0.5f);
        Dialogue_handler.Instance.DisplayBattleInfo(_statChangeMessage);
        
        foreach (var c in _activeSlideCoroutines)
            if (c != null) StopCoroutine(c);
        
        _activeSlideCoroutines.Clear();
        foreach (var image in _statChangeImages)
        {
            image.img.gameObject.SetActive(false);
            image.img.rectTransform.anchoredPosition = image.pos;
        }
        yield return null;
        yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        OnStatVisualDisplayed?.Invoke();
    }

    public IEnumerator DisplayConfusionVisuals(Battle_Participant participant)
    {
        participant.statusEffectAnimator.gameObject.SetActive(true);
        var rect = participant.statusEffectAnimator.GetComponent<RectTransform>();
        participant.pokemonImage.GetComponent<Canvas>().overrideSorting = true;
        
        var start = new Vector2(participant.pokemonImage.rectTransform.anchoredPosition.x - (participant.pokemonImage.rectTransform.rect.width*0.5f)
            , participant.pokemonImage.rectTransform.anchoredPosition.y+30f);
        participant.statusEffectAnimator.Play("Confusion"); 
        for (int i = 0; i < 2; i++)
        {
            var target = new Vector2(participant.pokemonImage.rectTransform.anchoredPosition.x + (participant.pokemonImage.rectTransform.rect.width*0.5f)
                , participant.pokemonImage.rectTransform.anchoredPosition.y+30f);
           if (i > 0)
           {
               yield return SlideRect(rect,target,start,200f);
           }
           else
           {
               yield return SlideRect(rect,start,target,200f);
           }
        }
        participant.pokemonImage.GetComponent<Canvas>().overrideSorting = false;
        participant.statusEffectAnimator.gameObject.SetActive(false);
    }

    public IEnumerator DisplayStatusEffectVisuals(Battle_Participant participant)
    {
        participant.statusEffectAnimator.gameObject.SetActive(true);
        var rect = participant.statusEffectAnimator.GetComponent<RectTransform>();
        
        if (participant.pokemon.statusEffect == StatusEffect.Sleep)
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
        if (participant.pokemon.statusEffect == StatusEffect.Paralysis)
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
    public IEnumerator DisplayDamageTakenVisual(Battle_Participant participant,DamageSource damageSource)
    {
        if(damageSource == DamageSource.Normal)
        {
            for (int i = 0; i < 4; i++)
            {
                yield return new WaitForSeconds(0.2f);
                participant.pokemonImage.color = new Color(0, 0, 0, 0);
                yield return new WaitForSeconds(0.2f);
                participant.pokemonImage.color = Color.white;
            }
        }
        if (damageSource == DamageSource.Poison)
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
        if (damageSource == DamageSource.Burn)
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
    {//wild battle is always single battle
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
        Wild_pkm.Instance.participant.pokemonImage.rectTransform.sizeDelta = _defaultParticipantImageSize;
        
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
        yield return new WaitForSeconds(1.6f);
        pokeballImage.SetActive(false);
    }
    public IEnumerator SendOutEnemyPokemon(Battle_Participant participant)
    {
        int positionIndex;
        int verticalPokeballPos = 300;
        if (Battle_handler.Instance.isDoubleBattle)
        {//magic numbers, im sorry.
            positionIndex = participant.GetPartnerIndex() > 2? 0:2;
        }
        else
        {
            positionIndex = 1;
        }
        swapOutAnimator.GetComponent<RectTransform>().anchoredPosition = new Vector2(_enemyPokeballXPositions[positionIndex],verticalPokeballPos);
        swapOutAnimator.Play("pokemon swap");
        yield return new WaitForSeconds(1f);
        participant.pokemonImage.rectTransform.sizeDelta = _defaultParticipantImageSize;
    }

    public IEnumerator RevealPokemon(Battle_Participant participant,bool withdraw=false)
    {
        var participantUIRect = participant.participantUI.GetComponent<RectTransform>(); 
        participant.participantUI.SetActive(true);
        
        if (participant.isEnemy) participant.pokemonImage.color = Color.white;
        var direction = participant.isEnemy?outOfViewDitance:-outOfViewDitance;
        if (withdraw && participant.isEnemy)
        {
            direction = 0;
        }
        participantUIRect.anchoredPosition = new Vector2(participantUIRect.anchoredPosition.x+direction,
            participantUIRect.anchoredPosition.y);
        yield return null;
    }

    public IEnumerator WithdrawPokemon(Battle_Participant participant)
    {
        var participantUIRect = participant.participantUI.GetComponent<RectTransform>(); 
        var direction = participant.isEnemy?-outOfViewDitance:outOfViewDitance;
        var targetForUI = new Vector2(participantUIRect.anchoredPosition.x+direction, participantUIRect.anchoredPosition.y);
        
        yield return SlideRect(participantUIRect,participantUIRect.anchoredPosition, targetForUI, 900f);
   
        if (participant.isEnemy)
        {
            yield return new WaitForSeconds(0.05f);
            participant.participantUI.SetActive(false);
            yield return new WaitForSeconds(0.25f);
            participant.pokemonImage.color = new Color(0, 0, 0, 0);
        }
    }
    public IEnumerator SendOutPlayerPokemon(Battle_Participant participant)
    {
        playerBattleAnimator.gameObject.SetActive(true);
        pokeballImage.SetActive(true);
        playerBattleAnimator.Play("pokemon release");
        yield return new WaitForSeconds(1.3f);
        participant.pokemonImage.rectTransform.sizeDelta = _defaultParticipantImageSize;
        yield return new WaitForSeconds(0.2f);
        playerBattleAnimator.gameObject.SetActive(false);
    }
    private void ResetAfterBattle()
    {
        pokeballImage.SetActive(false);
        //After wild battle, reset image size in-case of pokeball interaction
        Battle_handler.Instance.battleParticipants[2].pokemonImage.rectTransform.sizeDelta = _defaultParticipantImageSize;
    }
}
