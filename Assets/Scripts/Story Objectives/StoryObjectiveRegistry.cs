using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "story Registry", menuName = "Objectives/Story Progress registry")]
public class StoryObjectiveRegistry : ScriptableObject
{
    public List<StoryObjective> allStoryObjectives = new();
}
