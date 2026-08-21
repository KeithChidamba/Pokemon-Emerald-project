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

    private bool _viewingUI;

    public void LoadState(bool setDirection=true)
    {
        _rectTransform = GetComponent<RectTransform>();
        
        SetStartPosition(_rectTransform.anchoredPosition);
        
        if (!setDirection) return;
        switch (moveDirection)
        {
            case Direction.Up: _rectTransform.localRotation = Quaternion.Euler(0, 0, -90); break;
            case Direction.Down: _rectTransform.localRotation = Quaternion.Euler(0, 0, 90); break;
            case Direction.Left: _rectTransform.localRotation = Quaternion.Euler(0, 0, 0); break;
            case Direction.Right: _rectTransform.localRotation = Quaternion.Euler(0, 0, 180); break;
        }
    }

    public void SetStartPosition(Vector2 newPosition)
    {
        _rectTransform.anchoredPosition = newPosition;
        _startPos = newPosition;
        _targetPos = _startPos + GetDirectionVector() * moveDistance;
    }
    public void ChangeActiveState(bool isActive)
    {
        _viewingUI = isActive;
    }
    public void ResetPosition()
    {
        _rectTransform.anchoredPosition = _startPos;
    }
    private void Update()
    {
        if (!_viewingUI) return;
        MoveInLoop();
    }

    private void MoveInLoop()
    {
        Vector2 target = _movingToTarget ? _targetPos : _startPos;
        _rectTransform.anchoredPosition = Vector2.MoveTowards(
            _rectTransform.anchoredPosition,
            target,
            moveSpeed * Time.unscaledDeltaTime
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
