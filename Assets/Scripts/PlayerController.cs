using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    
    [Header("Interaction Settings")]
    [SerializeField] private float interactionRadius = 2f;
    [SerializeField] private LayerMask interactableLayers = -1; // Default to everything
    
    [Header("UI References")]
    [SerializeField] private GameObject menuCanvas; // Reference to the existing menu canvas
    
    private Rigidbody2D rb;
    private Vector2 movement;
    private bool canMove = true;
    private PlayerInventory inventory;
    private bool isInventoryOpen = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // If there's no Rigidbody2D, add one
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f; // No gravity for top-down movement
            rb.freezeRotation = true; // Prevent rotation
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
        
        // Add a collider if there isn't one
        if (GetComponent<Collider2D>() == null)
        {
            BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.8f, 0.8f); // Slightly smaller than the sprite
        }
        
        // Get or add PlayerInventory component
        inventory = GetComponent<PlayerInventory>();
        if (inventory == null)
        {
            inventory = gameObject.AddComponent<PlayerInventory>();
        }
        
        // Make sure menu canvas is assigned
        if (menuCanvas == null)
        {
            // Try to find it by name in the scene
            menuCanvas = GameObject.Find("Menu Canvas");
            if (menuCanvas == null)
            {
                Debug.LogError("Menu Canvas not found in scene. Inventory UI won't work.");
            }
            else
            {
                Debug.Log("Found Menu Canvas in scene");
            }
        }

        if (menuCanvas != null)
        {
            // Initial setup - make sure it's inactive
            menuCanvas.SetActive(false);
            
            // Set up the inventory UI if it exists
            InventoryUI inventoryUI = menuCanvas.GetComponent<InventoryUI>();
            if (inventoryUI == null)
            {
                // Add the component if it doesn't exist
                inventoryUI = menuCanvas.AddComponent<InventoryUI>();
                Debug.Log("Added InventoryUI component to Menu Canvas");
            }
            
            // Connect the inventory to the UI
            inventoryUI.SetInventory(inventory);
        }
        
        // Ensure SceneTransitionManager exists
        SceneTransitionManager.EnsureExists();
        
        Debug.Log("PlayerController initialized. Interaction radius: " + interactionRadius + ", Layer mask: " + interactableLayers.value);
    }

    private void Update()
    {
        // Check if dialogue is active
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive())
        {
            // If Z key is pressed while dialogue is active
            if (Input.GetKeyDown(KeyCode.Z))
            {
                DialogueManager dialogueManager = DialogueManager.Instance;
                
                // CRITICAL FIX: Check if there are active choices first
                // Use reflection to access the private choiceButtons field
                System.Type type = dialogueManager.GetType();
                System.Reflection.FieldInfo choiceButtonsField = type.GetField("choiceButtons", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (choiceButtonsField != null)
                {
                    List<GameObject> choiceButtons = (List<GameObject>)choiceButtonsField.GetValue(dialogueManager);
                    
                    // If there are active choices, select the current choice instead of continuing
                    if (choiceButtons != null && choiceButtons.Count > 0)
                    {
                        // Get the current choice index
                        System.Reflection.FieldInfo currentChoiceIndexField = type.GetField("currentChoiceIndex", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        
                        if (currentChoiceIndexField != null)
                        {
                            int currentChoiceIndex = (int)currentChoiceIndexField.GetValue(dialogueManager);
                            
                            // Call MakeChoice with the current choice index
                            System.Reflection.MethodInfo makeChoiceMethod = type.GetMethod("MakeChoice", 
                                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                            
                            if (makeChoiceMethod != null)
                            {
                                Debug.Log($"Selecting choice {currentChoiceIndex} via interact key");
                                makeChoiceMethod.Invoke(dialogueManager, new object[] { currentChoiceIndex });
                            }
                        }
                        
                        // Don't continue processing - we've handled the choice selection
                        movement = Vector2.zero;
                        return;
                    }
                }
                
                // If no choices are active, try to continue the Ink story
                // Use reflection to access the private currentInkHandler field
                System.Reflection.FieldInfo inkHandlerField = type.GetField("currentInkHandler", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (inkHandlerField != null)
                {
                    InkDialogueHandler inkHandler = (InkDialogueHandler)inkHandlerField.GetValue(dialogueManager);
                    
                    // Check if dialogue can be advanced (text fully revealed and not waiting for key release)
                    if (dialogueManager.CanAdvanceDialogue())
                    {
                        if (inkHandler != null && inkHandler.HasNextLine())
                        {
                            // Continue the Ink story
                            dialogueManager.ContinueInkStory();
                        }
                        else
                        {
                            // Close the dialogue if there's no more content
                            dialogueManager.CloseDialogue();
                            canMove = true;
                        }
                    }
                    else
                    {
                        // If the text isn't fully revealed yet, let DialogueManager handle skipping typing
                        // We don't need to do anything here as DialogueManager will handle it
                        Debug.Log("Player pressed Z, but dialogue not ready to advance - letting DialogueManager handle it");
                    }
                }
                else
                {
                    // If we can't access the ink handler, just close the dialogue
                    dialogueManager.CloseDialogue();
                    canMove = true;
                }
            }
            
            // Don't process movement while dialogue is active
            movement = Vector2.zero;
            return;
        }
        else
        {
            // FIX: If dialogue is not active, ensure player can move
            // This fixes the bug where player can't move after dialogue ends
            canMove = !isInventoryOpen; // Only allow movement if inventory is closed
        }
        
        // Check for inventory toggle with Escape key instead of Tab
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleInventory();
        }
        
        // Don't process movement if inventory is open
        if (isInventoryOpen)
        {
            movement = Vector2.zero;
            return;
        }
        
        // Process movement input
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
        
        // Normalize diagonal movement
        if (movement.sqrMagnitude > 1f)
        {
            movement.Normalize();
        }
        
        // Check for interaction input
        if (Input.GetKeyDown(KeyCode.Z))
        {
            TryInteract();
        }
    }
    
    private void ToggleInventory()
    {
        if (menuCanvas == null)
        {
            Debug.LogWarning("Menu Canvas not assigned. Cannot toggle inventory.");
            return;
        }
        
        isInventoryOpen = !isInventoryOpen;
        menuCanvas.SetActive(isInventoryOpen);
        
        // Update UI if inventory is opened
        if (isInventoryOpen)
        {
            InventoryUI inventoryUI = menuCanvas.GetComponent<InventoryUI>();
            if (inventoryUI != null)
            {
                inventoryUI.RefreshInventoryUI();
            }
        }
    }
    
    private void FixedUpdate()
    {
        // Apply movement if allowed
        if (canMove)
        {
            rb.velocity = movement * moveSpeed;
        }
        else
        {
            rb.velocity = Vector2.zero;
        }
    }
    
    private void TryInteract()
    {
        Debug.Log("Trying to interact. Radius: " + interactionRadius);
        
        // First try with the specified layer mask
        if (interactableLayers.value != 0 && interactableLayers.value != -1)
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, interactionRadius, interactableLayers);
            if (TryInteractWithColliders(colliders))
            {
                return; // Successfully interacted with something
            }
        }
        
        // If that didn't work, try with all layers
        Collider2D[] allColliders = Physics2D.OverlapCircleAll(transform.position, interactionRadius);
        if (TryInteractWithColliders(allColliders))
        {
            return; // Successfully interacted with something
        }
        
        Debug.Log("No interactable objects found");
    }
    
    private bool TryInteractWithColliders(Collider2D[] colliders)
    {
        Debug.Log("Found " + colliders.Length + " colliders in range");
        
        float closestDistance = float.MaxValue;
        IInteractable closestInteractable = null;
        GameObject closestGameObject = null;
        
        foreach (Collider2D collider in colliders)
        {
            if (collider.gameObject == gameObject)
            {
                continue; // Skip self
            }
            
            Debug.Log("Checking collider on: " + collider.gameObject.name + " (Layer: " + LayerMask.LayerToName(collider.gameObject.layer) + ")");
            
            // Try to get an IInteractable component directly
            IInteractable interactable = collider.GetComponent<IInteractable>();
            
            // If not found directly, try to get it from the parent
            if (interactable == null && collider.transform.parent != null)
            {
                interactable = collider.transform.parent.GetComponent<IInteractable>();
            }
            
            // If still not found, try to get it from children
            if (interactable == null)
            {
                interactable = collider.GetComponentInChildren<IInteractable>();
            }
            
            if (interactable != null)
            {
                // Calculate distance to this interactable
                float distance = Vector2.Distance(transform.position, collider.transform.position);
                
                // If this is closer than the current closest, update
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestInteractable = interactable;
                    closestGameObject = collider.gameObject;
                }
            }
        }
        
        // After checking all colliders, interact with only the closest one
        if (closestInteractable != null)
        {
            Debug.Log("Interacting with closest: " + closestGameObject.name + " at distance: " + closestDistance);
            
            // Interact with the object
            closestInteractable.Interact();
            
            // Temporarily prevent movement during interaction
            canMove = false;
            
            // Start a coroutine to check when dialogue is closed
            StartCoroutine(CheckDialogueStatus());
            
            return true; // Successfully interacted
        }
        
        return false; // No interaction occurred
    }
    
    // Coroutine to check when dialogue is closed and restore movement
    private IEnumerator CheckDialogueStatus()
    {
        // Wait a short time to ensure dialogue is properly started
        yield return new WaitForSeconds(0.1f);
        
        // Keep checking until dialogue is no longer active
        while (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive())
        {
            yield return new WaitForSeconds(0.2f);
        }
        
        // Once dialogue is closed, restore movement
        Debug.Log("Dialogue closed, restoring player movement");
        canMove = true;
    }
    
    // Draw the interaction radius in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
} 