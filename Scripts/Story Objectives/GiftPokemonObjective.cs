using UnityEngine;
[CreateAssetMenu(menuName = "Objectives/prop based objective/gift pokemon objective")]
public class GiftPokemonObjective : PropBasedObjective    
{
    private GameUiHandler gameUiHandler;
    private DialogueHandler _dialogueHandler;
    private DialogueOptionsEventHandler _dialogueOptionsHandler;
    private OverworldState _overworldStateHandler;
    
    protected override void OnObjectiveLoaded()
    {
        _dialogueHandler = serviceContainer.Resolve<DialogueHandler>(); 
        _dialogueOptionsHandler = serviceContainer.Resolve<DialogueOptionsEventHandler>();
        gameUiHandler = serviceContainer.Resolve<GameUiHandler>();
        _dialogueHandler.DisplayObjectiveText(objectiveHeading);
        _overworldStateHandler = serviceContainer.Resolve<OverworldState>(); 
        _dialogueOptionsHandler.OnInteractionOptionChosen += CheckInteractionOption;
        gameUiHandler.SetMenuAccessibility(false);
    }
   
    private void CheckInteractionOption(Interaction interaction, int optionChosen)
    {
        if (optionChosen>0)
        {
            _dialogueHandler.EndDialogue(); 
            return;
        }
        if (interaction.overworldInteraction != OverworldInteractionType.ReceiveGiftPokemon) return;

        var pokeballProps = objectiveObjectHandler.propGroupsForObjective[0];
        
        foreach(var prop in pokeballProps.propsForObjective)
        {
            var interactionOnProp = prop.propObject.GetComponent<OverworldInteractable>().interaction;
           
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
        gameUiHandler.SetMenuAccessibility(true);
        ClearObjective();
    }
   
    protected override void OnObjectiveCleared()
    {
        _dialogueOptionsHandler.OnInteractionOptionChosen -= CheckInteractionOption;
        _overworldStateHandler.ClearAndLoadNextObjective();
    }
}
