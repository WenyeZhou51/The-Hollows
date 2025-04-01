using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    // Singleton pattern
    public static SceneTransitionManager Instance { get; private set; }
    
    // Track if application is quitting
    private static bool isQuitting = false;
    
    // Scene names
    [SerializeField] private string overworldSceneName = "Overworld_entrance";
    [SerializeField] private string combatSceneName = "Battle_Obelisk";
    
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
    private Vector3 fallbackPosition;
    
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
        Debug.Log($"Beginning transition to combat scene: {combatSceneName}");
        
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
        Debug.Log($"Now loading combat scene: {combatSceneName}");
        SceneManager.LoadScene(combatSceneName);
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
        // Wait for a frame to make sure all objects are initialized
        yield return null;
        
        // Make sure the persistent game manager exists for enemy removal
        PersistentGameManager.EnsureExists();
        
        // If combat was won, find and destroy any enemies with the ID that initiated combat
        if (combatWon && !string.IsNullOrEmpty(enemyIdThatInitiatedCombat))
        {
            // Log that we're attempting to destroy enemies
            Debug.Log($"Searching for enemy with ID {enemyIdThatInitiatedCombat} to destroy after combat");
            
            // Find all enemies in the scene
            EnemyIdentifier[] allEnemies = FindObjectsOfType<EnemyIdentifier>();
            
            // Check each enemy
            foreach (EnemyIdentifier enemy in allEnemies)
            {
                string currentEnemyId = enemy.GetEnemyId();
                Debug.Log($"Checking enemy {enemy.gameObject.name} with ID {currentEnemyId}");
                
                if (currentEnemyId == enemyIdThatInitiatedCombat)
                {
                    Debug.Log($"FOUND THE ENEMY TO DESTROY: {enemy.gameObject.name} with ID {currentEnemyId}");
                    DestroyImmediate(enemy.gameObject);
                    break;
                }
            }
            
            // Double-check with the PersistentGameManager
            PersistentGameManager.Instance.LogDefeatedEnemies();
        }
        
        // Find the player in the scene
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        
        if (player != null)
        {
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
            player.transform.position = safePosition;
            
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
        
        // Store transition details
        targetSceneName = sceneName;
        targetMarkerId = markerId;
        
        // Store player inventory and current position as fallback
        playerInventory = player.GetComponent<PlayerInventory>();
        fallbackPosition = player.transform.position;
        
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
        Debug.Log($"Beginning transition to scene: {targetSceneName} with marker ID: {targetMarkerId}");
        
        // Validate the scene name before attempting transition
        if (!IsSceneValid(targetSceneName))
        {
            Debug.LogError($"Scene '{targetSceneName}' does not exist in build settings. Make sure to add it in File > Build Settings.");
            // Fade back from black since we're not transitioning
            StartCoroutine(ScreenFader.Instance.FadeFromBlack());
            yield break;
        }
        
        // Fade to black
        yield return StartCoroutine(ScreenFader.Instance.FadeToBlack());
        
        // IMPORTANT: Register for scene loaded event BEFORE loading scene
        SceneManager.sceneLoaded += OnSceneTransitionComplete;
        
        // Load the target scene
        Debug.Log($"Now loading scene: {targetSceneName}");
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
        // Wait a frame to make sure all objects are initialized
        yield return null;
        
        // Find the player in the scene
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        
        if (player != null)
        {
            // Try to find the target PlayerMarker
            bool markerFound = false;
            PlayerMarker[] markers = FindObjectsOfType<PlayerMarker>();
            
            Debug.Log($"Looking for marker with ID '{targetMarkerId}' in scene '{targetSceneName}'");
            foreach (PlayerMarker marker in markers)
            {
                Debug.Log($"Found marker with ID: {marker.MarkerId} in scene {SceneManager.GetActiveScene().name}");
                
                // Only compare the local ID part since we're already in the target scene
                if (marker.MarkerId == targetMarkerId)
                {
                    // Position the player at the marker
                    player.transform.position = marker.transform.position;
                    Debug.Log($"Positioned player at marker with ID {targetMarkerId} in scene {targetSceneName}");
                    markerFound = true;
                    break;
                }
            }
            
            if (!markerFound)
            {
                Debug.LogWarning($"PlayerMarker with ID '{targetMarkerId}' not found in scene {targetSceneName}. Using default position.");
                // If no matching marker was found, use a fallback position
                player.transform.position = fallbackPosition;
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
    }
} 