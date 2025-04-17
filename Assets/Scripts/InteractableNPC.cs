using UnityEngine;
using Ink.Runtime;
using System.Reflection;
using UnityEngine.SceneManagement;

public class InteractableNPC : MonoBehaviour, IInteractable
{
    [SerializeField] private string npcName = "NPC";
    [SerializeField] private TextAsset inkFile;
    [SerializeField] private bool resetOnInteract = true;
    
    // Unique ID for this NPC - used for persistence
    [SerializeField] private string npcId;
    
    private InkDialogueHandler inkHandler;
    private bool hasInteractedBefore = false;
    private bool storyWasEnded = false;
    
    private void Awake()
    {
        // Generate a unique ID if none exists
        if (string.IsNullOrEmpty(npcId))
        {
            npcId = $"{SceneManager.GetActiveScene().name}_{npcName}_{gameObject.GetInstanceID()}";
            Debug.Log($"[NPC Debug] Generated NPC ID: {npcId}");
        }
        
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
        
        Debug.Log($"[NPC Debug] NPC {npcName} awake, inkFile assigned: {(inkFile != null)}");
    }
    
    private void Start()
    {
        // Make sure we have a collider
        if (GetComponent<Collider2D>() == null)
        {
            BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
            Debug.Log($"[NPC Debug] Added BoxCollider2D to {gameObject.name}");
        }
        
        // CRITICAL: Load persistent state from PersistentGameManager
        LoadNPCState();
        
        Debug.Log($"[NPC Debug] InteractableNPC initialized on {gameObject.name} with name: {npcName}, ID: {npcId}, storyWasEnded: {storyWasEnded}, hasInteractedBefore: {hasInteractedBefore}");
    }
    
    // Save NPC dialogue state to persistent storage
    private void SaveNPCState()
    {
        // Ensure PersistentGameManager exists
        PersistentGameManager.EnsureExists();
        
        // Create a unique key for this NPC's dialogue state
        string storyEndedKey = $"{npcId}_storyEnded";
        string interactedKey = $"{npcId}_interacted";
        
        // Save dialogue state
        PersistentGameManager.Instance.SetCustomDataValue(storyEndedKey, storyWasEnded);
        PersistentGameManager.Instance.SetCustomDataValue(interactedKey, hasInteractedBefore);
        
        Debug.Log($"[NPC Debug] Saved state for NPC {npcName} (ID: {npcId}): storyWasEnded={storyWasEnded}, hasInteractedBefore={hasInteractedBefore}");
    }
    
    // Load NPC dialogue state from persistent storage
    private void LoadNPCState()
    {
        // Ensure PersistentGameManager exists
        PersistentGameManager.EnsureExists();
        
        // Create unique keys for this NPC's dialogue state
        string storyEndedKey = $"{npcId}_storyEnded";
        string interactedKey = $"{npcId}_interacted";
        
        // Load dialogue state with default values
        storyWasEnded = PersistentGameManager.Instance.GetCustomDataValue(storyEndedKey, false);
        hasInteractedBefore = PersistentGameManager.Instance.GetCustomDataValue(interactedKey, false);
        
        Debug.Log($"[NPC Debug] Loaded state for NPC {npcName} (ID: {npcId}): storyWasEnded={storyWasEnded}, hasInteractedBefore={hasInteractedBefore}");
    }
    
