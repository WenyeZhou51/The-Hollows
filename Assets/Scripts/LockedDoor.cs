using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Reflection;

public class LockedDoor : MonoBehaviour, IInteractable
{
    [SerializeField] private bool hasBeenUnlocked = false;
    [SerializeField] private TextAsset inkFile;
    [SerializeField] private GameObject lockedDoorway;
    
    private InkDialogueHandler inkHandler;
    private bool isUnlocking = false;
    
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
        
        // Load saved state from PersistentGameManager
        LoadState();
        
        // If door was previously unlocked, destroy it and activate doorway
        if (hasBeenUnlocked)
        {
            if (lockedDoorway != null)
            {
                lockedDoorway.SetActive(true);
            }
            Destroy(gameObject);
        }
        
        Debug.Log("LockedDoor initialized on " + gameObject.name);
    }
    
    /// <summary>
    /// Loads the door state from the persistent manager
    /// </summary>
    private void LoadState()
    {
        // Make sure the manager exists
        PersistentGameManager.EnsureExists();
        
        // Get the current scene name
        string currentScene = SceneManager.GetActiveScene().name;
        
        // Get the saved state for this door
        hasBeenUnlocked = PersistentGameManager.Instance.GetInteractableState(
            currentScene, 
            gameObject.name, 
            hasBeenUnlocked // Use the inspector value as the default
        );
        
        Debug.Log($"Loaded state for door {gameObject.name} in scene {currentScene}: hasBeenUnlocked = {hasBeenUnlocked}");
    }
    
    /// <summary>
    /// Saves the door state to the persistent manager
    /// </summary>
    private void SaveState()
    {
        // Make sure the manager exists
        PersistentGameManager.EnsureExists();
        
        // Get the current scene name
        string currentScene = SceneManager.GetActiveScene().name;
        
        // Save the state for this door
        PersistentGameManager.Instance.SaveInteractableState(
            currentScene,
            gameObject.name,
            hasBeenUnlocked
        );
    }
    
    /// <summary>
    /// Ensures that the typewriter effect is enabled in the DialogueManager
    /// </summary>
    private void EnsureTypewriterEffectEnabled()
    {
        if (DialogueManager.Instance != null)
        {
            // Use reflection to access the private useTypewriterEffect field
            FieldInfo typewriterField = typeof(DialogueManager).GetField("useTypewriterEffect", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (typewriterField != null)
            {
                // Ensure it's set to true
                bool currentValue = (bool)typewriterField.GetValue(DialogueManager.Instance);
                if (!currentValue)
                {
                    Debug.Log("Enabling typewriter effect for door dialogue");
                    typewriterField.SetValue(DialogueManager.Instance, true);
                }
                else
                {
                    Debug.Log("Typewriter effect is already enabled");
                }
            }
            else
            {
                Debug.LogError("Could not find useTypewriterEffect field using reflection");
            }
        }
    }
    
    public void Interact()
    {
        // If door is already unlocked or in the process of unlocking, do nothing
        if (hasBeenUnlocked || isUnlocking)
        {
            Debug.LogWarning("Interacting with already unlocked door or door that is in the process of unlocking");
            return;
        }
        
        // Find the player to check inventory
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("Player not found when interacting with locked door");
            return;
        }
        
        // Get the player inventory
        PlayerInventory inventory = player.GetComponent<PlayerInventory>();
        if (inventory == null)
        {
            Debug.LogError("PlayerInventory component not found on player");
            return;
        }
        
        // CRITICAL: Ensure typewriter effect is enabled BEFORE showing any dialogue
        EnsureTypewriterEffectEnabled();
        
        // Check if player has the Cold Key
        bool hasColdKey = inventory.HasItem("Cold Key");
        
        // Handle all door interactions through the Ink dialogue system
        if (inkFile != null)
        {
            // Ensure the ink handler is initialized
            if (inkHandler == null)
            {
                inkHandler = gameObject.AddComponent<InkDialogueHandler>();
                inkHandler.InkJSON = inkFile;
            }
            
            // Reset the story to ensure the dialogue flow works correctly
            inkHandler.ResetStory();
            
            // CRITICAL: Make sure the Ink story is initialized properly
            if (!inkHandler.IsInitialized())
            {
                Debug.LogError("Ink story not initialized after reset, attempting to initialize");
                inkHandler.InitializeStory();
                if (!inkHandler.IsInitialized())
                {
                    Debug.LogError("Failed to initialize Ink story, falling back to direct message");
                    if (hasColdKey)
                    {
                        DialogueManager.Instance?.ShowDialogue("You unlock the door with the Cold Key.");
                        // Start a coroutine to wait for dialogue to complete before unlocking
                        StartCoroutine(WaitForDialogueAndUnlock(inventory));
                    }
                    else
                    {
                        DialogueManager.Instance?.ShowDialogue("The door is locked. The iron bars feel cold to the touch.");
                    }
                    return;
                }
            }
            
            try {
                // Set the hasColdKey variable in the Ink story
                inkHandler.SetStoryVariable("hasColdKey", hasColdKey);
                Debug.Log($"Set hasColdKey to {hasColdKey} in Ink story");
                
                // If player has the key, set the variable to allow unlocking through Ink
                if (hasColdKey)
                {
                    inkHandler.SetStoryVariable("canUnlock", true);
                }
            } catch (System.Exception e) {
                Debug.LogError($"Failed to set variables in Ink: {e.Message}");
            }
            
            // Start the ink dialogue
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartInkDialogue(inkHandler);
                
                // If player has the key, wait for dialogue to complete before unlocking the door
                if (hasColdKey)
                {
                    // Start a coroutine to wait for dialogue to complete before unlocking
                    StartCoroutine(WaitForDialogueAndUnlock(inventory));
                }
            }
            else
            {
                Debug.LogError("DialogueManager not found when trying to start door dialogue");
            }
        }
        else
        {
            // Fallback if no ink file is provided
            if (hasColdKey)
            {
                DialogueManager.Instance?.ShowDialogue("You unlock the door with the Cold Key.");
                // Start a coroutine to wait for dialogue to complete before unlocking
                StartCoroutine(WaitForDialogueAndUnlock(inventory));
            }
            else
            {
                DialogueManager.Instance?.ShowDialogue("The door is locked. The iron bars feel cold to the touch.");
            }
        }
    }
    
    /// <summary>
    /// Waits for the dialogue to complete before unlocking the door
    /// </summary>
    private IEnumerator WaitForDialogueAndUnlock(PlayerInventory inventory)
    {
        // Mark that we're in the process of unlocking to prevent multiple interactions
        isUnlocking = true;
        
        // Wait for the dialogue to finish by checking DialogueManager's isDialogueActive status
        while (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive())
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        // Wait a little extra time to ensure typewriter has fully completed
        yield return new WaitForSeconds(0.5f);
        
        // Now that dialogue is done, unlock the door
        UnlockDoor(inventory);
    }
    
    /// <summary>
    /// Unlocks the door, removes the key from inventory, and activates the doorway
    /// </summary>
    private void UnlockDoor(PlayerInventory inventory)
    {
        // Remove the Cold Key from inventory
        ItemData coldKey = inventory.GetItem("Cold Key");
        if (coldKey != null)
        {
            inventory.RemoveItem(coldKey);
            Debug.Log("Removed Cold Key from player inventory after unlocking door");
        }
        
        // Mark the door as unlocked
        hasBeenUnlocked = true;
        
        // Save the unlocked state
        SaveState();
        
        // Activate the doorway
        if (lockedDoorway != null)
        {
            lockedDoorway.SetActive(true);
            Debug.Log("Activated doorway after unlocking door");
        }
        else
        {
            Debug.LogWarning("No LockedDoorway assigned to LockedDoor");
        }
        
        // Destroy the door object
        Destroy(gameObject);
        Debug.Log("Door unlocked and destroyed");
    }
} 