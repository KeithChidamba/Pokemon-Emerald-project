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

    private RectTransform _rectTransform;
    private Vector2 _startPos;
    private Vector2 _targetPos;
    private bool _movingToTarget = true;

    public bool viewingUI;

    private void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
        
        _startPos = _rectTransform.anchoredPosition;
        _targetPos = _startPos + GetDirectionVector() * moveDistance;
        
        switch (moveDirection)
        {
            case Direction.Up: _rectTransform.localRotation = Quaternion.Euler(0, 0, -90); break;
            case Direction.Down: _rectTransform.localRotation = Quaternion.Euler(0, 0, 90); break;
            case Direction.Left: _rectTransform.localRotation = Quaternion.Euler(0, 0, 0); break;
            case Direction.Right: _rectTransform.localRotation = Quaternion.Euler(0, 0, 180); break;
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
        _rectTransform.anchoredPosition = Vector2.MoveTowards(
            _rectTransform.anchoredPosition,
            target,
            moveSpeed * Time.deltaTime
        );

        if (Vector2.Distance(_rectTransform.anchoredPosition, target) < 0.01f)
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
