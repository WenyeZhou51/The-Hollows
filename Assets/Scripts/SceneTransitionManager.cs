using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    // Singleton pattern
    public static SceneTransitionManager Instance { get; private set; }
    
    // Scene names
    [SerializeField] private string overworldSceneName = "Overworld_entrance";
    [SerializeField] private string combatSceneName = "Battle_Obelisk";
    
    // Enemy that initiated the combat
    private GameObject enemyThatInitiatedCombat;
    
    // Player data to persist between scenes
    private PlayerInventory playerInventory;
    private Vector3 playerPosition;
    
    // Combat results
    private bool combatWon = false;
    
    private List<ItemData> storedItems = new List<ItemData>();
    
    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Ensures an instance of SceneTransitionManager exists in the scene
    /// </summary>
    /// <returns>The SceneTransitionManager instance</returns>
    public static SceneTransitionManager EnsureExists()
    {
        if (Instance == null)
        {
            // Look for existing instance
            SceneTransitionManager[] managers = FindObjectsOfType<SceneTransitionManager>();
            
            if (managers.Length > 0)
            {
                // Use first instance found
                Instance = managers[0];
                Debug.Log("Found existing SceneTransitionManager");
                
                // Destroy any extras
                for (int i = 1; i < managers.Length; i++)
                {
                    Debug.LogWarning("Destroying extra SceneTransitionManager instance");
                    Destroy(managers[i].gameObject);
                }
            }
            else
            {
                // Create new instance
                GameObject managerObj = new GameObject("SceneTransitionManager");
                Instance = managerObj.AddComponent<SceneTransitionManager>();
                Debug.Log("Created new SceneTransitionManager");
            }
        }
        
        return Instance;
    }
    
    /// <summary>
    /// Start combat with a specific enemy
    /// </summary>
    /// <param name="enemy">The enemy GameObject that initiated combat</param>
    /// <param name="player">The player GameObject</param>
    public void StartCombat(GameObject enemy, GameObject player)
    {
        // Store the enemy that initiated combat
        enemyThatInitiatedCombat = enemy;
        
        // Store player inventory and position
        playerInventory = player.GetComponent<PlayerInventory>();
        playerPosition = player.transform.position;
        
        // Debug log the exact player inventory contents
        Debug.Log("=== INVENTORY DEBUG: StartCombat ===");
        if (playerInventory != null && playerInventory.Items != null) 
        {
            Debug.Log($"Player inventory contains {playerInventory.Items.Count} items:");
            foreach (var item in playerInventory.Items)
            {
                Debug.Log($"Item: {item.name}, Amount: {item.amount}");
            }
        }
        else
        {
            Debug.LogWarning("Player inventory is null or empty when starting combat!");
        }
        
        storedItems.Clear();
        foreach(var item in playerInventory.Items) {
            // Create a new copy of each item
            storedItems.Add(new ItemData(item.name, item.description, item.amount, item.requiresTarget));
        }
        
        // Load the combat scene
        SceneManager.LoadScene(combatSceneName);
        
        // Register for scene loaded event
        SceneManager.sceneLoaded += OnCombatSceneLoaded;
        Debug.Log("SceneTransitionManager: Combat scene loaded");
    }
    
    /// <summary>
    /// Called when the combat scene has loaded
    /// </summary>
    private void OnCombatSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Only execute this for the combat scene
        if (scene.name == combatSceneName)
        {
            // Find the combat manager
            CombatManager combatManager = FindObjectOfType<CombatManager>();
            
            if (combatManager != null)
            {
                // Set up the combat scene with player inventory
                SetupCombatScene(combatManager);
            }
            else
            {
                Debug.LogError("CombatManager not found in combat scene!");
            }
        }
        
        // Unregister the event to prevent multiple calls
        SceneManager.sceneLoaded -= OnCombatSceneLoaded;
    }
    
    /// <summary>
    /// Setup the combat scene with the player's inventory
    /// </summary>
    private void SetupCombatScene(CombatManager combatManager)
    {
        // Pass player inventory to combat manager
        if (storedItems.Count > 0)
        {
            Debug.Log("=== INVENTORY DEBUG: SetupCombatScene ===");
            Debug.Log($"Setting up combat scene with {storedItems.Count} player inventory items");
            
            // Log each item being passed to combat
            foreach (var item in storedItems)
            {
                Debug.Log($"Passing to combat: {item.name}, Amount: {item.amount}");
            }
            
            combatManager.SetupPlayerInventory(storedItems);
            
            // Also set up individual player character inventories
            foreach (var player in combatManager.players)
            {
                if (player != null)
                {
                    // Clear existing inventory
                    player.items.Clear();
                    
                    // Copy items to player's inventory
                    foreach (var item in storedItems)
                    {
                        player.items.Add(item);
                        Debug.Log($"Added {item.name} (x{item.amount}) to player character: {player.characterName}");
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("No player inventory found when setting up combat scene");
        }
        
        // Listen for combat end event
        combatManager.OnCombatEnd += EndCombat;
        
        // For now, let's just use a debug test button to end combat
        // In a real game, this would be handled by your combat system
        GameObject testButton = new GameObject("TestEndCombatButton");
        testButton.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        GameObject buttonObject = new GameObject("Button");
        buttonObject.transform.SetParent(testButton.transform);
        UnityEngine.UI.Button button = buttonObject.AddComponent<UnityEngine.UI.Button>();
        UnityEngine.UI.Text text = buttonObject.AddComponent<UnityEngine.UI.Text>();
        text.text = "End Combat (Win)";
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.alignment = TextAnchor.MiddleCenter;
        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(100, 50);
        rectTransform.sizeDelta = new Vector2(200, 50);
        button.onClick.AddListener(() => EndCombat(true));
    }
    
    /// <summary>
    /// End combat and return to the overworld
    /// </summary>
    /// <param name="won">Whether the player won the combat</param>
    public void EndCombat(bool won)
    {
        // Store combat result
        combatWon = won;
        
        // Load the overworld scene
        SceneManager.LoadScene(overworldSceneName);
        
        // Register for scene loaded event
        SceneManager.sceneLoaded += OnOverworldSceneLoaded;
    }
    
    /// <summary>
    /// Called when the overworld scene has loaded
    /// </summary>
    private void OnOverworldSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Only execute this for the overworld scene
        if (scene.name == overworldSceneName)
        {
            StartCoroutine(SetupOverworldAfterCombat());
        }
        
        // Unregister the event to prevent multiple calls
        SceneManager.sceneLoaded -= OnOverworldSceneLoaded;
    }
    
    /// <summary>
    /// Setup the overworld after returning from combat
    /// </summary>
    private IEnumerator SetupOverworldAfterCombat()
    {
        // Wait for a frame to make sure all objects are initialized
        yield return null;
        
        // Find the player in the scene
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        
        if (player != null)
        {
            // Restore player position
            player.transform.position = playerPosition;
            
            // Restore player inventory
            PlayerInventory newInventory = player.GetComponent<PlayerInventory>();
            if (newInventory != null && playerInventory != null)
            {
                Debug.Log("=== INVENTORY DEBUG: SetupOverworldAfterCombat ===");
                Debug.Log("Stored inventory from combat:");
                foreach (var item in storedItems)
                {
                    Debug.Log($"Stored item: {item.name}, Amount: {item.amount}");
                }
                
                // Clear the old inventory first to prevent duplicating items
                newInventory.ClearInventory();
                
                // Copy items from saved inventory to new inventory
                foreach (ItemData item in storedItems)
                {
                    newInventory.AddItem(item);
                    Debug.Log($"Restored {item.name} (x{item.amount}) to overworld player inventory");
                }
                
                // Log final overworld inventory
                Debug.Log("Final overworld inventory:");
                foreach (var item in newInventory.Items)
                {
                    Debug.Log($"Final overworld item: {item.name}, Amount: {item.amount}");
                }
            }
            
            // If combat was won, destroy the enemy
            if (combatWon && enemyThatInitiatedCombat != null)
            {
                Destroy(enemyThatInitiatedCombat);
                enemyThatInitiatedCombat = null;
            }
        }
        else
        {
            Debug.LogError("Player not found in overworld scene!");
        }
    }
    
    /// <summary>
    /// Gets the player's inventory for use in combat
    /// </summary>
    /// <returns>A list of items from the player's inventory</returns>
    public List<ItemData> GetPlayerInventory()
    {
        if (storedItems.Count > 0)
        {
            Debug.Log($"SceneTransitionManager providing {storedItems.Count} items to combat");
            return storedItems;
        }
        else
        {
            Debug.LogWarning("No player inventory found when requested by combat scene");
            return new List<ItemData>();
        }
    }
    
    /// <summary>
    /// Updates the stored player inventory with changes from combat
    /// </summary>
    /// <param name="updatedItems">The updated list of items after combat</param>
    public void SetPlayerInventory(List<ItemData> updatedItems)
    {
        Debug.Log("=== INVENTORY DEBUG: SetPlayerInventory ===");
        
        if (storedItems != null && updatedItems != null)
        {
            // Log inventory before updating
            Debug.Log("Current inventory before update:");
            foreach (var item in storedItems)
            {
                Debug.Log($"Current item: {item.name}, Amount: {item.amount}");
            }
            
            // Log incoming updated inventory
            Debug.Log("Incoming updated inventory:");
            foreach (var item in updatedItems)
            {
                Debug.Log($"Updated item: {item.name}, Amount: {item.amount}");
            }
            
            // Clear existing inventory
            storedItems.Clear();
            
            // Add all items from the updated list
            foreach (ItemData item in updatedItems)
            {
                storedItems.Add(item);
            }
            
            Debug.Log($"SceneTransitionManager updated player inventory with {updatedItems.Count} items from combat");
            
            // Log final inventory after update
            Debug.Log("Final inventory after update:");
            foreach (var item in storedItems)
            {
                Debug.Log($"Final item: {item.name}, Amount: {item.amount}");
            }
        }
        else
        {
            Debug.LogError("No playerInventory available to update with combat items!");
        }
    }
} 