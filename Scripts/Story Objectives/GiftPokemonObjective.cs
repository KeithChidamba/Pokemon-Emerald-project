using UnityEngine;
[CreateAssetMenu(menuName = "Objectives/gift pokemon interaction objective")]
public class GiftPokemonObjective : InteractionObjective    
{
    [HideInInspector]public ObjectiveObjectHandler objectiveObjectHandler;//injected by object handler class
    private Game_ui_manager _gameUIManager;
    protected override void OnObjectiveLoaded()
    {
        dialogueHandler = serviceContainer.Resolve<Dialogue_handler>(); 
        dialogueOptionsHandler = serviceContainer.Resolve<DialogueOptionsEventHandler>();
        _gameUIManager = serviceContainer.Resolve<Game_ui_manager>();
        dialogueHandler.DisplayObjectiveText(objectiveHeading);
        overworldStateHandler = serviceContainer.Resolve<OverworldState>(); 
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
        if (interactionTypeForObjective != interaction.overworldInteraction) return;

        var pokeballProps = objectiveObjectHandler.propGroupsForObjective[0];
        
        foreach(var prop in pokeballProps.propsForObjective)
        {
            var interactionOnProp = prop.propObject.GetComponent<Overworld_interactable>().interaction;
           
            if (interactionOnProp==interaction)
            {
                //de-activate selected pokeball
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
        dialogueOptionsHandler.OnInteractionOptionChosen -= CheckInteractionOption;
        overworldStateHandler.ClearAndLoadNextObjective();
    }
}
