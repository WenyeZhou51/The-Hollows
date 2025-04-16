using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// This component marks a position in the scene where the player can spawn
/// Each PlayerMarker must have a unique ID within the scene
/// </summary>
public class PlayerMarker : MonoBehaviour
{
    [Tooltip("Unique ID for this marker within the scene. Used to determine where the player spawns after scene transitions.")]
    [SerializeField] private string markerId;
    
    [Tooltip("Debug mode - Shows additional logs about this marker's operation")]
    [SerializeField] private bool debug = false;
    
    // Exposed property to access the marker ID
    public string MarkerId 
    { 
        get { return markerId; } 
    }
    
    // Full ID that includes scene name for global uniqueness
    public string FullMarkerId => $"{SceneManager.GetActiveScene().name}:{markerId}";
    
    private void Awake()
    {
        // Basic validation
        if (string.IsNullOrEmpty(markerId))
        {
            Debug.LogError($"PlayerMarker on {gameObject.name} has an empty ID! This will cause transition problems.", this);
        }
        
        // Log debug information if enabled
        if (debug)
        {
            Debug.Log($"PlayerMarker '{markerId}' initialized at position {transform.position}", this);
        }
    }
    
    private void Start()
    {
        // In build mode, run a validation check on all markers
        if (!Application.isEditor)
        {
            ValidateUniqueMarkerId();
            
            // CRITICAL BUILD FIX: Check if this marker should position the player
            string savedMarkerId = PlayerPrefs.GetString("LastTargetMarkerId", "");
            string currentScene = SceneManager.GetActiveScene().name;
            string savedScene = PlayerPrefs.GetString("LastTargetSceneName", "");
            
            if (PlayerPrefs.GetInt("NeedsPlayerSetup", 0) == 1 && 
                string.Equals(markerId, savedMarkerId, System.StringComparison.OrdinalIgnoreCase) &&
                string.Equals(currentScene, savedScene, System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogError($"[BUILD FIX] PlayerMarker '{markerId}' detected it needs to position the player!");
                // Clear flag to prevent duplicate positioning
                PlayerPrefs.SetInt("NeedsPlayerSetup", 0);
                PlayerPrefs.Save();
                
                // Start a coroutine to position the player
                StartCoroutine(PositionPlayerAtMarker());
            }
        }
    }
    
    // CRITICAL BUILD FIX: Direct player positioning from the marker itself
    private System.Collections.IEnumerator PositionPlayerAtMarker()
    {
        Debug.LogError($"[BUILD FIX] PlayerMarker '{markerId}' starting player positioning");
        
        // Wait a few frames to ensure player is fully loaded and initialized
        yield return null;
        yield return null;
        yield return new WaitForSeconds(0.2f);
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Debug.LogError($"[BUILD FIX] PlayerMarker found player at {player.transform.position}, will position at {transform.position}");
            
            // Cache original position
            Vector3 originalPos = player.transform.position;
            
            // Disable any components that might fight positioning
            DisablePlayerMovementComponents(player);
            
            // Set position with multiple methods for redundancy
            Vector3 markerPosition = GetMarkerPosition();
            player.transform.SetPositionAndRotation(markerPosition, player.transform.rotation);
            
            // Force update Rigidbody2D if present
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.simulated = false;
                rb.position = markerPosition;
                rb.velocity = Vector2.zero;
            }
            
            // Set position again to ensure it took effect
            player.transform.position = markerPosition;
            
            // Wait one frame
            yield return null;
            
            // Re-enable components
            EnablePlayerMovementComponents(player);
            
            // Log final position
            Debug.LogError($"[BUILD FIX] PlayerMarker positioned player from {originalPos} to {player.transform.position}");
        }
        else
        {
            Debug.LogError($"[BUILD FIX] PlayerMarker '{markerId}' couldn't find the player to position!");
        }
    }
    
    // CRITICAL BUILD FIX: Duplicate component disabling functionality here
    private void DisablePlayerMovementComponents(GameObject player)
    {
        Debug.LogError($"[BUILD FIX] PlayerMarker disabling player movement components");
        
        // Disable Rigidbody2D if present
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = false;
            rb.velocity = Vector2.zero;
        }
        
        // Disable all MonoBehaviours that might control movement
        MonoBehaviour[] components = player.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour component in components)
        {
            if (!component.GetType().Name.Contains("UI") && component != this)
            {
                component.enabled = false;
            }
        }
        
        // Also disable all colliders to ensure they don't interfere
        Collider2D[] colliders = player.GetComponents<Collider2D>();
        foreach (Collider2D collider in colliders)
        {
            collider.enabled = false;
        }
    }
    
    // CRITICAL BUILD FIX: Duplicate component enabling functionality here
    private void EnablePlayerMovementComponents(GameObject player)
    {
        Debug.LogError($"[BUILD FIX] PlayerMarker re-enabling player movement components");
        
        // Re-enable Rigidbody2D if present
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = true;
        }
        
        // Re-enable all MonoBehaviours
        MonoBehaviour[] components = player.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour component in components)
        {
            if (!component.GetType().Name.Contains("UI") && component != this)
            {
                component.enabled = true;
            }
        }
        
        // Also re-enable all colliders
        Collider2D[] colliders = player.GetComponents<Collider2D>();
        foreach (Collider2D collider in colliders)
        {
            collider.enabled = true;
        }
    }
    
    private void ValidateUniqueMarkerId()
    {
        PlayerMarker[] allMarkers = FindObjectsOfType<PlayerMarker>();
        
        // Count how many markers have the same ID
        int count = 0;
        foreach (PlayerMarker marker in allMarkers)
        {
            if (marker != this && string.Equals(marker.MarkerId, markerId, System.StringComparison.OrdinalIgnoreCase))
            {
                count++;
                Debug.LogWarning($"Duplicate PlayerMarker ID '{markerId}' found on objects {gameObject.name} and {marker.gameObject.name}", this);
            }
        }
        
        if (count > 0)
        {
            Debug.LogError($"PlayerMarker ID '{markerId}' is not unique in this scene! Found {count} other marker(s) with the same ID.", this);
        }
    }
    
    private void OnDrawGizmos()
    {
        // Make the marker visible in the Scene view
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position, 0.3f);
        
        // Draw directional indicator (an arrow)
        Gizmos.color = Color.blue;
        Vector3 direction = transform.right * 0.5f; // Point to the right in local space
        Gizmos.DrawLine(transform.position, transform.position + direction);
        
        // Draw a triangle for the arrowhead
        Vector3 arrowHeadPos = transform.position + direction;
        Vector3 arrowRight = arrowHeadPos - transform.up * 0.15f;
        Vector3 arrowLeft = arrowHeadPos + transform.up * 0.15f;
        Gizmos.DrawLine(arrowHeadPos, arrowRight);
        Gizmos.DrawLine(arrowHeadPos, arrowLeft);
        Gizmos.DrawLine(arrowRight, arrowLeft);
        
        // Make sure the ID is visible in the Scene view
