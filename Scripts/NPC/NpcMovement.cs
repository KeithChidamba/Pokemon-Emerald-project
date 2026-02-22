using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class NpcMovement : MonoBehaviour
{
    public NpcAnimationData animationData;
    [SerializeField] private NpcLogic logicHandler;
    [SerializeField] private Transform movePoint;
    public float movementSpeed;
    [SerializeField] private LayerMask movementBlockers;
    [SerializeField] private int currentAnimationIndex;
    private NpcMovementDirection _currentMovement;
    private NpcSpriteData _currentSpriteData;
    [SerializeField]private SpriteRenderer bodySpriteRenderer;
    [SerializeField]private SpriteRenderer headSpriteRenderer;
    private int _currentSpriteIndex;
    [SerializeField]private int currentStepCount;
    [SerializeField]private bool moving;
    [SerializeField]private bool canMove;
    private Coroutine animationRoutine;
    private WaitForSeconds movePause = new (1f);
    private WaitForSeconds animDelay = new (0.25f);
    
    [SerializeField]private BoxCollider2D interactionCollider;
    public event Action<Vector2> OnMovementPaused;

    private void AdjustColliderSize()
    {
        var size = interactionCollider.size;
        var vertSize = new Vector2(.75f, size.y);
        var horSize = new Vector2(.5f, size.y);
        switch (_currentMovement.direction)
        {
            case MovementDirection.Down:
            case MovementDirection.Up:
                interactionCollider.size=vertSize; break;
            case MovementDirection.Left:
            case MovementDirection.Right:
                interactionCollider.size=horSize; break;
        }
    }

    public MovementDirection GetCurrentDirection()
    {
        return _currentMovement.direction;
    }

    private void SetSprites(Sprite newSprite)
    {
        headSpriteRenderer.sprite = newSprite;
        bodySpriteRenderer.sprite = newSprite;
    }
    public void FacePlayerDirection()
    {
        StopMovement();

        var playerDirection = (int)Player_movement.Instance.currentDirection;
        
        var directionConversions = new []{MovementDirection.Up,MovementDirection.Down
            ,MovementDirection.Right,MovementDirection.Left};

        var oppositeDirection = directionConversions[playerDirection-1];
        
        _currentSpriteData = animationData.spriteData.GetSpriteData(oppositeDirection);
        
        SetSprites(_currentSpriteData.idleSprite);
    }
    public static Vector3 SnapToGrid(Vector3 pos)
    {
        return new Vector3(
            Mathf.Round(pos.x),
            Mathf.Round(pos.y),
            0f
        );
    }
    public void StopMovement()
    {
        StopAllCoroutines();
        canMove = false;
        moving = false;
        
        Vector3 snapped = Vector3.Distance(transform.position, movePoint.position) < 0.05f
            ? movePoint.position
            : SnapToGrid(transform.position);
        
        movePoint.position = snapped;
        transform.position = snapped;
    }
    private void OnDisable()
    {
        if (animationData.isIdle) return;
        
        StopMovement();
        SetSprites(_currentSpriteData.idleSprite);
    }

    private void OnEnable()
    {
        SwitchMove();
        if (animationData.isIdle)
        {
            SetSprites(_currentSpriteData.idleSprite);
            canMove = false;
            moving = false;
        }
        else
        {
            canMove = true;
            StartCoroutine(MovementLoop());
        }
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
        SetSprites(_currentSpriteData.spritesForDirection[_currentSpriteIndex]);
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
            SetSprites(_currentSpriteData.idleSprite);
            
            OnMovementPaused?.Invoke(GetDirectionAsVector());    
        

            // Pause before next move
            yield return movePause;

            // Switch animation direction
            SwitchMove();
        }
    }

    public Vector2 GetDirectionAsVector()
    {
        switch (_currentMovement.direction)
        {
            case MovementDirection.Up:
                return Vector2.up;
            case MovementDirection.Down:
                return Vector2.down;
            case MovementDirection.Left:
                return Vector2.left;
        }
        return Vector2.right;
    }
    private bool TrySetNextMove()
    {
        AdjustColliderSize();
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
            
            Debug.Log(_currentMovement.direction);
            var hit = Physics2D.Raycast(
                logicHandler.rayCastPoint.position,GetDirectionAsVector(),
                1f,movementBlockers
            );
            
            Debug.DrawRay(
                logicHandler.rayCastPoint.position,
                GetDirectionAsVector() * 1f,
                Color.red
            );
            
            if (hit.transform)
            {
               Debug.Log(hit.transform.name);
                break;
            }
            
            lastValidPos = SnapToGrid(checkPos);
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

