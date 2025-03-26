using UnityEngine;

public class InteractableBox : MonoBehaviour, IInteractable
{
    [SerializeField] private bool hasBeenLooted = false;
    [SerializeField] private TextAsset inkFile;
    [SerializeField] private LootTable lootTable;
    
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
        
        Debug.Log("InteractableBox initialized on " + gameObject.name);
    }
    
    public void Interact()
    {
        // Handle all box interactions through the Ink dialogue system
        if (inkFile != null)
        {
            // Ensure the ink handler is initialized
            if (inkHandler == null)
            {
                inkHandler = gameObject.AddComponent<InkDialogueHandler>();
                inkHandler.InkJSON = inkFile;
            }
            
            // Always reset/initialize the story to ensure the dialogue flow works correctly
            inkHandler.ResetStory();
            
            // CRITICAL: Make sure the Ink story is initialized properly
            if (!inkHandler.IsInitialized())
            {
                Debug.LogError("Ink story not initialized after reset, attempting to initialize");
                inkHandler.InitializeStory();
                if (!inkHandler.IsInitialized())
                {
                    Debug.LogError("Failed to initialize Ink story, falling back to direct message");
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
                Debug.Log($"Set hasBeenLooted to {hasBeenLooted}");
            } catch (System.Exception e) {
                Debug.LogError($"Failed to set hasBeenLooted: {e.Message}");
            }
            
            // Only generate loot if this is the first interaction
            if (!hasBeenLooted)
            {
                // Generate random loot
                ItemData lootedItem = null;
                if (lootTable != null)
                {
                    lootedItem = lootTable.GetRandomLoot();
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
                        }
                        
                        inventory.AddItem(lootedItem);
                        
                        // CRITICAL FIX: Get the actual item name directly from lootedItem's name field
                        string actualItemName = lootedItem.name;
                        Debug.Log($"Player looted '{actualItemName}' from box");
                        
                        // CRITICAL FIX: Force the itemName to be set in Ink, and if it fails, handle it directly
                        bool inkVariableSet = false;
                        try {
                            inkHandler.SetStoryVariable("itemName", actualItemName);
                            inkVariableSet = true;
                            Debug.Log($"Successfully set itemName to '{actualItemName}' in Ink story");
                        } catch (System.Exception e) {
                            Debug.LogError($"Failed to set itemName in Ink: {e.Message}");
                        }
                        
                        // If we couldn't set the Ink variable, use a direct approach as fallback
                        if (!inkVariableSet || !hasBeenLootedSet) {
                            Debug.LogWarning("Using fallback due to Ink variable issues");
                            DialogueManager.Instance?.ShowDialogue($"You found: <b>{actualItemName}</b>!");
                            hasBeenLooted = true;
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
                        Debug.Log("Box contained no items, set itemName to 'nothing'");
                    } catch (System.Exception e) {
                        Debug.LogError($"Failed to set itemName in Ink: {e.Message}");
                    }
                    
                    // Fallback if we couldn't set the Ink variable
                    if (!inkVariableSet || !hasBeenLootedSet) {
                        DialogueManager.Instance?.ShowDialogue("The box is empty.");
                        hasBeenLooted = true;
                        return;
                    }
                }
                
                // Mark as looted after the first interaction
                hasBeenLooted = true;
            }
            
            // Start the ink dialogue - the Ink script will handle different responses based on hasBeenLooted
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartInkDialogue(inkHandler);
                Debug.Log($"Started Ink dialogue for box (hasBeenLooted: {hasBeenLooted})");
            }
            else
            {
                Debug.LogError("DialogueManager instance not found!");
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
                }
                else
                {
                    Debug.LogError("DialogueManager instance not found!");
                }
            }
            else
            {
                // Generate random loot
                ItemData lootedItem = null;
                if (lootTable != null)
                {
                    lootedItem = lootTable.GetRandomLoot();
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
                        }
                        
                        inventory.AddItem(lootedItem);
                        
                        // CRITICAL FIX: Use the correct property
                        string itemName = lootedItem.name;
                        Debug.Log($"Player looted {itemName} from box");
                        
                        // Use dynamic message with actual item name
                        string message = $"You found <b>{itemName}!</b>";
                        DialogueManager.Instance.ShowDialogue(message);
                    }
                }
                else
                {
                    // No item was dropped or no loot table assigned
                    DialogueManager.Instance.ShowDialogue("The box is empty.");
                    Debug.Log("Box contained no items");
                }
                
                // Mark as looted regardless of whether an item was found
                hasBeenLooted = true;
            }
        }
    }
} 