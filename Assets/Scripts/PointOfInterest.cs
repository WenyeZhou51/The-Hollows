using UnityEngine;

public class PointOfInterest : MonoBehaviour
{
    [Header("Point of Interest Settings")]
    [Tooltip("The distance at which the camera starts focusing on this point of interest")]
    [SerializeField] private float activationDistance = 5f;
    
    [Tooltip("The offset from this object's position for the camera to focus on")]
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, 0, -10);
    
    [Tooltip("How quickly the camera transitions to focusing on this point")]
    [SerializeField] private float transitionSpeed = 2f;
    
    // Private references
    private Transform playerTransform;
    private CameraFollow cameraFollow;
    private Transform originalTarget;
    private bool isInfluencingCamera = false;
    private float currentInfluence = 0f;
    
    private void Start()
    {
        // Find the player transform and camera controller
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null)
        {
            Debug.LogError("PointOfInterest: Player not found! Make sure the player has the 'Player' tag.");
            enabled = false;
            return;
        }
        
        // Find the camera controller
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("PointOfInterest: Main camera not found!");
            enabled = false;
            return;
        }
        
        cameraFollow = mainCamera.GetComponent<CameraFollow>();
        if (cameraFollow == null)
        {
            Debug.LogError("PointOfInterest: CameraFollow script not found on main camera!");
            enabled = false;
            return;
        }
        
        // Store the original target
        originalTarget = cameraFollow.target;
    }
    
    private void Update()
    {
        if (playerTransform == null || cameraFollow == null) return;
        
        // Calculate distance to player
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // Determine if player is within activation distance
        if (distanceToPlayer <= activationDistance)
        {
            // Calculate influence based on how close the player is (closer = more influence)
            float targetInfluence = 1f - (distanceToPlayer / activationDistance);
            
            // Smoothly transition to full influence
            currentInfluence = Mathf.Lerp(currentInfluence, targetInfluence, Time.deltaTime * transitionSpeed);
            
            if (!isInfluencingCamera && currentInfluence > 0.01f)
            {
                isInfluencingCamera = true;
                StartInfluencingCamera();
            }
        }
        else
        {
            // Smoothly transition back to no influence
            currentInfluence = Mathf.Lerp(currentInfluence, 0f, Time.deltaTime * transitionSpeed);
            
            if (isInfluencingCamera && currentInfluence < 0.01f)
            {
                isInfluencingCamera = false;
                StopInfluencingCamera();
            }
        }
        
        // Update camera target if we're influencing it
        if (isInfluencingCamera)
        {
            UpdateCameraTarget();
        }
    }
    
    private void StartInfluencingCamera()
    {
        // We don't change the camera's target directly
        // Instead we'll use a custom target position in UpdateCameraTarget
    }
    
    private void StopInfluencingCamera()
    {
        // Restore original target
        cameraFollow.target = originalTarget;
    }
    
    private void UpdateCameraTarget()
    {
        // Create a temporary target position that blends between player and point of interest
        Vector3 playerPos = playerTransform.position;
        Vector3 poiPos = transform.position + cameraOffset;
        
        // Create a temporary GameObject to serve as the camera target
        GameObject tempTarget = new GameObject("TempCameraTarget");
        tempTarget.transform.position = Vector3.Lerp(playerPos, poiPos, currentInfluence);
        
        // Assign the temporary target to the camera follow script
        cameraFollow.target = tempTarget.transform;
        
        // Destroy the temporary target after a frame
        Destroy(tempTarget, Time.deltaTime);
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw the activation radius
        Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.4f);
        Gizmos.DrawSphere(transform.position, activationDistance);
        
        // Draw the camera focus point
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position + cameraOffset, 0.5f);
        
        // Draw a line between the object and the camera focus point
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + cameraOffset);
    }
} 