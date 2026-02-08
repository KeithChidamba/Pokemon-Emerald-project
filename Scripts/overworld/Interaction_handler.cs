using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;


public class Interaction_handler : MonoBehaviour
{
    [SerializeField] LayerMask interactable;
    [SerializeField] Transform interactionPoint;
    [SerializeField] float detectDistance=0.3f;
    private bool _canCheckForInteraction;
    private bool _stopInteractions;
    private bool _interactionCooldown;
    public Tilemap waterTilemap;
    public static Interaction_handler Instance;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        _stopInteractions = false;
        _canCheckForInteraction = true;
    }
    void Update()
    {
        if(!Dialogue_handler.Instance.displaying && !_stopInteractions)
        {
            if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.C))
                if(_canCheckForInteraction)
                    RaycastForInteraction();
        }

        if ((Input.GetKeyUp(KeyCode.Z) || Input.GetKeyUp(KeyCode.C)) 
            && !_canCheckForInteraction)
        {
            _canCheckForInteraction = true;
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
        yield return new WaitForSeconds(0.5f);
        _stopInteractions = false;
        _interactionCooldown = false;
    }
    void RaycastForInteraction()
    {
        _canCheckForInteraction = false;
        
        var currentDirectionIndex = (int)Player_movement.Instance.currentDirection-1;
        
        //1-down:   2-up:   3-left: 4-right
        List<Vector2> directionConversions = new (){ new(0, -1), new(0, 1), new(-1, 0), new(1, 0) };
        
        Vector2 directionVector = directionConversions[currentDirectionIndex]; 
        
        Vector2 origin = (Vector2)interactionPoint.position + directionVector * 0.1f;

        var hit = Physics2D.Raycast(
            origin,
            directionVector,
            detectDistance + 0.1f,interactable
        );

        if (hit.transform && !Dialogue_handler.Instance.displaying && !overworld_actions.Instance.usingUI)
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                var interactableObject = hit.transform.GetComponent<Overworld_interactable>();
                if (interactableObject.interaction != null)
                    Dialogue_handler.Instance.StartInteraction(interactableObject);
            }
            if (Input.GetKeyDown(KeyCode.C) 
                && overworld_actions.Instance.IsEquipped(Equipable.FishingRod))
            {
                if (hit.transform.gameObject.CompareTag("Water"))
                { 
                    var tile = Collider_checks.FindTileAtPosition<AnimatedEncounterTile>(waterTilemap,hit.point,Vector3.down);
                    overworld_actions.Instance.fishingArea = tile.area;
                    Dialogue_handler.Instance.DisplayList("Would you like to fish for pokemon"
                       , "fishing...", 
                       new[]
                       {
                           InteractionOptions.Fish,InteractionOptions.None
                       }
                       , new[]{"Yes", "No"});
                }
                else
                {
                    Dialogue_handler.Instance.DisplayDetails("Cant fish here");
                }
            }
        }
        if (Input.GetKeyDown(KeyCode.C) 
            && !hit.transform
            && overworld_actions.Instance.IsEquipped(Equipable.FishingRod))
        {
            Dialogue_handler.Instance.DisplayDetails("Cant fish here");
        }
    }
}
