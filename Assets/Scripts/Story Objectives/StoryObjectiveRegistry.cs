using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "story Registry", menuName = "Objectives/Story Progress registry")]
public class StoryObjectiveRegistry : ScriptableObject
{
    public List<StoryObjectiveGroup> storyObjectiveGroups = new();
}
[Serializable]
public struct StoryObjectiveGroup
{
    public StoryObjectiveSection section;
    public List<StoryObjective> storyObjectives;
}
public enum StoryObjectiveSection
{
    FishingTutorial,BerryTutorial
}
