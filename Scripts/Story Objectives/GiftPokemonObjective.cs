using UnityEngine;
[CreateAssetMenu(menuName = "Objectives/gift pokemon interaction objective")]
public class GiftPokemonObjective : InteractionObjective    
{
    public ObjectiveObjectHandler objectiveObjectHandler;
    private Game_ui_manager _gameUIManager;
    protected override void OnObjectiveLoaded()
    {
        dialogueHandler = serviceContainer.Resolve<Dialogue_handler>(); 
        dialogueOptionsHandler = serviceContainer.Resolve<DialogueOptionsEventHandler>();
        _gameUIManager = serviceContainer.Resolve<Game_ui_manager>();
        dialogueHandler.DisplayObjectiveText(objectiveHeading);
        dialogueOptionsHandler.OnInteractionOptionChosen += CheckInteractionOption;
        _gameUIManager.SetMenuAccessibility(false);
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
        _gameUIManager.SetMenuAccessibility(true);
        ClearObjective();
    }
   
    protected override void OnObjectiveCleared()
    {
        var overworldStateHandler = serviceContainer.Resolve<OverworldState>(); 
        dialogueOptionsHandler.OnInteractionOptionChosen -= CheckInteractionOption;
        overworldStateHandler.ClearAndLoadNextObjective();
    }
    
    
}
