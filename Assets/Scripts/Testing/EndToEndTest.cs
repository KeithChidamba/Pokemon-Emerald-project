using System;
using System.Collections;
using System.Collections.Generic;

public class EndToEndTest
{
    public string testName;
    public TestStatus testStatus;
    
    private List<TestCase> testCases = new();
    private Dictionary<int,Action> testScenarios = new();
    private TestingEnvironmentHandler _testingHandler;

    private int NextIndex => testCases.Count;
    
    public bool testOperationsComplete;
    public bool testOperationStarted;
    
    private int currentTestCaseIndex;
    
    protected ServiceContainer serviceContainer;
    public ServiceContainer GetContainer => serviceContainer;
    
    public virtual void Inject(ServiceContainer container) { }
    
    public virtual IEnumerator BeginTest(EndToEndTestData testData)
    {
        yield return null;
    }
/// <summary>
/// This is for manipulating the current test state to align
/// with test cases
/// </summary>
    protected void AddTestCaseScenario(int caseIndex,Action scenario)
    {
        testScenarios.Add(caseIndex,scenario);
    }
    /// <summary>
    /// This is for manipulating the current test state to align
    /// with test cases. This overload works with automatic indexing
    /// </summary>
    protected void AddTestCaseScenario(Action scenario)
    {
        testScenarios.Add(testScenarios.Count,scenario);
    }
/// <summary>
/// Run the current scenario action to prepare for
/// the test case
/// </summary>
    public void SetupCurrentScenario()
    {
        serviceContainer.Resolve<DialogueHandler>()
            .DisplayTestCaseText(
                testCases[currentTestCaseIndex].GetCaseMessages());
        
        if(testScenarios.TryGetValue(testCases[currentTestCaseIndex].caseIndex, out var action))
        {
            action();
        }
    }
    protected void AddTestCase(string message,Func<bool> condition)
    {
        AddTestCase(new List<TestCaseCondition>
        {
            new(message, condition)
        });
    }
    protected void AddTestCase(List<TestCaseCondition> conditions)
    {
        testCases.Add(new TestCase(NextIndex, conditions));
    }

    /// <summary>
    /// Occurs once a test case is checked. At this point
    /// [currentTestCaseIndex] is +1 the index of the test case that was
    /// just checked. By default, this method runs the current scenario, unless overloaded
    /// </summary>
    protected virtual void OnTestCaseChecked()
    {
        SetupCurrentScenario();
    }
    protected virtual void OnTestCaseFailed() { }
    public void ValidateCurrentTestCase(TestingEnvironmentHandler testHandler)
    {
        if (!testOperationStarted) return;
        TestCaseHandler.ValidateTestCases(
            testHandler,
            testCases[currentTestCaseIndex],
            () =>
            {
                testStatus = TestStatus.Passed;
                currentTestCaseIndex++;
                if (currentTestCaseIndex == testCases.Count)
                {
                    EndTest();
                }
                else
                {
                    OnTestCaseChecked();
                }
            }
            , () =>
            {
                testStatus = TestStatus.Failed;
                EndTest();
                OnTestCaseFailed();
            });
    }
    private void EndTest()
    {
        serviceContainer.Resolve<DialogueHandler>()
            .DisplayTestCaseText(string.Empty);
        testOperationStarted = false;
        testOperationsComplete = true;
    }
}
