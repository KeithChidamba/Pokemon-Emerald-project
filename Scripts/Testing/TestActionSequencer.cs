using System;
using System.Collections.Generic;
using UnityEngine;

public class TestAction
{
    public Action action;
    public TestAction(Action action)
    {
        this.action = action;
    }
}

public class TestActionSequencer
{
    public int CurrentSequenceIndex { get; private set; }
    private Dictionary<int, TestAction> _testSequence = new();
    private int NextIndex => _testSequence.Count;

    public virtual int GetTestCaseIndex()
    {
        return CurrentSequenceIndex;
    }
    protected TestActionSequencer()
    {
        CurrentSequenceIndex = 0;
    }
    public void AddAction(Action action)
    {
        _testSequence.Add(NextIndex,new TestAction(action));
    }
    public void RemoveAction(int index)
    {
        _testSequence.Remove(index);
    }
    public void CallNextAction()
    {
        if (CurrentSequenceIndex == _testSequence.Count)
        {
            Debug.LogWarning("attempted out of range hit on action access");
            return;
        }
        var action = _testSequence[CurrentSequenceIndex].action;
        CurrentSequenceIndex++;
        action?.Invoke();
    }
    public bool SequenceComplete()
    { 
        return CurrentSequenceIndex == _testSequence.Count;
    }
}