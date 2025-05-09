using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class LockedDoorway : MonoBehaviour, IInteractable
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
    
    private void Start()
    {
        // Make sure we have a collider
        if (GetComponent<Collider2D>() == null)
        {
            BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            Debug.Log("Added trigger BoxCollider2D to " + gameObject.name);
        }
        else
        {
            // Ensure the collider is set as a trigger
            Collider2D existingCollider = GetComponent<Collider2D>();
            existingCollider.isTrigger = true;
        }
        
        // Load state from PersistentGameManager
        LoadState();
        
        Debug.Log("LockedDoorway initialized on " + gameObject.name);
    }
    
    /// <summary>
    /// Loads the doorway state from the persistent manager
    /// </summary>
    private void LoadState()
    {
        // Make sure the manager exists
        PersistentGameManager.EnsureExists();
        
        // Get the current scene name
        string currentScene = SceneManager.GetActiveScene().name;
        
        // Check if the corresponding door is unlocked, as that determines if this doorway should be active
        bool shouldBeActive = PersistentGameManager.Instance.GetInteractableState(
            currentScene, 
            "LockedDoor", // This assumes the door is named "LockedDoor"
            false // Default to inactive
        );
        
        // Set active state based on the door's state
        gameObject.SetActive(shouldBeActive);
        
        Debug.Log($"Loaded state for doorway {gameObject.name} in scene {currentScene}: active = {shouldBeActive}");
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
        
        // CRITICAL FIX: Close any open dialogue BEFORE transition
        // This ensures dialogue system is properly reset before scene change
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.CloseDialogue();
            
            // Wait one additional frame to ensure dialogue is fully closed
            yield return null;
        }
        
        // Make sure DialogueManager isn't in an active state before transitioning
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive())
        {
            Debug.LogWarning("DialogueManager still active after attempting to close - forcing inactive state");
            yield return new WaitForSeconds(0.2f);
        }
        
        TriggerTransition(player);
    }
    
    private void TriggerTransition(GameObject player)
    {
        // Make sure SceneTransitionManager exists
        SceneTransitionManager.EnsureExists();
        
        // Call the transition method
        SceneTransitionManager.Instance.TransitionToScene(targetSceneName, targetMarkerId, player);
        
        Debug.Log($"Transitioning to scene: {targetSceneName} at marker ID: {targetMarkerId}");
    }
    
    // Make the transition area visible in the editor
    private void OnDrawGizmos()
    {
        // Get the collider if it exists
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            // Set the color semi-transparent green (different from regular transition areas)
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            // Draw a cube using the box collider's bounds
            Gizmos.DrawCube(transform.position + (Vector3)boxCollider.offset, boxCollider.size);
            
            // Draw text for the target scene and marker
            if (!string.IsNullOrEmpty(targetSceneName))
            {
                #if UNITY_EDITOR
                // Display scene and marker info in editor for better readability
                UnityEditor.Handles.color = Color.white;
                string infoText = $"→ Scene: {targetSceneName}\n   Marker: {targetMarkerId}\n   [Locked Doorway]";
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