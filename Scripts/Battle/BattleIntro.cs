using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class BattleIntro : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform topBlackPanel;
    public RectTransform bottomBlackPanel;
    public RectTransform dialogueBox;
    public RectTransform parallaxObject;
    public RectTransform leftPlatform;
    public RectTransform rightPlatform;
    [SerializeField]private Encounter_Area.Biome[] introTerrainBiomes;
    public Sprite[] terrainForPlatforms;
    public Image playerPlatform;
    public Image enemyPlatform;
    public Sprite[] introTerrains;
    public Image terrainParallaxImage;
    public Image[] participantIntroImages;
    public Sprite playerSprite;
    
    [Header("Animation Settings")]
    public float blackPanelsSpeed = 300f;
    public float parallaxDistance = 700f;
    public float parallaxDuration = 5f;
    public float platformSlideSpeed = 700f;
    private List<Vector2> _defaultParticipantImagePositions = new ();
    private Vector2 topBlackStart, bottomBlackStart;
    private Vector2 topBlackTarget, bottomBlackTarget;
    private Vector2 redBoxStart, redBoxTarget, leftPlatformTarget,rightPlatformTarget;
    private Vector2 leftPlatformStart, rightPlatformStart;
    private Vector2 terrainParallaxStart;

    public PokeballRolloutUI enemyPokeballs;
    public PokeballRolloutUI playerPokeballs;
    public List<BattlePokeball> thrownPokeballs;
    private List<string> challengers = new ();
    public static BattleIntro Instance;
    
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
        introTerrainBiomes = new[]
        {
            Encounter_Area.Biome.Desert, Encounter_Area.Biome.Ocean,Encounter_Area.Biome.UnderWater,
            Encounter_Area.Biome.OpenField,Encounter_Area.Biome.TallGrass,Encounter_Area.Biome.Mountain
        };
        
        // Store initial positions
        topBlackStart = GetAnchoredFromWorld(topBlackPanel);
        bottomBlackStart = GetAnchoredFromWorld(bottomBlackPanel);
        
        leftPlatformStart = leftPlatform.anchoredPosition; 
        leftPlatform.anchoredPosition = new Vector2(leftPlatformStart.x-leftPlatform.rect.width,leftPlatformStart.y);
        leftPlatformStart = leftPlatform.anchoredPosition;
        
        rightPlatformStart = rightPlatform.anchoredPosition; 
        rightPlatform.anchoredPosition = new Vector2(rightPlatformStart.x+rightPlatform.rect.width,rightPlatformStart.y);
        rightPlatformStart = rightPlatform.anchoredPosition;

        topBlackTarget = topBlackStart + Vector2.up * topBlackPanel.rect.height;
        bottomBlackTarget = bottomBlackStart + Vector2.down * bottomBlackPanel.rect.height;
        
        rightPlatformTarget = rightPlatformStart + Vector2.left * rightPlatform.rect.width;
        leftPlatformTarget = leftPlatformStart + Vector2.right * leftPlatform.rect.width;
        terrainParallaxStart = terrainParallaxImage.rectTransform.anchoredPosition;
    }

    public void SetPlatformSprite(Encounter_Area battleArea)
    {
        playerPlatform.sprite = terrainForPlatforms[Array.IndexOf(introTerrainBiomes, battleArea.biome)];
        enemyPlatform.sprite = terrainForPlatforms[Array.IndexOf(introTerrainBiomes, battleArea.biome)];
    }
    private Vector2 GetAnchoredFromWorld(RectTransform rect)
    {
        RectTransform parent = rect.parent as RectTransform;

        // Convert world position to local position relative to parent
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent, 
            RectTransformUtility.WorldToScreenPoint(null, rect.position), 
            null, 
            out localPoint
        );

        return localPoint;
    }
    
    public void SlideOutOfView(RectTransform rectTransform,float distance)
    {
        var startPos = rectTransform.anchoredPosition;
        var target = new Vector2(rectTransform.anchoredPosition.x + distance, rectTransform.anchoredPosition.y);
         StartCoroutine(BattleVisuals.Instance.SlideRect(rectTransform, startPos, target , platformSlideSpeed*5));
    }
    private IEnumerator MainIntroSequence(Encounter_Area battleArea)
    {
        _defaultParticipantImagePositions.Clear();
        for (var i=0;i<4;i++)
        {
            _defaultParticipantImagePositions.Add(participantIntroImages[i].rectTransform.anchoredPosition);
        }
        
        dialogueBox.gameObject.SetActive(true);
        Dialogue_handler.Instance.battleDialogueBox.gameObject.SetActive(false);
        StartCoroutine(MovePanelsApart());
        if(!Battle_handler.Instance.isTrainerBattle)
        {
            IEnumerator DropParallax(float delay)
            {
                yield return new WaitForSeconds(delay);
                var droppedPosition = new Vector2(parallaxObject.anchoredPosition.x + 100f,parallaxObject.anchoredPosition.y-parallaxObject.rect.height);
                yield return BattleVisuals.Instance.SlideRect(parallaxObject, parallaxObject.anchoredPosition, droppedPosition, 200f);
                parallaxObject.gameObject.SetActive(false);
            }
            terrainParallaxImage.rectTransform.anchoredPosition = terrainParallaxStart;
            
            terrainParallaxImage.sprite = introTerrains[Array.IndexOf(introTerrainBiomes, battleArea.biome)];

            parallaxObject.gameObject.SetActive(true);

            StartCoroutine(ParallaxMove(parallaxObject, parallaxDistance, parallaxDuration));

            StartCoroutine(DropParallax(parallaxDuration));
        }
        
        leftPlatform.gameObject.SetActive(true);
        rightPlatform.gameObject.SetActive(true);
        // 5️⃣ Platforms slide from opposite sides to center
        StartCoroutine(BattleVisuals.Instance.SlideRect(leftPlatform, leftPlatformStart, leftPlatformTarget, platformSlideSpeed));
        yield return StartCoroutine(BattleVisuals.Instance.SlideRect(rightPlatform, rightPlatformStart, rightPlatformTarget, platformSlideSpeed*0.96f));
    }
    public IEnumerator PlayWildIntroSequence(Encounter_Area battleArea)
    {
        Dialogue_handler.Instance.DisplayBattleInfo("");
        var participants = Battle_handler.Instance.battleParticipants;

        for (var i=0;i<4;i++)
        {
            if (!participants[i].isActive)
            {
                participantIntroImages[i].gameObject.SetActive(false);
                continue;
            }
            
            participantIntroImages[i].gameObject.SetActive(true);
            participantIntroImages[i].sprite = participants[i].isEnemy?
                participants[i].pokemon.frontPicture: playerSprite;
        }

        yield return MainIntroSequence(battleArea);
        
        var wildParticipant = Wild_pkm.Instance.participant;
        Dialogue_handler.Instance.DisplayBattleInfo(
            $"A wild {wildParticipant.rawName} has appeared");
        
        wildParticipant.pokemonImage.gameObject.SetActive(false);
        var wildPokemonRect = wildParticipant.participantUI.GetComponent<RectTransform>();
        wildParticipant.participantUI.SetActive(true);
        var start = new Vector2(wildPokemonRect.anchoredPosition.x - 500, wildPokemonRect.anchoredPosition.y);
        yield return StartCoroutine(BattleVisuals.Instance.SlideRect(wildPokemonRect, start, wildPokemonRect.anchoredPosition, platformSlideSpeed*5));
        
        wildParticipant.pokemonImage.gameObject.SetActive(true);
        participantIntroImages[2].gameObject.SetActive(false); 
        
        StartCoroutine(PokemonIntroAnimation(wildParticipant));
        StartCoroutine(PokemonIntroAnimationMovement(wildParticipant));
        yield return new WaitForSeconds(3f);
        
        Dialogue_handler.Instance.DisplayBattleInfo( $"Go! {participants[0].pokemon.pokemonName}");
        StartCoroutine(BattleVisuals.Instance.DisplayPokemonRelease());
        yield return new WaitForSeconds(1.5f);
        
        for (var i=0;i<2;i++)
        {
            if (!participants[i].isActive) continue;
            StartCoroutine(PokemonIntroAnimationMovement(participants[i]));
            participants[i].participantUI.SetActive(true);//change to slide
        }
        participantIntroImages[0].gameObject.SetActive(false);
        participantIntroImages[0].sprite = playerSprite;
        participantIntroImages[0].rectTransform.anchoredPosition = _defaultParticipantImagePositions[0];
    }
    public IEnumerator PlayTrainerIntroSequence(Encounter_Area battleArea)
    {
        Dialogue_handler.Instance.DisplayBattleInfo("");
        string message = "";
        challengers.Clear();
        var participants = Battle_handler.Instance.battleParticipants;

        if (Battle_handler.Instance.currentBattleType == TrainerData.BattleType.SingleDouble)
        {
            participantIntroImages[2].sprite = participants[2].pokemonTrainerAI.trainerData.battleIntroSprite; 
            participantIntroImages[2].gameObject.SetActive(true);
            challengers.Add(participants[2].pokemonTrainerAI.trainerData.TrainerName);
            participantIntroImages[0].sprite = playerSprite;
            participantIntroImages[0].gameObject.SetActive(true);
        }
        else
        {
            for (var i=0;i<4;i++)
            {
                participantIntroImages[i].gameObject.SetActive(true);
                if (participants[i].isEnemy)
                {
                    participantIntroImages[i].sprite = participants[i].pokemonTrainerAI.trainerData.battleIntroSprite;
                    challengers.Add(participants[i].pokemonTrainerAI.trainerData.TrainerName);
                }
                else
                {
                    participantIntroImages[i].sprite = playerSprite;
                    
                }
            }
        }
        for (var i = 0; i < 4; i++)
        {
            if (!participants[i].isActive) participantIntroImages[i].gameObject.SetActive(false);
        }
        
        message = challengers.Count > 1? 
                $"{challengers[0]} and {challengers[1]} challenge you to a battle"
                :    $"{challengers[0]} challenges you to a battle";
        
        yield return MainIntroSequence(battleArea);
        
        Dialogue_handler.Instance.DisplayBattleInfo(message);
        yield return new WaitUntil(()=>!Dialogue_handler.Instance.messagesLoading);
        //show pokeballs
        StartCoroutine(enemyPokeballs.LoadPokeballs());
        yield return playerPokeballs.LoadPokeballs();
        
        StartCoroutine(enemyPokeballs.HidePokeballs());
        if (Battle_handler.Instance.currentBattleType != TrainerData.BattleType.SingleDouble)
        {
            for (var i = 0; i < challengers.Count; i++)
            {
                Dialogue_handler.Instance.DisplayBattleInfo(
                    $"{challengers[i]} sent out {participants[i + 2].pokemon.pokemonName}!");
                StartCoroutine(thrownPokeballs[i + 1].ThrowPokeball(true));
                yield return new WaitForSeconds(1f);
                SlideOutOfView(participantIntroImages[i + 2].rectTransform, 2000f);
                participants[i + 2].participantUI.SetActive(true);
                StartCoroutine(PokemonIntroAnimation(participants[i + 2]));
                StartCoroutine(PokemonIntroAnimationMovement(participants[i + 2]));
                yield return new WaitForSeconds(3f);
                yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
            }
        }
        else
        {
            Dialogue_handler.Instance.DisplayBattleInfo(
                $"{challengers[0]} sent out {participants[2].pokemon.pokemonName} and {participants[3].pokemon.pokemonName}!");

            StartCoroutine(thrownPokeballs[1].ThrowPokeball(true));
            StartCoroutine(thrownPokeballs[2].ThrowPokeball(true));
            SlideOutOfView(participantIntroImages[2].rectTransform, 2000f);
            yield return new WaitForSeconds(1f);
            StartCoroutine(PokemonIntroAnimation(participants[2]));
            StartCoroutine(PokemonIntroAnimationMovement(participants[2]));
            StartCoroutine(PokemonIntroAnimation(participants[3]));
            StartCoroutine(PokemonIntroAnimationMovement(participants[3]));
            participants[2].participantUI.SetActive(true);
            participants[3].participantUI.SetActive(true);
            yield return new WaitForSeconds(3f);
            yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        }
        
        message = Battle_handler.Instance.isDoubleBattle
            ? $"Go! {participants[0].pokemon.pokemonName}" +
              $" and {participants[1].pokemon.pokemonName}!"
            : $"Go! {participants[0].pokemon.pokemonName}!";
        
        Dialogue_handler.Instance.DisplayBattleInfo(message);

        StartCoroutine(BattleVisuals.Instance.DisplayPokemonRelease());
        if (Battle_handler.Instance.isDoubleBattle)
        {
            yield return new WaitForSeconds(0.5f);
            StartCoroutine(thrownPokeballs[0].ThrowPokeball(false));
        }
        StartCoroutine(playerPokeballs.HidePokeballs());
        yield return new WaitForSeconds(1.3f);
        for (var i=0;i<2;i++)
        {
            if (!participants[i].isActive) continue;
            StartCoroutine(PokemonIntroAnimationMovement(participants[i]));
            participants[i].participantUI.SetActive(true);
        }
        yield return new WaitUntil(()=>!Dialogue_handler.Instance.messagesLoading);
       
        for (var i = 0; i < 4; i++)
        {
            participantIntroImages[i].gameObject.SetActive(false);
            participantIntroImages[i].rectTransform.anchoredPosition = _defaultParticipantImagePositions[i];
        }
        participantIntroImages[0].sprite = playerSprite;
    }

    public IEnumerator ShowEnemiesAfterBattle()
    {
        var participants = Battle_handler.Instance.battleParticipants;
        for (var i = 0; i < challengers.Count; i++)
        {
            participants[i + 2].participantUI.SetActive(false);
            participantIntroImages[i + 2].gameObject.SetActive(true);
            var startPos = new Vector2(enemyPlatform.rectTransform.anchoredPosition.x-50,
                participantIntroImages[i + 2].rectTransform.anchoredPosition.y);
            var target = participantIntroImages[i + 2].rectTransform.anchoredPosition;
            
            yield return BattleVisuals.Instance.SlideRect(participantIntroImages[i + 2].rectTransform, startPos, target , platformSlideSpeed*5);
        }
    }

    public IEnumerator SwitchInPokemon(Battle_Participant swapParticipant, Pokemon newPokemon)
    {
        if(!swapParticipant.isPlayer)
        {
            yield return enemyPokeballs.ShowPokeballs();
            yield return new WaitForSeconds(0.5f);
            yield return enemyPokeballs.HidePokeballs();
        }
        
        swapParticipant.pokemonImage.rectTransform.sizeDelta = new Vector2(0,0);
        Battle_handler.Instance.SetParticipant(swapParticipant,newPokemon:newPokemon);

        if (swapParticipant.isEnemy)
        {
            yield return BattleVisuals.Instance.SendOutEnemyPokemon(swapParticipant);
            StartCoroutine(PokemonIntroAnimation(swapParticipant));
        }
        else
            yield return BattleVisuals.Instance.SendOutPlayerPokemon(swapParticipant);
    }


    private IEnumerator PokemonIntroAnimationMovement(Battle_Participant participant)
    {
        if (participant.pokemon.statusEffect == PokemonOperations.StatusEffect.Sleep)
        {
            yield break;
        }
        var rect = participant.pokemonImage.rectTransform;
        var movementSpeed = platformSlideSpeed * 0.4f;
        var startPos = rect.anchoredPosition;
        if(participant.isEnemy)
        {//LEFT AND RIGHT SLIDE
            var target = new Vector2(rect.anchoredPosition.x + 10f, rect.anchoredPosition.y);
            yield return StartCoroutine(BattleVisuals.Instance.SlideRect(rect,
                startPos, target, movementSpeed));
            target = new Vector2(startPos.x - 20f, rect.anchoredPosition.y);
            yield return StartCoroutine(BattleVisuals.Instance.SlideRect(rect, rect.anchoredPosition, target, movementSpeed));
            target = new Vector2(startPos.x, rect.anchoredPosition.y);
            yield return StartCoroutine(BattleVisuals.Instance.SlideRect(rect, rect.anchoredPosition, target, movementSpeed));
        }
        else
        {
            var xOffset = 12f; 
            var yOffset = 6f;
            //LEFT AND RIGHT BOUNCE
            var target = new Vector2(startPos.x - xOffset, startPos.y + yOffset);
            yield return StartCoroutine(BattleVisuals.Instance.SlideRect(rect, startPos, target, movementSpeed));
            
            target = new Vector2(startPos.x - xOffset*2, startPos.y - yOffset);
            yield return StartCoroutine(BattleVisuals.Instance.SlideRect(rect, rect.anchoredPosition, target, movementSpeed));

            target = new Vector2(startPos.x + xOffset, startPos.y + yOffset);
            yield return StartCoroutine(BattleVisuals.Instance.SlideRect(rect, rect.anchoredPosition, target, movementSpeed));
    
            target = new Vector2(startPos.x + xOffset*2, startPos.y - yOffset);
            yield return StartCoroutine(BattleVisuals.Instance.SlideRect(rect, rect.anchoredPosition, target, movementSpeed));
            //RETURN TO POSITION    
            yield return StartCoroutine(BattleVisuals.Instance.SlideRect(rect, rect.anchoredPosition, startPos, movementSpeed));
        }
    }
    private IEnumerator PokemonIntroAnimation(Battle_Participant participant)
    {
        if (participant.pokemon.statusEffect == PokemonOperations.StatusEffect.Sleep)
        {
            yield break;
        }
        yield return new WaitForSeconds(0.2f);
        participant.pokemonImage.sprite = participant.pokemon.battleIntroFrame;
        yield return new WaitForSeconds(0.45f);
        participant.pokemonImage.sprite = participant.pokemon.frontPicture;
        yield return new WaitForSeconds(0.45f);
        participant.pokemonImage.sprite = participant.pokemon.battleIntroFrame;
        yield return new WaitForSeconds(0.45f);
        participant.pokemonImage.sprite = participant.pokemon.frontPicture;
    }
    private IEnumerator MovePanelsApart()
    {
        topBlackPanel.gameObject.SetActive(true);
        bottomBlackPanel.gameObject.SetActive(true);
        topBlackTarget = topBlackPanel.anchoredPosition + Vector2.up * topBlackPanel.rect.height;
        bottomBlackTarget = bottomBlackPanel.anchoredPosition + Vector2.down * bottomBlackPanel.rect.height;
        while (Vector2.Distance(topBlackPanel.anchoredPosition, topBlackTarget) > 0.5f)
        {
            topBlackPanel.anchoredPosition = Vector2.MoveTowards(topBlackPanel.anchoredPosition, topBlackTarget, blackPanelsSpeed * Time.deltaTime);
            bottomBlackPanel.anchoredPosition = Vector2.MoveTowards(bottomBlackPanel.anchoredPosition, bottomBlackTarget, blackPanelsSpeed * Time.deltaTime);
            yield return null;
        }
        bottomBlackPanel.gameObject.SetActive(false);
        topBlackPanel.gameObject.SetActive(false);
        dialogueBox.gameObject.SetActive(false);
        Dialogue_handler.Instance.battleDialogueBox.gameObject.SetActive(true);
    }
    public IEnumerator BlackFade()
    {
        Dialogue_handler.Instance.DisplayBattleInfo("");
        bottomBlackPanel.gameObject.SetActive(true);
        topBlackPanel.gameObject.SetActive(true);
        dialogueBox.gameObject.SetActive(true);
        Dialogue_handler.Instance.battleDialogueBox.gameObject.SetActive(false);
        Battle_handler.Instance.optionsUI.SetActive(false);
        topBlackTarget = topBlackPanel.anchoredPosition + Vector2.down * topBlackPanel.rect.height;
        bottomBlackTarget = bottomBlackPanel.anchoredPosition + Vector2.up * bottomBlackPanel.rect.height;
        while (Vector2.Distance(topBlackPanel.anchoredPosition, topBlackTarget) > 0.5f)
        {
            topBlackPanel.anchoredPosition = Vector2.MoveTowards(topBlackPanel.anchoredPosition, topBlackTarget, blackPanelsSpeed * Time.deltaTime);
            bottomBlackPanel.anchoredPosition = Vector2.MoveTowards(bottomBlackPanel.anchoredPosition, bottomBlackTarget, blackPanelsSpeed * Time.deltaTime);
            yield return null;
        }
    }


    private IEnumerator ParallaxMove(RectTransform rect, float distance, float duration)
    {
        Vector2 startPos = rect.anchoredPosition;
        Vector2 endPos = startPos + Vector2.left * distance;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            rect.anchoredPosition = Vector2.Lerp(startPos, endPos, timer / duration);
            yield return null;
        }
        rect.anchoredPosition = endPos;
    }
}
