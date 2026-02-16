using System.Collections;
using UnityEngine;

public class NpcMovement : MonoBehaviour
{
    public NpcAnimationData animationData;
    [SerializeField] private Transform movePoint;
    public float movementSpeed;
    [SerializeField] private LayerMask movementBlockers;
    [SerializeField] private int currentAnimationIndex;
    private NpcSpriteData _currentAnimation;
    [SerializeField]private SpriteRenderer spriteRenderer;
    private int _currentSpriteIndex;

    private bool moving;

    private Coroutine movementRoutine;
    private Coroutine animationRoutine;

    private static readonly WaitForSeconds movePause = new (1f);
    private static readonly WaitForSeconds animDelay = new (0.25f);
    
    private void Start()
    {
        movementBlockers = 1 << LayerMask.NameToLayer("Movement blockers");
    }


private void Awake()
{
    SwitchMove();
    movementRoutine = StartCoroutine(MovementLoop());
}

private IEnumerator Animate()
{
    while (moving)
    {
        ChangeSprite();
        yield return animDelay;
    }
}

private void ChangeSprite()
{
    _currentSpriteIndex++;
    if (_currentSpriteIndex >= _currentAnimation.spritesForDirection.Length)
        _currentSpriteIndex = 0;
    spriteRenderer.sprite = _currentAnimation.spritesForDirection[_currentSpriteIndex];
}

private IEnumerator MovementLoop()
{
    while (true)
    {
        // Try to set next move
        if (!TrySetNextMove())
        {
            yield return movePause;
            continue;
        }

        moving = true;

        _currentSpriteIndex = 0;

        animationRoutine = StartCoroutine(Animate());

        // Move to target tile
        while (Vector3.Distance(transform.position, movePoint.position) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                movePoint.position,
                movementSpeed * Time.deltaTime
            );
            yield return null;
        }

        // Snap to grid
        transform.position = movePoint.position;

        moving = false;

        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        // Reset sprite
        spriteRenderer.sprite = _currentAnimation.spritesForDirection[0];

        // Pause before next move
        yield return movePause;

        // Switch animation direction
        SwitchMove();
    }
}

private bool TrySetNextMove()
{
    Vector3 direction;

    if (animationData.IsVerticalMovement(_currentAnimation))
    {
        direction = new Vector3(
            0,
            animationData.GetDirectionAsMagnitude(_currentAnimation),
            0
        );
    }
    else
    {
        direction = new Vector3(
            animationData.GetDirectionAsMagnitude(_currentAnimation),
            0,
            0
        );
    }

    Vector3 targetPos = movePoint.position + direction;

    if (Physics2D.OverlapCircle(targetPos, 0.12f, movementBlockers))
        return false;

    movePoint.position = targetPos;
    return true;
}

private void SwitchMove()
{
    currentAnimationIndex++;

    if (currentAnimationIndex >= animationData.spriteData.Count)
        currentAnimationIndex = 0;

    _currentAnimation = animationData.spriteData[currentAnimationIndex];
}

}

