using UnityEngine;

public class DialogueTriggerArea : MonoBehaviour
{
    [SerializeField] private string areaName = "Dialogue Area";
    [SerializeField] private TextAsset inkFile;
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private bool hasEnteredBefore = false;
    [SerializeField] private bool oncePerRun = false; // Only trigger once per game run (persists across scene transitions)
    
    private InkDialogueHandler inkHandler;
    private string uniqueDialogueId;
    private bool hasTriggeredThisSession = false; // Track if triggered during this scene session
    
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
        
        // Generate a unique ID for this dialogue trigger
        // Make the ID more specific without using GetInstanceID() which can change
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        uniqueDialogueId = $"DialogueTrigger_{sceneName}_{areaName}_{transform.position}";
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
        
        // Check if this dialogue has already been triggered in this run
        if (oncePerRun && PersistentGameManager.Instance != null)
        {
            // Get the current run ID from PersistentGameManager (deaths counter)
            int currentRunId = PersistentGameManager.Instance.GetDeaths();
            
            // Create a run-specific dialogue ID that changes on death
            string runSpecificId = $"{uniqueDialogueId}_run{currentRunId}";
            
            // Check if this dialogue has been triggered in the current run
            hasEnteredBefore = PersistentGameManager.Instance.GetInteractableState(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, 
                runSpecificId, 
                false);
            
            Debug.Log($"DialogueTriggerArea '{areaName}' oncePerRun check for run {currentRunId}: hasEnteredBefore = {hasEnteredBefore}");
        }
        
        // Subscribe to dialogue state change events
        DialogueManager.OnDialogueStateChanged += HandleDialogueStateChanged;
        
        Debug.Log("DialogueTriggerArea initialized on " + gameObject.name + " with name: " + areaName);
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events when destroyed
        DialogueManager.OnDialogueStateChanged -= HandleDialogueStateChanged;
    }
    
    private void HandleDialogueStateChanged(bool isActive)
    {
        // Reset the trigger state when dialogue closes
        if (!isActive && hasTriggeredThisSession)
        {
            hasTriggeredThisSession = false;
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the object entering the trigger is the player
        if (other.CompareTag("Player"))
        {
            // Prevent multiple triggers in same scene session
            if (hasTriggeredThisSession)
            {
                Debug.Log($"DialogueTrigger '{areaName}' already triggered this session - ignoring new trigger");
                return;
            }
            
            // If this trigger should only fire once and has already fired, don't trigger again
            if ((triggerOnce || oncePerRun) && hasEnteredBefore)
            {
                Debug.Log($"DialogueTrigger '{areaName}' hasEnteredBefore = {hasEnteredBefore} - ignoring trigger");
                return;
            }
            
            // Get the PlayerController to stop movement immediately
            PlayerController playerController = other.GetComponent<PlayerController>();
            if (playerController != null)
            {
                // Use the public method to disable movement
                playerController.SetCanMove(false);
            }
            
            // Mark as triggered in this session to prevent immediate retriggering
            hasTriggeredThisSession = true;
            
            // Trigger the dialogue
            TriggerDialogue();
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        // When player exits the area, don't immediately retrigger if they re-enter
        if (other.CompareTag("Player") && !hasEnteredBefore)
        {
            // Allow retriggering after a while
            Invoke("ResetSessionTrigger", 1.0f);
        }
    }
    
    private void ResetSessionTrigger()
    {
        // Only reset the session trigger if we're not using oncePerRun or triggerOnce
        if (!oncePerRun && !triggerOnce)
        {
            hasTriggeredThisSession = false;
            Debug.Log($"DialogueTrigger '{areaName}' session trigger reset - can trigger again");
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
            
            // CRITICAL FIX: Initialize the story before checking for variables
            Debug.Log($"[DEATHCOUNT DEBUG] Before InitializeStory - Area: {areaName}, File: {inkFile.name}");
            inkHandler.InitializeStory(); // Added this line to initialize the story BEFORE checking variables
            Debug.Log($"[DEATHCOUNT DEBUG] After InitializeStory - Area: {areaName}, File: {inkFile.name}");
            
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
            
            // BUGFIX: Set the death count variable in the Ink story if it exists
            // Make sure PersistentGameManager exists
            PersistentGameManager.EnsureExists();
            
            // Get death count from PersistentGameManager
            int deathCount = 0;
            if (PersistentGameManager.Instance != null)
            {
                deathCount = PersistentGameManager.Instance.GetDeaths();
                
                // Debug logs for diagnosis
                Debug.Log($"[DEATHCOUNT DEBUG] Area: {areaName}, File: {inkFile.name}, Death Count: {deathCount}");
                
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
            
            // Start the ink dialogue
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartInkDialogue(inkHandler);
                Debug.Log("Started Ink dialogue for area: " + areaName);
                
                // Update the trigger state for future triggers
                hasEnteredBefore = true;
                
                // If this dialogue should only trigger once per run, save its state to the PersistentGameManager
                if (oncePerRun && PersistentGameManager.Instance != null)
                {
                    // Get the current run ID (deaths counter)
                    int currentRunId = PersistentGameManager.Instance.GetDeaths();
                    
                    // Create a run-specific dialogue ID that changes on death
                    string runSpecificId = $"{uniqueDialogueId}_run{currentRunId}";
                    
                    // Save state with the run-specific ID
                    PersistentGameManager.Instance.SaveInteractableState(
                        UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                        runSpecificId,
                        true);
                    
                    Debug.Log($"Saved oncePerRun dialogue state for '{areaName}' for run {currentRunId} to PersistentGameManager");
                }
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
                
                // If this dialogue should only trigger once per run, save its state to the PersistentGameManager
                if (oncePerRun && PersistentGameManager.Instance != null)
                {
                    // Get the current run ID (deaths counter)
                    int currentRunId = PersistentGameManager.Instance.GetDeaths();
                    
                    // Create a run-specific dialogue ID that changes on death
                    string runSpecificId = $"{uniqueDialogueId}_run{currentRunId}";
                    
                    // Save state with the run-specific ID
                    PersistentGameManager.Instance.SaveInteractableState(
                        UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                        runSpecificId,
                        true);
                    
                    Debug.Log($"Saved oncePerRun dialogue state for '{areaName}' for run {currentRunId} to PersistentGameManager");
                }
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