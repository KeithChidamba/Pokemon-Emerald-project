using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Interaction_handler : MonoBehaviour
{
    [SerializeField] LayerMask Interactable;
    [SerializeField] Transform interaction_point;
    [SerializeField] float detect_distance=0.15f;
    [SerializeField] private bool checkForInteraction = false;
    private void Start()
    {
        Interactable = 1 << LayerMask.NameToLayer("Interactable");
    }
    void Update()
    {
        if (Input.GetKey(KeyCode.F) | Input.GetKey(KeyCode.Q))
            if(!checkForInteraction)
                StartCoroutine(DelayRayCast());
        if(checkForInteraction)
            CheckFront();
    }

    IEnumerator DelayRayCast()
    {
        checkForInteraction = true;
        yield return new WaitForSeconds(5f);
        checkForInteraction = false;
    }
    void CheckFront()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, interaction_point.forward, detect_distance, Interactable);
        if (hit.transform && !Dialogue_handler.instance.displaying && !overworld_actions.instance.using_ui)
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                if (hit.transform.GetComponent<Overworld_interactable>().interaction != null)
                {
                    Dialogue_handler.instance.Current_interaction = hit.transform.GetComponent<Overworld_interactable>().interaction;
                    Dialogue_handler.instance.Display(Dialogue_handler.instance.Current_interaction);
                }
            }
            if (Input.GetKeyDown(KeyCode.Q) && !overworld_actions.instance.doing_action)
            {
                if (hit.transform.gameObject.CompareTag("Water"))
                { 
                   overworld_actions.instance.fishingArea = hit.transform.GetComponent<Overworld_interactable>().area;
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
