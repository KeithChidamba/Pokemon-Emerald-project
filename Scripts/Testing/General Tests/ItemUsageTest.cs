using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemUsageTest : IntegrationTest
{
    private Bag _playerBagHandler;
    public override void Inject(ServiceContainer container)
    {
        _playerBagHandler = container.Resolve<Bag>();
    }
    
    public override IEnumerator BeginTest()
    {
        yield return null;
        onTestResult.Invoke();
    }
}
