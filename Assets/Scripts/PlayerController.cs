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
    
    [Header("Animation")]
    [SerializeField] private Animator animator; // Reference to the animator component
    
    private Rigidbody2D rb;
    private Vector2 movement;
    private bool canMove = true;
    private PlayerInventory inventory;
    private bool isInventoryOpen = false;
    private SpriteRenderer spriteRenderer;
    
    // Last direction the player was facing
    private Vector2 lastDirection = Vector2.down; // Default facing down

    // CRITICAL: Add a cooldown flag and timer to prevent multiple interactions from the same key press
    private float interactionCooldown = 0.3f; // Seconds to wait after dialogue closes before allowing interaction
    private float lastDialogueEndTime = 0f;   // When the last dialogue ended
    private bool canInteract = true;          // Whether player can currently interact

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
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
        
        // Get Animator component if not assigned
        if (animator == null)
        {
            animator = GetComponent<Animator>();
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

    // Subscribe to dialogue state changes
    private void OnEnable()
    {
        DialogueManager.OnDialogueStateChanged += HandleDialogueStateChanged;
    }
    
    // Unsubscribe from dialogue state changes
    private void OnDisable()
    {
        DialogueManager.OnDialogueStateChanged -= HandleDialogueStateChanged;
    }
    
    // Handle dialogue state changes to detect when dialogue ends
    private void HandleDialogueStateChanged(bool isActive)
    {
        if (!isActive) // Dialogue just ended
        {
            // Start the interaction cooldown
            lastDialogueEndTime = Time.time;
            canInteract = false;
            Debug.Log($"[Player Debug] Dialogue ended, starting interaction cooldown");
        }
    }

    private void Update()
    {
        // Handle keyboard input for inventory
        if (Input.GetKeyDown(KeyCode.I) && canMove)
        {
            ToggleInventory();
        }
        
        // Update the interaction cooldown
        if (!canInteract && Time.time >= lastDialogueEndTime + interactionCooldown)
        {
            canInteract = true;
            Debug.Log("[Player Debug] Interaction cooldown ended, player can interact again");
        }
        
        // Check if screen is fading - disable movement during fade
        if (ScreenFader.Instance != null && ScreenFader.Instance.IsFading)
        {
            // If we're fading, ensure player can't move
            movement = Vector2.zero;
            return;
        }
        
        // If the inventory is open, don't process movement and dialogue controls
        if (isInventoryOpen)
        {
            if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Escape))
            {
                // Close inventory on confirm, cancel, or escape keys
                isInventoryOpen = false;
                menuCanvas.SetActive(false);
            }
            
            return;
        }
        
        // Check for ESC key to open inventory when it's closed
        if (Input.GetKeyDown(KeyCode.Escape) && canMove)
        {
            ToggleInventory();
            return;
        }
        
        // Check if dialogue is active
        bool isDialogueActive = (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive());
        
        // If dialogue is active, handle dialogue controls
        if (isDialogueActive)
        {
            // Don't handle any other controls while dialogue is active
            // The DialogueManager itself handles advancing dialogue
            return;
        }
        
        // Handle movement input if player can move
        if (canMove)
        {
            movement.x = Input.GetAxisRaw("Horizontal");
            movement.y = Input.GetAxisRaw("Vertical");
            
            // Normalize diagonal movement to prevent faster speeds
            if (movement.magnitude > 1f)
            {
                movement.Normalize();
            }
        }
        else
        {
            // Player cannot move, ensure no residual movement
            movement = Vector2.zero;
        }
        
        // Update animation based on movement direction
        UpdateAnimation(movement);
        
        // Check for Z key press - handle both dialogue and interaction exclusively
        if (Input.GetKeyDown(KeyCode.Z))
        {
            // First check if DialogueManager exists and is actually active
            isDialogueActive = (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive());
            
            if (!isDialogueActive && canInteract)  // CRITICAL FIX: Only allow interaction if cooldown has expired
            {
                Debug.Log("[Player Debug] Z pressed, dialogue NOT active and cooldown expired - trying to interact");
                TryInteract();
            }
            else if (!canInteract)
            {
                Debug.Log("[Player Debug] Z pressed but interaction on cooldown - ignoring");
            }
            // Note: If dialogue is active, it's already handled at the beginning of this Update method
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
            
            // Set idle animation when not moving
            if (animator != null)
            {
                animator.SetBool("IsMoving", false);
            }
        }
    }
    
    private void TryInteract()
    {
        // SAFETY CHECK: Double-check that dialogue is not active before attempting interaction
        // This catches any case where dialogue might become active between the Update check and this method
        if (DialogueManager.Instance != null)
        {
            bool dialogueActive = DialogueManager.Instance.IsDialogueActive();
            if (dialogueActive)
            {
                Debug.Log("[Player Debug] SAFETY: TryInteract caught active dialogue, aborting interaction");
                return;
            }
        }
        
        Debug.Log("[Player Debug] TryInteract proceeding - dialogue confirmed inactive");
        
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
        
        Debug.Log("[Player Debug] No interactable objects found");
    }
    
    private bool TryInteractWithColliders(Collider2D[] colliders)
    {
        // CRITICAL FIX: Check cooldown again to ensure we don't interact if recently ended dialogue
        if (!canInteract)
        {
            Debug.Log("[Player Debug] Attempted interaction during cooldown period - ignoring");
            return false;
        }
        
        Debug.Log("[Player Debug] Found " + colliders.Length + " colliders in range");
        
        float closestDistance = float.MaxValue;
        IInteractable closestInteractable = null;
        GameObject closestGameObject = null;
        
        foreach (Collider2D collider in colliders)
        {
            if (collider.gameObject == gameObject)
            {
                continue; // Skip self
            }
            
            Debug.Log("[Player Debug] Checking collider on: " + collider.gameObject.name + " (Layer: " + LayerMask.LayerToName(collider.gameObject.layer) + ")");
            
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
            Debug.Log("[Player Debug] Interacting with closest: " + closestGameObject.name + " at distance: " + closestDistance);
            
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
        Debug.Log("[Player Debug] Dialogue closed, restoring player movement");
        canMove = true;
    }
    
    // Draw the interaction radius in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
    
    // Updates animation based on movement input
    private void UpdateAnimation(Vector2 moveInput)
    {
        if (animator == null) return;
        
        // Check if we're moving or not
        bool isMoving = moveInput.sqrMagnitude > 0;
        
        // Only update direction if actually moving
        if (isMoving)
        {
            // If we were previously stopped, reset animator speed
            if (animator.speed == 0)
            {
                animator.speed = 1;
            }
            
            lastDirection = moveInput;
            
            // Set animation parameters
            animator.SetBool("IsMoving", true);
            animator.SetFloat("Horizontal", lastDirection.x);
            animator.SetFloat("Vertical", lastDirection.y);
            
            // Handle flipping sprite for left/right movement
            if (spriteRenderer != null && Mathf.Abs(lastDirection.x) > Mathf.Abs(lastDirection.y))
            {
                // Flip sprite when moving left
                spriteRenderer.flipX = (lastDirection.x < 0);
            }
        }
        else
        {
            // When not moving, pause the animator to freeze on current frame
            animator.SetBool("IsMoving", false);
            animator.speed = 0;
            
            // Keep the direction values (don't update them)
        }
    }
} 