    // Helper method to get the Story object from the InkDialogueHandler
    private Story GetStoryFromHandler(InkDialogueHandler handler)
    {
        if (handler == null) return null;
        
        // Use reflection to access the private _story field
        System.Type type = handler.GetType();
        FieldInfo storyField = type.GetField("_story", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (storyField != null)
        {
            return (Story)storyField.GetValue(handler);
        }
        
        return null;
    }
    
    // Helper method to check if the story has ended (no more content)
    private bool HasStoryEnded(InkDialogueHandler handler)
    {
        Story storyRef = GetStoryFromHandler(handler);
        if (storyRef == null) return false;
        
        // Story has ended if it can't continue and has no choices
        bool ended = !storyRef.canContinue && storyRef.currentChoices.Count == 0;
        Debug.Log($"[NPC Debug] Story ended check for {npcName}: {ended} (canContinue={storyRef.canContinue}, choiceCount={storyRef.currentChoices.Count})");
        return ended;
    }
    
    public void Interact()
    {
        Debug.Log($"[NPC Debug] NPC interaction triggered! NPC: {npcName}, resetOnInteract={resetOnInteract}, hasInteractedBefore={hasInteractedBefore}, storyWasEnded={storyWasEnded}");
        
        if (inkFile != null)
        {
            // Ensure the ink handler is initialized
            if (inkHandler == null)
            {
                inkHandler = gameObject.AddComponent<InkDialogueHandler>();
                inkHandler.InkJSON = inkFile;
                Debug.Log($"[NPC Debug] Created new InkDialogueHandler for {npcName}");
            }
            
            // Check if the current story state is ended
            bool currentStateEnded = HasStoryEnded(inkHandler);
            
            // CRITICAL FIX: Always reset the story in these cases:
            // 1. If resetOnInteract is true (explicit setting)
            // 2. If the story previously ended (storyWasEnded flag from persistent storage)
            // 3. If the current story object is in an end state
            if (resetOnInteract || storyWasEnded || currentStateEnded)
            {
                Debug.Log($"[NPC Debug] RESETTING story for NPC {npcName} (resetOnInteract={resetOnInteract}, storyWasEnded={storyWasEnded}, currentStateEnded={currentStateEnded})");
                inkHandler.ResetStory();
            }
            
            // CRITICAL FIX: Always initialize the story
            Debug.Log($"[DEATHCOUNT DEBUG] Before InitializeStory - NPC: {npcName}, File: {inkFile.name}");
            inkHandler.InitializeStory();
            Debug.Log($"[DEATHCOUNT DEBUG] After InitializeStory - NPC: {npcName}, File: {inkFile.name}");
            
            // CRITICAL: Verify initialization was successful
            if (!inkHandler.IsInitialized())
            {
                Debug.LogError($"[NPC Debug] Ink story not initialized after reset for NPC {npcName}, attempting to initialize");
                inkHandler.InitializeStory();
                if (!inkHandler.IsInitialized())
                {
                    Debug.LogError($"[NPC Debug] Failed to initialize Ink story for NPC {npcName}, falling back to direct message");
                    if (DialogueManager.Instance != null)
                    {
                        DialogueManager.Instance.ShowDialogue($"{npcName} has nothing to say.");
                    }
                    return;
                }
            }
            
            Debug.Log($"[NPC Debug] Story successfully initialized for NPC {npcName}");
            
            // Set the hasInteractedBefore variable in the Ink story with proper error handling
            bool hasInteractedBeforeSet = false;
            try
            {
                inkHandler.SetStoryVariable("hasInteractedBefore", hasInteractedBefore);
                hasInteractedBeforeSet = true;
                Debug.Log($"[NPC Debug] Set hasInteractedBefore to {hasInteractedBefore} for NPC {npcName}");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[NPC Debug] Variable 'hasInteractedBefore' not found in Ink story for {npcName}: {e.Message}");
            }
            
            // BUGFIX: Set the death count variable in the Ink story if it exists
            // Make sure PersistentGameManager exists
            PersistentGameManager.EnsureExists();
            
            // Get death count from PersistentGameManager
            int deathCount = 0;
            if (PersistentGameManager.Instance != null)
            {
                deathCount = PersistentGameManager.Instance.GetDeaths();
                
                // Debug logs for diagnosis
                Debug.Log($"[DEATHCOUNT DEBUG] NPC: {npcName}, File: {inkFile.name}, Death Count: {deathCount}");
                
                // Check if story has deathCount variable
                bool hasDeathCountVar = inkHandler.HasStoryVariable("deathCount");
                Debug.Log($"[DEATHCOUNT DEBUG] Does '{inkFile.name}' have deathCount variable? {hasDeathCountVar}");
                
                // Only set the variable if the story has it
                if (hasDeathCountVar)
                {
                    inkHandler.SetStoryVariable("deathCount", deathCount);
                    Debug.Log($"[DEATHCOUNT DEBUG] Successfully set deathCount={deathCount} in {inkFile.name} as INTEGER");
                }
                else
                {
                    Debug.LogWarning($"[DEATHCOUNT DEBUG] Failed to find deathCount variable in {inkFile.name} - dialogue may not change with death count");
                }
            }
            
            // Register for dialogue events to track when dialogue ends
            DialogueManager.OnDialogueStateChanged += HandleDialogueStateChanged;
            
            // Update the interaction state for future interactions
            hasInteractedBefore = true;
            
            // IMPORTANT: Save state immediately when we've updated hasInteractedBefore
            SaveNPCState();
            
            // Start the ink dialogue
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartInkDialogue(inkHandler);
                Debug.Log($"[NPC Debug] Started Ink dialogue for NPC: {npcName}");
            }
            else
            {
                Debug.LogError($"[NPC Debug] DialogueManager instance not found for NPC {npcName}!");
                
                // Clean up event subscription
                DialogueManager.OnDialogueStateChanged -= HandleDialogueStateChanged;
            }
        }
        else
        {
            // Fallback to simple dialogue if no ink file is assigned
            string message = hasInteractedBefore ? 
                $"Talking to {npcName} again." : 
                $"Starting dialogue with {npcName}";
            
            Debug.Log($"[NPC Debug] No ink file assigned for {npcName}, using fallback dialogue: {message}");
            
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.ShowDialogue(message);
                Debug.Log($"[NPC Debug] Fallback dialogue shown for {npcName}: {message}");
                
                // Update the interaction state for future interactions
                hasInteractedBefore = true;
                SaveNPCState();
            }
            else
            {
                Debug.LogError($"[NPC Debug] DialogueManager instance not found for NPC {npcName} fallback dialogue!");
            }
        }
        
        Debug.Log($"[NPC Debug] NPC {npcName} interaction complete");
    }
    
    // Handler for dialogue state changes to detect when dialogue ends
    private void HandleDialogueStateChanged(bool isActive)
    {
        if (!isActive) // Dialogue ended
        {
            Debug.Log($"[NPC Debug] Dialogue ended for {npcName}, checking final story state");
            
            // Check if the story has ended (reached END in the Ink file)
            bool hasEnded = HasStoryEnded(inkHandler);
            if (hasEnded)
            {
                Debug.Log($"[NPC Debug] Story for {npcName} has reached its END, setting storyWasEnded=true");
                storyWasEnded = true;
                SaveNPCState(); // CRITICAL: Save the end state to persistent storage
            }
            
            // Unsubscribe from the event to prevent memory leaks
            DialogueManager.OnDialogueStateChanged -= HandleDialogueStateChanged;
        }
    }
    
    private void OnDestroy()
    {
        // Clean up event subscription when the object is destroyed
        DialogueManager.OnDialogueStateChanged -= HandleDialogueStateChanged;
    }
} 