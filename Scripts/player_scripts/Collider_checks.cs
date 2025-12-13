using UnityEngine;


public class Collider_checks : MonoBehaviour
{
    public Area_manager area;
    private LayerMask _door;
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private float detectionDistance = 0.15f;
    private void Start()
    {
        _door = 1 << LayerMask.NameToLayer("Door");
    }
    private void Update()
    {
        if (area.currentArea == null) return;
        if(area.currentArea.areaData.insideArea)
            Player_movement.Instance.canUseBike = false;
    }
    
    private void OnCollisionStay2D(Collision2D collision)
    { 
        if (!collision.gameObject.CompareTag("Switch_Area")) return;
        var hit = Physics2D.Raycast(transform.position, interactionPoint.forward, detectionDistance, _door);
        if (!hit.transform) return; 
            var areaEntryPoint = collision.transform.GetComponent<Switch_Area>();
            if (areaEntryPoint.areaData.exitingArea)
                area.GoToOverworld();
            else
                area.EnterBuilding(areaEntryPoint,1f);
        
    }
}
