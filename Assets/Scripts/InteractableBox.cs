using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class InteractableBox : MonoBehaviour, IInteractable
{
    [SerializeField] private bool hasBeenLooted = false;
    [SerializeField] private TextAsset inkFile;
    [SerializeField] private LootTable lootTable;
    [Tooltip("If enabled, the box will be destroyed after being looted")]
    [SerializeField] private bool destroyWhenLooted = false;
    
    private InkDialogueHandler inkHandler;
    private bool pendingLootState = false; // Track if we need to save state after dialogue
    
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

        // Register for dialogue state changes
        DialogueManager.OnDialogueStateChanged += OnDialogueStateChanged;
        
        Debug.Log($"[DESTROY AFTER LOOTED] Box {gameObject.name} initialized, destroyWhenLooted = {destroyWhenLooted}");
    }
    
    private void OnDestroy()
    {
        // Unregister from dialogue state changes to prevent memory leaks
        DialogueManager.OnDialogueStateChanged -= OnDialogueStateChanged;
        Debug.Log($"[DESTROY AFTER LOOTED] Box {gameObject.name} OnDestroy called, cleaning up event subscriptions");
    }
    
    // Called when the dialogue system state changes
    private void OnDialogueStateChanged(bool isActive)
    {
        Debug.Log($"[DESTROY AFTER LOOTED] Box {gameObject.name} - Dialogue state changed to: {isActive}, pendingLootState = {pendingLootState}");
        
        // If dialogue has ended and we have a pending state to save
        if (!isActive && pendingLootState)
        {
            Debug.Log($"[DESTROY AFTER LOOTED] Box {gameObject.name} - Dialogue completed, now saving state");
            // Reset pending state
            pendingLootState = false;
            // Now it's safe to save state and potentially destroy the object
            SaveState();
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
        
        // Load saved state from PersistentGameManager
        LoadState();
        
        Debug.Log("InteractableBox initialized on " + gameObject.name);
    }
    
    /// <summary>
    /// Loads the box state from the persistent manager
    /// </summary>
    private void LoadState()
    {
        // Make sure the manager exists
        PersistentGameManager.EnsureExists();
        
        // Get the current scene name
        string currentScene = SceneManager.GetActiveScene().name;
        
        // Get the saved state for this box
        hasBeenLooted = PersistentGameManager.Instance.GetInteractableState(
            currentScene, 
            gameObject.name, 
            hasBeenLooted // Use the inspector value as the default
        );
        
        Debug.Log($"Loaded state for box {gameObject.name} in scene {currentScene}: hasBeenLooted = {hasBeenLooted}");
    }
    
    /// <summary>
    /// Saves the box state to the persistent manager
    /// </summary>
    private void SaveState()
    {
        Debug.Log($"[DESTROY AFTER LOOTED] Box {gameObject.name} - SaveState called, hasBeenLooted = {hasBeenLooted}, destroyWhenLooted = {destroyWhenLooted}");
        
        // Make sure the manager exists
        PersistentGameManager.EnsureExists();
        
        // Get the current scene name
        string currentScene = SceneManager.GetActiveScene().name;
        
        // Save the state for this box
        PersistentGameManager.Instance.SaveInteractableState(
            currentScene,
            gameObject.name,
            hasBeenLooted
        );
        
        // If box has been looted and destroyWhenLooted is true, destroy the object
        if (hasBeenLooted && destroyWhenLooted)
        {
            Debug.Log($"[DESTROY AFTER LOOTED] Box {gameObject.name} - Destroying after save state");
            // Add a small delay to ensure all processes are complete
            StartCoroutine(DestroyAfterDelay(0.1f));
        }
    }
    
    private IEnumerator DestroyAfterDelay(float delay)
    {
        Debug.Log($"[DESTROY AFTER LOOTED] Box {gameObject.name} - Starting destruction delay of {delay} seconds");
        yield return new WaitForSeconds(delay);
        Debug.Log($"[DESTROY AFTER LOOTED] Box {gameObject.name} - Delay complete, destroying object now");
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Set the looted state (called by the PersistentGameManager)
    /// </summary>
    /// <param name="looted">Whether this box has been looted</param>
    public void SetLootedState(bool looted)
    {
        hasBeenLooted = looted;
    }
    
    /// <summary>
    /// Get the looted state
    /// </summary>
    /// <returns>Whether this box has been looted</returns>
    public bool GetLootedState()
    {
        return hasBeenLooted;
    }
    
    public void Interact()
    {
        // Check if this is the first interaction (not looted yet)
        bool wasNotLootedBefore = !hasBeenLooted;
        
        Debug.Log($"[DESTROY AFTER LOOTED] Box {gameObject.name} Interact called, wasNotLootedBefore = {wasNotLootedBefore}, hasBeenLooted = {hasBeenLooted}, inkHandler = {(inkHandler != null ? "valid" : "null")}");
        
        // Handle all box interactions through the Ink dialogue system
        if (inkFile != null)
        {
            // Ensure the ink handler is initialized
            if (inkHandler == null)
            {
                inkHandler = gameObject.AddComponent<InkDialogueHandler>();
                inkHandler.InkJSON = inkFile;
                Debug.Log($"[DESTROY AFTER LOOTED] Box {gameObject.name} - Created new inkHandler because it was null");
            }
            
            // Always reset/initialize the story to ensure the dialogue flow works correctly
            inkHandler.ResetStory();
            Debug.Log($"[DESTROY AFTER LOOTED] Box {gameObject.name} - Reset story");
            
            // CRITICAL: Make sure the Ink story is initialized properly
            if (!inkHandler.IsInitialized())
            {
                Debug.LogError($"[DESTROY AFTER LOOTED] Box {gameObject.name} - Ink story not initialized after reset, attempting to initialize");
                inkHandler.InitializeStory();
                if (!inkHandler.IsInitialized())
                {
                    Debug.LogError($"[DESTROY AFTER LOOTED] Box {gameObject.name} - Failed to initialize Ink story, falling back to direct message");
                    if (hasBeenLooted)
                    {
                        DialogueManager.Instance?.ShowDialogue("Nothing Left");
                    }
                    else
                    {
                        DialogueManager.Instance?.ShowDialogue("Error opening box");
                    }
                    return;
                }
            }
            
            // Set the looted state in the Ink story
            bool hasBeenLootedSet = false;
            try {
                inkHandler.SetStoryVariable("hasBeenLooted", hasBeenLooted);
                hasBeenLootedSet = true;
                Debug.Log($"[DESTROY AFTER LOOTED] Box {gameObject.name} - Set hasBeenLooted to {hasBeenLooted} in Ink story");
            } catch (System.Exception e) {
                Debug.LogError($"[DESTROY AFTER LOOTED] Box {gameObject.name} - Failed to set hasBeenLooted: {e.Message}");
            }
            
            // Only generate loot if this is the first interaction
            if (!hasBeenLooted)
            {
                // Generate random loot
                ItemData lootedItem = null;
                if (lootTable != null)
                {
                    lootedItem = lootTable.GetRandomLoot();
                    Debug.Log($"[DESTROY AFTER LOOTED] Box {gameObject.name} - Generated loot: {(lootedItem != null ? lootedItem.name : "none")}");
                }
                
                if (lootedItem != null)
                {
                    // Add to player inventory
                    PlayerController player = FindObjectOfType<PlayerController>();
                    if (player != null)
                    {
                        PlayerInventory inventory = player.GetComponent<PlayerInventory>();
                        
                        if (inventory == null)
                        {
                            inventory = player.gameObject.AddComponent<PlayerInventory>();
                            Debug.Log($"[DESTROY AFTER LOOTED] Box {gameObject.name} - Added PlayerInventory component to player");
                        }
                        
                        inventory.AddItem(lootedItem);
                        
                        // CRITICAL FIX: Get the actual item name directly from lootedItem's name field
                        string actualItemName = lootedItem.name;
                        Debug.Log($"[DESTROY AFTER LOOTED] Box {gameObject.name} - Player looted '{actualItemName}' from box");
                        
                        // CRITICAL FIX: Force the itemName to be set in Ink, and if it fails, handle it directly
                        bool inkVariableSet = false;
                        try {
                            inkHandler.SetStoryVariable("itemName", actualItemName);
                            inkVariableSet = true;
                            Debug.Log($"[DESTROY AFTER LOOTED] Box {gameObject.name} - Successfully set itemName to '{actualItemName}' in Ink story");
                        } catch (System.Exception e) {
                            Debug.LogError($"[DESTROY AFTER LOOTED] Box {gameObject.name} - Failed to set itemName in Ink: {e.Message}");
                        }
                        
                        // If we couldn't set the Ink variable, use a direct approach as fallback
                        if (!inkVariableSet || !hasBeenLootedSet) {
                            Debug.LogWarning($"[DESTROY AFTER LOOTED] Box {gameObject.name} - Using fallback due to Ink variable issues");
                            DialogueManager.Instance?.ShowDialogue($"You found: <b>{actualItemName}</b>!");
                            hasBeenLooted = true;
                            // Set flag to indicate we need to save state after dialogue
                            pendingLootState = true;
                            return;
                        }
                    }
                }
                else
                {
                    // No item was dropped - but we still need to set a default itemName
                    bool inkVariableSet = false;
                    try {
                        inkHandler.SetStoryVariable("itemName", "nothing");
                        inkVariableSet = true;
                        Debug.Log($"[DESTROY AFTER LOOTED] Box {gameObject.name} - Box contained no items, set itemName to 'nothing'");
                    } catch (System.Exception e) {
                        Debug.LogError($"[DESTROY AFTER LOOTED] Box {gameObject.name} - Failed to set itemName in Ink: {e.Message}");
                    }
                    
                    // Fallback if we couldn't set the Ink variable
                    if (!inkVariableSet || !hasBeenLootedSet) {
                        DialogueManager.Instance?.ShowDialogue("The box is empty.");
                        hasBeenLooted = true;
                        // Set flag to indicate we need to save state after dialogue
                        pendingLootState = true;
                        return;
                    }
                }
                
                // Mark as looted after the first interaction
                hasBeenLooted = true;
                
                // Set flag to indicate we need to save state after dialogue
                pendingLootState = true;
            }
            
            // Start the ink dialogue - the Ink script will handle different responses based on hasBeenLooted
            if (DialogueManager.Instance != null)
            {
                Debug.Log($"[DESTROY AFTER LOOTED] Box {gameObject.name} - About to start Ink dialogue, currentHandler = {inkHandler}");
                DialogueManager.Instance.StartInkDialogue(inkHandler);
                Debug.Log($"[DESTROY AFTER LOOTED] Box {gameObject.name} - Started Ink dialogue for box (hasBeenLooted: {hasBeenLooted})");
            }
            else
            {
                Debug.LogError($"[DESTROY AFTER LOOTED] Box {gameObject.name} - DialogueManager instance not found!");
                // If we can't start dialogue, save state directly
                if (pendingLootState)
                {
                    pendingLootState = false;
                    SaveState();
                }
            }
        }
        else
        {
            // Fallback for when no ink file is assigned
            if (hasBeenLooted)
            {
                if (DialogueManager.Instance != null)
                {
                    DialogueManager.Instance.ShowDialogue("Nothing Left");
                    Debug.Log($"[DESTROY AFTER LOOTED] Box {gameObject.name} - Showed 'Nothing Left' message (no ink file)");
                }
                else
                {
                    Debug.LogError($"[DESTROY AFTER LOOTED] Box {gameObject.name} - DialogueManager instance not found!");
                }
            }
            else
            {
                // Generate random loot
                ItemData lootedItem = null;
                if (lootTable != null)
                {
                    lootedItem = lootTable.GetRandomLoot();
                    Debug.Log($"[DESTROY AFTER LOOTED] Box {gameObject.name} - Generated loot without ink: {(lootedItem != null ? lootedItem.name : "none")}");
                }
                
                if (lootedItem != null)
                {
                    // Add to player inventory and mark as looted
                    PlayerController player = FindObjectOfType<PlayerController>();
                    if (player != null)
                    {
                        PlayerInventory inventory = player.GetComponent<PlayerInventory>();
                        
                        if (inventory == null)
                        {
                            inventory = player.gameObject.AddComponent<PlayerInventory>();
                            Debug.Log($"[DESTROY AFTER LOOTED] Box {gameObject.name} - Added PlayerInventory component to player (no ink path)");
                        }
                        
                        inventory.AddItem(lootedItem);
                        
                        // CRITICAL FIX: Use the correct property
                        string itemName = lootedItem.name;
                        Debug.Log($"[DESTROY AFTER LOOTED] Box {gameObject.name} - Player looted {itemName} from box (no ink path)");
                        
                        // Use dynamic message with actual item name
                        string message = $"You found <b>{itemName}!</b>";
                        DialogueManager.Instance.ShowDialogue(message);
                        
                        // Mark as looted
                        hasBeenLooted = true;
                        
                        // Set flag to indicate we need to save state after dialogue
                        pendingLootState = true;
                    }
                }
                else
                {
                    // No item was dropped or no loot table assigned
                    DialogueManager.Instance.ShowDialogue("The box is empty.");
                    Debug.Log($"[DESTROY AFTER LOOTED] Box {gameObject.name} - Box contained no items (no ink path)");
                    
                    // Mark as looted
                    hasBeenLooted = true;
                    
                    // Set flag to indicate we need to save state after dialogue
                    pendingLootState = true;
                }
            }
        }
        
        // If this was the first time looting, make sure chestsLooted is incremented
        if (wasNotLootedBefore && hasBeenLooted)
        {
            // Make sure the manager exists
            PersistentGameManager.EnsureExists();
            
            // Increment the chests looted counter
            PersistentGameManager.Instance.IncrementChestsLooted();
            Debug.Log($"[DESTROY AFTER LOOTED] Box {gameObject.name} - Incremented chestsLooted counter");
        }
    }
} 