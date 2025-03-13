using UnityEngine;

// Add this component to a GameObject in your scene to ensure the setup runs
[DefaultExecutionOrder(-100)] // Ensure this runs before other scripts
public class SceneBootstrapper : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log("SceneBootstrapper starting...");
        
        // Check if RuntimeSetupInitializer already exists
        RuntimeSetupInitializer initializer = FindObjectOfType<RuntimeSetupInitializer>();
        if (initializer == null)
        {
            // Add RuntimeSetupInitializer to this GameObject
            gameObject.AddComponent<RuntimeSetupInitializer>();
            Debug.Log("Added RuntimeSetupInitializer");
        }
    }
} 