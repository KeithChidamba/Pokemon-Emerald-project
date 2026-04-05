using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;


public class Interaction_handler : MonoBehaviour,IInjectable
{
    [SerializeField] LayerMask interactable;
    [SerializeField] Transform interactionPoint;
    [SerializeField] float detectDistance=0.3f;
    private bool _canCheckForInteraction;
    private bool _stopInteractions;
    private bool _interactionCooldown;
    public Tilemap waterTilemap;
    public Tilemap interactionTilemap;

    private overworld_actions _overworldActions;
    private Dialogue_handler _dialogueHandler;
    private Player_movement _playerMovementHandler;

    public void Inject(ServiceContainer container)
    {
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _overworldActions = container.Resolve<overworld_actions>();
        _playerMovementHandler = container.Resolve<Player_movement>();
        OnInject();
    }
    private void OnInject()
    {
        _stopInteractions = false;
        _canCheckForInteraction = true;
        gameObject.SetActive(true);
    }
    void Update()
    {
        
        if(!_dialogueHandler.displaying && !_stopInteractions)
        {
            if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.C))
            {
                if (_canCheckForInteraction)
                    RaycastForInteraction();
            }
            
            if (Input.GetKeyUp(KeyCode.C) || Input.GetKeyUp(KeyCode.Z))
            {
                _canCheckForInteraction = true;
            }
        }

    }
    public void DisableInteraction()
    {
        _stopInteractions = true;
    }
    public void AllowInteraction()
    {
        if (_interactionCooldown) return;
        _interactionCooldown = true;
        StartCoroutine(SetInteractionAllowed());
    }

    private IEnumerator SetInteractionAllowed()
    {
        yield return new WaitForSeconds(1f);
        _stopInteractions = false;
        _interactionCooldown = false;
    }
    
    void RaycastForInteraction()
    {
        _canCheckForInteraction = false;

        var directionVector = _playerMovementHandler.GetDirectionAsVector2();
        
         Vector2 origin = (Vector2)interactionPoint.position + directionVector * 0.1f;
        
       
         var hit = Physics2D.Raycast(
             origin,
             directionVector,
             detectDistance,interactable
         );
        
        
        if (hit.transform && !_dialogueHandler.displaying && !_overworldActions.usingUI)
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                var interactableTile = Collider_checks.FindTileAtPositionRadius<InteractionTile>(interactionTilemap,hit.point,Vector3.down);
                if (interactableTile != null)
                {
                    _dialogueHandler.StartInteraction(interactableTile.interaction);
                }
                else
                {
                    var interactableObject = hit.transform.GetComponent<Overworld_interactable>();
                    if (interactableObject != null)
                        _dialogueHandler.StartInteraction(interactableObject);
                }
               
            }
            if (Input.GetKeyDown(KeyCode.C) 
                && _overworldActions.IsEquipped(Equipable.FishingRod))
            {
                if (hit.transform.gameObject.CompareTag("Water"))
                {
                    Encounter_Area areaOfEncounter;
                    var animatedWaterTile = Collider_checks.FindTileAtPosition<AnimatedEncounterTile>(waterTilemap,hit.point,Vector3.down);
                    if (animatedWaterTile == null)
                    {
                        var stillWaterTile = Collider_checks.FindTileAtPosition<EncounterTile>(waterTilemap,hit.point,Vector3.down);
                        areaOfEncounter = stillWaterTile.area;
                    }
                    else
                    {
                        areaOfEncounter = animatedWaterTile.area;
                    }
                    _overworldActions.fishingArea = areaOfEncounter;
                    _dialogueHandler.DisplayList("Would you like to fish for pokemon"
                       , "fishing...", 
                       new[]
                       {
                           InteractionOptions.Fish,InteractionOptions.None
                       }
                       , new[]{"Yes", "No"});
                }
                else
                {
                    _dialogueHandler.DisplayDetails("Cant fish here");
                }
            }
        }
        if (Input.GetKeyDown(KeyCode.C) 
            && !hit.transform
            && _overworldActions.IsEquipped(Equipable.FishingRod))
        {
            _dialogueHandler.DisplayDetails("Cant fish here");
        }
    }
}
