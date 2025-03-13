using UnityEngine;

// Add this component to an empty GameObject in your scene
public class OverworldSceneManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject playerObject;
    [SerializeField] private GameObject boxObject;
    [SerializeField] private GameObject npcObject;
    [SerializeField] private Camera mainCamera;
    
    private void Awake()
    {
        Debug.Log("OverworldSceneManager starting...");
        
        // Find objects if not assigned
        if (playerObject == null) playerObject = GameObject.Find("Player");
        if (boxObject == null) boxObject = GameObject.Find("Square");
        if (npcObject == null) npcObject = GameObject.Find("Door");
        if (mainCamera == null) mainCamera = Camera.main;
        
        // Add the bootstrapper component
        if (!gameObject.GetComponent<SceneBootstrapper>())
        {
            gameObject.AddComponent<SceneBootstrapper>();
        }
        
        Debug.Log("OverworldSceneManager initialized");
    }
    
    // This method can be called from the Inspector to manually set up the scene
    [ContextMenu("Setup Scene")]
    public void SetupScene()
    {
        Debug.Log("Manual scene setup requested");
        
        // Create the auto setup object
        GameObject setupObj = new GameObject("AutoSetup");
        setupObj.AddComponent<AutoSceneSetup>();
        
        Debug.Log("Manual scene setup complete");
    }
} 