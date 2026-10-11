using System.Collections;
using UnityEngine;

public class BattlePokeball : MonoBehaviour
{
    public Animator animator;
    public IEnumerator ThrowPokeball(bool isEnemyDrop)
    {
        gameObject.SetActive(true);
        animator.Play(isEnemyDrop?"enemy pokeball drop":"player partner pokeball drop");
        yield return new WaitForSeconds(1f);
        gameObject.SetActive(false);
    }
}
