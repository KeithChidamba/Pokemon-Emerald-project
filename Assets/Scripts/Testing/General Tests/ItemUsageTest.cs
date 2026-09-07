using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemUsageTest : IntegrationTest
{
    private PlayerBagHandler _playerBagHandler;
    public override void Inject(ServiceContainer container)
    {
        _playerBagHandler = container.Resolve<PlayerBagHandler>();
    }
    
    public override IEnumerator BeginTest()
    {
        yield return null;
        onTestResult.Invoke();
    }
}
