using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float minVerticalAngle = 10f;
    [SerializeField] private float maxVerticalAngle = 80f;

    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minDistance = 5f;
    [SerializeField] private float maxDistance = 50f;

    [Header("Pan Settings")]
    [SerializeField] private float panSpeed = 0.002f;
    [SerializeField] private float maxPanDistance = 40f;
    [SerializeField] private float scrollZoomSpeed = 3f;
    [SerializeField] private float orbitSpeed = 3f;

    private Vector3 panOffset;
    [Header("Key Bindings")]
    [SerializeField] private KeyCode rotateLeftKey = KeyCode.LeftArrow;
    [SerializeField] private KeyCode rotateRightKey = KeyCode.RightArrow;
    [SerializeField] private KeyCode rotateUpKey = KeyCode.UpArrow;
    [SerializeField] private KeyCode rotateDownKey = KeyCode.DownArrow;
    [SerializeField] private KeyCode zoomInKey = KeyCode.Equals; 
    [SerializeField] private KeyCode zoomOutKey = KeyCode.Minus; 
    [SerializeField] private KeyCode resetKey = KeyCode.R;

    [Header("References")]
    [SerializeField] private Transform targetPoint;
    [SerializeField] private Transform cameraTransform;

    // Private variables
    private float currentDistance;
    private float currentHorizontalAngle;
    private float currentVerticalAngle;
    private Vector3 targetPosition;

    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = transform;

        if (targetPoint == null)
        {
            GameObject homeBase = GameObject.FindGameObjectWithTag("MainTower");
            if (homeBase != null)
                targetPoint = homeBase.transform;
            else
                Debug.LogWarning("No target point assigned and no object with tag 'MainTower' found!");
        }

        InitializeCamera();
    }

    void InitializeCamera()
    {
        currentDistance = Vector3.Distance(cameraTransform.position, targetPoint.position);
        currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);

        Vector3 direction = (cameraTransform.position - targetPoint.position).normalized;
        currentHorizontalAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        currentVerticalAngle = Mathf.Asin(direction.y) * Mathf.Rad2Deg;
        currentVerticalAngle = Mathf.Clamp(currentVerticalAngle, minVerticalAngle, maxVerticalAngle);

        UpdateCameraPosition();
    }

    void Update()
    {
        if (Input.GetKey(rotateLeftKey))
        {
            RotateCameraHorizontal(-1f);
        }
        else if (Input.GetKey(rotateRightKey))
        {
            RotateCameraHorizontal(1f);
        }

        if (Input.GetKey(rotateUpKey))
        {
            RotateCameraVertical(1f); 
        }
        else if (Input.GetKey(rotateDownKey))
        {
            RotateCameraVertical(-1f);
        }

        // Handle zoom
        if (Input.GetKey(zoomInKey))
        {
            ZoomCamera(1f);
        }
        else if (Input.GetKey(zoomOutKey))
        {
            ZoomCamera(-1f);
        }

        //Mouse scroll zoom and drag-pan
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            currentDistance -= scroll * scrollZoomSpeed;
            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
        }

        if (Input.GetMouseButton(1))
        {
            currentHorizontalAngle -= Input.GetAxis("Mouse X") * orbitSpeed;
            currentVerticalAngle -= Input.GetAxis("Mouse Y") * orbitSpeed;
            currentVerticalAngle = Mathf.Clamp(currentVerticalAngle, minVerticalAngle, maxVerticalAngle);
        }
        // Handle reset
        if (Input.GetKeyDown(resetKey))
        {
            ResetCamera();
        }

        UpdateCameraPosition();
    }

    void RotateCameraHorizontal(float direction)
    {
        currentHorizontalAngle += direction * rotationSpeed * Time.deltaTime;
    }

    void RotateCameraVertical(float direction)
    {
        currentVerticalAngle += direction * rotationSpeed * Time.deltaTime;
        currentVerticalAngle = Mathf.Clamp(currentVerticalAngle, minVerticalAngle, maxVerticalAngle);
    }

    void ZoomCamera(float scrollInput)
    {
        currentDistance -= scrollInput * zoomSpeed * 10f * Time.deltaTime;
        currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
    }

    void UpdateCameraPosition()
    {
        float horizontalRad = currentHorizontalAngle * Mathf.Deg2Rad;
        float verticalRad = currentVerticalAngle * Mathf.Deg2Rad;

        Vector3 direction = new Vector3(
            Mathf.Sin(horizontalRad) * Mathf.Cos(verticalRad),
            Mathf.Sin(verticalRad),
            Mathf.Cos(horizontalRad) * Mathf.Cos(verticalRad)
        );

        Vector3 focus = targetPoint.position + panOffset;
        targetPosition = focus + direction * currentDistance;

        cameraTransform.position = targetPosition;
        cameraTransform.LookAt(focus);
    }

    public void SetTarget(Transform newTarget)
    {
        targetPoint = newTarget;
        InitializeCamera();
    }

    public void ResetCamera()
    {
        currentDistance = (minDistance + maxDistance) / 2f;
        currentHorizontalAngle = 0f;
        currentVerticalAngle = 45f;
        panOffset = Vector3.zero;
        UpdateCameraPosition();
    }

    void OnDrawGizmosSelected()
    {
        if (targetPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(targetPoint.position, currentDistance);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(targetPoint.position, cameraTransform.position);
        }
    }
}