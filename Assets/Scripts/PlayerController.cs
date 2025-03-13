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
    
    private Rigidbody2D rb;
    private Vector2 movement;
    private bool canMove = true;

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
        
        Debug.Log("PlayerController initialized. Interaction radius: " + interactionRadius + ", Layer mask: " + interactableLayers.value);
    }

    private void Update()
    {
        // Check if dialogue is active
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive())
        {
            // If Z key is pressed while dialogue is active, continue or close the dialogue
            if (Input.GetKeyDown(KeyCode.Z))
            {
                // Try to continue the Ink story first
                DialogueManager dialogueManager = DialogueManager.Instance;
                
                // Use reflection to access the private currentInkHandler field
                System.Type type = dialogueManager.GetType();
                System.Reflection.FieldInfo inkHandlerField = type.GetField("currentInkHandler", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (inkHandlerField != null)
                {
                    InkDialogueHandler inkHandler = (InkDialogueHandler)inkHandlerField.GetValue(dialogueManager);
                    
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
                    // If we can't access the ink handler, just close the dialogue
                    dialogueManager.CloseDialogue();
                    canMove = true;
                }
            }
            
            // Don't process movement while dialogue is active
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
                Debug.Log("Found interactable on: " + collider.gameObject.name);
                
                // Interact with the object
                interactable.Interact();
                canMove = false; // Prevent movement during interaction
                return true; // Successfully interacted
            }
        }
        
        return false; // No interaction occurred
    }
    
    // Draw the interaction radius in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
} 