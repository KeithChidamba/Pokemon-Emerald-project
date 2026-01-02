
using UnityEngine;
using UnityEngine.UI;

public class DestinationPointer : MonoBehaviour
{
    private Image pointerUIImage;
    [SerializeField] float edgePadding = 20f;

    private Transform player;
    private Camera cam;
    private RectTransform canvasRect;
    public DestinationObjective objectiveData;
    public GameObject overworldObject;
    public bool displaying;
    private void Start()
    {
        overworldObject.SetActive(false);
        objectiveData.OnLoad += LoadPointer;
    }

    private void LoadPointer()
    {
        displaying = true;
        player = Player_movement.Instance.playerObject.transform;
        cam = Camera.main; 
        pointerUIImage = Game_ui_manager.Instance.destinationPointerUI;
        canvasRect = pointerUIImage.canvas.GetComponent<RectTransform>();
        Collider_checks.OnCollision += ConfirmDestination;
        overworldObject.SetActive(true);
        //call game ui to display objective data
    }

    private void ConfirmDestination(Transform currentCollision)
    {
        if (currentCollision.gameObject.CompareTag(objectiveData.destination))
        {
            Collider_checks.OnCollision -= ConfirmDestination;
            objectiveData.ClearObjective();
            overworldObject.SetActive(false);
            pointerUIImage.gameObject.SetActive(false);
            displaying = false;
        }
    }
    void Update()
    {
        if(!displaying)return;
        float deltaY = transform.position.y - player.position.y;
        float absY = Mathf.Abs(deltaY);

        float camHalfHeight = cam.orthographicSize;

        // Hide when within view
        if (absY <= camHalfHeight)
        {
            pointerUIImage.gameObject.SetActive(false);
            return;
        }

        pointerUIImage.gameObject.SetActive(true);

        bool isAbove = deltaY > 0f;

        float canvasHalfHeight = canvasRect.rect.height / 2f;

        float yPos = isAbove
            ? canvasHalfHeight - edgePadding
            : -canvasHalfHeight + edgePadding;

        var rt = pointerUIImage.rectTransform;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, yPos);
    }
}
