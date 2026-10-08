using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Tilemaps;

public class NpcMovement : MonoBehaviour
{
    public NpcAnimationData animationData;
    [SerializeField] private Transform rayCastPoint;
    [SerializeField] private Transform movePoint;
    public int movementSpeed;
    [SerializeField] private LayerMask movementBlockers;
    [SerializeField] private int currentAnimationIndex;
    private NpcMovementDirection _currentMovement;
    private NpcSpriteData _currentSpriteData;
    [SerializeField]private SpriteRenderer bodySpriteRenderer;
    [SerializeField]private SpriteRenderer headSpriteRenderer;
    private int _currentSpriteIndex;
    [SerializeField]private int currentStepCount;
    public bool isControlled;
    public bool Moving { get; private set; }
    [SerializeField]private bool canMove;
  
    private WaitForSeconds movePause = new (1f);
    private WaitForSeconds animDelay = new (0.25f);
    
    public event Action OnMovementPaused;
    public event Action OnMovementStarted;
    public event Action OnMovementEnded;
    
    private Coroutine animationRoutine;
    private Coroutine _patrolRoutine;
    private Coroutine _specificRoutine;
    
    private void OnDisable()
    {
        if (animationData.isIdle) return;
        
        StopMovement();
        SetSprites(_currentSpriteData.idleSprite);
    }

    private void OnEnable()
    {
        if (isControlled)
        {
            _currentSpriteData = animationData.spriteData.GetSpriteData(MovementDirection.Down);
            SetSprites(_currentSpriteData.idleSprite);
            canMove = false;
            Moving = false;
            return;
        }
        SwitchMove();
        if (animationData.isIdle)
        {
            SetSprites(_currentSpriteData.idleSprite);
            canMove = false;
            Moving = false;
        }
        else
        {
            canMove = true;
            _patrolRoutine = StartCoroutine(MovementLoop());
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
    public void FacePlayerDirection(PlayerMovementHandler playerMovement)
    {
        StopMovement();

        var playerDirection = (int)playerMovement.currentDirection;
        
        var directionConversions = new []{MovementDirection.Up,MovementDirection.Down
            ,MovementDirection.Right,MovementDirection.Left};

        var oppositeDirection = directionConversions[playerDirection-1];
        
        _currentSpriteData = animationData.spriteData.GetSpriteData(oppositeDirection);
        
        SetSprites(_currentSpriteData.idleSprite);
    }
    private Vector3 SnapToGrid(Vector3 pos)
    {
        return new Vector3(
            Mathf.Round(pos.x),
            Mathf.Round(pos.y),
            0f
        );
    }
    private void CancelRoutines()
    {
        if (_patrolRoutine != null)   { StopCoroutine(_patrolRoutine);   _patrolRoutine = null; }
        if (_specificRoutine != null) { StopCoroutine(_specificRoutine); _specificRoutine = null; }
        if (animationRoutine != null) { StopCoroutine(animationRoutine); animationRoutine = null; }
    }

    public void StopMovement(bool snapPosition = true)
    {
        CancelRoutines();
        HaltMotion(snapPosition);
    }

    private void HaltMotion(bool snapPosition)
    {
        canMove = false;
        Moving = false;
        if (!snapPosition) return;

        Vector3 snapped = Vector3.Distance(transform.position, movePoint.position) < 0.05f
            ? movePoint.position
            : SnapToGrid(transform.position);

        movePoint.position = snapped;
        transform.position = snapped;
    }

  
    public void SetPauseDelay(float delay)
    {
        movePause = new WaitForSeconds(delay);
    }
   

    private IEnumerator Animate()
    {
        while (Moving)
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

    public void MoveToSpecific(MovementDirection direction, int numTiles, int moveSpeed = 2)
    {
        CancelRoutines();
        HaltMotion(true);
        _specificRoutine = StartCoroutine(Move());
        return;

        IEnumerator Move()
        {
            canMove = true;
            _currentMovement = new NpcMovementDirection(direction, numTiles);
            _currentSpriteData = animationData.spriteData.GetSpriteData(_currentMovement.direction);

            yield return MovementLoop(true, moveSpeed);

            HaltMotion(false);          // not StopMovement, so it doesn't cancel itself
            _specificRoutine = null;
            OnMovementEnded?.Invoke();
        }
    }
     
    private IEnumerator MovementLoop(bool specificMovement = false,int moveSpeed = 2)
    {
        var currentSpeed = moveSpeed != 2 ? moveSpeed : movementSpeed;
        while (canMove)
        {
            // Try to set next move
            if (!TrySetNextMove())
            {
                if (specificMovement)
                {
                    canMove = false;
                    yield break;        // MoveToSpecific fires OnMovementEnded
                }
                SwitchMove();           // patrol blocked: try the next segment
                yield return movePause;
                continue;
            }
              
            Moving = true;

            _currentSpriteIndex = 0;

            animationRoutine = StartCoroutine(Animate());

            // Move to target tile
            while (Vector3.Distance(transform.position, movePoint.position) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    movePoint.position,
                    currentSpeed * Time.deltaTime
                );
                yield return null;
            }
            
            // Snap to grid
            transform.position = movePoint.position;
            Moving = false;

            if (animationRoutine is not null)
            {
                StopCoroutine(animationRoutine);
                animationRoutine = null;
            }

            // Reset sprite
            SetSprites(_currentSpriteData.idleSprite);

            if (specificMovement)
            {
                yield return movePause;
                canMove = false;
                yield break;
            }
            
            OnMovementPaused?.Invoke();
            // Pause before next move
            yield return movePause;
            
            OnMovementStarted?.Invoke();  
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
    
    /// <summary>How many consecutive tiles in `dir` (up to maxTiles) are free of blockers.</summary>
    public int CountClearTiles(Vector2 dir, int maxTiles)
    {
        for (int i = 0; i < maxTiles; i++)
        {
            // ray covers exactly the gap between tile i and tile i+1
            Vector2 origin = (Vector2)rayCastPoint.position + dir * i;
            if (Physics2D.Raycast(origin, dir, 1f, movementBlockers).transform)
                return i;
        }
        return maxTiles;
    }

    private bool TrySetNextMove()
    {
        var dir = GetDirectionAsVector();
        int clear = CountClearTiles(dir, _currentMovement.numTilesToTravel);
        if (clear == 0) return false;

        movePoint.position = SnapToGrid(movePoint.position + (Vector3)dir * clear);
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

