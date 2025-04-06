using UnityEngine;

/// <summary>
/// Initializes the ComicsDisplayController at the start of the game.
/// Attach this script to a GameObject in a startup scene.
/// </summary>
public class ComicsDisplayInitializer : MonoBehaviour
{
    [SerializeField] private bool debugMode = true;
    
    private void Awake()
    {
        if (debugMode) Debug.Log("[ComicsInitializer] Checking for existing ComicsDisplayController");
        
        // Check if there's already an instance
        if (ComicsDisplayController.Instance == null)
        {
            if (debugMode) Debug.Log("[ComicsInitializer] No ComicsDisplayController found, creating one");
            
            // Create a new controller
            GameObject controllerObject = new GameObject("ComicsDisplayController");
            ComicsDisplayController controller = controllerObject.AddComponent<ComicsDisplayController>();
            
            if (debugMode) Debug.Log("[ComicsInitializer] ComicsDisplayController created successfully");
            
            // Don't destroy it when loading a new scene
            DontDestroyOnLoad(controllerObject);
        }
        else
        {
            if (debugMode) Debug.Log("[ComicsInitializer] Existing ComicsDisplayController found");
        }
    }
} 