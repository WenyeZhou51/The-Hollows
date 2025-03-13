using UnityEngine;

public class InteractableNPC : MonoBehaviour, IInteractable
{
    [SerializeField] private string npcName = "NPC";
    [SerializeField] private TextAsset inkFile;
    
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
            Debug.Log("Added BoxCollider2D to " + gameObject.name);
        }
        
        Debug.Log("InteractableNPC initialized on " + gameObject.name + " with name: " + npcName);
    }
    
    public void Interact()
    {
        Debug.Log("NPC interaction triggered! NPC: " + npcName);
        
        if (inkFile != null)
        {
            // Ensure the ink handler is initialized
            if (inkHandler == null)
            {
                inkHandler = gameObject.AddComponent<InkDialogueHandler>();
                inkHandler.InkJSON = inkFile;
            }
            
            // Explicitly initialize the story before starting dialogue
            inkHandler.InitializeStory();
            
            // Start the ink dialogue
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
            string message = $"Starting dialogue with {npcName}";
            
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.ShowDialogue(message);
                Debug.Log("Dialogue shown: " + message);
            }
            else
            {
                Debug.LogError("DialogueManager instance not found!");
            }
        }
    }
} 