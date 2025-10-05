using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LoopingUiAnimation : MonoBehaviour
{
    public enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }

    [Header("Movement Settings")]
    public Direction moveDirection = Direction.Right;
    public float moveSpeed = 2f;
    public float moveDistance = 3f;

    private Vector2 _startPos;
    private Vector2 _targetPos;
    private bool _movingToTarget = true;
    public bool viewingUI;
    private void Start()
    {
        _startPos = transform.position;  
        _targetPos = _startPos + GetDirectionVector() * moveDistance;
        switch (moveDirection)
        {
            case Direction.Up: transform.rotation = Quaternion.Euler(0,0,-90); break;
            case Direction.Down:transform.rotation = Quaternion.Euler(0,0,90); break;
            case Direction.Left: transform.rotation = Quaternion.Euler(0,0,0); break;
            case Direction.Right: transform.rotation = Quaternion.Euler(0,0,180); break;
        }
    }
    
    private void Update()
    {
        if (!viewingUI) return;
        MoveInLoop();
    }

    private void MoveInLoop()
    {
        Vector2 target = _movingToTarget ? _targetPos : _startPos;
        transform.position = Vector2.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, target) < 0.01f)
            _movingToTarget = !_movingToTarget;
    }

    private Vector2 GetDirectionVector()
    {
        switch (moveDirection)
        {
            case Direction.Up: return Vector2.up;
            case Direction.Down: return Vector2.down;
            case Direction.Left: return Vector2.left;
            case Direction.Right: return Vector2.right;
            default: return Vector2.zero;
        }
    }
}
