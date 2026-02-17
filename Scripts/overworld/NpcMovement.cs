using System;
using System.Collections;
using UnityEngine;

public class NpcMovement : MonoBehaviour
{
    public NpcAnimationData animationData;
    [SerializeField] private Transform movePoint;
    public float movementSpeed;
    [SerializeField] private LayerMask movementBlockers;
    [SerializeField] private int currentAnimationIndex;
    private NpcMovementDirection _currentMovement;
    private NpcSpriteData _currentSpriteData;
    [SerializeField]private SpriteRenderer spriteRenderer;
    private int _currentSpriteIndex;
    [SerializeField]private int currentStepCount;
    [SerializeField]private bool moving;
    [SerializeField]private bool canMove;
    private Coroutine animationRoutine;

    private WaitForSeconds movePause = new (1f);
    private WaitForSeconds animDelay = new (0.25f);
    
    private void Start()
    {
        movementBlockers = 1 << LayerMask.NameToLayer("Movement blockers");
    }

private void OnDisable()
{
    canMove = false;
    moving = false;
    StopAllCoroutines();
}

private void OnEnable()
{
    canMove = true;
    SwitchMove();
    StartCoroutine(MovementLoop());
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
    if (_currentSpriteIndex >= _currentSpriteData.spritesForDirection.Length)
        _currentSpriteIndex = 0;
    spriteRenderer.sprite = _currentSpriteData.spritesForDirection[_currentSpriteIndex];
}

private IEnumerator MovementLoop()
{
    while (canMove)
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
        spriteRenderer.sprite = _currentSpriteData.idleSprite;

        // Pause before next move
        yield return movePause;

        // Switch animation direction
        SwitchMove();
    }
}

private bool TrySetNextMove()
{
    bool isVertical = animationData.IsVerticalMovement(_currentMovement.direction);
    int totalTiles = Mathf.Abs(_currentMovement.numTilesToTravel);
    var sign = Mathf.Sign(animationData.GetDirectionAsMagnitude(_currentMovement));

    Vector3 step = isVertical
        ? new Vector3(0, sign, 0)
        : new Vector3(sign, 0, 0);

    Vector3 lastValidPos = movePoint.position;

    for (int i = 1; i <= totalTiles; i++)
    {
        Vector3 checkPos = movePoint.position + step * i;

        if (Physics2D.OverlapCircle(checkPos, 0.12f, movementBlockers))
            break;

        lastValidPos = checkPos;
    }

    // No movement possible at all
    if (lastValidPos == movePoint.position)
        return false;
    
    movePoint.position = lastValidPos;
    return true;
}

private void SwitchMove()
{
    currentAnimationIndex++;

    if (currentAnimationIndex >= animationData.movementDirections.Count)
        currentAnimationIndex = 0;

    _currentMovement = animationData.movementDirections[currentAnimationIndex];
    _currentSpriteData = animationData.spriteData.GetSpriteData(_currentMovement.direction);
}

}

