using System.Collections;
using System;

public abstract class IntegrationTest
{
    public TestingEnvironmentHandler testingHandler;
    public string testName;
    public enum TestStatus{Passed,Failed}
    public TestStatus testStatus;
    public Action onTestResult;
    public virtual IEnumerator BeginTest()
    {
        yield return null;
    }
    public virtual void Inject(ServiceContainer container) { }

    public void SetStatus(bool condition)
    {
        testStatus = condition ? TestStatus.Passed : TestStatus.Failed;
    }
}
