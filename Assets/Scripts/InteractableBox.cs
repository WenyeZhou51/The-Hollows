using UnityEngine;

public class InteractableBox : MonoBehaviour, IInteractable
{
    [SerializeField] private bool hasBeenLooted = false;
    [SerializeField] private TextAsset inkFile;
    [SerializeField] private LootTable customLootTable;
    
    private InkDialogueHandler inkHandler;
    private LootTable lootTable;
    
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
        
        // Set up the loot table
        if (customLootTable != null)
        {
            lootTable = customLootTable;
        }
        else
        {
            // Add a loot table component if not assigned
            lootTable = GetComponent<LootTable>();
            if (lootTable == null)
            {
                lootTable = gameObject.AddComponent<LootTable>();
            }
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
        if (hasBeenLooted)
        {
            // Already looted message
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.ShowDialogue("Nothing Left");
            }
            return;
        }
        
        // Generate random loot
        ItemData lootedItem = lootTable.GetRandomLoot();
        
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
            hasBeenLooted = true;
            
            Debug.Log($"Player looted {lootedItem.name} from box");
        }
        
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
                Debug.Log("Started Ink dialogue for box");
            }
            else
            {
                Debug.LogError("DialogueManager instance not found!");
            }
        }
        else
        {
            // Fallback to simple dialogue if no ink file is assigned
            string message = $"You found <b>{lootedItem.name}!</b>";
            
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