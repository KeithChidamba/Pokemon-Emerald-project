using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "MultiTargetDMG", menuName = "MultiTargetDMG")]
public class MultiTargetDamageInfo : AdditionalInfoModule
{
public enum Target{AllExceptSelf,AllEnemies}

public Target target;
}
