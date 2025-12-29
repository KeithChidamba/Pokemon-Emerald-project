using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "MultiTargetDMG", menuName = "MultiTargetDMG")]
public class MultiTargetDamageInfo : AdditionalInfoModule
{
    public Target target;
}

public enum Target{AllExceptSelf,AllEnemies}
