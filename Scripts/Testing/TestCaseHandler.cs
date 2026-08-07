using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TestCase
{
   public int caseIndex;
   public string message;
   public Func<bool> condition;

   public TestCase(int caseIndex,string message, Func<bool> condition)
   {
      this.caseIndex = caseIndex;
      this.message = message;
      this.condition = condition;
   }
}

public class TestCaseHandler
{
   private Dictionary<int,TestCase> testCases = new();
   private TestingEnvironmentHandler _testHandler;
   private TestActionSequencer _sequencer;
   private int NextIndex => testCases.Count;
   
   public TestCaseHandler(TestingEnvironmentHandler testHandler,TestActionSequencer sequencer)
   {
      _testHandler = testHandler;
      _sequencer = sequencer;
   }

   public void AddTestCase(string message,Func<bool> condition)
   {
      testCases.Add(NextIndex,new TestCase(NextIndex,message,condition));
   }
   public void AddTestCase(int caseIndex,string message,Func<bool> condition)
   {
      testCases.Add(caseIndex,new TestCase(caseIndex,message,condition));
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
         if (!testCaseResult.condition.Invoke())
         {
            _testHandler.LogMessage($"Test case({testCaseResult.caseIndex + 1}) failed due to violation" +
                                    $" of rule ({testCaseResult.message})", TestLogType.Error);
            failureCallBack?.Invoke();
         }
         else
         {
            successCallBack?.Invoke();
         }
         return true;
      }
      return false;
   }
}
