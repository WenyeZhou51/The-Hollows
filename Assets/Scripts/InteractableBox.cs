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
        // Initialize the Ink system regardless of looted state
        if (inkFile != null)
        {
            // Ensure the ink handler is initialized FIRST
            if (inkHandler == null)
            {
                inkHandler = gameObject.AddComponent<InkDialogueHandler>();
                inkHandler.InkJSON = inkFile;
            }
            
            // Initialize the story BEFORE setting variables
            inkHandler.InitializeStory();  // This creates the Story object
            
            // Set the looted state in the Ink story
            inkHandler.SetStoryVariable("hasBeenLooted", hasBeenLooted);
            
            if (!hasBeenLooted)
            {
                // Generate random loot ONLY if not looted before
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
                        Debug.Log($"Player looted {lootedItem.name} from box");
                        
                        // Set the looted item in the Ink story
                        inkHandler.SetStoryVariable("itemName", lootedItem.name);
                    }
                }
                else
                {
                    // No item was dropped
                    inkHandler.SetStoryVariable("itemName", "nothing");
                    Debug.Log("Box contained no items");
                }
                
                // Mark as looted regardless of whether an item was found
                hasBeenLooted = true;
            }
            
            // Start the ink dialogue
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartInkDialogue(inkHandler);
                Debug.Log("Started Ink dialogue for box");
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
                DialogueManager.Instance.ShowDialogue("Nothing Left");
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
                        Debug.Log($"Player looted {lootedItem.name} from box");
                        
                        // Use dynamic message with actual item name
                        string message = $"You found <b>{lootedItem.name}!</b>";
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