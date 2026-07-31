using System;
using System.Collections.Generic;

public class TestAction
{
    public Action action;
    public bool displayLogs;
    public TestAction(Action action, bool displayLogs)
    {
        this.action = action;
        this.displayLogs = displayLogs;
    }
}
public class TestActionSequencer
{
    public int CurrentSequenceIndex { get; private set; }
    private Dictionary<int, TestAction> _testSequence = new();
    private int NextIndex => _testSequence.Count;
    private int _numSequencesCompleted;
    private int _numSequenceRepetitions;
    
    protected TestActionSequencer(int numSequenceRepetitions = 0)
    {
        CurrentSequenceIndex = 0;
        _numSequencesCompleted = 0;
        _numSequenceRepetitions = numSequenceRepetitions;
    }
    public void AddAction(Action action,bool displayLogs=false)
    {
        _testSequence.Add(NextIndex,new TestAction(action,displayLogs));
    }
    public void RemoveAction(int index)
    {
        _testSequence.Remove(index);
    }

    public void RemoveLastAction()
    {
        _testSequence.Remove(_testSequence.Count-1);
    }
    public bool CanDisplayCurrentLogs()
    {
        return _testSequence[CurrentSequenceIndex].displayLogs;
    }
    public void CallNextAction()
    {
        var action = _testSequence[CurrentSequenceIndex].action;
        if (CurrentSequenceIndex == _testSequence.Count-1)
        {
            _numSequencesCompleted++;
            if(_numSequencesCompleted < _numSequenceRepetitions + 1)
            {
                //Repeat Sequence
                CurrentSequenceIndex = 0;
            }
        }
        CurrentSequenceIndex++;
        action?.Invoke();
    }
    public bool SequenceComplete()
    {
        return _numSequencesCompleted == _numSequenceRepetitions + 1;
    }
}