using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleIntro : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform topBlackPanel;
    public RectTransform bottomBlackPanel;
    public RectTransform redBox;
    public RectTransform parallaxObject;
    public RectTransform leftPlatform;
    public RectTransform rightPlatform;

    [Header("Animation Settings")]
    public float blackPanelsSpeed = 800f;
    public float redBoxSlideSpeed = 400f;
    public float parallaxDistance = 300f;
    public float parallaxDuration = 1.5f;
    public float platformSlideSpeed = 700f;

    private Vector2 topBlackStart, bottomBlackStart;
    private Vector2 topBlackTarget, bottomBlackTarget;
    private Vector2 redBoxStart, redBoxTarget;
    private Vector2 leftPlatformStart, rightPlatformStart, platformTarget;
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
        // Store initial positions
        topBlackStart = GetAnchoredFromWorld(topBlackPanel);
        bottomBlackStart = GetAnchoredFromWorld(bottomBlackPanel);
        redBoxStart = redBox.anchoredPosition; 
        redBox.anchoredPosition = new Vector2(redBoxStart.x,redBoxStart.y-redBox.rect.height);
        redBoxStart = redBox.anchoredPosition;
        leftPlatformStart = leftPlatform.anchoredPosition;
        rightPlatformStart = rightPlatform.anchoredPosition;

        // Targets (these assume centered layout)
        topBlackTarget = topBlackStart + Vector2.up * topBlackPanel.rect.height;
        bottomBlackTarget = bottomBlackStart + Vector2.down * bottomBlackPanel.rect.height;
        
        redBoxTarget = redBoxStart + Vector2.up * redBox.rect.height;
        platformTarget = Vector2.zero;
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
    public IEnumerator PlayIntroSequence()
    {
        // 1️⃣ Black panels separate to reveal UI
        yield return StartCoroutine(MovePanelsApart());

        // 3️⃣ Red box slides upward
        redBox.gameObject.SetActive(true);
        yield return StartCoroutine(SlideRect(redBox, redBoxStart, redBoxTarget, redBoxSlideSpeed));

        parallaxObject.gameObject.SetActive(true);
        // 4️⃣ Parallax object moves left for 1.5 seconds
        yield return StartCoroutine(ParallaxMove(parallaxObject, parallaxDistance, parallaxDuration));

        leftPlatform.gameObject.SetActive(true);
        rightPlatform.gameObject.SetActive(true);
        // 5️⃣ Platforms slide from opposite sides to center
        yield return StartCoroutine(SlideRect(leftPlatform, leftPlatformStart, platformTarget, platformSlideSpeed));
        yield return StartCoroutine(SlideRect(rightPlatform, rightPlatformStart, platformTarget, platformSlideSpeed));
    }

    private IEnumerator MovePanelsApart()
    {
        while (Vector2.Distance(topBlackPanel.anchoredPosition, topBlackTarget) > 0.5f)
        {
            topBlackPanel.anchoredPosition = Vector2.MoveTowards(topBlackPanel.anchoredPosition, topBlackTarget, blackPanelsSpeed * Time.deltaTime);
            bottomBlackPanel.anchoredPosition = Vector2.MoveTowards(bottomBlackPanel.anchoredPosition, bottomBlackTarget, blackPanelsSpeed * Time.deltaTime);
            yield return null;
        }
    }
    private IEnumerator SlideRect(RectTransform rect, Vector2 start, Vector2 target, float speed)
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
