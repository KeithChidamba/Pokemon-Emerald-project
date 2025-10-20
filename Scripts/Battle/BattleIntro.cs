using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.WSA;

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
    public Sprite[] introTerrains;
    public Image terrainParallaxImage;
    public Image[] participantIntroImages;
    public Sprite playerSprite;
    
    [Header("Animation Settings")]
    public float blackPanelsSpeed = 300f;
    public float parallaxDistance = 700f;
    public float parallaxDuration = 5f;
    public float platformSlideSpeed = 700f;

    private Vector2 topBlackStart, bottomBlackStart;
    private Vector2 topBlackTarget, bottomBlackTarget;
    private Vector2 redBoxStart, redBoxTarget, leftPlatformTarget,rightPlatformTarget;
    private Vector2 leftPlatformStart, rightPlatformStart;
    public Animator playerAnimator;
    public PokeballRolloutUI enemyPokeballs;
    public PokeballRolloutUI playerPokeballs;
    
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
        StartCoroutine(SlideRect(rectTransform,
            startPos, target , platformSlideSpeed*5));
    }
    private IEnumerator MainIntroSequence(Encounter_Area battleArea)
    {
        StartCoroutine(MovePanelsApart());
        dialogueBox.gameObject.SetActive(true);
        
        if(!Battle_handler.Instance.isTrainerBattle)
        {
            IEnumerator DropParallax(float delay)
            {
                yield return new WaitForSeconds(delay);
                var droppedPosition = parallaxObject.anchoredPosition + Vector2.down * parallaxObject.rect.height;
                StartCoroutine(SlideRect(parallaxObject, parallaxObject.anchoredPosition, droppedPosition, 2f));
                parallaxObject.gameObject.SetActive(false);
            }

            terrainParallaxImage.sprite =
                introTerrains[Array.IndexOf(introTerrainBiomes, battleArea.biome)];

            parallaxObject.gameObject.SetActive(true);

            StartCoroutine(ParallaxMove(parallaxObject, parallaxDistance, parallaxDuration));

            StartCoroutine(DropParallax(parallaxDuration));
        }
        
        leftPlatform.gameObject.SetActive(true);
        rightPlatform.gameObject.SetActive(true);
        // 5️⃣ Platforms slide from opposite sides to center
        StartCoroutine(SlideRect(leftPlatform, leftPlatformStart, leftPlatformTarget, platformSlideSpeed));
        yield return StartCoroutine(SlideRect(rightPlatform, rightPlatformStart, rightPlatformTarget, platformSlideSpeed*0.96f));
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
        
        Dialogue_handler.Instance.DisplayBattleInfo(
            $"A wild {Wild_pkm.Instance.participant.pokemon.pokemonName} has appeared");
        //go pokemon
        //pokeball throw
        //show ui
    }
    public IEnumerator PlayTrainerIntroSequence(Encounter_Area battleArea)
    {
        string message = "";
        var challengers = new List<string>();
        var participants = Battle_handler.Instance.battleParticipants;

        for (var i=0;i<4;i++)
        {
            if (!participants[i].isActive)
            {
                participantIntroImages[i].gameObject.SetActive(false);
                continue;
            }
            
            participantIntroImages[i].gameObject.SetActive(true);
            if (participants[i].isEnemy)
            {
                participantIntroImages[i].sprite = participants[i].pokemonTrainerAI.trainerData.battleIntroSprite;
                challengers.Add(participants[i].pokemonTrainerAI.trainerData.TrainerName);
            }
            else
                participantIntroImages[i].sprite = playerSprite;
        }

        message = challengers.Count > 1? 
                $"{challengers[0]} and {challengers[1]} challenge you to a battle"
                :    $"{challengers[0]} challenges you to a battle";
        
        yield return MainIntroSequence(battleArea);
        
        Dialogue_handler.Instance.DisplayBattleInfo(message);
        yield return new WaitUntil(()=>!Dialogue_handler.Instance.messagesLoading);
        //show pokeballs
        enemyPokeballs.gameObject.SetActive(true);
        playerPokeballs.gameObject.SetActive(true);
        
        StartCoroutine(enemyPokeballs.LoadPokeballs());
        yield return playerPokeballs.LoadPokeballs();
        
        StartCoroutine(enemyPokeballs.HidePokeballs());
        for (var i=0;i<challengers.Count;i++)
        {
            Dialogue_handler.Instance.DisplayBattleInfo($"{challengers[i]} sent out {participants[i+2].pokemon.pokemonName}!");
            participants[i+2].participantUI.SetActive(true);
            yield return new WaitUntil(()=>!Dialogue_handler.Instance.messagesLoading);
            SlideOutOfView(participantIntroImages[i+2].rectTransform,2000f);
        }
        
        message = Battle_handler.Instance.isDoubleBattle
            ? $"{Game_Load.Instance.playerData.playerName} sent out {participants[0].pokemon.pokemonName}!" +
              $" and {participants[1].pokemon.pokemonName}!"
            : $"{Game_Load.Instance.playerData.playerName} sent out {participants[0].pokemon.pokemonName}!";
        
        Dialogue_handler.Instance.DisplayBattleInfo(message);
        playerAnimator.Play("pokeball throw");
        StartCoroutine(playerPokeballs.HidePokeballs());
        yield return new WaitForSeconds(2f);
        for (var i=0;i<2;i++)
        {
            if (!participants[i].isActive) continue;
            participants[i].participantUI.SetActive(true);
        }
        yield return new WaitUntil(()=>!Dialogue_handler.Instance.messagesLoading);
       
        for (var i = 0; i < 4; i++)
        {
            participantIntroImages[i].gameObject.SetActive(false);
        }
        
    }

    private IEnumerator MovePanelsApart()
    {
        topBlackPanel.gameObject.SetActive(true);
        bottomBlackPanel.gameObject.SetActive(true);
        while (Vector2.Distance(topBlackPanel.anchoredPosition, topBlackTarget) > 0.5f)
        {
            topBlackPanel.anchoredPosition = Vector2.MoveTowards(topBlackPanel.anchoredPosition, topBlackTarget, blackPanelsSpeed * Time.deltaTime);
            bottomBlackPanel.anchoredPosition = Vector2.MoveTowards(bottomBlackPanel.anchoredPosition, bottomBlackTarget, blackPanelsSpeed * Time.deltaTime);
            yield return null;
        }
        bottomBlackPanel.gameObject.SetActive(false);
        topBlackPanel.gameObject.SetActive(false);
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
