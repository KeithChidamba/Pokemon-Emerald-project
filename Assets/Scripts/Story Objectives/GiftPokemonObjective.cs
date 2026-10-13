using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "Objectives/prop based objective/gift pokemon objective")]
public class GiftPokemonObjective : PropBasedObjective
{
    public List<Interaction> giftInteractions = new();
    
    private GameUiHandler gameUiHandler;
    private DialogueHandler _dialogueHandler;
    private InteractionOptionsEventHandler _interactionOptionsHandler;
    private OverworldState _overworldStateHandler;
    
    protected override void OnObjectiveLoaded()
    {
        _dialogueHandler = serviceContainer.Resolve<DialogueHandler>(); 
        _interactionOptionsHandler = serviceContainer.Resolve<InteractionOptionsEventHandler>();
        gameUiHandler = serviceContainer.Resolve<GameUiHandler>();
        _dialogueHandler.DisplayObjectiveText(objectiveHeading);
        _overworldStateHandler = serviceContainer.Resolve<OverworldState>(); 
        _interactionOptionsHandler.OnInteractionOptionChosen += CheckInteractionOption;
        gameUiHandler.SetMenuAccessibility(false);
        LoadObjects(_overworldStateHandler);
    }
    
    protected override void OnPropsCreated()
    {
        //give pokeballs their interactions
        for(int i=0;i<propsForObjective.Count;i++)
        {
            propsForObjective[i].propObject
                .GetComponent<OverworldInteractable>().interaction = giftInteractions[i];
            
            //correcting visual offset caused by table sprite
            var currentPos = propsForObjective[i].propObject.transform.position;
            propsForObjective[i].propObject.transform.position = new Vector3(currentPos.x+.5f,currentPos.y+.5f,0);
        }
    }
    
    private void CheckInteractionOption(Interaction interaction, int optionChosen)
    {
        if (optionChosen>0)
        {
            _dialogueHandler.EndDialogue(); 
            return;
        }

        var isGiftInteraction = interaction.GetDynamicModule<PokemonGiftInteractoinInfo>() is not null;
        if (!isGiftInteraction) return;
        
        foreach(var prop in propsForObjective)
        {
            var interactionOnProp = prop.propObject.GetComponent<OverworldInteractable>().interaction;
            //de-activate selected pokeball and make the others in-accessible
            prop.propState = interactionOnProp.GetID == interaction.GetID?
                propState.InActive : propState.InAccessible;
        }
        
        gameUiHandler.SetMenuAccessibility(true);
        ClearObjective();
    }
    
    protected override void OnObjectiveCleared()
    {
        UnLoadObjects();
        _interactionOptionsHandler.OnInteractionOptionChosen -= CheckInteractionOption;
        _overworldStateHandler.ClearAndLoadNextObjective();
    }
}
