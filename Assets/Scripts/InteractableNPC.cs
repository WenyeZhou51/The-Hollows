using UnityEngine;

public class InteractableNPC : MonoBehaviour, IInteractable
{
    [SerializeField] private string npcName = "NPC";
    [SerializeField] private TextAsset inkFile;
    [SerializeField] private bool resetOnInteract = true;
    
    private InkDialogueHandler inkHandler;
    private bool hasInteractedBefore = false;
    
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
            Debug.Log("Added BoxCollider2D to " + gameObject.name);
        }
        
        Debug.Log("InteractableNPC initialized on " + gameObject.name + " with name: " + npcName);
    }
    
    public void Interact()
    {
        Debug.Log($"NPC interaction triggered! NPC: {npcName}, resetOnInteract={resetOnInteract}");
        
        if (inkFile != null)
        {
            // Ensure the ink handler is initialized
            if (inkHandler == null)
            {
                inkHandler = gameObject.AddComponent<InkDialogueHandler>();
                inkHandler.InkJSON = inkFile;
            }
            
            // For NPC dialogue, we need to ALWAYS reset the story when interacting again
            // to avoid the "no active ink story" error
            if (resetOnInteract || hasInteractedBefore)
            {
                Debug.Log($"Resetting story for NPC {npcName}");
                inkHandler.ResetStory();
            }
            else
            {
                // Only initialize if not already initialized
                inkHandler.InitializeStory();
            }
            
            try
            {
                // Set the hasInteractedBefore variable in the Ink story
                // Use try-catch to handle case where variable doesn't exist in the Ink file
                inkHandler.SetStoryVariable("hasInteractedBefore", hasInteractedBefore);
                Debug.Log($"Set hasInteractedBefore to {hasInteractedBefore} for NPC {npcName}");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Variable 'hasInteractedBefore' not found in Ink story for {npcName}: {e.Message}");
                // This is fine - not all dialogue files need this variable
            }
            
            // Update the interaction state for future interactions
            hasInteractedBefore = true;
            
            // Start the ink dialogue - this will properly initialize the dialogue
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartInkDialogue(inkHandler);
                Debug.Log("Started Ink dialogue for NPC: " + npcName);
            }
            else
            {
                Debug.LogError("DialogueManager instance not found!");
            }
        }
        else
        {
            // Fallback to simple dialogue if no ink file is assigned
            string message = hasInteractedBefore ? 
                $"Talking to {npcName} again." : 
                $"Starting dialogue with {npcName}";
            
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.ShowDialogue(message);
                Debug.Log("Dialogue shown: " + message);
                
                // Update the interaction state for future interactions
                hasInteractedBefore = true;
            }
            else
            {
                Debug.LogError("DialogueManager instance not found!");
            }
        }
    }
} 