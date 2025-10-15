using UnityEngine;
using Ink.Runtime;
using System.Reflection;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Fortune Teller NPC that tracks conversation count across all runs
/// and can only be interacted with once per run before disappearing
/// </summary>
public class FortuneNeller : MonoBehaviour, IInteractable
{
    [SerializeField] private string npcName = "Fortune Teller";
    [SerializeField] private TextAsset inkFile;
    
    // Unique ID for this NPC - used for persistence
    [SerializeField] private string npcId = "FortuneNeller";
    
    private InkDialogueHandler inkHandler;
    private bool hasInteractedThisRun = false;
    private int conversationCount = 0;
    
    // Persistent data keys
    private const string CONVERSATION_COUNT_KEY = "FortuneNeller_ConversationCount";
    private const string INTERACTED_THIS_RUN_KEY = "FortuneNeller_InteractedThisRun";
    
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
        
        Debug.Log($"[Fortune Teller] Fortune Teller awake, inkFile assigned: {(inkFile != null)}");
    }
    
    private void Start()
    {
        // Make sure we have a collider
        if (GetComponent<Collider2D>() == null)
        {
            BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
            Debug.Log($"[Fortune Teller] Added BoxCollider2D to {gameObject.name}");
        }
        
        // Load persistent state
        LoadState();
        
        // If already interacted this run, disappear immediately
        if (hasInteractedThisRun)
        {
            Debug.Log($"[Fortune Teller] Already interacted this run, making invisible");
            MakeInvisible();
        }
        
        Debug.Log($"[Fortune Teller] Initialized - Conversation Count: {conversationCount}, Interacted This Run: {hasInteractedThisRun}");
        
        // Subscribe to death events to reset per-run state
        if (PersistentGameManager.Instance != null)
        {
            // Reset the "interacted this run" flag when the player dies (starts a new run)
            // Note: This will be called from the PersistentGameManager's death handling
        }
    }
    
    private void LoadState()
    {
        // Ensure PersistentGameManager exists
        PersistentGameManager.EnsureExists();
        
        // Load conversation count (persists forever)
        conversationCount = PersistentGameManager.Instance.GetCustomDataValue(CONVERSATION_COUNT_KEY, 0);
        
        // Load interacted this run flag (resets each run)
        hasInteractedThisRun = PersistentGameManager.Instance.GetCustomDataValue(INTERACTED_THIS_RUN_KEY, false);
        
        Debug.Log($"[Fortune Teller] Loaded state - Conversation Count: {conversationCount}, Interacted This Run: {hasInteractedThisRun}");
    }
    
    private void SaveState()
    {
        // Ensure PersistentGameManager exists
        PersistentGameManager.EnsureExists();
        
        // Save conversation count (persists forever)
        PersistentGameManager.Instance.SetCustomDataValue(CONVERSATION_COUNT_KEY, conversationCount);
        
        // Save interacted this run flag
        PersistentGameManager.Instance.SetCustomDataValue(INTERACTED_THIS_RUN_KEY, hasInteractedThisRun);
        
        Debug.Log($"[Fortune Teller] Saved state - Conversation Count: {conversationCount}, Interacted This Run: {hasInteractedThisRun}");
    }
    
    /// <summary>
    /// Public method to reset the per-run interaction flag
    /// Should be called by PersistentGameManager when player dies/starts new run
    /// </summary>
    public static void ResetRunState()
    {
        // Ensure PersistentGameManager exists
        PersistentGameManager.EnsureExists();
        
        // Reset the interacted this run flag
        PersistentGameManager.Instance.SetCustomDataValue(INTERACTED_THIS_RUN_KEY, false);
        
        Debug.Log($"[Fortune Teller] Reset run state - Fortune Teller can now be interacted with");
    }
    
    private void MakeInvisible()
    {
        // Disable the sprite renderer to make invisible
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
        
        // Disable the collider so player can't interact
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        
        Debug.Log($"[Fortune Teller] Made invisible");
    }
    
    public void Interact()
    {
        // Prevent interaction if already interacted this run
        if (hasInteractedThisRun)
        {
            Debug.Log($"[Fortune Teller] Already interacted this run, ignoring interaction");
            return;
        }
        
        // CRITICAL: Reload the conversation count from PersistentGameManager before starting dialogue
        // This ensures we get the latest count if it was changed (e.g., by pressing F1)
        conversationCount = PersistentGameManager.Instance.GetCustomDataValue(CONVERSATION_COUNT_KEY, 0);
        
        Debug.Log($"[Fortune Teller] Interaction triggered - Conversation Count: {conversationCount}");
        
        if (inkFile != null)
        {
            // Ensure the ink handler is initialized
            if (inkHandler == null)
            {
                inkHandler = gameObject.AddComponent<InkDialogueHandler>();
                inkHandler.InkJSON = inkFile;
                Debug.Log($"[Fortune Teller] Created new InkDialogueHandler");
            }
            
            // Always reset the story for clean state
            inkHandler.ResetStory();
            inkHandler.InitializeStory();
            
            // Verify initialization was successful
            if (!inkHandler.IsInitialized())
            {
                Debug.LogError($"[Fortune Teller] Failed to initialize Ink story");
                return;
            }
            
            Debug.Log($"[Fortune Teller] Story initialized successfully");
            
            // Set the conversation count variable in the Ink story
            try
            {
                inkHandler.SetStoryVariable("conversationCount", conversationCount);
                Debug.Log($"[Fortune Teller] Set conversationCount to {conversationCount} in Ink story");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Fortune Teller] Failed to set conversationCount variable: {e.Message}");
            }
            
            // Register for dialogue events to track when dialogue ends
            DialogueManager.OnDialogueStateChanged += HandleDialogueStateChanged;
            
            // Mark as interacted this run
            hasInteractedThisRun = true;
            SaveState();
            
            // Start the ink dialogue
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartInkDialogue(inkHandler);
                Debug.Log($"[Fortune Teller] Started Ink dialogue");
            }
            else
            {
                Debug.LogError($"[Fortune Teller] DialogueManager instance not found!");
                
                // Clean up event subscription
                DialogueManager.OnDialogueStateChanged -= HandleDialogueStateChanged;
            }
        }
        else
        {
            Debug.LogError($"[Fortune Teller] No ink file assigned!");
        }
    }
    
    // Handler for dialogue state changes to detect when dialogue ends
    private void HandleDialogueStateChanged(bool isActive)
    {
        if (!isActive) // Dialogue ended
        {
            Debug.Log($"[Fortune Teller] Dialogue ended");
            
            // Increment conversation count for next time
            conversationCount++;
            SaveState();
            
            Debug.Log($"[Fortune Teller] Incremented conversation count to {conversationCount}");
            
            // Make the Fortune Teller disappear with a small delay
            StartCoroutine(DisappearAfterDelay(0.5f));
            
            // Unsubscribe from the event to prevent memory leaks
            DialogueManager.OnDialogueStateChanged -= HandleDialogueStateChanged;
        }
    }
    
    private IEnumerator DisappearAfterDelay(float delay)
    {
        Debug.Log($"[Fortune Teller] Will disappear in {delay} seconds");
        yield return new WaitForSeconds(delay);
        MakeInvisible();
        Debug.Log($"[Fortune Teller] Disappeared");
    }
    
    private void OnDestroy()
    {
        // Clean up event subscription when the object is destroyed
        DialogueManager.OnDialogueStateChanged -= HandleDialogueStateChanged;
    }
}

