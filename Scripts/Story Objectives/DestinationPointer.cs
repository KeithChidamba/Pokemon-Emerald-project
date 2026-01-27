
using UnityEngine;
using UnityEngine.UI;

public class DestinationPointer : MonoBehaviour
{
    private Image pointerUIImage;
    [SerializeField] float edgePadding = 20f;
    
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
        cam = Camera.main; 
        pointerUIImage = Game_ui_manager.Instance.destinationPointerUI;
        canvasRect = pointerUIImage.canvas.GetComponent<RectTransform>();
        Collider_checks.OnCollision += ConfirmDestination;
        overworldObject.SetActive(true);
    }

    private void ConfirmDestination(Transform currentCollision)
    {
        if (currentCollision.gameObject.CompareTag(objectiveData.destinationTag))
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
        Vector3 viewportPos = cam.WorldToViewportPoint(transform.position);

// Hide if inside view
        bool isInsideView =
            viewportPos.x >= 0f && viewportPos.x <= 1f &&
            viewportPos.y >= 0f && viewportPos.y <= 1f &&
            viewportPos.z > 0f;

        if (isInsideView)
        {
            pointerUIImage.gameObject.SetActive(false);
            return;
        }
        pointerUIImage.gameObject.SetActive(true);

// Clamp viewport position to screen edge
        float margin = 0.05f;

        float clampedX = Mathf.Clamp(viewportPos.x, margin, 1f - margin);
        float clampedY = Mathf.Clamp(viewportPos.y, margin, 1f - margin);

// Convert viewport → canvas space
        float canvasWidth  = canvasRect.rect.width;
        float canvasHeight = canvasRect.rect.height;

        float xPos = (clampedX - 0.5f) * canvasWidth;
        float yPos = (clampedY - 0.5f) * canvasHeight;

// Apply padding toward center
        Vector2 dir = new Vector2(xPos, yPos).normalized;
        Vector2 paddedPos = new Vector2(xPos, yPos) - dir * edgePadding;

        pointerUIImage.rectTransform.anchoredPosition = paddedPos;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        pointerUIImage.rectTransform.rotation = Quaternion.Euler(0, 0, angle);
    }
}
