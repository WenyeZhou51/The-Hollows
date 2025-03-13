using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(OverworldSetupMenu))]
public class OverworldSetupMenuEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        OverworldSetupMenu setupMenu = (OverworldSetupMenu)target;
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Setup Camera Follow"))
        {
            setupMenu.SetupCameraFollow();
        }
        
        if (GUILayout.Button("Setup Dialogue System"))
        {
            setupMenu.SetupDialogueSystem();
        }
        
        if (GUILayout.Button("Setup Player"))
        {
            setupMenu.SetupPlayer();
        }
        
        if (GUILayout.Button("Setup Box"))
        {
            setupMenu.SetupBox();
        }
        
        if (GUILayout.Button("Setup NPC"))
        {
            setupMenu.SetupNPC();
        }
    }
}
#endif

public class OverworldSetupMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject playerObject;
    [SerializeField] private GameObject boxObject;
    [SerializeField] private GameObject npcObject;
    [SerializeField] private Camera mainCamera;
    
    [Header("Layers")]
    [SerializeField] private LayerMask interactableLayer;
    
    public void SetupCameraFollow()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("Main camera not found!");
                return;
            }
        }
        
        // Add or get camera follow script
        CameraFollow cameraFollow = mainCamera.gameObject.GetComponent<CameraFollow>();
        if (cameraFollow == null)
        {
            cameraFollow = mainCamera.gameObject.AddComponent<CameraFollow>();
        }
        
        // Set target to player
        if (playerObject != null)
        {
            cameraFollow.target = playerObject.transform;
            Debug.Log("Camera follow set up successfully!");
        }
        else
        {
            Debug.LogError("Player object not assigned!");
        }
    }
    
    public void SetupDialogueSystem()
    {
        // Create dialogue system if it doesn't exist
        GameObject dialogueSystem = GameObject.Find("DialogueSystem");
        if (dialogueSystem == null)
        {
            dialogueSystem = new GameObject("DialogueSystem");
            dialogueSystem.AddComponent<DialogueBoxCreator>();
            Debug.Log("Dialogue system created successfully!");
        }
        else
        {
            Debug.Log("Dialogue system already exists!");
        }
    }
    
    public void SetupPlayer()
    {
        if (playerObject == null)
        {
            Debug.LogError("Player object not assigned!");
            return;
        }
        
        // Add player controller if it doesn't exist
        PlayerController playerController = playerObject.GetComponent<PlayerController>();
        if (playerController == null)
        {
            playerController = playerObject.AddComponent<PlayerController>();
        }
        
        // Add rigidbody if it doesn't exist
        Rigidbody2D rb = playerObject.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = playerObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
        
        // Add collider if it doesn't exist
        BoxCollider2D collider = playerObject.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = playerObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.8f, 0.8f);
        }
        
        // Set layer mask for interaction
        playerController.GetType().GetField("interactableLayers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(playerController, interactableLayer);
        
        Debug.Log("Player setup completed successfully!");
    }
    
    public void SetupBox()
    {
        if (boxObject == null)
        {
            Debug.LogError("Box object not assigned!");
            return;
        }
        
        // Set layer to interactable
        if (interactableLayer != 0)
        {
            int layerIndex = (int)Mathf.Log(interactableLayer.value, 2);
            boxObject.layer = layerIndex;
        }
        
        // Add box collider if it doesn't exist
        BoxCollider2D collider = boxObject.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = boxObject.AddComponent<BoxCollider2D>();
        }
        
        // Add interactable box script if it doesn't exist
        InteractableBox interactableBox = boxObject.GetComponent<InteractableBox>();
        if (interactableBox == null)
        {
            interactableBox = boxObject.AddComponent<InteractableBox>();
        }
        
        Debug.Log("Box setup completed successfully!");
    }
    
    public void SetupNPC()
    {
        if (npcObject == null)
        {
            Debug.LogError("NPC object not assigned!");
            return;
        }
        
        // Set layer to interactable
        if (interactableLayer != 0)
        {
            int layerIndex = (int)Mathf.Log(interactableLayer.value, 2);
            npcObject.layer = layerIndex;
        }
        
        // Add box collider if it doesn't exist
        BoxCollider2D collider = npcObject.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = npcObject.AddComponent<BoxCollider2D>();
        }
        
        // Add interactable NPC script if it doesn't exist
        InteractableNPC interactableNPC = npcObject.GetComponent<InteractableNPC>();
        if (interactableNPC == null)
        {
            interactableNPC = npcObject.AddComponent<InteractableNPC>();
        }
        
        Debug.Log("NPC setup completed successfully!");
    }
} 