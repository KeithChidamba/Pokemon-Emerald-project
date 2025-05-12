using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;


public class Interaction_handler : MonoBehaviour
{
    [SerializeField] LayerMask interactable;
    [SerializeField] Transform interactionPoint;
    [SerializeField] float detectDistance=0.15f;
    [SerializeField] private bool canCheckForInteraction;
    private void Start()
    {
        interactable = 1 << LayerMask.NameToLayer("Interactable");
    }
    void Update()
    {
        if (Input.GetKey(KeyCode.F) | Input.GetKey(KeyCode.Q))
            if(!canCheckForInteraction)
                StartCoroutine(DelayRayCast());
        if(canCheckForInteraction)
            RaycastForInteraction();
    }

    IEnumerator DelayRayCast()
    {
        canCheckForInteraction = true;
        yield return new WaitForSeconds(1f);
        canCheckForInteraction = false;
    }
    void RaycastForInteraction()
    {
        var hit = Physics2D.Raycast(transform.position, interactionPoint.forward, detectDistance, interactable);
        if (hit.transform && !Dialogue_handler.instance.displaying && !overworld_actions.Instance.usingUI)
        {
            var interactableObject = hit.transform.GetComponent<Overworld_interactable>();
            if (Input.GetKeyDown(KeyCode.F))
            {
                if (interactableObject.interaction != null)
                {
                    Dialogue_handler.instance.Current_interaction = interactableObject.interaction;
                    Dialogue_handler.instance.Display(Dialogue_handler.instance.Current_interaction);
                }
            }
            if (Input.GetKeyDown(KeyCode.Q) && !overworld_actions.Instance.doingAction)
            {
                if (hit.transform.gameObject.CompareTag("Water"))
                { 
                   overworld_actions.Instance.fishingArea = interactableObject.area;
                   Dialogue_handler.instance.Write_Info("Would you like to fish for pokemon", "Options", "Fish", "fishing...","","Yes","No");
                }
                else
                {
                    Dialogue_handler.instance.Write_Info("Cant fish here", "Info");
                }
            }
        }
        if (Input.GetKeyDown(KeyCode.Q) && !hit.transform)
        {
            Dialogue_handler.instance.Write_Info("Cant fish here", "Info");
        }
    }
}
