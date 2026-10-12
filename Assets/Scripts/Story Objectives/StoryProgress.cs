using System;

[Serializable]
public class StoryProgress
{
    public int numRemainingInGroup;
    public StoryObjectiveSection lastActiveGroup;
    public bool allObjectivesComplete;
}