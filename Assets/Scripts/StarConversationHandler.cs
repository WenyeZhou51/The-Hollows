using UnityEngine;

public class StarConversationHandler : MonoBehaviour
{
    [SerializeField] private TextAsset inkDialogueFile;
    private InkDialogueHandler inkHandler;
    
    private void Awake()
    {
        // Add the InkDialogueHandler component if it doesn't exist
        inkHandler = GetComponent<InkDialogueHandler>();
        if (inkHandler == null)
        {
            inkHandler = gameObject.AddComponent<InkDialogueHandler>();
        }
        
        // Set the ink file if provided
        if (inkDialogueFile != null)
        {
            inkHandler.InkJSON = inkDialogueFile;
        }
        else
        {
            // Try to load the ink file from resources if not assigned
            TextAsset inkFile = Resources.Load<TextAsset>("Ink/StarConversation");
            if (inkFile != null)
            {
                inkHandler.InkJSON = inkFile;
            }
            else
            {
                Debug.LogError("StarConversationHandler: Ink file not assigned and couldn't be found in Resources!");
            }
        }
    }
    
    public void StartStarConversation()
    {
        if (inkHandler != null)
        {
            // Make sure PersistentGameManager exists
            PersistentGameManager.EnsureExists();
            
            // Get death count from PersistentGameManager
            int deathCount = 0;
            if (PersistentGameManager.Instance != null)
            {
                deathCount = PersistentGameManager.Instance.GetDeaths();
                Debug.Log($"[StarConversationHandler] Current death count: {deathCount}");
            }
            
            // Initialize the story and set the death count
            inkHandler.InitializeStory();
            inkHandler.SetStoryVariable("deathCount", deathCount.ToString());
            
            // Start the dialogue
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartInkDialogue(inkHandler);
                Debug.Log("[StarConversationHandler] Started star conversation based on death count");
            }
            else
            {
                Debug.LogError("[StarConversationHandler] DialogueManager instance not found!");
                // Create DialogueManager instance if it doesn't exist
                DialogueManager.CreateInstance();
                DialogueManager.Instance.StartInkDialogue(inkHandler);
            }
        }
        else
        {
            Debug.LogError("[StarConversationHandler] InkDialogueHandler not initialized properly!");
        }
    }
    
    // Call this method to start the dialogue with a specific death count (for testing)
    public void StartStarConversationWithDeathCount(int deathCount)
    {
        if (inkHandler != null)
        {
            // Initialize the story and set the specified death count
            inkHandler.InitializeStory();
            inkHandler.SetStoryVariable("deathCount", deathCount.ToString());
            
            // Start the dialogue
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartInkDialogue(inkHandler);
                Debug.Log($"[StarConversationHandler] Started star conversation with override death count: {deathCount}");
            }
            else
            {
                Debug.LogError("[StarConversationHandler] DialogueManager instance not found!");
                DialogueManager.CreateInstance();
                DialogueManager.Instance.StartInkDialogue(inkHandler);
            }
        }
        else
        {
            Debug.LogError("[StarConversationHandler] InkDialogueHandler not initialized properly!");
        }
    }
} 