#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, markerId);
#endif
    }
    
    // Public method to get the position - can be called from anywhere
    public Vector3 GetMarkerPosition()
    {
        if (!Application.isEditor)
        {
            // In build mode, ensure we're returning the most accurate position
            // by recalculating from the transform - some transforms might not be fully initialized
            // when the scene first loads in a build
            Vector3 worldPosition = transform.position;
            
            // Perform additional validation to ensure position is correct
            // This is a critical area for build reliability
            bool isValid = !float.IsNaN(worldPosition.x) && !float.IsNaN(worldPosition.y) && !float.IsNaN(worldPosition.z);
            if (!isValid)
            {
                Debug.LogError($"[BUILD FIX] CRITICAL ERROR: Invalid position detected in marker '{markerId}': {worldPosition}");
                // Fall back to local position calculation if world position is invalid
                worldPosition = transform.localPosition;
                if (transform.parent != null)
                {
                    worldPosition = transform.parent.TransformPoint(worldPosition);
                    Debug.LogError($"[BUILD FIX] Recalculated position from local coordinates: {worldPosition}");
                }
            }
            
            // Log position for debugging in build mode
            Debug.LogError($"[BUILD FIX] GetMarkerPosition called for marker '{markerId}' returning position {worldPosition}");
            
            return worldPosition;
        }
        
        // Default behavior for editor - transform.position is reliable here
        return transform.position;
    }
} 