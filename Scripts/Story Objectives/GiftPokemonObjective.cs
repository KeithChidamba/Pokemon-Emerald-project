using UnityEngine;
[CreateAssetMenu(menuName = "Objectives/gift pokemon interaction objective")]
public class GiftPokemonObjective : InteractionObjective    
{
    public ObjectiveObjectHandler objectiveObjectHandler;
    
    protected override void OnObjectiveLoaded()
    {
        dialogueHandler = serviceContainer.Resolve<Dialogue_handler>(); 
        dialogueOptionsHandler = serviceContainer.Resolve<Options_manager>(); 
        dialogueHandler.DisplayObjectiveText(objectiveHeading);
        dialogueOptionsHandler.OnInteractionOptionChosen += CheckInteractionOption;
    }
   
    private void CheckInteractionOption(Interaction interaction, int optionChosen)
    {
        if (optionChosen>0)
        {
            dialogueHandler.EndDialogue(); 
            return;
        }
        if (interactionForObjective.overworldInteraction != interaction.overworldInteraction) return;

        var pokeballProps = objectiveObjectHandler.propGroupsForObjective[0];
        
        for(int i=0;i < pokeballProps.propsForObjective.Count;i++)
        {
            var prop = pokeballProps.propsForObjective[i];
            //de-activate selected pokeball
            if (int.Parse(interaction.resultMessage) == i + 1)
            {
                prop.propState = propState.InActive;
            }
            else
            {
                //make the others in-accessible
                prop.propState = propState.InAccessible;
            }
        }
        ClearObjective();
    }
   
    protected override void OnObjectiveCleared()
    {
        var overworldStateHandler = serviceContainer.Resolve<OverworldState>(); 
        dialogueOptionsHandler.OnInteractionOptionChosen -= CheckInteractionOption;
        overworldStateHandler.ClearAndLoadNextObjective();
    }
    
    
}
