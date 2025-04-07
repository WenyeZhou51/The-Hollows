using UnityEngine;

public class DialogueTriggerArea : MonoBehaviour
{
    [SerializeField] private string areaName = "Dialogue Area";
    [SerializeField] private TextAsset inkFile;
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private bool hasEnteredBefore = false;
    
    private InkDialogueHandler inkHandler;
    
    private void Awake()
    {
        // Add the InkDialogueHandler component if it doesn't exist
        inkHandler = GetComponent<InkDialogueHandler>();
        if (inkHandler == null)
        {
            inkHandler = gameObject.AddComponent<InkDialogueHandler>();
        }
        
        // Set the ink file
        if (inkFile != null)
        {
            inkHandler.InkJSON = inkFile;
        }
    }
    
    private void Start()
    {
        // Make sure we have a collider
        if (GetComponent<Collider2D>() == null)
        {
            BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true; // Make sure it's a trigger
            Debug.Log("Added BoxCollider2D to " + gameObject.name);
        }
        else
        {
            // If there's already a collider, make sure it's set as a trigger
            Collider2D existingCollider = GetComponent<Collider2D>();
            existingCollider.isTrigger = true;
        }
        
        Debug.Log("DialogueTriggerArea initialized on " + gameObject.name + " with name: " + areaName);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the object entering the trigger is the player
        if (other.CompareTag("Player"))
        {
            // If this trigger should only fire once and has already fired, don't trigger again
            if (triggerOnce && hasEnteredBefore)
            {
                return;
            }
            
            TriggerDialogue();
        }
    }
    
    private void TriggerDialogue()
    {
        Debug.Log($"Dialogue trigger activated! Area: {areaName}");
        
        if (inkFile != null)
        {
            // Ensure the ink handler is initialized
            if (inkHandler == null)
            {
                inkHandler = gameObject.AddComponent<InkDialogueHandler>();
                inkHandler.InkJSON = inkFile;
            }
            
            // Reset the story each time
            inkHandler.ResetStory();
            
            try
            {
                // Set the hasEnteredBefore variable in the Ink story
                // Use try-catch to handle case where variable doesn't exist in the Ink file
                inkHandler.SetStoryVariable("hasEnteredBefore", hasEnteredBefore);
                Debug.Log($"Set hasEnteredBefore to {hasEnteredBefore} for area {areaName}");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Variable 'hasEnteredBefore' not found in Ink story for {areaName}: {e.Message}");
                // This is fine - not all dialogue files need this variable
            }
            
            // Start the ink dialogue
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartInkDialogue(inkHandler);
                Debug.Log("Started Ink dialogue for area: " + areaName);
                
                // Update the trigger state for future triggers
                hasEnteredBefore = true;
            }
            else
            {
                Debug.LogError("DialogueManager instance not found!");
            }
        }
        else
        {
            // Fallback to simple dialogue if no ink file is assigned
            string message = hasEnteredBefore ? 
                $"Returning to {areaName}." : 
                $"You've discovered {areaName}!";
            
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.ShowDialogue(message);
                Debug.Log("Dialogue shown: " + message);
                
                // Update the trigger state for future triggers
                hasEnteredBefore = true;
            }
            else
            {
                Debug.LogError("DialogueManager instance not found!");
            }
        }
    }
    
    // Draw the trigger area in the editor
    private void OnDrawGizmos()
    {
        // Visualize the trigger area with a semi-transparent color
        Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.3f);
        
        // Use the collider bounds if available, otherwise use a default size
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            // Draw the actual collider shape
            if (collider is BoxCollider2D)
            {
                BoxCollider2D boxCollider = collider as BoxCollider2D;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(boxCollider.offset, boxCollider.size);
            }
            else
            {
                // For other collider types, just show the bounds
                Gizmos.DrawCube(collider.bounds.center, collider.bounds.size);
            }
        }
        else
        {
            // No collider yet, draw a default box
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero, new Vector3(2f, 2f, 0.1f));
        }
    }
} 