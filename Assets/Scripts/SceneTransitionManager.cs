using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using System;
using TMPro;

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
    
    // Change back to static variable since that's likely the issue - the instance value isn't persisting across scene changes
    // Static flag to track transition state across scene changes
    private static bool isFadingInProgress = false;
    
    // Static string to track which transition is currently in progress
    private static string currentTransitionDescription = "";
    
    // Keep the static timestamp to track transition start time
    private static float lastTransitionStartTime = 0f;
    
    // Flag to track if we're already subscribed to CombatManager.OnCombatEnd
    private bool isSubscribedToCombatEnd = false;
    
    // New field to store disabled components
    private Dictionary<object, bool> _disabledComponents = new Dictionary<object, bool>();
    
    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log($"[SCENE TRANSITION DEBUG] SceneTransitionManager Awake - set as singleton instance (ID: {GetInstanceID()})");
            Debug.Log($"[SCENE TRANSITION DEBUG] DontDestroyOnLoad set on SceneTransitionManager");
            
            // CRITICAL FIX: When a new instance is created, check if we have scene info in PlayerPrefs
            if (PlayerPrefs.HasKey("ReturnSceneName"))
            {
                currentSceneName = PlayerPrefs.GetString("ReturnSceneName");
                Debug.Log($"[SCENE TRANSITION DEBUG] On Awake: Loaded return scene from PlayerPrefs: {currentSceneName}");
            }
            
            // CRITICAL FIX: Subscribe to scene loaded events to hook into CombatManager when needed
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Debug.Log($"[SCENE TRANSITION DEBUG] Destroying duplicate SceneTransitionManager (ID: {GetInstanceID()}, keeping existing ID: {Instance.GetInstanceID()})");
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
            
            Debug.Log($"[TRANSITION DEBUG] EnsureExists found {managers.Length} SceneTransitionManager instances");
            
            if (managers.Length > 0)
            {
                // Use first instance found
                Instance = managers[0];
                Debug.Log($"[TRANSITION DEBUG] Using existing SceneTransitionManager (ID: {Instance.GetInstanceID()}) in scene {SceneManager.GetActiveScene().name}");
                
                // Mark it with DontDestroyOnLoad if it's not already
                if (Instance.gameObject.scene.name == SceneManager.GetActiveScene().name)
                {
                    DontDestroyOnLoad(Instance.gameObject);
                    Debug.Log($"[TRANSITION DEBUG] Applied DontDestroyOnLoad to existing SceneTransitionManager");
                }
                
                // Destroy any extras
                for (int i = 1; i < managers.Length; i++)
                {
                    Debug.Log($"[TRANSITION DEBUG] Destroying extra SceneTransitionManager instance (ID: {managers[i].GetInstanceID()})");
                    Destroy(managers[i].gameObject);
                }
            }
            else
            {
                // Only create a new instance if we're not during scene unloading
                GameObject managerObj = new GameObject("SceneTransitionManager");
                Instance = managerObj.AddComponent<SceneTransitionManager>();
                DontDestroyOnLoad(managerObj);
                Debug.Log($"[TRANSITION DEBUG] Created new SceneTransitionManager (ID: {Instance.GetInstanceID()})");
            }
        }
        else
        {
            // Double check for any new duplicates that might have been created
            SceneTransitionManager[] managers = FindObjectsOfType<SceneTransitionManager>();
            if (managers.Length > 1)
            {
                Debug.Log($"[TRANSITION DEBUG] Found {managers.Length} SceneTransitionManager instances despite Instance already set");
                
                // Destroy all instances except the one we're already tracking
                foreach (var manager in managers)
                {
                    if (manager != Instance)
                    {
                        Debug.Log($"[TRANSITION DEBUG] Destroying duplicate SceneTransitionManager (ID: {manager.GetInstanceID()})");
                        Destroy(manager.gameObject);
                    }
                }
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
        Debug.Log("[TRANSITION DEBUG] StartCombat initiated");
        
        // Check if we're already in the middle of a transition (either fade or scene change)
        // This prevents overworld-to-battle transitions from stacking on top of other transitions
        if (isFadingInProgress)
        {
            Debug.Log("[TRANSITION DEBUG] Ignoring combat request because a scene transition is already in progress");
            return;
        }
        
        // Set fading flag to prevent multiple transitions
        isFadingInProgress = true;
        
        // Store the current scene name to return to after combat
        currentSceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"[TRANSITION DEBUG] Stored scene name: {currentSceneName} to return after combat");
        
        // Store the enemy that initiated combat
        enemyThatInitiatedCombat = enemy;
        Debug.Log($"[TRANSITION DEBUG] Stored enemy reference: {enemy.name}");
        
        // Get and store the enemy's unique ID
        EnemyIdentifier enemyIdentifier = enemy.GetComponent<EnemyIdentifier>();
        if (enemyIdentifier != null)
        {
            enemyIdThatInitiatedCombat = enemyIdentifier.GetEnemyId();
            Debug.Log($"[TRANSITION DEBUG] Stored enemy ID: {enemyIdThatInitiatedCombat}");
        }
        else
        {
            Debug.LogWarning("[TRANSITION DEBUG] Enemy is missing EnemyIdentifier component. Enemy won't be tracked for defeat.");
            enemyIdThatInitiatedCombat = null;
        }
        
        // CRITICAL CHANGE: Randomly select either Weaver or Aperture battle (50/50 chance)
        // Never use Obelisk battle for random enemies
        int randomIndex = Random.Range(0, battleScenes.Length);
        combatSceneName = battleScenes[randomIndex];
        Debug.Log($"[TRANSITION DEBUG] Randomly selected battle scene: {combatSceneName}");
        
        // Store player inventory and position
        playerInventory = player.GetComponent<PlayerInventory>();
        playerPosition = player.transform.position;
        Debug.Log($"[TRANSITION DEBUG] Stored player position: {playerPosition}");
        
        // Debug log the exact player inventory contents
        Debug.Log("[TRANSITION DEBUG] === Storing player inventory ===");
        if (playerInventory != null && playerInventory.Items != null) 
        {
            Debug.Log($"[TRANSITION DEBUG] Player inventory contains {playerInventory.Items.Count} items");
            foreach (var item in playerInventory.Items)
            {
                Debug.Log($"[TRANSITION DEBUG] Inventory item: {item.name}, Amount: {item.amount}, Type: {item.type}");
            }
        }
        else
        {
            Debug.LogWarning("[TRANSITION DEBUG] Player inventory is null or empty when starting combat!");
        }
        
        // Clear previous stored items
        storedItems.Clear();
        Debug.Log("[TRANSITION DEBUG] Cleared previous stored items");
        
        // Create proper clones of each item to ensure all properties are preserved
        if (playerInventory != null && playerInventory.Items != null)
        {
            Debug.Log("[TRANSITION DEBUG] Starting to clone inventory items");
            foreach(var item in playerInventory.Items) 
            {
                // Use the Clone method to ensure we keep all properties, including type
                ItemData clonedItem = item.Clone();
                Debug.Log($"[TRANSITION DEBUG] Cloned inventory item: {clonedItem.name}, Amount: {clonedItem.amount}, Type: {clonedItem.type}");
                
                // Extra verification specifically for Cold Key
                if (item.name == "Cold Key" || item.type == ItemData.ItemType.KeyItem)
                {
                    // Force item type to be KeyItem
                    clonedItem.type = ItemData.ItemType.KeyItem;
                    Debug.Log($"[TRANSITION DEBUG] Ensured {clonedItem.name} is properly tagged as KeyItem type");
                }
                
                storedItems.Add(clonedItem);
                Debug.Log($"[TRANSITION DEBUG] Added cloned item to stored items list: {clonedItem.name}");
            }
            Debug.Log($"[TRANSITION DEBUG] Completed cloning {storedItems.Count} inventory items");
        }
        
        // Make sure ScreenFader exists
        ScreenFader.EnsureExists();
        Debug.Log("[TRANSITION DEBUG] Ensured ScreenFader exists");
        
        // Start the transition with fade effect
        Debug.Log("[TRANSITION DEBUG] Starting TransitionToCombat coroutine");
        StartCoroutine(TransitionToCombat());
    }
    
    /// <summary>
    /// Handle the transition to combat scene with fade effect
    /// </summary>
    private IEnumerator TransitionToCombat()
    {
        Debug.Log($"[TRANSITION DEBUG] TransitionToCombat coroutine started for scene: {combatSceneName}");
        
        // Validate the scene name before attempting transition
        if (!IsSceneValid(combatSceneName))
        {
            Debug.LogError($"[TRANSITION DEBUG] Combat scene '{combatSceneName}' does not exist in build settings. Make sure to add it in File > Build Settings.");
            // Fade back from black since we're not transitioning
            StartCoroutine(ScreenFader.Instance.FadeFromBlack());
            yield break;
        }
        
        // Fade to black
        Debug.Log("[TRANSITION DEBUG] Starting fade to black");
        yield return StartCoroutine(ScreenFader.Instance.FadeToBlack());
        Debug.Log("[TRANSITION DEBUG] Fade to black completed");
        
        // IMPORTANT: Register for scene loaded event BEFORE loading scene
        Debug.Log("[TRANSITION DEBUG] Registering OnCombatSceneLoaded event");
        SceneManager.sceneLoaded += OnCombatSceneLoaded;
        
        // Load the combat scene
        Debug.Log($"[TRANSITION DEBUG] Now loading combat scene: {combatSceneName}");
        SceneManager.LoadScene(combatSceneName);
    }
    
    /// <summary>
    /// Called when the combat scene has loaded
    /// </summary>
    private void OnCombatSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[TRANSITION DEBUG] OnCombatSceneLoaded called for scene: {scene.name}");
        
        // Only execute this for combat scenes (Weaver or Aperture)
        if (scene.name == combatSceneName || scene.name.StartsWith("Battle_"))
        {
            Debug.Log("[TRANSITION DEBUG] Confirmed this is the correct combat scene");
            
            // Find the combat manager
            CombatManager combatManager = FindObjectOfType<CombatManager>();
            
            if (combatManager != null)
            {
                Debug.Log("[TRANSITION DEBUG] Found CombatManager, calling SetupCombatScene");
                // Set up the combat scene with player inventory
                SetupCombatScene(combatManager);
            }
            else
            {
                Debug.LogError($"[TRANSITION DEBUG] CombatManager not found in combat scene: {scene.name}!");
            }
            
            // Fade from black once the scene is set up
            Debug.Log("[TRANSITION DEBUG] Starting fade from black");
            StartCoroutine(ScreenFader.Instance.FadeFromBlack());
        }
        
        // Unregister the event to prevent multiple calls
        Debug.Log("[TRANSITION DEBUG] Unregistering OnCombatSceneLoaded event");
        SceneManager.sceneLoaded -= OnCombatSceneLoaded;
    }
    
    /// <summary>
    /// Setup the combat scene with the player's inventory
    /// </summary>
    private void SetupCombatScene(CombatManager combatManager)
    {
        Debug.Log("[TRANSITION DEBUG] SetupCombatScene started");
        
        // Filter items first to remove key items
        List<ItemData> filteredItems = new List<ItemData>();
        Debug.Log("[TRANSITION DEBUG] Starting to filter items (removing key items)");
        
        foreach (var item in storedItems)
        {
            // Skip key items completely for combat
            if (item.IsKeyItem())
            {
                Debug.Log($"[TRANSITION DEBUG] Filtering out key item: {item.name} - Key items not available in combat");
                continue;
            }
            
            // Only add non-key items to the filtered list
            filteredItems.Add(item.Clone());
            Debug.Log($"[TRANSITION DEBUG] Keeping item for combat: {item.name}, Amount: {item.amount}, Type: {item.type}");
        }
        Debug.Log($"[TRANSITION DEBUG] Filtered inventory contains {filteredItems.Count} items (key items removed)");
        
        // Pass player inventory to combat manager - ONLY filtered items
        if (filteredItems.Count > 0)
        {
            Debug.Log("[TRANSITION DEBUG] Setting up combat manager with filtered inventory items");
            
            // Pass FILTERED inventory to combat manager
            combatManager.SetupPlayerInventory(filteredItems);
            Debug.Log("[TRANSITION DEBUG] SetupPlayerInventory called on combat manager");
            
            // Also set up individual player character inventories with FILTERED items only
            Debug.Log("[TRANSITION DEBUG] Setting up individual player character inventories");
            foreach (var player in combatManager.players)
            {
                if (player != null)
                {
                    Debug.Log($"[TRANSITION DEBUG] Setting up inventory for player: {player.characterName}");
                    // Clear existing inventory
                    player.items.Clear();
                    
                    // Copy FILTERED items to player's inventory
                    foreach (var item in filteredItems)
                    {
                        player.items.Add(item);
                        Debug.Log($"[TRANSITION DEBUG] Added {item.name} (x{item.amount}) to player character: {player.characterName}");
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("[TRANSITION DEBUG] No filtered items for combat - all items were KeyItems or inventory was empty");
            
            // Ensure the combat manager has an empty list, not null
            combatManager.SetupPlayerInventory(new List<ItemData>());
            Debug.Log("[TRANSITION DEBUG] Set up combat manager with empty inventory list");
            
            // Also clear all player character inventories
            foreach (var player in combatManager.players)
            {
                if (player != null)
                {
                    player.items.Clear();
                    Debug.Log($"[TRANSITION DEBUG] Cleared inventory for player character: {player.characterName}");
                }
            }
        }
        
        // Listen for combat end event - only if not already subscribed
        if (!isSubscribedToCombatEnd)
        {
            combatManager.OnCombatEnd += EndCombat;
            isSubscribedToCombatEnd = true;
            Debug.Log("[TRANSITION DEBUG] Subscribed to combatManager.OnCombatEnd event");
        }
        else
        {
            Debug.Log("[TRANSITION DEBUG] Already subscribed to OnCombatEnd event, skipping subscription in SetupCombatScene");
        }
    }
    
    /// <summary>
    /// End combat and return to the overworld
    /// </summary>
    /// <param name="won">Whether the player won the combat</param>
    public void EndCombat(bool won)
    {
        Debug.Log($"[TRANSITION DEBUG] EndCombat called with result: {(won ? "WIN" : "LOSE")}");
        
        // IMPORTANT: Ensure combat-to-overworld transitions can always happen, regardless of other transitions
        // Overworld-to-overworld and overworld-to-battle transitions are still protected with isFadingInProgress check
        // We intentionally skip the isFadingInProgress check here to make sure combat always returns to overworld
        
        // CRITICAL FIX: For Obelisk battle, check if phase transition is in progress
        // If we're in the Obelisk battle scene and a phase transition is happening, don't transition to overworld
        if (SceneManager.GetActiveScene().name == "Battle_Obelisk")
        {
            BattleDialogueTrigger dialogueTrigger = FindObjectOfType<BattleDialogueTrigger>();
            if (dialogueTrigger != null && dialogueTrigger.IsTransitioningPhases)
            {
                Debug.Log("[TRANSITION DEBUG] ⚠️ Obelisk phase transition detected - skipping overworld transition");
                return; // Skip transition to overworld since phase transition is happening
            }
        }
        
        // Set fading flag to prevent duplicate transitions
        isFadingInProgress = true;
        
        // Check if we have a valid return scene, and if not, try to recover from PlayerPrefs
        if (string.IsNullOrEmpty(currentSceneName) && PlayerPrefs.HasKey("ReturnSceneName"))
        {
            currentSceneName = PlayerPrefs.GetString("ReturnSceneName");
            Debug.Log($"[TRANSITION DEBUG] Recovered return scene from PlayerPrefs: {currentSceneName}");
        }
        
        // Store combat result
        combatWon = won;
        Debug.Log($"[TRANSITION DEBUG] Stored combat result: {combatWon}");
        
        // If the player won, mark the enemy as defeated in the persistent manager
        if (won && !string.IsNullOrEmpty(enemyIdThatInitiatedCombat))
        {
            Debug.Log($"[TRANSITION DEBUG] Player won combat, marking enemy {enemyIdThatInitiatedCombat} as defeated");
            
            // Make sure the persistent game manager exists
            PersistentGameManager.EnsureExists();
            Debug.Log("[TRANSITION DEBUG] Ensured PersistentGameManager exists");
            
            // Mark this enemy as defeated in the persistent game manager
            PersistentGameManager.Instance.MarkEnemyDefeated(enemyIdThatInitiatedCombat);
            Debug.Log($"[TRANSITION DEBUG] Marked enemy {enemyIdThatInitiatedCombat} as defeated in PersistentGameManager");
            
            // Log all defeated enemies for debugging
            PersistentGameManager.Instance.LogDefeatedEnemies();
        }
        
        // Reset the subscription flag - we're no longer subscribed after combat ends and scene changes
        isSubscribedToCombatEnd = false;
        Debug.Log("[TRANSITION DEBUG] Reset isSubscribedToCombatEnd flag");
        
        // CRITICAL FIX: Cleanup any combat-related objects that might have DontDestroyOnLoad
        Debug.Log("[TRANSITION DEBUG] Starting cleanup of combat objects");
        CleanupCombatObjects();
        
        // Make sure ScreenFader exists
        ScreenFader.EnsureExists();
        Debug.Log("[TRANSITION DEBUG] Ensured ScreenFader exists");
        
        // CRITICAL: Double check we have a valid scene to return to
        if (string.IsNullOrEmpty(currentSceneName))
        {
            Debug.LogError("[TRANSITION DEBUG] No return scene set! Falling back to default overworld scene");
            currentSceneName = overworldSceneName;
            Debug.Log($"[TRANSITION DEBUG] Set currentSceneName to default: {overworldSceneName}");
        }
        
        // Start the transition with fade effect
        Debug.Log($"[TRANSITION DEBUG] Starting TransitionToOverworld coroutine to scene: {currentSceneName}");
        StartCoroutine(TransitionToOverworld());
    }
    
    /// <summary>
    /// Cleanup any combat-related objects that might have DontDestroyOnLoad
    /// </summary>
    private void CleanupCombatObjects()
    {
        Debug.Log("[TRANSITION DEBUG] CleanupCombatObjects started");
        
        // CRITICAL FIX: SPECIFICALLY TARGET THE TEXT PANEL THAT SHOWS ENEMY ACTIONS
        GameObject textPanel = GameObject.Find("TextPanel");
        if (textPanel != null)
        {
            Debug.Log("[TRANSITION DEBUG] Found and destroying TextPanel that shows enemy actions");
            Destroy(textPanel);
            
            // Also try to find it by transform hierarchy
            var canvases = FindObjectsOfType<Canvas>();
            Debug.Log($"[TRANSITION DEBUG] Searching through {canvases.Length} canvases for TextPanel");
            foreach (var canvas in canvases)
            {
                // Try to find it as a direct child of the main canvas
                Transform foundPanel = canvas.transform.Find("TextPanel");
                if (foundPanel != null)
                {
                    Debug.Log($"[TRANSITION DEBUG] Found TextPanel through canvas.transform.Find on canvas: {canvas.name}");
                    Destroy(foundPanel.gameObject);
                }
                
                // Try looking through all children of the canvas (in case it's nested)
                int childrenChecked = 0;
                foreach (Transform child in canvas.transform)
                {
                    childrenChecked++;
                    if (child.name == "TextPanel")
                    {
                        Debug.Log($"[TRANSITION DEBUG] Found TextPanel as child of canvas {canvas.name} (child #{childrenChecked})");
                        Destroy(child.gameObject);
                    }
                }
                Debug.Log($"[TRANSITION DEBUG] Checked {childrenChecked} children of canvas {canvas.name}");
            }
        }
        
        // Find all root GameObjects in the scene
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        Debug.Log($"[TRANSITION DEBUG] Checking {rootObjects.Length} root objects for combat tags");
        
        // Combat-related tag names to look for
        string[] combatTags = new string[] { "CombatUI", "BattleUI", "EnemyUI", "PlayerUI" };
        int objectsDestroyed = 0;
        
        // Remove any objects with combat-related tags
        foreach (GameObject root in rootObjects)
        {
            foreach (string tag in combatTags)
            {
                if (root.CompareTag(tag))
                {
                    Debug.Log($"[TRANSITION DEBUG] Destroying combat object with tag '{tag}': {root.name}");
                    Destroy(root);
                    objectsDestroyed++;
                    break;
                }
            }
        }
        Debug.Log($"[TRANSITION DEBUG] Destroyed {objectsDestroyed} objects with combat tags");
        
        // Find and destroy core combat gameplay objects
        // These might have DontDestroyOnLoad set on them
        string[] combatObjectNames = new string[] {
            "CombatManager", "BattleManager", "CombatUI", "BattleUI", 
            "BattleDialogueTrigger", "CombatCanvas", "ActionMenu",
            "SkillMenu", "ItemMenu", "CharacterStatsPanel", "TurnText",
            "StatsPanelContainer", "EnemyContainer", "PlayerContainer", 
            "ActionLabel", "ActionMenuCanvas", "CombatUIRoot"
        };
        
        int namedObjectsDestroyed = 0;
        foreach (string name in combatObjectNames)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null)
            {
                Debug.Log($"[TRANSITION DEBUG] Destroying combat object by name: {name}");
                Destroy(obj);
                namedObjectsDestroyed++;
            }
        }
        Debug.Log($"[TRANSITION DEBUG] Destroyed {namedObjectsDestroyed} combat objects by name");
        
        // CRITICAL FIX: Find ALL canvases with combat-related names
        // This is a more aggressive approach that will catch renamed objects
        Canvas[] allCanvases = FindObjectsOfType<Canvas>(true); // Include inactive objects
        Debug.Log($"[TRANSITION DEBUG] Checking {allCanvases.Length} canvases for combat-related names");
        int canvasesDestroyed = 0;
        
        foreach (Canvas canvas in allCanvases)
        {
            string canvasName = canvas.gameObject.name.ToLower();
            if (canvasName.Contains("combat") || canvasName.Contains("battle") || 
                canvasName.Contains("enemy") || canvasName.Contains("action") || 
                canvasName.Contains("menu") || canvasName.Contains("skill") || 
                canvasName.Contains("item") || canvasName.Contains("stats"))
            {
                Debug.Log($"[TRANSITION DEBUG] Destroying combat canvas with matching name: {canvas.gameObject.name}");
                Destroy(canvas.gameObject);
                canvasesDestroyed++;
            }
        }
        Debug.Log($"[TRANSITION DEBUG] Destroyed {canvasesDestroyed} combat-related canvases");
        Debug.Log("[TRANSITION DEBUG] CleanupCombatObjects completed");
    }
    
    /// <summary>
    /// Handle the transition to overworld scene with fade effect
    /// </summary>
    private IEnumerator TransitionToOverworld()
    {
        Debug.Log($"[TRANSITION DEBUG] TransitionToOverworld coroutine started for scene: {currentSceneName}");
        
        // CRITICAL FIX: Check if currentSceneName is empty or null, and if so, try to get it from PlayerPrefs
        if (string.IsNullOrEmpty(currentSceneName))
        {
            Debug.Log("[TRANSITION DEBUG] currentSceneName is null or empty, trying to recover from PlayerPrefs");
            // Try to recover the scene name from PlayerPrefs
            if (PlayerPrefs.HasKey("ReturnSceneName"))
            {
                currentSceneName = PlayerPrefs.GetString("ReturnSceneName");
                Debug.Log($"[TRANSITION DEBUG] Recovered scene name from PlayerPrefs: {currentSceneName}");
            }
            else
            {
                Debug.LogError("[TRANSITION DEBUG] Could not recover return scene name from PlayerPrefs, falling back to default scene");
                currentSceneName = overworldSceneName; // Fallback to the default overworld scene
                Debug.Log($"[TRANSITION DEBUG] Set currentSceneName to default: {overworldSceneName}");
            }
        }
        
        // Validate the scene name before attempting transition
        if (!IsSceneValid(currentSceneName))
        {
            Debug.LogError($"[TRANSITION DEBUG] Scene '{currentSceneName}' does not exist in build settings. Falling back to entrance scene.");
            // Fall back to the entrance scene if the current scene is invalid
            currentSceneName = overworldSceneName;
            Debug.Log($"[TRANSITION DEBUG] Set currentSceneName to default: {overworldSceneName}");
            
            if (!IsSceneValid(overworldSceneName)) {
                Debug.LogError($"[TRANSITION DEBUG] Fallback scene '{overworldSceneName}' also does not exist. Make sure to add it in File > Build Settings.");
                // Fade back from black since we're not transitioning
                StartCoroutine(ScreenFader.Instance.FadeFromBlack());
                // Reset the fading flag since we're not going to complete the transition
                isFadingInProgress = false;
                yield break;
            }
        }
        
        // IMPORTANT: When coming from combat, the screen is already black, so we don't need another fade to black
        // Skip the fade to black and proceed directly to loading the scene
        Debug.Log("[TRANSITION DEBUG] Screen is already black from combat, skipping fade to black");
        
        // IMPORTANT: Register for scene loaded event BEFORE loading scene
        Debug.Log("[TRANSITION DEBUG] Registering OnOverworldSceneLoaded event");
        SceneManager.sceneLoaded += OnOverworldSceneLoaded;
        
        // Load the scene we came from
        Debug.Log($"[TRANSITION DEBUG] Now loading scene: {currentSceneName}");
        SceneManager.LoadScene(currentSceneName);
    }
    
    /// <summary>
    /// Called when the overworld scene has loaded
    /// </summary>
    private void OnOverworldSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[TRANSITION DEBUG] OnOverworldSceneLoaded called for scene: {scene.name}");
        
        // Only execute this if we loaded the correct scene
        if (scene.name == currentSceneName || scene.name == overworldSceneName)
        {
            Debug.Log("[TRANSITION DEBUG] Confirmed this is the correct return scene, starting SetupOverworldAfterCombat");
            StartCoroutine(SetupOverworldAfterCombat());
        }
        else
        {
            Debug.LogWarning($"[TRANSITION DEBUG] Unexpected scene loaded: {scene.name}, expected: {currentSceneName} or {overworldSceneName}");
        }
        
        // Unregister the event to prevent multiple calls
        Debug.Log("[TRANSITION DEBUG] Unregistering OnOverworldSceneLoaded event");
        SceneManager.sceneLoaded -= OnOverworldSceneLoaded;
    }
    
    /// <summary>
    /// Setup the overworld after returning from combat
    /// </summary>
    private IEnumerator SetupOverworldAfterCombat()
    {
        Debug.Log("[TRANSITION DEBUG] SetupOverworldAfterCombat coroutine started");
        
        // DO NOT reset the fading flag yet - we're still in the transition process
        // We'll reset it after the fade from black completes
        
        // Wait a frame to ensure all scene objects are initialized
        yield return null;
        Debug.Log("[TRANSITION DEBUG] Waited one frame for scene initialization");
        
        Debug.Log("[TRANSITION DEBUG] Running aggressive UI cleanup in overworld");
        
        // CRITICAL FIX: SPECIFICALLY TARGET THE TEXT PANEL THAT SHOWS ENEMY ACTIONS
        // This is the object that wasn't being properly cleaned up in builds
        GameObject textPanel = GameObject.Find("TextPanel");
        if (textPanel != null)
        {
            Debug.Log("[TRANSITION DEBUG] Found and destroying TextPanel in overworld");
            Destroy(textPanel);
        }
        
        // Also check if TextPanel is still a child of some parent canvas
        var allCanvases = FindObjectsOfType<Canvas>();
        Debug.Log($"[TRANSITION DEBUG] Checking {allCanvases.Length} canvases for TextPanel");
        foreach (var canvas in allCanvases)
        {
            // Look for direct children named TextPanel
            Transform textPanelTransform = canvas.transform.Find("TextPanel");
            if (textPanelTransform != null)
            {
                Debug.Log($"[TRANSITION DEBUG] Found TextPanel as direct child of {canvas.name}");
                Destroy(textPanelTransform.gameObject);
            }
            
            // Look for TMPro components that might be part of the text panel
            TextMeshProUGUI[] textComponents = canvas.GetComponentsInChildren<TextMeshProUGUI>(true);
            Debug.Log($"[TRANSITION DEBUG] Checking {textComponents.Length} TMPro components in canvas {canvas.name}");
            foreach (var text in textComponents)
            {
                if (text.transform.parent != null && text.transform.parent.name == "TextPanel")
                {
                    Debug.Log("[TRANSITION DEBUG] Found TextPanel through its TMPro child");
                    Destroy(text.transform.parent.gameObject);
                }
            }
        }
        
        // Check for common combat UI objects by name
        // Extended the list to catch more possible UI names
        string[] combatUINames = new string[] {
            "BattleCanvas", "CombatCanvas", "SkillMenu", "ItemMenu", "ActionMenu",
            "CharacterStatsPanel", "EnemyContainer", "PlayerContainer", "TurnText",
            "StatsPanelContainer", "ActionLabel", "ActionMenuCanvas", "CombatUIRoot",
            "CombatManager", "BattleManager", "BattleDialogueTrigger", "MenuSelector",
            "EnemyUI", "PlayerUI", "SkillButton", "ItemButton", "ActionContainer"
        };
        
        // Try to destroy by name
        Debug.Log("[TRANSITION DEBUG] Checking for combat UI objects by name");
        int uiObjectsDestroyed = 0;
        foreach (string uiName in combatUINames)
        {
            GameObject obj = GameObject.Find(uiName);
            if (obj != null)
            {
                Debug.Log($"[TRANSITION DEBUG] Found and destroying combat UI element: {uiName}");
                Destroy(obj);
                uiObjectsDestroyed++;
            }
        }
        Debug.Log($"[TRANSITION DEBUG] Destroyed {uiObjectsDestroyed} combat UI objects by name");
        
        // CRITICAL FIX: Look for objects with combat tags as a backup
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        Debug.Log($"[TRANSITION DEBUG] Checking {allObjects.Length} objects for combat tags");
        string[] combatTags = new string[] { "CombatUI", "BattleUI", "EnemyUI", "PlayerUI" };
        int taggedObjectsDestroyed = 0;
        foreach (GameObject obj in allObjects)
        {
            foreach (string tag in combatTags)
            {
                if (obj.CompareTag(tag))
                {
                    Debug.Log($"[TRANSITION DEBUG] Found and destroying object with tag '{tag}': {obj.name}");
                    Destroy(obj);
                    taggedObjectsDestroyed++;
                }
            }
        }
        Debug.Log($"[TRANSITION DEBUG] Destroyed {taggedObjectsDestroyed} objects with combat tags");
        
        // Find and destroy the combat manager
        CombatManager combatManager = FindObjectOfType<CombatManager>();
        if (combatManager != null)
        {
            Debug.Log("[TRANSITION DEBUG] Found and destroying lingering CombatManager");
            Destroy(combatManager.gameObject);
        }
        
        // Find and destroy the battle dialogue trigger
        BattleDialogueTrigger battleDialogue = FindObjectOfType<BattleDialogueTrigger>();
        if (battleDialogue != null)
        {
            Debug.Log("[TRANSITION DEBUG] Found and destroying lingering BattleDialogueTrigger");
            Destroy(battleDialogue.gameObject);
        }
        
        // Find the player in the scene
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Debug.Log("[TRANSITION DEBUG] Found player in overworld, setting up after combat");
            
            // Wait another frame to ensure all components are initialized
            yield return null;
            Debug.Log("[TRANSITION DEBUG] Waited additional frame for player components to initialize");
            
            // Calculate a safe position that's slightly offset from where combat was initiated
            // This prevents immediate re-collision with enemies
            Vector3 safePosition = playerPosition;
            Debug.Log($"[TRANSITION DEBUG] Original player position: {playerPosition}");
            
            // If the player won combat, apply a small offset to avoid immediate collision
            if (combatWon)
            {
                // Offset the player by 0.5 units in the -Y direction (backing away from the enemy)
                safePosition += new Vector3(0, -0.5f, 0);
                Debug.Log($"[TRANSITION DEBUG] Applied safety offset to position, new position: {safePosition}");
            }
            
            // Restore player position with the safe offset
            Debug.Log($"[TRANSITION DEBUG] Positioning player at {safePosition} after returning from combat");
            player.transform.position = safePosition;
            
            // Restore player inventory
            PlayerInventory newInventory = player.GetComponent<PlayerInventory>();
            if (newInventory != null && playerInventory != null)
            {
                Debug.Log("[TRANSITION DEBUG] === Restoring player inventory after combat ===");
                Debug.Log("[TRANSITION DEBUG] Stored inventory from combat:");
                foreach (var item in storedItems)
                {
                    Debug.Log($"[TRANSITION DEBUG] Stored item: {item.name}, Amount: {item.amount}, Type: {item.type}");
                }
                
                // Special Check: Look for Cold Key in PersistentGameManager
                var persistentInventory = PersistentGameManager.Instance?.GetPlayerInventory();
                bool hasColdKeyInPersistent = persistentInventory != null && persistentInventory.ContainsKey("Cold Key");
                
                if (hasColdKeyInPersistent)
                {
                    Debug.Log("[TRANSITION DEBUG] Cold Key found in PersistentGameManager - ensuring it's preserved");
                    // Make sure Cold Key exists in stored items
                    bool coldKeyInStored = false;
                    foreach (var item in storedItems)
                    {
                        if (item.name == "Cold Key")
                        {
                            coldKeyInStored = true;
                            // Make sure it's marked as a KeyItem
                            item.type = ItemData.ItemType.KeyItem;
                            Debug.Log("[TRANSITION DEBUG] Updated Cold Key type to KeyItem in stored items");
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
                        Debug.Log("[TRANSITION DEBUG] Added missing Cold Key to stored items list");
                    }
                }
                
                // Clear the old inventory first to prevent duplicating items
                newInventory.ClearInventory();
                Debug.Log("[TRANSITION DEBUG] Cleared current player inventory");
                
                // Copy items from saved inventory to new inventory
                Debug.Log("[TRANSITION DEBUG] Adding stored items to player inventory");
                foreach (ItemData item in storedItems)
                {
                    newInventory.AddItem(item);
                    Debug.Log($"[TRANSITION DEBUG] Restored {item.name} (x{item.amount}, Type: {item.type}) to overworld player inventory");
                }
                
                // Double-check for Cold Key
                bool hasColdKeyInFinal = false;
                Debug.Log("[TRANSITION DEBUG] Verifying final overworld inventory:");
                foreach (var item in newInventory.Items)
                {
                    Debug.Log($"[TRANSITION DEBUG] Final overworld item: {item.name}, Amount: {item.amount}, Type: {item.type}");
                    if (item.name == "Cold Key")
                    {
                        hasColdKeyInFinal = true;
                        Debug.Log("[TRANSITION DEBUG] Confirmed Cold Key is in final inventory");
                    }
                }
                
                // Final verification for Cold Key
                if (hasColdKeyInPersistent && !hasColdKeyInFinal)
                {
                    Debug.LogWarning("[TRANSITION DEBUG] CRITICAL ERROR: Cold Key was in PersistentGameManager but failed to transfer to player inventory - forcing addition");
                    ItemData coldKey = new ItemData(
                        "Cold Key", 
                        "A frigid key that seems to emanate cold. You won it from a mysterious figure in a game of Ravenbond.", 
                        1, 
                        false, 
                        ItemData.ItemType.KeyItem
                    );
                    newInventory.AddItem(coldKey);
                    Debug.Log("[TRANSITION DEBUG] Forcibly added Cold Key to player inventory as final failsafe");
                }
            }
            
            // Only do ONE fade from black operation, and ensure we reset the flag afterward
            Debug.Log("[TRANSITION DEBUG] Starting fade from black (only fade in the transition process)");
            
            // CRITICAL FIX: Make sure the screen is black before fading from black
            ScreenFader.Instance.SetBlackScreen();
            
            // Use a longer duration (1.5 seconds) for the fade to make it more noticeable
            yield return StartCoroutine(ScreenFader.Instance.FadeFromBlack(1f));
            
            // Reset the fading flag now that we've completed the transition
            isFadingInProgress = false;
            Debug.Log("[TRANSITION DEBUG] Fade completed and flag reset");
        }
        else
        {
            Debug.LogError("[TRANSITION DEBUG] Player not found in overworld scene!");
            
            // Fade from black even if player wasn't found to prevent screen staying black
            Debug.Log("[TRANSITION DEBUG] Starting fade from black (only fade, even though player wasn't found)");
            
            // CRITICAL FIX: Make sure the screen is black before fading from black
            ScreenFader.Instance.SetBlackScreen();
            
            // Use a longer duration (1.5 seconds) for the fade to make it more noticeable
            yield return StartCoroutine(ScreenFader.Instance.FadeFromBlack(1f));
            
            // Reset the fading flag now that we've completed the transition
            isFadingInProgress = false;
            Debug.Log("[TRANSITION DEBUG] Fade completed and flag reset");
        }
        
        Debug.Log("[TRANSITION DEBUG] SetupOverworldAfterCombat completed");
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
        
        // Get the current scene name once
        string currentSceneName = SceneManager.GetActiveScene().name;
        
        // Check if we're already in the middle of a transition (either fade or scene change)
        // This prevents overworld-to-overworld transitions from stacking on top of other transitions
        // Note that combat-to-overworld transitions can bypass this check (see EndCombat method)
        if (isFadingInProgress)
        {
            Debug.Log($"[SCENE TRANSITION] Ignoring transition request from {currentSceneName} to {sceneName} because another transition is already in progress: {currentTransitionDescription}");
            
            // DIRECT FIX: Check how long the transition has been in progress
            // If it's been more than 3 seconds, force reset the flag regardless
            if (Time.time - lastTransitionStartTime > 3f)
            {
                Debug.LogWarning($"[SCENE TRANSITION] Detected stuck transition: {currentTransitionDescription} started {Time.time - lastTransitionStartTime}s ago. Resetting flag and allowing transition to {sceneName}");
                isFadingInProgress = false;
                currentTransitionDescription = "";
            }
            else
            {
                return; // Exit if transition still valid
            }
        }
        
        // Set fading flag to prevent multiple transitions
        isFadingInProgress = true;
        lastTransitionStartTime = Time.time; // Track when this transition started
        
        // Store transition details
        targetSceneName = sceneName;
        targetMarkerId = markerId;
        
        // Store current transition description for debugging
        currentTransitionDescription = $"Transition from {currentSceneName} to {sceneName} (marker: {markerId})";
        
        // Store player inventory for restoration after transition
        playerInventory = player.GetComponent<PlayerInventory>();
        
        // Log the transition in detail for debugging
        Debug.Log($"[SCENE TRANSITION] Starting {currentTransitionDescription}");
        
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
        
        // IMPORTANT: Force reset static flag on all scene loads to prevent stuck state
        SceneManager.sceneLoaded += ForceResetFlagOnSceneLoad;
        
        // Store scene and marker targets in PlayerPrefs to survive scene unloading
        PlayerPrefs.SetString("LastTargetSceneName", targetSceneName);
        PlayerPrefs.SetString("LastTargetMarkerId", targetMarkerId);
        PlayerPrefs.SetInt("NeedsPlayerSetup", 1);
        PlayerPrefs.Save();
        
        Debug.Log($"[SCENE TRANSITION] STORED in PlayerPrefs - Scene: {targetSceneName}, Marker: {targetMarkerId}");
        
        // Validate the scene name before attempting transition
        if (!IsSceneValid(targetSceneName))
        {
            Debug.LogError($"[SCENE TRANSITION] CRITICAL ERROR: Scene '{targetSceneName}' does not exist in build settings. Make sure to add it in File > Build Settings.");
            // Fade back from black since we're not transitioning
            StartCoroutine(ScreenFader.Instance.FadeFromBlack());
            // Reset the fading flag
            CleanupTransitionState();
            yield break;
        }
        
        // Fade to black
        yield return StartCoroutine(ScreenFader.Instance.FadeToBlack());
        
        // REBUILD FIX: No longer rely on scene loaded event to survive scene loading
        // Instead we'll make the main Awake/Start handle this
        Debug.Log($"[SCENE TRANSITION] Now loading scene: {targetSceneName} - will perform setup through Start()");
        
        // IMPORTANT: Capture the current flags before scene load for debugging
        bool flagBeforeLoad = isFadingInProgress;
        string transitionBeforeLoad = currentTransitionDescription;
        
        // Load the scene
        SceneManager.LoadScene(targetSceneName);
        
        Debug.Log($"[SCENE TRANSITION] LoadScene called. Flags before load: isFadingInProgress={flagBeforeLoad}, transition={transitionBeforeLoad}");
        
        // Set a backup timeout to reset the flag if the scene transition fails
        StartCoroutine(TransitionBackupReset());
    }
    
    /// <summary>
    /// Force reset the transition flag on any scene load as a final failsafe
    /// </summary>
    private void ForceResetFlagOnSceneLoad(Scene scene, LoadSceneMode mode)
    {
        // Skip reset in the startroom as it needs to maintain a black screen initially
        bool isStartRoom = scene.name.Contains("Startroom") || scene.name.Contains("start_room");
        
        if (isStartRoom)
        {
            Debug.Log($"[SCENE TRANSITION] Skipping transition flag reset in startroom to preserve black screen");
            // Unregister this event to avoid multiple calls
            SceneManager.sceneLoaded -= ForceResetFlagOnSceneLoad;
            return;
        }
        
        // Reset transition flags after a short delay to ensure all systems initialize
        StartCoroutine(DelayedFlagReset(scene.name));
        
        // Unregister this event to avoid multiple calls
        SceneManager.sceneLoaded -= ForceResetFlagOnSceneLoad;
    }
    
    /// <summary>
    /// Helper coroutine to reset flag with a delay
    /// </summary>
    private IEnumerator DelayedFlagReset(string sceneName)
    {
        yield return new WaitForSecondsRealtime(1f);
        
        Debug.Log($"[SCENE TRANSITION] Force-resetting transition flag from ForceResetFlagOnSceneLoad in scene {sceneName}");
        isFadingInProgress = false;
        currentTransitionDescription = "";
    }
    
    /// <summary>
    /// Validates if a scene exists in build settings
    /// </summary>
    private bool IsSceneValid(string sceneName)
    {
        Debug.Log($"[SCENE TRANSITION DEBUG] Checking if scene '{sceneName}' is valid in build settings");
        
        // Check if the scene exists in build settings
        int sceneCount = SceneManager.sceneCountInBuildSettings;
        Debug.Log($"[SCENE TRANSITION DEBUG] Total scenes in build settings: {sceneCount}");
        
        for (int i = 0; i < sceneCount; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            
            // Log each scene to help with debugging
            Debug.Log($"[SCENE TRANSITION DEBUG] Build index {i}: '{sceneNameFromPath}' (Path: {scenePath})");
            
            if (string.Equals(sceneNameFromPath, sceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"[SCENE TRANSITION DEBUG] Found matching scene '{sceneName}' at build index {i}");
                return true;
            }
        }
        
        Debug.LogError($"[SCENE TRANSITION DEBUG] Scene '{sceneName}' was NOT found in any build settings entries!");
        return false;
    }
    
    /// <summary>
    /// Called when a scene transition has completed
    /// This is now a direct call method rather than an event handler
    /// </summary>
    private void OnSceneTransitionComplete(Scene scene, LoadSceneMode mode)
    {
        Debug.LogError($"[BUILD FIX] OnSceneTransitionComplete called directly for scene: {scene.name}. Current transition: {currentTransitionDescription}");
        
        // In build mode, we need to wait a bit longer before setting player position
        if (!Application.isEditor)
        {
            // Use a delayed setup for build mode to ensure everything is loaded
            StartCoroutine(DelayedSetupAfterTransition(0.2f));
        }
        else
        {
            // In editor, we can start the setup immediately
            StartCoroutine(SetupPlayerAfterTransition());
        }
        
        // IMPORTANT: Already reset the flag here so it's reset earlier in the process
        isFadingInProgress = false;
        Debug.LogError($"[BUILD FIX] Reset transition flag in OnSceneTransitionComplete for {scene.name}");
        
        // Safety check: Set a shorter timeout to reset the transition flag if something goes wrong
        StartCoroutine(EnsureTransitionFlagReset());
    }
    
    /// <summary>
    /// Delayed setup specifically for build mode to ensure everything is loaded
    /// </summary>
    private IEnumerator DelayedSetupAfterTransition(float delay)
    {
        Debug.LogError($"[SCENE TRANSITION BUILD FIX] Delaying player setup for {delay} seconds to ensure scene is fully loaded");
        
        // Wait for the specified delay
        yield return new WaitForSeconds(delay);
        
        // Now run the normal setup
        StartCoroutine(SetupPlayerAfterTransition());
    }
    
    /// <summary>
    /// Safety coroutine to ensure the transition flag gets reset even if there's an error
    /// </summary>
    private IEnumerator EnsureTransitionFlagReset()
    {
        // Reduced timeout from 5 to 3 seconds
        yield return new WaitForSecondsRealtime(3f);
        
        // If flag is still set, it means something went wrong
        if (isFadingInProgress)
        {
            Debug.LogWarning("[SCENE TRANSITION] Safety timeout triggered - Forcing reset of transition flag");
            CleanupTransitionState();
        }
    }
    
    /// <summary>
    /// Setup the player after scene transition
    /// </summary>
    private IEnumerator SetupPlayerAfterTransition()
    {
        Debug.LogError($"[BUILD FIX] SetupPlayerAfterTransition started with targetMarkerId={targetMarkerId}");
        
        // Wait for two frames to make sure all objects are fully initialized in the scene
        yield return null;
        yield return null;
        
        // Add an additional wait specifically for builds to ensure everything is fully loaded
        if (!Application.isEditor)
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            // Add a small delay in build mode to ensure all components are fully initialized
            yield return new WaitForSeconds(0.1f);
        }
        
        Debug.LogError($"[BUILD FIX] SetupPlayerAfterTransition running, isFadingInProgress={isFadingInProgress}, targetMarkerId={targetMarkerId}");
        
        // Find the player in the scene
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        
        if (player != null)
        {
            Debug.LogError($"[BUILD FIX] Found player at position {player.transform.position}");
            
            // Try to find the target PlayerMarker
            bool markerFound = false;
            PlayerMarker[] markers = FindObjectsOfType<PlayerMarker>();
            
            // Log all markers to debug output to help diagnose issues
            Debug.LogError($"[BUILD FIX] Looking for marker with ID '{targetMarkerId}' in scene '{SceneManager.GetActiveScene().name}'");
            Debug.LogError($"[BUILD FIX] Found {markers.Length} PlayerMarker objects in the scene:");
            foreach (PlayerMarker marker in markers)
            {
                Debug.LogError($"[BUILD FIX] Available marker: ID='{marker.MarkerId}', Position={marker.transform.position}, GameObject={marker.gameObject.name}");
            }
            
            // IMPORTANT BUILD FIX: Cache the player's original position for comparison later
            Vector3 originalPlayerPos = player.transform.position;
            
            // In build mode, preemptively disable components that might fight position changes
            if (!Application.isEditor)
            {
                DisablePlayerMovementComponents(player);
                Debug.LogError($"[BUILD FIX] Disabled movement components on player");
            }
            
            // Search for the matching marker
            foreach (PlayerMarker marker in markers)
            {
                // Case insensitive comparison to avoid common errors
                if (string.Equals(marker.MarkerId, targetMarkerId, System.StringComparison.OrdinalIgnoreCase))
                {
                    // Position the player at the marker - use GetMarkerPosition for more reliable positioning in builds
                    Vector3 markerPosition = marker.GetMarkerPosition();
                    Debug.LogError($"[BUILD FIX] Found marker at position {markerPosition}, now positioning player");
                    
                    // For build mode, use transform.SetPositionAndRotation which is more reliable
                    if (!Application.isEditor)
                    {
                        // Force position with multiple methods for redundancy
                        player.transform.SetPositionAndRotation(markerPosition, player.transform.rotation);
                        
                        // Force update any physics components
                        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
                        if (rb != null)
                        {
                            rb.position = markerPosition;
                            rb.velocity = Vector2.zero;
                        }
                        
                        Debug.LogError($"[BUILD FIX] Set player position using SetPositionAndRotation, now at {player.transform.position}");
                    }
                    else
                    {
                        // Original positioning for editor mode which works fine
                        player.transform.position = markerPosition;
                        Debug.LogError($"[BUILD FIX] Set player position in editor mode, now at {player.transform.position}");
                    }
                    
                    Debug.LogError($"[BUILD FIX] Successfully positioned player at marker with ID '{targetMarkerId}' at position {markerPosition}");
                    markerFound = true;
                    break;
                }
            }
            
            // CRITICAL BUILD FIX: Specifically for builds, add additional checks to ensure the player is actually moved
            if (markerFound && !Application.isEditor)
            {
                // Compare if player position actually changed
                float positionDifference = Vector3.Distance(originalPlayerPos, player.transform.position);
                Debug.LogError($"[BUILD FIX] Position difference after first attempt: {positionDifference}");
                
                if (positionDifference < 0.01f)
                {
                    Debug.LogError($"[BUILD FIX] Player position didn't change! Original: {originalPlayerPos}, Current: {player.transform.position}");
                    
                    // Try to find the marker again
                    foreach (PlayerMarker marker in markers)
                    {
                        if (string.Equals(marker.MarkerId, targetMarkerId, System.StringComparison.OrdinalIgnoreCase))
                        {
                            Debug.LogError($"[BUILD FIX] Second positioning attempt for marker {marker.MarkerId}");
                            
                            // Get position directly from marker's method for more reliability
                            Vector3 markerPosition = marker.GetMarkerPosition();
                            
                            // More aggressive position setting
                            player.transform.SetPositionAndRotation(markerPosition, player.transform.rotation);
                            Debug.LogError($"[BUILD FIX] Second attempt position: {player.transform.position}");
                            
                            // Wait a frame for physics to update
                            yield return null;
                            
                            // Set position again to be sure
                            player.transform.position = markerPosition;
                            
                            // Force update all child transforms
                            foreach (Transform child in player.GetComponentsInChildren<Transform>())
                            {
                                child.position = child.position; // Force update
                            }
                            
                            Debug.LogError($"[BUILD FIX] Forcefully positioned player at {player.transform.position}");
                            break;
                        }
                    }
                }
                
                // Re-enable the player movement components now that position is set
                EnablePlayerMovementComponents(player);
                Debug.LogError($"[BUILD FIX] Re-enabled movement components, final position: {player.transform.position}");
            }
            
            if (!markerFound)
            {
                Debug.LogError($"[BUILD FIX] CRITICAL ERROR: PlayerMarker with ID '{targetMarkerId}' not found in scene '{SceneManager.GetActiveScene().name}'!");
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
                        Debug.LogError($"[BUILD FIX] Restored {pair.Key} (x{pair.Value}) to player inventory in new scene");
                    }
                    
                    Debug.LogError("Player inventory restored from PersistentGameManager");
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
                        
                        Debug.LogError($"[BUILD FIX] Restored {item.name} (x{item.amount}) to player inventory in new scene");
                    }
                    
                    Debug.LogError("Player inventory restored from original inventory and saved to PersistentGameManager");
                }
            }
            
            // Character stats are automatically loaded by the Character component
            // when it starts in the new scene, through the PersistentGameManager
        }
        else
        {
            Debug.LogError("CRITICAL ERROR: Player not found in scene after transition!");
        }
        
        // Fade from black to reveal the scene
        yield return StartCoroutine(ScreenFader.Instance.FadeFromBlack());
        
        // IMPORTANT: Reset the fading flag now that transition is complete
        CleanupTransitionState();
        
        Debug.LogError($"[BUILD FIX] Transition complete - flag reset by SetupPlayerAfterTransition");
    }
    
    /// <summary>
    /// Temporarily disable player movement components to avoid position conflicts
    /// </summary>
    private void DisablePlayerMovementComponents(GameObject player)
    {
        // Store original components state for re-enabling later
        _disabledComponents.Clear();
        
        // Disable Rigidbody2D if present
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            _disabledComponents.Add(rb, rb.simulated);
            rb.simulated = false;
            rb.velocity = Vector2.zero; // Clear any velocity
            rb.angularVelocity = 0f; // Clear any rotation
        }
        
        // Disable CharacterController if present (common in 3D games)
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null)
        {
            _disabledComponents.Add(controller, controller.enabled);
            controller.enabled = false;
        }
        
        // Disable ALL MonoBehaviour scripts to ensure nothing fights our position change
        MonoBehaviour[] components = player.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour component in components)
        {
            // Skip UI components and this component
            if (!component.GetType().Name.Contains("UI") && component != this)
            {
                _disabledComponents.Add(component, component.enabled);
                component.enabled = false;
                Debug.LogError($"[BUILD FIX] Disabled component: {component.GetType().Name}");
            }
        }
        
        // Also disable all colliders to ensure they don't interfere
        Collider2D[] colliders = player.GetComponents<Collider2D>();
        foreach (Collider2D collider in colliders)
        {
            _disabledComponents.Add(collider, collider.enabled);
            collider.enabled = false;
        }
        
        Debug.LogError($"[BUILD FIX] Disabled {_disabledComponents.Count} components on player");
    }
    
    /// <summary>
    /// Re-enable player movement components after positioning
    /// </summary>
    private void EnablePlayerMovementComponents(GameObject player)
    {
        Debug.LogError($"[BUILD FIX] Re-enabling {_disabledComponents.Count} components on player");
        
        // Restore all components to their original state
        foreach (var pair in _disabledComponents)
        {
            if (pair.Key is Behaviour behaviour)
            {
                behaviour.enabled = (bool)pair.Value;
                Debug.LogError($"[BUILD FIX] Re-enabled component: {pair.Key.GetType().Name}");
            }
            else if (pair.Key is Rigidbody2D rb)
            {
                rb.simulated = (bool)pair.Value;
                Debug.LogError($"[BUILD FIX] Re-enabled Rigidbody2D simulation");
            }
            else if (pair.Key is Collider2D collider)
            {
                collider.enabled = (bool)pair.Value;
                Debug.LogError($"[BUILD FIX] Re-enabled Collider2D: {collider.GetType().Name}");
            }
        }
        
        _disabledComponents.Clear();
    }
    
    /// <summary>
    /// Set the return scene name for when combat ends
    /// </summary>
    /// <param name="sceneName">The scene name to return to after combat</param>
    public void SetReturnScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SCENE TRANSITION DEBUG] Cannot set empty scene name as return scene");
            return;
        }
        
        // Store the scene name to return to after combat
        currentSceneName = sceneName;
        
        // CRITICAL FIX: Also store in PlayerPrefs to survive scene transitions in builds
        PlayerPrefs.SetString("ReturnSceneName", sceneName);
        PlayerPrefs.Save();
        
        Debug.Log($"[SCENE TRANSITION DEBUG] Return scene set to: {currentSceneName} (Instance ID: {GetInstanceID()})");
        Debug.Log($"[SCENE TRANSITION DEBUG] Return scene also saved to PlayerPrefs: {PlayerPrefs.GetString("ReturnSceneName")}");
        
        // Log the stack trace to see what's calling this method
        Debug.Log($"[SCENE TRANSITION DEBUG] SetReturnScene called from:\n{System.Environment.StackTrace}");
    }
    
    // CRITICAL FIX: Add a method to handle scene loaded events and hook into CombatManager
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Check if we loaded a battle scene
        if (scene.name.StartsWith("Battle_"))
        {
            Debug.LogError($"[SCENE TRANSITION DEBUG] Battle scene loaded: {scene.name}, looking for CombatManager");
            
            // Wait a frame for everything to initialize
            StartCoroutine(ConnectToCombatManager());
        }
    }
    
    // CRITICAL FIX: Coroutine to find and connect to the CombatManager
    private IEnumerator ConnectToCombatManager()
    {
        // Wait for two frames to ensure everything is initialized
        yield return null;
        yield return null;
        
        // Current scene check
        Debug.LogError($"[CRITICAL DEBUG] ConnectToCombatManager running in scene: {SceneManager.GetActiveScene().name}");
        
        // Check return scene in PlayerPrefs
        if (PlayerPrefs.HasKey("ReturnSceneName")) 
        {
            string savedScene = PlayerPrefs.GetString("ReturnSceneName");
            Debug.LogError($"[CRITICAL DEBUG] Return scene in PlayerPrefs: '{savedScene}'");
        }
        else
        {
            Debug.LogError("[CRITICAL DEBUG] NO return scene found in PlayerPrefs during ConnectToCombatManager!");
        }
        
        // Find the CombatManager
        CombatManager combatManager = FindObjectOfType<CombatManager>();
        
        if (combatManager != null)
        {
            Debug.LogError($"[SCENE TRANSITION DEBUG] Found CombatManager in scene {SceneManager.GetActiveScene().name}");
            
            // Only subscribe if we're not already subscribed
            if (!isSubscribedToCombatEnd)
            {
                combatManager.OnCombatEnd += EndCombat;
                isSubscribedToCombatEnd = true;
                Debug.LogError("[CRITICAL DEBUG] Successfully subscribed to CombatManager.OnCombatEnd");
            }
            else
            {
                Debug.LogError("[CRITICAL DEBUG] Already subscribed to OnCombatEnd event, skipping second subscription");
            }
            
            // Add safety check for Obelisk battle
            if (SceneManager.GetActiveScene().name == "Battle_Obelisk")
            {
                Debug.LogError("[CRITICAL DEBUG] This is Battle_Obelisk - adding extra safety check");
                StartCoroutine(EnsureTransitionWorks());
            }
        }
        else
        {
            Debug.LogError($"[SCENE TRANSITION DEBUG] CRITICAL ERROR: Could not find CombatManager in battle scene!");
        }
    }
    
    // Safety method for Obelisk battle
    private IEnumerator EnsureTransitionWorks()
    {
        // Wait 5 seconds as safety check for battles that might not trigger EndCombat properly
        yield return new WaitForSeconds(5f);
        
        // Check if we're still in the battle scene
        if (SceneManager.GetActiveScene().name == "Battle_Obelisk")
        {
            // Check if PlayerPrefs has return scene
            if (PlayerPrefs.HasKey("ReturnSceneName"))
            {
                currentSceneName = PlayerPrefs.GetString("ReturnSceneName");
                Debug.LogError($"[CRITICAL DEBUG] Safety check: starting TransitionToOverworld with scene {currentSceneName}");
            }
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from scene loaded events
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        // Log when this object is destroyed
        Debug.LogWarning($"[SCENE TRANSITION DEBUG] SceneTransitionManager OnDestroy called (ID: {GetInstanceID()})");
        
        // Check if this was the active instance
        if (Instance == this)
        {
            Debug.LogError("[SCENE TRANSITION DEBUG] The active SceneTransitionManager instance was destroyed!");
        }
    }
    
    /// <summary>
    /// Reset all combat-related status
    /// </summary>
    public void ResetCombatStatus()
    {
        Debug.Log("[TRANSITION DEBUG] ResetCombatStatus called - resetting combat state");
        
        // Reset combat flags and variables
        combatWon = false;
        enemyThatInitiatedCombat = null;
        enemyIdThatInitiatedCombat = null;
        isFadingInProgress = false;
        isSubscribedToCombatEnd = false;
        
        // Do an additional cleanup of combat objects to be thorough
        CleanupCombatObjects();
        
        // Look for any remaining text panels that could be persistent
        GameObject textPanel = GameObject.Find("TextPanel");
        if (textPanel != null)
        {
            Debug.Log("[TRANSITION DEBUG] Found and destroying lingering TextPanel");
            Destroy(textPanel);
        }
        
        // Look for any other common combat UI elements that might persist
        string[] combatUINames = new string[] {
            "CombatUI", "BattleUI", "SkillMenu", "ItemMenu", "ActionMenu",
            "CharacterStatsPanel", "TurnText", "ActionLabel", "CharacterUI"
        };
        
        foreach (string uiName in combatUINames)
        {
            GameObject obj = GameObject.Find(uiName);
            if (obj != null)
            {
                Debug.Log($"[TRANSITION DEBUG] Found and destroying lingering combat UI: {uiName}");
                Destroy(obj);
            }
        }
        
        // Find all canvases with combat-related names
        Canvas[] allCanvases = FindObjectsOfType<Canvas>(true);
        foreach (Canvas canvas in allCanvases)
        {
            string canvasName = canvas.gameObject.name.ToLower();
            if (canvasName.Contains("combat") || canvasName.Contains("battle") || 
                canvasName.Contains("enemy") || canvasName.Contains("action") || 
                canvasName.Contains("menu") || canvasName.Contains("skill") || 
                canvasName.Contains("item") || canvasName.Contains("stats"))
            {
                Debug.Log($"[TRANSITION DEBUG] Destroying combat canvas: {canvas.gameObject.name}");
                Destroy(canvas.gameObject);
            }
        }
    }
    
    /// <summary>
    /// Public method to explicitly reset the transition state from outside
    /// Use this as a last resort if transitions get stuck
    /// </summary>
    public void ResetTransitionState()
    {
        CleanupTransitionState();
    }
    
    /// <summary>
    /// Clean up and reset transition state - can be called from multiple places
    /// </summary>
    public void CleanupTransitionState()
    {
        isFadingInProgress = false;
        lastTransitionStartTime = 0f;
        string oldTransition = currentTransitionDescription;
        currentTransitionDescription = "";
        Debug.LogError($"[BUILD FIX] Transition state force-reset by CleanupTransitionState. Old transition: {oldTransition}");
        
        // Also reset screen fader if it exists
        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.ResetToVisible();
        }
    }
    
    /// <summary>
    /// Additional backup timeout to ensure transition flag gets reset even if the scene load fails
    /// </summary>
    private IEnumerator TransitionBackupReset()
    {
        // Wait a bit longer than the other timeouts (10 seconds)
        yield return new WaitForSecondsRealtime(10f);
        
        // If flag is still set, force reset
        if (isFadingInProgress)
        {
            Debug.LogWarning("[SCENE TRANSITION] Backup timeout triggered - Forcing transition state reset");
            CleanupTransitionState();
        }
    }

    /// <summary>
    /// Awakening of the script - make sure transition flag is reset on scene load
    /// </summary>
    private void Start()
    {
        // On startup, ensure the transition flag is reset
        Debug.LogError($"[BUILD FIX] SceneTransitionManager started in scene {SceneManager.GetActiveScene().name}. Resetting transition flag.");
        
        // NEW BUILD FIX: Check if we need to position player from a scene transition
        if (PlayerPrefs.GetInt("NeedsPlayerSetup", 0) == 1)
        {
            // Clear the flag immediately to prevent duplicate setups
            PlayerPrefs.SetInt("NeedsPlayerSetup", 0);
            PlayerPrefs.Save();
            
            Debug.LogError($"[BUILD FIX] Detected pending player setup from PlayerPrefs in scene {SceneManager.GetActiveScene().name}");
            
            // Get transition data
            string savedSceneName = PlayerPrefs.GetString("LastTargetSceneName", "");
            string savedMarkerId = PlayerPrefs.GetString("LastTargetMarkerId", "");
            
            // Verify we're in the right scene
            if (savedSceneName == SceneManager.GetActiveScene().name && !string.IsNullOrEmpty(savedMarkerId))
            {
                Debug.LogError($"[BUILD FIX] Will setup player at marker {savedMarkerId} in scene {savedSceneName}");
                
                // Store the marker ID for use in setup
                targetMarkerId = savedMarkerId;
                
                // Start the delayed setup since we're in build mode
                if (!Application.isEditor)
                {
                    Debug.LogError($"[BUILD FIX] Starting delayed setup from Start() in build");
                    StartCoroutine(DelayedSetupAfterTransition(0.5f)); // Longer delay for added safety
                }
                else
                {
                    // In editor, immediate setup is fine
                    Debug.LogError($"[BUILD FIX] Starting immediate setup from Start() in editor");
                    StartCoroutine(SetupPlayerAfterTransition());
                }
            }
            else
            {
                Debug.LogError($"[BUILD FIX] Scene mismatch or invalid marker - Expected scene: {savedSceneName}, Current scene: {SceneManager.GetActiveScene().name}, Marker: {savedMarkerId}");
            }
        }
        
        // Standard flag reset
        isFadingInProgress = false;
        currentTransitionDescription = "";
    }

    // Add a method that can be called from anywhere to force reset the static transition state
    /// <summary>
    /// Static method to force reset the transition flags from anywhere
    /// </summary>
    public static void ForceResetTransitionState()
    {
        // Skip reset if we're in the startroom scene
        bool isStartRoom = SceneManager.GetActiveScene().name.Contains("Startroom") || 
                          SceneManager.GetActiveScene().name.Contains("start_room");
        
        if (isStartRoom)
        {
            Debug.Log("[SCENE TRANSITION] Skipping static transition flag reset in startroom to preserve black screen");
            return;
        }
        
        isFadingInProgress = false;
        currentTransitionDescription = "";
        lastTransitionStartTime = 0f;
        Debug.LogError("[SCENE TRANSITION] Static transition flags forcibly reset by ForceResetTransitionState");
        
        // Also reset screen fader if it exists
        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.ResetToVisible();
        }
    }
} 