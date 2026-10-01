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

    [Header("Key Bindings")]
    [SerializeField] private KeyCode rotateLeftKey = KeyCode.LeftArrow;
    [SerializeField] private KeyCode rotateRightKey = KeyCode.RightArrow;
    [SerializeField] private KeyCode rotateUpKey = KeyCode.UpArrow;
    [SerializeField] private KeyCode rotateDownKey = KeyCode.DownArrow;
    [SerializeField] private KeyCode zoomInKey = KeyCode.Equals; // = key
    [SerializeField] private KeyCode zoomOutKey = KeyCode.Minus; // - key
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

        targetPosition = targetPoint.position + direction * currentDistance;

        cameraTransform.position = targetPosition;

        cameraTransform.LookAt(targetPoint.position);
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