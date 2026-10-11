using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TypingCharacterBox : MonoBehaviour
{
    public TMP_Text characterText;
    public LoopingUiAnimation boxImageAnimation;

    public void LoadBox()
    {
        characterText.text = string.Empty;
        gameObject.SetActive(true);
        boxImageAnimation.moveDirection = LoopingUiAnimation.Direction.Up;
        boxImageAnimation.moveDistance = 5;
        boxImageAnimation.moveSpeed = 25;
        boxImageAnimation.LoadState(false);
    }
    public void FreezeCharacterBox()
    {
        boxImageAnimation.ChangeActiveState(false);
        boxImageAnimation.ResetPosition();
    }
    public void AnimateCharacterBox()
    {
        boxImageAnimation.ChangeActiveState(true);
    }
}
