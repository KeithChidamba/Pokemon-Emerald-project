using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndToEndTest
{
    public string testName;
    public TestStatus testStatus;
    
    private Dictionary<int,TestCase> testCases = new(); 
    private TestingEnvironmentHandler _testingHandler;

    public bool testOperationsComplete;
    public bool testOperationStarted;
    private int NextIndex => testCases.Count;

    protected ServiceContainer serviceContainer;
    
    public virtual void Inject(ServiceContainer container) { }
    
    public virtual IEnumerator BeginTest(EndToEndTestData testData)
    {
        yield return null;
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
        testCases.Add(NextIndex,new TestCase(NextIndex, conditions));
    }
    protected virtual void OnTestCasesChecked() { }
    public void ValidateTestAndEnd(TestingEnvironmentHandler testHandler)
    {
        if (!testOperationStarted) return;
        foreach(var testCase in testCases)
        {
            TestCaseHandler.ValidateTestCases(
                testHandler,
                testCase.Value,
                () =>
                {
                    testStatus = TestStatus.Passed;
                }
                , () =>
                {
                    testStatus = TestStatus.Failed;
                });
        }
        OnTestCasesChecked();
        testOperationStarted = false;
        testOperationsComplete = true;
    }
}
