using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class TransitionArea : MonoBehaviour, IInteractable
{
    [Header("Transition Settings")]
    [Tooltip("Name of the scene to transition to. IMPORTANT: Make sure this scene is added in File > Build Settings!")]
    [SerializeField] private string targetSceneName;
    [Tooltip("ID of the marker to spawn at in the target scene. Only needs to be unique within that scene.")]
    [SerializeField] private string targetMarkerId;
    
    [Header("Optional Settings")]
    [Tooltip("If true, transition happens automatically on trigger enter. If false, player must press interact key.")]
    [SerializeField] private bool autoTransition = true;
    [Tooltip("Optional message to display before transition")]
    [SerializeField] private string transitionMessage = "";
    
    // For detecting repeated transition attempts
    private float lastTransitionAttemptTime = 0f;
    private int consecutiveAttempts = 0;
    
    private void Start()
    {
        // Check if we're in the startroom - don't reset flags there as it needs the black screen
        bool isStartRoom = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Startroom") || 
                           UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("start_room");
                           
        if (isStartRoom)
        {
            Debug.Log("[TRANSITION AREA] Skipping flag reset in StartRoom to preserve initial black screen");
            return;
        }
        
        // Force reset transition state when any transition area loads
        SceneTransitionManager.ForceResetTransitionState();
        
        // Also use the instance method
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.CleanupTransitionState();
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the colliding object is the player
        if (other.CompareTag("Player") && autoTransition)
        {
            TriggerTransition(other.gameObject);
        }
    }
    
    // IInteractable implementation
    public void Interact()
    {
        // Find the player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && !autoTransition)
        {
            // Display transition message if specified
            if (!string.IsNullOrEmpty(transitionMessage) && DialogueManager.Instance != null)
            {
                // Show a quick message before transitioning
                DialogueManager.Instance.ShowDialogue(transitionMessage);
                
                // Wait a moment before transitioning to let player read the message
                StartCoroutine(DelayedTransition(player, 1.5f));
            }
            else
            {
                // Transition immediately
                TriggerTransition(player);
            }
        }
    }
    
    // Delay transition to allow reading message
    private IEnumerator DelayedTransition(GameObject player, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Close any open dialogue
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.CloseDialogue();
        }
        
        TriggerTransition(player);
    }
    
    // Called when player interacts with this area (for non-auto transitions)
    public void OnPlayerInteract(GameObject player)
    {
        if (!autoTransition)
        {
            TriggerTransition(player);
        }
    }
    
    private void TriggerTransition(GameObject player)
    {
        // Check if we're trying transitions too quickly (potential stuck situation)
        float timeSinceLastAttempt = Time.time - lastTransitionAttemptTime;
        lastTransitionAttemptTime = Time.time;
        
        // If we've tried to transition multiple times in quick succession, force reset the state
        if (timeSinceLastAttempt < 0.5f)
        {
            consecutiveAttempts++;
            
            // After 3 quick attempts, force reset the transition state
            if (consecutiveAttempts >= 3)
            {
                Debug.LogWarning($"[TRANSITION AREA] Detected {consecutiveAttempts} rapid transition attempts - forcing transition state reset");
                
                // Use the static reset method directly for the most aggressive reset
                SceneTransitionManager.ForceResetTransitionState();
                
                // Also use the instance method as a backup
                if (SceneTransitionManager.Instance != null)
                {
                    SceneTransitionManager.Instance.CleanupTransitionState();
                }
                
                // Reset counter
                consecutiveAttempts = 0;
            }
        }
        else
        {
            // Reset counter if attempts are spaced out
            consecutiveAttempts = 0;
        }
        
        // Make sure SceneTransitionManager exists
        SceneTransitionManager.EnsureExists();
        
        // Get a reference to use in our coroutines
        SceneTransitionManager transitionManager = SceneTransitionManager.Instance;
        
        // Call the transition method
        transitionManager.TransitionToScene(targetSceneName, targetMarkerId, player);
        
        Debug.Log($"Transitioning to scene: {targetSceneName} at marker ID: {targetMarkerId}");
        
        // Always add multiple failsafes with different timeouts
        StartCoroutine(TransitionFailsafe(transitionManager, 4f));
        StartCoroutine(TransitionFailsafe(transitionManager, 8f));
    }
    
    // Failsafe for stuck transitions in builds
    private IEnumerator TransitionFailsafe(SceneTransitionManager manager, float timeout)
    {
        // Wait for the specified timeout
        yield return new WaitForSecondsRealtime(timeout);
        
        // If we're still in the same scene, the transition might be stuck
        if (manager != null && SceneManager.GetActiveScene().name == gameObject.scene.name)
        {
            Debug.LogWarning($"Possible stuck transition detected after {timeout}s - attempting to reset transition state");
            
            // Call the more aggressive cleanup method
            manager.CleanupTransitionState();
        }
    }
    
    // Make the transition area visible in the editor
    private void OnDrawGizmos()
    {
        // Get the collider if it exists
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            // Set the color semi-transparent blue
            Gizmos.color = new Color(0, 0, 1, 0.3f);
            // Draw a cube using the box collider's bounds
            Gizmos.DrawCube(transform.position + (Vector3)boxCollider.offset, boxCollider.size);
            
            // Draw text for the target scene and marker
            if (!string.IsNullOrEmpty(targetSceneName))
            {
                #if UNITY_EDITOR
                // Display scene and marker info in editor for better readability
                UnityEditor.Handles.color = Color.white;
                string infoText = $"→ Scene: {targetSceneName}\n   Marker: {targetMarkerId}";
                if (!autoTransition)
                {
                    infoText += "\n   [Requires Interaction]";
                }
                UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, infoText);
                #endif
                
                Gizmos.color = Color.white;
                Vector3 textPos = transform.position + Vector3.up * 1.5f;
                Gizmos.DrawLine(transform.position, textPos);
            }
        }
    }
} 