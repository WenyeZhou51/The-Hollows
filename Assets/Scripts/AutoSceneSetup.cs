using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AutoSceneSetup : MonoBehaviour
{
    // This script will automatically set up the scene when it starts
    
    private void Awake()
    {
        Debug.Log("AutoSceneSetup starting...");
        
        // Create the interactable layer if it doesn't exist
        CreateInteractableLayer();
        
        // Create the dialogue system
        SetupDialogueSystem();
        
        // Set up the player
        SetupPlayer();
        
        // Set up interactable objects
        SetupInteractables();
        
        // Set up camera follow
        SetupCameraFollow();
        
        Debug.Log("AutoSceneSetup complete!");
        
        // Destroy this object after setup is complete
        Destroy(gameObject);
    }
    
    private void CreateInteractableLayer()
    {
        Debug.Log("Checking for Interactable layer...");
        
        // Check if the Interactable layer already exists
        int interactableLayer = LayerMask.NameToLayer("Interactable");
        if (interactableLayer == -1)
        {
            Debug.LogWarning("Interactable layer not found! Objects will use default layer.");
            Debug.LogWarning("Please create an 'Interactable' layer in Edit > Project Settings > Tags and Layers");
        }
        else
        {
            Debug.Log("Interactable layer found at index: " + interactableLayer);
        }
    }
    
    private void SetupDialogueSystem()
    {
        Debug.Log("Setting up dialogue system...");
        
        // Check if dialogue system already exists
        if (DialogueManager.Instance != null)
        {
            Debug.Log("DialogueManager already exists");
            return;
        }
        
        // Create canvas - make sure it's active
        GameObject canvasObj = new GameObject("DialogueCanvas");
        canvasObj.SetActive(true); // Explicitly set canvas active
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        // Add canvas scaler
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // Add graphic raycaster
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Create dialogue panel
        GameObject panelObj = new GameObject("DialoguePanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        
        // Add image component for background
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f);
        
        // Set panel position and size
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.1f, 0.1f);
        panelRect.anchorMax = new Vector2(0.9f, 0.3f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        // Create text object
        GameObject textObj = new GameObject("DialogueText");
        textObj.transform.SetParent(panelObj.transform, false);
        
        // Add TextMeshPro component
        TextMeshProUGUI dialogueText = textObj.AddComponent<TextMeshProUGUI>();
        dialogueText.alignment = TextAlignmentOptions.Left;
        dialogueText.fontSize = 36;
        dialogueText.color = Color.white;
        dialogueText.text = "Dialogue Text";
        
        // Set text position and size
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.05f, 0.1f);
        textRect.anchorMax = new Vector2(0.95f, 0.9f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        // Create choices panel
        GameObject choicesPanelObj = new GameObject("ChoicesPanel");
        choicesPanelObj.transform.SetParent(canvasObj.transform, false);
        
        // Add image component for background
        Image choicesPanelImage = choicesPanelObj.AddComponent<Image>();
        choicesPanelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        
        // Set panel position and size
        RectTransform choicesPanelRect = choicesPanelObj.GetComponent<RectTransform>();
        choicesPanelRect.anchorMin = new Vector2(0.3f, 0.35f);
        choicesPanelRect.anchorMax = new Vector2(0.7f, 0.65f);
        choicesPanelRect.offsetMin = Vector2.zero;
        choicesPanelRect.offsetMax = Vector2.zero;
        
        // Add vertical layout group
        VerticalLayoutGroup layoutGroup = choicesPanelObj.AddComponent<VerticalLayoutGroup>();
        layoutGroup.padding = new RectOffset(10, 10, 10, 10);
        layoutGroup.spacing = 10;
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.childControlHeight = true;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childForceExpandWidth = true;
        
        // Create choice button prefab
        GameObject choiceButtonPrefab = new GameObject("ChoiceButtonPrefab");
        // Don't parent the prefab to the canvas - keep it as a separate object
        // choiceButtonPrefab.transform.SetParent(canvasObj.transform, false);
        
        // Add button component
        Button buttonComponent = choiceButtonPrefab.AddComponent<Button>();
        Image buttonImage = choiceButtonPrefab.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        
        // Add text to button
        GameObject buttonTextObj = new GameObject("Text");
        buttonTextObj.transform.SetParent(choiceButtonPrefab.transform, false);
        
        TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.fontSize = 24;
        buttonText.color = Color.white;
        buttonText.text = "Choice";
        
        // Set text position and size
        RectTransform buttonTextRect = buttonTextObj.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = new Vector2(10, 5);
        buttonTextRect.offsetMax = new Vector2(-10, -5);
        
        // Set button colors
        ColorBlock colors = buttonComponent.colors;
        colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        colors.pressedColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        colors.selectedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        buttonComponent.colors = colors;
        
        // Make sure the button has a RectTransform
        RectTransform buttonRect = choiceButtonPrefab.GetComponent<RectTransform>();
        if (buttonRect == null)
        {
            buttonRect = choiceButtonPrefab.AddComponent<RectTransform>();
        }
        buttonRect.sizeDelta = new Vector2(200, 50); // Set a default size
        
        // Don't deactivate the prefab - the DialogueManager will handle activation
        // choiceButtonPrefab.SetActive(false);
        
        // Create dialogue manager
        GameObject managerObj = new GameObject("DialogueManager");
        DialogueManager manager = managerObj.AddComponent<DialogueManager>();
        
        // Set references using reflection - FIXING THE SERIALIZEFIELD ISSUE
        // We need to use different binding flags to access SerializeField private fields
        var bindingFlags = System.Reflection.BindingFlags.NonPublic | 
                           System.Reflection.BindingFlags.Instance | 
                           System.Reflection.BindingFlags.Public;
                           
        System.Type type = manager.GetType();
        var panelField = type.GetField("dialoguePanel", bindingFlags);
        var textField = type.GetField("dialogueText", bindingFlags);
        var choicesPanelField = type.GetField("choicesPanel", bindingFlags);
        var choiceButtonPrefabField = type.GetField("choiceButtonPrefab", bindingFlags);
        
        // Log what we found via reflection
        Debug.Log($"Reflection found fields - Panel: {(panelField != null ? "Found" : "NOT FOUND")}, " +
                 $"Text: {(textField != null ? "Found" : "NOT FOUND")}, " +
                 $"ChoicesPanel: {(choicesPanelField != null ? "Found" : "NOT FOUND")}, " +
                 $"ButtonPrefab: {(choiceButtonPrefabField != null ? "Found" : "NOT FOUND")}");
        
        // Use the new direct initialization method - this is the key fix
        manager.Initialize(panelObj, dialogueText, choicesPanelObj, choiceButtonPrefab);
        Debug.Log("Used direct initialization method instead of reflection");
        
        // As a fallback, also try the reflection approach
        if (panelField != null && textField != null && choicesPanelField != null && choiceButtonPrefabField != null)
        {
            // Set via reflection
            panelField.SetValue(manager, panelObj);
            textField.SetValue(manager, dialogueText);
            choicesPanelField.SetValue(manager, choicesPanelObj);
            choiceButtonPrefabField.SetValue(manager, choiceButtonPrefab);
            
            // Verify the references were set
            var verifyPanel = panelField.GetValue(manager) as GameObject;
            var verifyText = textField.GetValue(manager) as TextMeshProUGUI;
            
            Debug.Log($"Verified references - Panel: {(verifyPanel != null ? verifyPanel.name : "NULL")}, " +
                     $"Text: {(verifyText != null ? verifyText.name : "NULL")}");
                     
            Debug.Log("Set DialogueManager references using reflection");
        }
        else
        {
            Debug.LogError("Failed to find some DialogueManager fields via reflection!");
        }
        
        // Hide dialogue panel initially but keep canvas active
        panelObj.SetActive(false);
        choicesPanelObj.SetActive(false);
        // Ensure canvas stays active
        canvasObj.SetActive(true);
        
        Debug.Log("Dialogue system setup complete");
    }
    
    private void SetupPlayer()
    {
        Debug.Log("Setting up player...");
        
        // Find player object
        GameObject playerObj = GameObject.Find("Player");
        if (playerObj == null)
        {
            Debug.LogError("Player object not found!");
            return;
        }
        
        // Add player controller if it doesn't exist
        PlayerController playerController = playerObj.GetComponent<PlayerController>();
        if (playerController == null)
        {
            playerController = playerObj.AddComponent<PlayerController>();
            Debug.Log("Added PlayerController to Player");
        }
        
        // Add rigidbody if it doesn't exist
        Rigidbody2D rb = playerObj.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = playerObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            Debug.Log("Added Rigidbody2D to Player");
        }
        
        // Add collider if it doesn't exist
        BoxCollider2D collider = playerObj.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = playerObj.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.8f, 0.8f);
            Debug.Log("Added BoxCollider2D to Player");
        }
        
        // Set the layer mask for interaction
        int interactableLayer = LayerMask.NameToLayer("Interactable");
        if (interactableLayer != -1)
        {
            // Set the interactable layers field using reflection
            System.Type type = playerController.GetType();
            System.Reflection.FieldInfo layersField = type.GetField("interactableLayers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (layersField != null)
            {
                int layerMask = 1 << interactableLayer;
                layersField.SetValue(playerController, layerMask);
                Debug.Log("Set player's interactableLayers to layer " + interactableLayer);
            }
        }
        
        Debug.Log("Player setup complete");
    }
    
    private void SetupInteractables()
    {
        Debug.Log("Setting up interactable objects...");
        
        // Get the interactable layer
        int interactableLayer = LayerMask.NameToLayer("Interactable");
        if (interactableLayer == -1)
        {
            Debug.LogWarning("Interactable layer not found! Using default layer for interactables.");
            interactableLayer = 0; // Default layer
        }
        
        // Find box object (Square)
        GameObject boxObj = GameObject.Find("Square");
        if (boxObj != null)
        {
            // Set layer
            boxObj.layer = interactableLayer;
            Debug.Log("Set Square layer to " + LayerMask.LayerToName(interactableLayer));
            
            // Add interactable component
            InteractableBox interactableBox = boxObj.GetComponent<InteractableBox>();
            if (interactableBox == null)
            {
                interactableBox = boxObj.AddComponent<InteractableBox>();
                Debug.Log("Added InteractableBox to Square");
            }
            
            // Add collider if it doesn't exist
            BoxCollider2D collider = boxObj.GetComponent<BoxCollider2D>();
            if (collider == null)
            {
                collider = boxObj.AddComponent<BoxCollider2D>();
                Debug.Log("Added BoxCollider2D to Square");
            }
            
            // Ensure the collider matches the sprite size
            SpriteRenderer spriteRenderer = boxObj.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && collider != null)
            {
                collider.size = spriteRenderer.sprite.bounds.size;
                Debug.Log("Adjusted Square collider size to match sprite");
            }
        }
        else
        {
            Debug.LogWarning("Box object (Square) not found!");
        }
        
        // Find NPC object (Door)
        GameObject npcObj = GameObject.Find("Door");
        if (npcObj != null)
        {
            // Set layer
            npcObj.layer = interactableLayer;
            Debug.Log("Set Door layer to " + LayerMask.LayerToName(interactableLayer));
            
            // Add interactable component
            InteractableNPC interactableNPC = npcObj.GetComponent<InteractableNPC>();
            if (interactableNPC == null)
            {
                interactableNPC = npcObj.AddComponent<InteractableNPC>();
                Debug.Log("Added InteractableNPC to Door");
            }
            
            // Add collider if it doesn't exist
            BoxCollider2D collider = npcObj.GetComponent<BoxCollider2D>();
            if (collider == null)
            {
                collider = npcObj.AddComponent<BoxCollider2D>();
                Debug.Log("Added BoxCollider2D to Door");
            }
            
            // Ensure the collider matches the sprite size
            SpriteRenderer spriteRenderer = npcObj.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && collider != null)
            {
                collider.size = spriteRenderer.sprite.bounds.size;
                Debug.Log("Adjusted Door collider size to match sprite");
            }
        }
        else
        {
            Debug.LogWarning("NPC object (Door) not found!");
        }
        
        Debug.Log("Interactable objects setup complete");
    }
    
    private void SetupCameraFollow()
    {
        Debug.Log("Setting up camera follow...");
        
        // Find camera
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main camera not found!");
            return;
        }
        
        // Find player
        GameObject playerObj = GameObject.Find("Player");
        if (playerObj == null)
        {
            Debug.LogError("Player object not found!");
            return;
        }
        
        // Add camera follow component
        CameraFollow cameraFollow = mainCamera.gameObject.GetComponent<CameraFollow>();
        if (cameraFollow == null)
        {
            cameraFollow = mainCamera.gameObject.AddComponent<CameraFollow>();
            Debug.Log("Added CameraFollow to Main Camera");
        }
        
        // Set target
        cameraFollow.target = playerObj.transform;
        Debug.Log("Set camera target to Player");
        
        Debug.Log("Camera follow setup complete");
    }
} 