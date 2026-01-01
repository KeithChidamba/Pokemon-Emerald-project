using UnityEngine;
[CreateAssetMenu(fileName = "des", menuName = "destination")]
public class DestinationObjective : StoryObjective
{
    private readonly string destination = "Destination";
    public GameObject destinationPrefab;
    
    public override void LoadObjective()
    {
        destinationPrefab.SetActive(true);
        destinationPrefab.GetComponent<DestinationPointer>().LoadPointer();
        Collider_checks.OnCollision += ConfirmDestination;
        //call game ui to display objective data
    }

    private void ConfirmDestination(Transform currentCollision)
    {
        Debug.Log("collided: " + currentCollision.name);
        if (currentCollision.gameObject.CompareTag(destination))
        {
            Collider_checks.OnCollision -= ConfirmDestination;
            ClearObjective();
        }
    }
    public override void ClearObjective()
    {
        destinationPrefab.GetComponent<DestinationPointer>().RemovePointer();
        destinationPrefab.SetActive(false);
    }
}
