using UnityEngine;
[CreateAssetMenu(menuName = "Objectives/prop based objective/gift pokemon objective")]
public class GiftPokemonObjective : PropBasedObjective    
{
    private Game_ui_manager _gameUIManager;
    private Dialogue_handler _dialogueHandler;
    private DialogueOptionsEventHandler _dialogueOptionsHandler;
    private OverworldState _overworldStateHandler;
    
    protected override void OnObjectiveLoaded()
    {
        _dialogueHandler = serviceContainer.Resolve<Dialogue_handler>(); 
        _dialogueOptionsHandler = serviceContainer.Resolve<DialogueOptionsEventHandler>();
        _gameUIManager = serviceContainer.Resolve<Game_ui_manager>();
        _dialogueHandler.DisplayObjectiveText(objectiveHeading);
        _overworldStateHandler = serviceContainer.Resolve<OverworldState>(); 
        _dialogueOptionsHandler.OnInteractionOptionChosen += CheckInteractionOption;
        _gameUIManager.SetMenuAccessibility(false);
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
        _dialogueOptionsHandler.OnInteractionOptionChosen -= CheckInteractionOption;
        _overworldStateHandler.ClearAndLoadNextObjective();
    }
}
