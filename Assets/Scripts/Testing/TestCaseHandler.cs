using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TestCase
{
   public int caseIndex;
   public List<TestCaseCondition> conditions;

   public TestCase(int caseIndex, List<TestCaseCondition> conditions)
   {
      this.caseIndex = caseIndex;
      this.conditions = conditions;
   }
}
public class TestCaseCondition
{
   public string message;
   public Func<bool> requirement;
   public TestCaseCondition(string message, Func<bool> requirement)
   {
      this.message = message;
      this.requirement = requirement;
   }
}
public class TestCaseHandler
{
   private enum TestCaseIndexHandling
   {
      None,DynamicIndexing, StaticIndexing
   }
   private TestCaseIndexHandling _handlingState;
   private Dictionary<int,TestCase> testCases = new();
   private TestingEnvironmentHandler _testHandler;
   private TestActionSequencer _sequencer;
   private int NextIndex => testCases.Count;
   
   public TestCaseHandler(TestingEnvironmentHandler testHandler,TestActionSequencer sequencer)
   {
      _handlingState = TestCaseIndexHandling.None;
      _testHandler = testHandler;
      _sequencer = sequencer;
   }
   /// <summary>
   /// [For Singular Condition]
   /// Use this if every test action has a test case.
   /// Don't use both overloads together
   /// </summary>
   public void AddTestCase(string message,Func<bool> condition)
   {
      AddTestCase(new List<TestCaseCondition>
      {
         new(message, condition)
      });
   }
   /// <summary>
   /// Use this if every test action has a test case.
   /// Don't use both overloads together
   /// </summary>
   public void AddTestCase(List<TestCaseCondition> conditions)
   {
      if (_handlingState == TestCaseIndexHandling.None)
      {
         _handlingState = TestCaseIndexHandling.DynamicIndexing;
      }
      else
      {
         if (_handlingState != TestCaseIndexHandling.DynamicIndexing)
         {
            Debug.LogError($"Conflicting testcase index handling.[Only one can be used]. Current state{_handlingState}");
            return;
         }
      }
      testCases.Add(NextIndex,new TestCase(NextIndex, conditions));
   }
   
   /// <summary>
   /// [For Singular Condition]
   /// Use this if you need exact index matching for test cases and test actions.
   /// Only if certain actions don't have a test case
   /// </summary>
   public void AddTestCase(int caseIndex,string message,Func<bool> condition)
   {
      AddTestCase(caseIndex,new List<TestCaseCondition>
      {
         new(message,condition)
      });
   }   
   public void AddTestCase(int caseIndex,List<TestCaseCondition> conditions)
   {
      if (_handlingState == TestCaseIndexHandling.None)
      {
         _handlingState = TestCaseIndexHandling.StaticIndexing;
      }
      else
      {
         if (_handlingState != TestCaseIndexHandling.StaticIndexing)
         {
            Debug.LogError($"Conflicting testcase index handling.[Only one can be used]. Current state{_handlingState}");
            return;
         }
      }
      testCases.Add(caseIndex,new TestCase(caseIndex, conditions));
   }
   
   private TestCase GetCurrentTestCase(int currentIndex)
   {
      return testCases.FirstOrDefault(c=>c.Key == currentIndex).Value;
   }
   
   
   /// <summary>
   /// Should only be used when test cases are guaranteed to exist at every possible index
   /// </summary>
   public void HandleCurrentTestCase(Action successCallBack,Action failureCallBack)
   {
      var result = CheckForCurrentTestCase(successCallBack, failureCallBack);
   }
   public bool CheckForCurrentTestCase(Action successCallBack,Action failureCallBack)
   {
      var testCaseResult = GetCurrentTestCase(_sequencer.GetTestCaseIndex());
      if (testCaseResult != null)
      {
         _testHandler.LogMessage($"Test case({testCaseResult.caseIndex + 1}) Conditions :", TestLogType.TestCase);
         for (int i=0;i< testCaseResult.conditions.Count;i++)
         {
            var condition = testCaseResult.conditions[i];
            
            if (!condition.requirement.Invoke())
            {
               _testHandler.LogMessage($"Test case condition({i + 1}) FAILED due to violation" +
                                       $" of case ({condition.message})", TestLogType.Error);
               failureCallBack?.Invoke();
               return true;
            }
            _testHandler.LogMessage($"Test case condition({i + 1}) PASSED({condition.message})", TestLogType.TestCaseCondition);
         }
         _testHandler.LogMessage($"Test case({testCaseResult.caseIndex + 1}) PASSED", TestLogType.TestCase);
         successCallBack?.Invoke();
         return true;
      }
      return false;
   }
}
