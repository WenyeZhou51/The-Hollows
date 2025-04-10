using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class SceneTransitionManager : MonoBehaviour
{
    // Singleton pattern
    public static SceneTransitionManager Instance { get; private set; }
    
    // Track if application is quitting
    private static bool isQuitting = false;
    
    // Scene names
    [SerializeField] private string overworldSceneName = "Overworld_Startroom";
    [SerializeField] private string combatSceneName = "Battle_Obelisk"; // Will be set dynamically at runtime for enemies
    
    // Enemy that initiated the combat
    private GameObject enemyThatInitiatedCombat;
    
    // Store the enemy's unique ID that initiated combat
    private string enemyIdThatInitiatedCombat;
    
    // Player data to persist between scenes
    private PlayerInventory playerInventory;
    private Vector3 playerPosition;
    
    // Combat results
    private bool combatWon = false;
    
    // Store the current scene name to return to after combat
    private string currentSceneName;
    
    private List<ItemData> storedItems = new List<ItemData>();
    
    // New fields to track scene transitions
    private string targetSceneName;
    private string targetMarkerId;
    
    // New fields for multiple battle types
    private string[] battleScenes = new string[] { "Battle_Weaver", "Battle_Aperture" };
    
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
    
    private void OnApplicationQuit()
    {
        // Set flag to avoid creating objects during application exit
        isQuitting = true;
    }
    
    /// <summary>
    /// Ensures an instance of SceneTransitionManager exists in the scene
    /// </summary>
    /// <returns>The SceneTransitionManager instance</returns>
    public static SceneTransitionManager EnsureExists()
    {
        // Don't create a new instance if the application is quitting or we're switching scenes
        if (isQuitting || SceneManager.GetActiveScene().isLoaded == false)
        {
            return Instance;
        }
        
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
                // Only create a new instance if we're not during scene unloading
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
        // Store the current scene name to return to after combat
        currentSceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"Combat initiated in scene: {currentSceneName}. Will return here after combat.");
        
        // Store the enemy that initiated combat
        enemyThatInitiatedCombat = enemy;
        
        // Get and store the enemy's unique ID
        EnemyIdentifier enemyIdentifier = enemy.GetComponent<EnemyIdentifier>();
        if (enemyIdentifier != null)
        {
            enemyIdThatInitiatedCombat = enemyIdentifier.GetEnemyId();
            Debug.Log($"Combat initiated by enemy with ID: {enemyIdThatInitiatedCombat}");
        }
        else
        {
            Debug.LogWarning("Enemy is missing EnemyIdentifier component. Enemy won't be tracked for defeat.");
            enemyIdThatInitiatedCombat = null;
        }
        
        // CRITICAL CHANGE: Randomly select either Weaver or Aperture battle (50/50 chance)
        // Never use Obelisk battle for random enemies
        int randomIndex = Random.Range(0, battleScenes.Length);
        combatSceneName = battleScenes[randomIndex];
        Debug.Log($"Randomly selected battle scene: {combatSceneName}");
        
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
                Debug.Log($"Item: {item.name}, Amount: {item.amount}, Type: {item.type}");
            }
        }
        else
        {
            Debug.LogWarning("Player inventory is null or empty when starting combat!");
        }
        
        // Clear previous stored items
        storedItems.Clear();
        
        // Create proper clones of each item to ensure all properties are preserved
        if (playerInventory != null && playerInventory.Items != null)
        {
            foreach(var item in playerInventory.Items) 
            {
                // Use the Clone method to ensure we keep all properties, including type
                ItemData clonedItem = item.Clone();
                
                // Extra verification specifically for Cold Key
                if (item.name == "Cold Key" || item.type == ItemData.ItemType.KeyItem)
                {
                    // Force item type to be KeyItem
                    clonedItem.type = ItemData.ItemType.KeyItem;
                    Debug.Log($"Ensuring item {clonedItem.name} is properly tagged as KeyItem type");
                }
                
                storedItems.Add(clonedItem);
                Debug.Log($"Stored cloned item for combat transition: {clonedItem.name}, Type: {clonedItem.type}");
            }
        }
        
        // Make sure ScreenFader exists
        ScreenFader.EnsureExists();
        
        // Start the transition with fade effect
        StartCoroutine(TransitionToCombat());
    }
    
    /// <summary>
    /// Handle the transition to combat scene with fade effect
    /// </summary>
    private IEnumerator TransitionToCombat()
    {
        Debug.Log($"Beginning transition to combat scene: {combatSceneName} (randomly selected from Weaver/Aperture)");
        
        // Validate the scene name before attempting transition
        if (!IsSceneValid(combatSceneName))
        {
            Debug.LogError($"Combat scene '{combatSceneName}' does not exist in build settings. Make sure to add it in File > Build Settings.");
            // Fade back from black since we're not transitioning
            StartCoroutine(ScreenFader.Instance.FadeFromBlack());
            yield break;
        }
        
        // Fade to black
        yield return StartCoroutine(ScreenFader.Instance.FadeToBlack());
        
        // IMPORTANT: Register for scene loaded event BEFORE loading scene
        SceneManager.sceneLoaded += OnCombatSceneLoaded;
        
        // Load the combat scene
        Debug.Log($"Now loading randomly selected combat scene: {combatSceneName}");
        SceneManager.LoadScene(combatSceneName);
    }
    
    /// <summary>
    /// Called when the combat scene has loaded
    /// </summary>
    private void OnCombatSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Only execute this for combat scenes (Weaver or Aperture)
        if (scene.name == combatSceneName || scene.name.StartsWith("Battle_"))
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
                Debug.LogError($"CombatManager not found in combat scene: {scene.name}!");
            }
            
            // Fade from black once the scene is set up
            StartCoroutine(ScreenFader.Instance.FadeFromBlack());
        }
        
        // Unregister the event to prevent multiple calls
        SceneManager.sceneLoaded -= OnCombatSceneLoaded;
    }
    
    /// <summary>
    /// Setup the combat scene with the player's inventory
    /// </summary>
    private void SetupCombatScene(CombatManager combatManager)
    {
        // Filter items first to remove key items
        List<ItemData> filteredItems = new List<ItemData>();
        
        foreach (var item in storedItems)
        {
            // Skip key items completely for combat
            if (item.IsKeyItem())
            {
                Debug.Log($"FILTERING OUT key item from combat completely: {item.name} - Key items not available in combat");
                continue;
            }
            
            // Only add non-key items to the filtered list
            filteredItems.Add(item.Clone());
            Debug.Log($"Keeping item for combat: {item.name}, Amount: {item.amount}, Type: {item.type}");
        }
        
        // Pass player inventory to combat manager - ONLY filtered items
        if (filteredItems.Count > 0)
        {
            Debug.Log("=== INVENTORY DEBUG: SetupCombatScene ===");
            Debug.Log($"Setting up combat scene with {filteredItems.Count} filtered player inventory items (KeyItems removed)");
            
            // Pass FILTERED inventory to combat manager
            combatManager.SetupPlayerInventory(filteredItems);
            
            // Also set up individual player character inventories with FILTERED items only
            foreach (var player in combatManager.players)
            {
                if (player != null)
                {
                    // Clear existing inventory
                    player.items.Clear();
                    
                    // Copy FILTERED items to player's inventory
                    foreach (var item in filteredItems)
                    {
                        player.items.Add(item);
                        Debug.Log($"Added {item.name} (x{item.amount}) to player character: {player.characterName}");
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("No filtered items for combat - all items were KeyItems or inventory was empty");
            
            // Ensure the combat manager has an empty list, not null
            combatManager.SetupPlayerInventory(new List<ItemData>());
            
            // Also clear all player character inventories
            foreach (var player in combatManager.players)
            {
                if (player != null)
                {
                    player.items.Clear();
                }
            }
        }
        
        // Listen for combat end event
        combatManager.OnCombatEnd += EndCombat;
    }
    
    /// <summary>
    /// End combat and return to the overworld
    /// </summary>
    /// <param name="won">Whether the player won the combat</param>
    public void EndCombat(bool won)
    {
        // Store combat result
        combatWon = won;
        
        // If the player won, mark the enemy as defeated in the persistent manager
        if (won && !string.IsNullOrEmpty(enemyIdThatInitiatedCombat))
        {
            // Make sure the persistent game manager exists
            PersistentGameManager.EnsureExists();
            
            // Mark this enemy as defeated in the persistent game manager
            PersistentGameManager.Instance.MarkEnemyDefeated(enemyIdThatInitiatedCombat);
            Debug.Log($"Marked enemy {enemyIdThatInitiatedCombat} as defeated");
            
            // Log all defeated enemies for debugging
            PersistentGameManager.Instance.LogDefeatedEnemies();
        }
        
        // Make sure ScreenFader exists
        ScreenFader.EnsureExists();
        
        // Start the transition with fade effect
        StartCoroutine(TransitionToOverworld());
    }
    
    /// <summary>
    /// Handle the transition to overworld scene with fade effect
    /// </summary>
    private IEnumerator TransitionToOverworld()
    {
        Debug.Log($"Beginning transition back to scene: {currentSceneName}");
        
        // Validate the scene name before attempting transition
        if (!IsSceneValid(currentSceneName))
        {
            Debug.LogError($"Scene '{currentSceneName}' does not exist in build settings. Falling back to entrance scene.");
            // Fall back to the entrance scene if the current scene is invalid
            currentSceneName = overworldSceneName;
            
            if (!IsSceneValid(overworldSceneName)) {
                Debug.LogError($"Fallback scene '{overworldSceneName}' also does not exist. Make sure to add it in File > Build Settings.");
                // Fade back from black since we're not transitioning
                StartCoroutine(ScreenFader.Instance.FadeFromBlack());
                yield break;
            }
        }
        
        // Fade to black
        yield return StartCoroutine(ScreenFader.Instance.FadeToBlack());
        
        // IMPORTANT: Register for scene loaded event BEFORE loading scene
        SceneManager.sceneLoaded += OnOverworldSceneLoaded;
        
        // Load the scene we came from
        Debug.Log($"Now loading scene: {currentSceneName}");
        SceneManager.LoadScene(currentSceneName);
    }
    
    /// <summary>
    /// Called when the overworld scene has loaded
    /// </summary>
    private void OnOverworldSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Only execute this if we loaded the correct scene
        if (scene.name == currentSceneName || scene.name == overworldSceneName)
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
        // Wait a frame to ensure all scene objects are initialized
        yield return null;
        
        // Find the player in the scene
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Debug.Log("Found player in overworld, setting up after combat");
            
            // Wait another frame to ensure all components are initialized
            yield return null;
            
            // Calculate a safe position that's slightly offset from where combat was initiated
            // This prevents immediate re-collision with enemies
            Vector3 safePosition = playerPosition;
            
            // If the player won combat, apply a small offset to avoid immediate collision
            if (combatWon)
            {
                // Offset the player by 0.5 units in the -Y direction (backing away from the enemy)
                safePosition += new Vector3(0, -0.5f, 0);
            }
            
            // Restore player position with the safe offset
            Debug.Log($"Positioning player at {safePosition} after returning from combat");
            player.transform.position = safePosition;
            
            // Restore player inventory
            PlayerInventory newInventory = player.GetComponent<PlayerInventory>();
            if (newInventory != null && playerInventory != null)
            {
                Debug.Log("=== INVENTORY DEBUG: SetupOverworldAfterCombat ===");
                Debug.Log("Stored inventory from combat:");
                foreach (var item in storedItems)
                {
                    Debug.Log($"Stored item: {item.name}, Amount: {item.amount}, Type: {item.type}");
                }
                
                // Special Check: Look for Cold Key in PersistentGameManager
                var persistentInventory = PersistentGameManager.Instance?.GetPlayerInventory();
                bool hasColdKeyInPersistent = persistentInventory != null && persistentInventory.ContainsKey("Cold Key");
                
                if (hasColdKeyInPersistent)
                {
                    Debug.Log("CRITICAL NOTICE: Cold Key found in PersistentGameManager - ensuring it's preserved");
                    // Make sure Cold Key exists in stored items
                    bool coldKeyInStored = false;
                    foreach (var item in storedItems)
                    {
                        if (item.name == "Cold Key")
                        {
                            coldKeyInStored = true;
                            // Make sure it's marked as a KeyItem
                            item.type = ItemData.ItemType.KeyItem;
                            Debug.Log("Updated Cold Key type to KeyItem in stored items");
                            break;
                        }
                    }
                    
                    // If not in stored items, add it
                    if (!coldKeyInStored)
                    {
                        int amount = persistentInventory["Cold Key"];
                        ItemData coldKey = new ItemData(
                            "Cold Key", 
                            "A frigid key that seems to emanate cold. You won it from a mysterious figure in a game of Ravenbond.", 
                            amount, 
                            false, 
                            ItemData.ItemType.KeyItem
                        );
                        storedItems.Add(coldKey);
                        Debug.Log("Added missing Cold Key to stored items list");
                    }
                }
                
                // Clear the old inventory first to prevent duplicating items
                newInventory.ClearInventory();
                
                // Copy items from saved inventory to new inventory
                foreach (ItemData item in storedItems)
                {
                    newInventory.AddItem(item);
                    Debug.Log($"Restored {item.name} (x{item.amount}, Type: {item.type}) to overworld player inventory");
                }
                
                // Double-check for Cold Key
                bool hasColdKeyInFinal = false;
                Debug.Log("Final overworld inventory:");
                foreach (var item in newInventory.Items)
                {
                    Debug.Log($"Final overworld item: {item.name}, Amount: {item.amount}, Type: {item.type}");
                    if (item.name == "Cold Key")
                    {
                        hasColdKeyInFinal = true;
                    }
                }
                
                // Final verification for Cold Key
                if (hasColdKeyInPersistent && !hasColdKeyInFinal)
                {
                    Debug.LogWarning("CRITICAL ERROR: Cold Key was in PersistentGameManager but failed to transfer to player inventory - forcing addition");
                    ItemData coldKey = new ItemData(
                        "Cold Key", 
                        "A frigid key that seems to emanate cold. You won it from a mysterious figure in a game of Ravenbond.", 
                        1, 
                        false, 
                        ItemData.ItemType.KeyItem
                    );
                    newInventory.AddItem(coldKey);
                    Debug.Log("Forcibly added Cold Key to player inventory as final failsafe");
                }
            }
            
            // Fade from black after setup is complete
            StartCoroutine(ScreenFader.Instance.FadeFromBlack());
        }
        else
        {
            Debug.LogError("Player not found in overworld scene!");
            
            // Fade from black even if player wasn't found to prevent screen staying black
            StartCoroutine(ScreenFader.Instance.FadeFromBlack());
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
    
    /// <summary>
    /// Transition to a different scene using a PlayerMarker as spawn point
    /// </summary>
    /// <param name="sceneName">Name of the target scene</param>
    /// <param name="markerId">ID of the PlayerMarker to spawn at</param>
    /// <param name="player">The player GameObject</param>
    public void TransitionToScene(string sceneName, string markerId, GameObject player)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("Cannot transition to empty scene name");
            return;
        }
        
        if (string.IsNullOrEmpty(markerId))
        {
            Debug.LogError("Cannot transition with empty marker ID");
            return;
        }
        
        // Store transition details
        targetSceneName = sceneName;
        targetMarkerId = markerId;
        
        // Store player inventory for restoration after transition
        playerInventory = player.GetComponent<PlayerInventory>();
        
        // Log the transition in detail for debugging
        Debug.Log($"[SCENE TRANSITION] Starting transition from '{SceneManager.GetActiveScene().name}' to '{sceneName}' with marker ID '{markerId}'");
        
        // Make sure ScreenFader exists
        ScreenFader.EnsureExists();
        
        // Start the transition with fade effect
        StartCoroutine(PerformSceneTransition());
    }
    
    /// <summary>
    /// Handle the actual scene transition
    /// </summary>
    private IEnumerator PerformSceneTransition()
    {
        Debug.Log($"[SCENE TRANSITION] Beginning transition to scene: {targetSceneName} with marker ID: {targetMarkerId}");
        
        // Validate the scene name before attempting transition
        if (!IsSceneValid(targetSceneName))
        {
            Debug.LogError($"[SCENE TRANSITION] CRITICAL ERROR: Scene '{targetSceneName}' does not exist in build settings. Make sure to add it in File > Build Settings.");
            // Fade back from black since we're not transitioning
            StartCoroutine(ScreenFader.Instance.FadeFromBlack());
            yield break;
        }
        
        // Fade to black
        yield return StartCoroutine(ScreenFader.Instance.FadeToBlack());
        
        // IMPORTANT: Register for scene loaded event BEFORE loading scene
        SceneManager.sceneLoaded += OnSceneTransitionComplete;
        
        // Load the target scene
        Debug.Log($"[SCENE TRANSITION] Now loading scene: {targetSceneName}");
        SceneManager.LoadScene(targetSceneName);
    }
    
    /// <summary>
    /// Validates if a scene exists in build settings
    /// </summary>
    private bool IsSceneValid(string sceneName)
    {
        // Check if the scene exists in build settings
        int sceneCount = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < sceneCount; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            
            if (string.Equals(sceneNameFromPath, sceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Called when a scene transition has completed
    /// </summary>
    private void OnSceneTransitionComplete(Scene scene, LoadSceneMode mode)
    {
        // Start the setup coroutine
        StartCoroutine(SetupPlayerAfterTransition());
        
        // Unregister the event to prevent multiple calls
        SceneManager.sceneLoaded -= OnSceneTransitionComplete;
    }
    
    /// <summary>
    /// Setup the player after scene transition
    /// </summary>
    private IEnumerator SetupPlayerAfterTransition()
    {
        // Wait for two frames to make sure all objects are fully initialized in the scene
        yield return null;
        yield return null;
        
        // Find the player in the scene
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        
        if (player != null)
        {
            // Try to find the target PlayerMarker
            bool markerFound = false;
            PlayerMarker[] markers = FindObjectsOfType<PlayerMarker>();
            
            // Log all markers to debug output to help diagnose issues
            Debug.Log($"Looking for marker with ID '{targetMarkerId}' in scene '{SceneManager.GetActiveScene().name}'");
            Debug.Log($"Found {markers.Length} PlayerMarker objects in the scene:");
            foreach (PlayerMarker marker in markers)
            {
                Debug.Log($"Available marker: ID='{marker.MarkerId}', Position={marker.transform.position}, GameObject={marker.gameObject.name}");
            }
            
            // Search for the matching marker
            foreach (PlayerMarker marker in markers)
            {
                // Case insensitive comparison to avoid common errors
                if (string.Equals(marker.MarkerId, targetMarkerId, System.StringComparison.OrdinalIgnoreCase))
                {
                    // Position the player at the marker
                    player.transform.position = marker.transform.position;
                    Debug.Log($"Successfully positioned player at marker with ID '{targetMarkerId}' at position {marker.transform.position}");
                    markerFound = true;
                    break;
                }
            }
            
            if (!markerFound)
            {
                Debug.LogError($"CRITICAL ERROR: PlayerMarker with ID '{targetMarkerId}' not found in scene '{SceneManager.GetActiveScene().name}'!");
                Debug.LogError("Make sure the marker exists in the scene and the ID matches exactly what's specified in the TransitionArea.");
                
                // Without a marker, we cannot position the player properly.
                // This is intentional - we never want to fall back to a default position.
                // The game designers must ensure that all necessary markers exist in each scene.
            }
            
            // Restore player inventory if needed
            PlayerInventory newInventory = player.GetComponent<PlayerInventory>();
            if (newInventory != null)
            {
                // Ensure PersistentGameManager exists
                PersistentGameManager.EnsureExists();
                
                // If we have a stored inventory in the PersistentGameManager, use that
                Dictionary<string, int> persistentInventory = PersistentGameManager.Instance.GetPlayerInventory();
                
                if (persistentInventory.Count > 0)
                {
                    // Clear current inventory
                    newInventory.ClearInventory();
                    
                    // Add each item from the persistent inventory
                    foreach (var pair in persistentInventory)
                    {
                        // Create a new item with the stored data
                        ItemData item = new ItemData(pair.Key, "", pair.Value, false);
                        newInventory.AddItem(item);
                        Debug.Log($"Restored {pair.Key} (x{pair.Value}) to player inventory in new scene");
                    }
                    
                    Debug.Log("Player inventory restored from PersistentGameManager");
                }
                // If no persistent inventory but we have the original inventory from before transition
                else if (playerInventory != null && playerInventory != newInventory)
                {
                    // Clear current inventory
                    newInventory.ClearInventory();
                    
                    // Add items from the original inventory (fallback)
                    foreach (ItemData item in playerInventory.Items)
                    {
                        newInventory.AddItem(item);
                        
                        // Also update the persistent manager
                        PersistentGameManager.Instance.AddItemToInventory(item.name, item.amount);
                        
                        Debug.Log($"Restored {item.name} (x{item.amount}) to player inventory in new scene");
                    }
                    
                    Debug.Log("Player inventory restored from original inventory and saved to PersistentGameManager");
                }
            }
            
            // Character stats are automatically loaded by the Character component
            // when it starts in the new scene, through the PersistentGameManager
        }
        else
        {
            Debug.LogError("Player not found in target scene!");
        }
        
        // Fade from black
        yield return StartCoroutine(ScreenFader.Instance.FadeFromBlack());

        // CRITICAL FIX: Add safety check to ensure screen is properly reset
        if (ScreenFader.Instance != null && ScreenFader.Instance.gameObject.activeInHierarchy)
        {
            // Force a second check after a brief delay to ensure fade completed properly
            yield return new WaitForSeconds(0.1f);
            
            // Force reset the screen to visible if it's still not clear
            Image fadeImage = ScreenFader.Instance.GetComponentInChildren<Image>();
            if (fadeImage != null && fadeImage.color.a > 0.05f)
            {
                Debug.LogWarning("⚠️ Screen still not clear after scene transition fade! Forcing reset to visible");
                ScreenFader.Instance.ResetToVisible();
            }
        }
    }
} 