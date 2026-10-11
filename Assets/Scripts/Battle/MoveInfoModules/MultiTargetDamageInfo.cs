using System;

[Serializable]
public class MultiTargetDamageInfo : DynamicAdditionalInfo
{
    public Target target;
}

public enum Target{AllExceptSelf,AllEnemies}
