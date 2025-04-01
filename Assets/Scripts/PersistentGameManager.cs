using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent game manager that stores global game state across scene transitions
/// </summary>
public class PersistentGameManager : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool showDebugUI = false;
    [SerializeField] private KeyCode toggleDebugUIKey = KeyCode.F9;
    
    // Singleton pattern
    public static PersistentGameManager Instance { get; private set; }
    
    // List of defeated enemy IDs
    private HashSet<string> defeatedEnemyIds = new HashSet<string>();
    
    // Scene was just loaded flag
    private bool sceneJustLoaded = false;
    
    // Character stats persistence
    private Dictionary<string, int> characterHealth = new Dictionary<string, int>();
    private Dictionary<string, int> characterMind = new Dictionary<string, int>();
    private Dictionary<string, int> characterMaxHealth = new Dictionary<string, int>();
    private Dictionary<string, int> characterMaxMind = new Dictionary<string, int>();
    
    // Interactable object states
    private Dictionary<string, bool> interactableStates = new Dictionary<string, bool>();
    
    // Player inventory persistence - key: itemName, value: amount
    private Dictionary<string, int> playerInventory = new Dictionary<string, int>();
    
    // Main characters (hardcoded for easy reference)
    public readonly string[] mainCharacters = new string[] 
    {
        "The Magician", 
        "The Fighter", 
        "The Bard", 
        "The Ranger"
    };
    
    // Custom variables
    [System.Serializable]
    public class GameVariables
    {
        public int chestsLooted = 0;
        public int deaths = 0;
    }
    
    public GameVariables variables = new GameVariables();
    
    // Events
    public delegate void GameVariableChangedHandler(string variableName);
    public event GameVariableChangedHandler OnGameVariableChanged;
    
    // Debug UI settings
    private Vector2 characterStatsScroll = Vector2.zero;
    private Vector2 interactableScroll = Vector2.zero;
    private Vector2 enemyScroll = Vector2.zero;
    private Vector2 inventoryScroll = Vector2.zero;
    private bool showCharacterStats = true;
    private bool showInteractableStates = true;
    private bool showDefeatedEnemies = true;
    private bool showInventory = true;
    private GUIStyle headerStyle;
    private GUIStyle valueStyle;
    private GUIStyle boxStyle;
    
    // Timer for reset confirmation
    private float resetConfirmationTime = 0;
    
    // Default values for main characters
    private const int DEFAULT_MAX_HEALTH = 100;
    private const int DEFAULT_MAX_MIND = 100;
    
    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Register for scene loaded events
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void OnDestroy()
    {
        // Unregister when destroyed
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void Update()
    {
        // Toggle debug UI with key press
        if (Input.GetKeyDown(toggleDebugUIKey))
        {
            showDebugUI = !showDebugUI;
        }
    }
    
    private void InitializeGUIStyles()
    {
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.fontSize = 14;
        }
        
        if (valueStyle == null)
        {
            valueStyle = new GUIStyle(GUI.skin.label);
            valueStyle.normal.textColor = Color.white;
        }
        
        if (boxStyle == null)
        {
            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = MakeTexture(2, 2, new Color(0f, 0f, 0f, 0.7f));
        }
    }
    
    private Texture2D MakeTexture(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
        
        Texture2D texture = new Texture2D(width, height);
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
    
    private void OnGUI()
    {
        if (!showDebugUI) return;
        
        InitializeGUIStyles();
        
        // Debug window
        GUILayout.BeginArea(new Rect(10, 10, 400, Screen.height - 20), boxStyle);
        
        GUILayout.Label("Persistent Game Manager Debug", headerStyle);
        GUILayout.Space(5);
        
        // Global variables section
        GUILayout.Label("Global Variables:", headerStyle);
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label($"Chests Looted: {variables.chestsLooted}", valueStyle);
        GUILayout.Label($"Deaths: {variables.deaths}", valueStyle);
        GUILayout.Label($"Current Scene: {SceneManager.GetActiveScene().name}", valueStyle);
        GUILayout.EndVertical();
        
        GUILayout.Space(10);
        
        // Character stats section
        showCharacterStats = GUILayout.Toggle(showCharacterStats, "Character Stats", GUI.skin.button);
        if (showCharacterStats)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            
            // Display main character stats
            foreach (string character in mainCharacters)
            {
                int health = GetCharacterHealth(character, DEFAULT_MAX_HEALTH);
                int maxHealth = GetCharacterMaxHealth(character);
                int mind = GetCharacterMind(character, DEFAULT_MAX_MIND);
                int maxMind = GetCharacterMaxMind(character);
                
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{character}:", GUILayout.Width(120));
                GUILayout.Label($"HP: {health}/{maxHealth}", GUILayout.Width(100));
                GUILayout.Label($"Mind: {mind}/{maxMind}");
                GUILayout.EndHorizontal();
            }
            
            // Show other characters if there are any not in the main list
            if (characterHealth.Count > 0)
            {
                bool hasOthers = false;
                foreach (var pair in characterHealth)
                {
                    if (!mainCharacters.Contains(pair.Key))
                    {
                        if (!hasOthers)
                        {
                            GUILayout.Space(10);
                            GUILayout.Label("Other Characters:", valueStyle);
                            hasOthers = true;
                            
                            characterStatsScroll = GUILayout.BeginScrollView(characterStatsScroll, GUILayout.Height(100));
                        }
                        
                        int mind = 0;
                        characterMind.TryGetValue(pair.Key, out mind);
                        GUILayout.Label($"{pair.Key}: Health={pair.Value}, Mind={mind}", valueStyle);
                    }
                }
                
                if (hasOthers)
                {
                    GUILayout.EndScrollView();
                }
            }
            
            GUILayout.EndVertical();
        }
        
        GUILayout.Space(5);
        
        // Player Inventory section
        showInventory = GUILayout.Toggle(showInventory, "Player Inventory", GUI.skin.button);
        if (showInventory)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            
            if (playerInventory.Count == 0)
            {
                GUILayout.Label("Inventory is empty", valueStyle);
            }
            else
            {
                inventoryScroll = GUILayout.BeginScrollView(inventoryScroll, GUILayout.Height(100));
                
                foreach (var pair in playerInventory)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(pair.Key, GUILayout.Width(150));
                    GUILayout.Label($"x{pair.Value}");
                    GUILayout.EndHorizontal();
                }
                
                GUILayout.EndScrollView();
            }
            
            GUILayout.EndVertical();
        }
        
        GUILayout.Space(5);
        
        // Interactable states section
        showInteractableStates = GUILayout.Toggle(showInteractableStates, "Interactable States", GUI.skin.button);
        if (showInteractableStates)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            
            if (interactableStates.Count == 0)
            {
                GUILayout.Label("No interactable states stored", valueStyle);
            }
            else
            {
                interactableScroll = GUILayout.BeginScrollView(interactableScroll, GUILayout.Height(100));
                
                foreach (var pair in interactableStates)
                {
                    GUILayout.Label($"{pair.Key}: Looted={pair.Value}", valueStyle);
                }
                
                GUILayout.EndScrollView();
            }
            
            GUILayout.EndVertical();
        }
        
        GUILayout.Space(5);
        
        // Defeated enemies section
        showDefeatedEnemies = GUILayout.Toggle(showDefeatedEnemies, "Defeated Enemies", GUI.skin.button);
        if (showDefeatedEnemies)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            
            if (defeatedEnemyIds.Count == 0)
            {
                GUILayout.Label("No defeated enemies", valueStyle);
            }
            else
            {
                enemyScroll = GUILayout.BeginScrollView(enemyScroll, GUILayout.Height(100));
                
                foreach (string enemyId in defeatedEnemyIds)
                {
                    GUILayout.Label(enemyId, valueStyle);
                }
                
                GUILayout.EndScrollView();
            }
            
            GUILayout.EndVertical();
        }
        
        GUILayout.Space(15);
        
        // Reset all data button
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("RESET ALL DATA", GUILayout.Height(30)))
        {
            if (resetConfirmationTime > 0)
            {
                ResetAllData();
                resetConfirmationTime = 0;
            }
            else
            {
                resetConfirmationTime = Time.time + 3f; // 3 second window to confirm
            }
        }
        GUI.backgroundColor = Color.white;
        
        // Show confirmation countdown if needed
        if (resetConfirmationTime > 0)
        {
            float remaining = resetConfirmationTime - Time.time;
            if (remaining > 0)
            {
                GUILayout.Label($"Click again to confirm reset ({remaining:F1}s)", valueStyle);
            }
            else
            {
                resetConfirmationTime = 0;
            }
        }
        
        GUILayout.Space(10);
        
        GUILayout.Label("Press " + toggleDebugUIKey.ToString() + " to toggle this debug view", valueStyle);
        
        GUILayout.EndArea();
    }
    
    /// <summary>
    /// Called when a scene is loaded
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        sceneJustLoaded = true;
        
        // Start a delayed check for enemies to destroy after everything is initialized
        StartCoroutine(DestroyDefeatedEnemiesAfterLoad());
        
        // Start a delayed check to apply interactable states
        StartCoroutine(ApplyInteractableStatesAfterLoad());
    }
    
    /// <summary>
    /// Ensures an instance of PersistentGameManager exists in the scene
    /// </summary>
    /// <returns>The PersistentGameManager instance</returns>
    public static PersistentGameManager EnsureExists()
    {
        if (Instance == null)
        {
            // Look for existing instance
            PersistentGameManager[] managers = FindObjectsOfType<PersistentGameManager>();
            
            if (managers.Length > 0)
            {
                // Use first instance found
                Instance = managers[0];
                Debug.Log("Found existing PersistentGameManager");
                
                // Destroy any extras
                for (int i = 1; i < managers.Length; i++)
                {
                    Debug.LogWarning("Destroying extra PersistentGameManager instance");
                    Destroy(managers[i].gameObject);
                }
            }
            else
            {
                // Create new instance
                GameObject managerObj = new GameObject("PersistentGameManager");
                Instance = managerObj.AddComponent<PersistentGameManager>();
                Debug.Log("Created new PersistentGameManager");
            }
        }
        
        return Instance;
    }
    
    /// <summary>
    /// Marks an enemy as defeated
    /// </summary>
    /// <param name="enemyId">The unique ID of the defeated enemy</param>
    public void MarkEnemyDefeated(string enemyId)
    {
        if (!string.IsNullOrEmpty(enemyId))
        {
            Debug.Log($"Marking enemy as defeated: {enemyId}");
            defeatedEnemyIds.Add(enemyId);
        }
    }
    
    /// <summary>
    /// Checks if an enemy is already defeated
    /// </summary>
    /// <param name="enemyId">The unique ID of the enemy to check</param>
    /// <returns>True if the enemy has been defeated, false otherwise</returns>
    public bool IsEnemyDefeated(string enemyId)
    {
        bool isDefeated = defeatedEnemyIds.Contains(enemyId);
        if (isDefeated)
        {
            Debug.Log($"Enemy check: {enemyId} is defeated");
        }
        return isDefeated;
    }
    
    /// <summary>
    /// Gets the current list of defeated enemy IDs
    /// </summary>
    /// <returns>Array of defeated enemy IDs</returns>
    public string[] GetDefeatedEnemyIds()
    {
        string[] ids = new string[defeatedEnemyIds.Count];
        defeatedEnemyIds.CopyTo(ids);
        return ids;
    }
    
    /// <summary>
    /// Destroys all defeated enemies after scene load when everything is initialized
    /// </summary>
    private IEnumerator DestroyDefeatedEnemiesAfterLoad()
    {
        // Wait until the end of the frame to ensure all objects are initialized
        yield return new WaitForEndOfFrame();
        
        // Wait a bit more to make sure everything is fully initialized
        yield return new WaitForSeconds(0.1f);
        
        // Find all enemies in the scene
        EnemyIdentifier[] allEnemies = FindObjectsOfType<EnemyIdentifier>();
        int destroyedCount = 0;
        
        // Check each enemy
        foreach (EnemyIdentifier enemy in allEnemies)
        {
            string enemyId = enemy.GetEnemyId();
            if (!string.IsNullOrEmpty(enemyId) && defeatedEnemyIds.Contains(enemyId))
            {
                Debug.Log($"Destroying defeated enemy {enemyId} after scene load");
                Destroy(enemy.gameObject);
                destroyedCount++;
            }
        }
        
        Debug.Log($"Scene load check complete. Destroyed {destroyedCount} defeated enemies out of {allEnemies.Length} total enemies");
        
        // Reset the scene loaded flag
        sceneJustLoaded = false;
    }
    
    /// <summary>
    /// Apply the saved state to interactable objects in the newly loaded scene
    /// </summary>
    private IEnumerator ApplyInteractableStatesAfterLoad()
    {
        // Wait until the end of the frame to ensure all objects are initialized
        yield return new WaitForEndOfFrame();
        
        // Wait a bit more to make sure everything is fully initialized
        yield return new WaitForSeconds(0.2f);
        
        string currentScene = SceneManager.GetActiveScene().name;
        
        // Find all interactable boxes in the scene
        InteractableBox[] interactableBoxes = FindObjectsOfType<InteractableBox>();
        int updatedCount = 0;
        
        // Check each box
        foreach (InteractableBox box in interactableBoxes)
        {
            // Create a unique identifier for this box
            string boxId = $"{currentScene}:{box.gameObject.name}";
            
            // Check if we have a saved state for this box
            if (interactableStates.TryGetValue(boxId, out bool hasBeenLooted))
            {
                // Apply the saved state
                box.SetLootedState(hasBeenLooted);
                updatedCount++;
                
                Debug.Log($"Restored interactable state for {boxId}: hasBeenLooted = {hasBeenLooted}");
            }
        }
        
        Debug.Log($"Applied saved state to {updatedCount} interactable objects in scene {currentScene}");
    }
    
    /// <summary>
    /// Log the current defeated enemies (debug helper)
    /// </summary>
    public void LogDefeatedEnemies()
    {
        Debug.Log($"==== DEFEATED ENEMIES ({defeatedEnemyIds.Count}) ====");
        foreach (string id in defeatedEnemyIds)
        {
            Debug.Log($"Enemy ID: {id}");
        }
        Debug.Log("================================");
    }
    
    #region Character Stats Persistence
    
    /// <summary>
    /// Save a character's health and mind values
    /// </summary>
    /// <param name="characterId">Unique ID for the character</param>
    /// <param name="currentHealth">Current health value</param>
    /// <param name="maxHealth">Maximum health value</param>
    /// <param name="currentMind">Current mind value</param>
    /// <param name="maxMind">Maximum mind value</param>
    public void SaveCharacterStats(string characterId, int currentHealth, int maxHealth, int currentMind, int maxMind)
    {
        if (string.IsNullOrEmpty(characterId))
        {
            Debug.LogError("Cannot save character stats with null ID");
            return;
        }
        
        characterHealth[characterId] = currentHealth;
        characterMaxHealth[characterId] = maxHealth;
        characterMind[characterId] = currentMind;
        characterMaxMind[characterId] = maxMind;
        
        Debug.Log($"Saved stats for {characterId}: Health={currentHealth}/{maxHealth}, Mind={currentMind}/{maxMind}");
    }
    
    /// <summary>
    /// Try to get a character's health value
    /// </summary>
    /// <param name="characterId">Unique ID for the character</param>
    /// <param name="defaultHealth">Default value if not found</param>
    /// <returns>The stored health value or the default value if not found</returns>
    public int GetCharacterHealth(string characterId, int defaultHealth)
    {
        if (characterHealth.TryGetValue(characterId, out int health))
        {
            return health;
        }
        return defaultHealth;
    }
    
    /// <summary>
    /// Try to get a character's maximum health value
    /// </summary>
    /// <param name="characterId">Unique ID for the character</param>
    /// <returns>The stored maximum health value or the default value if not found</returns>
    public int GetCharacterMaxHealth(string characterId)
    {
        if (characterMaxHealth.TryGetValue(characterId, out int maxHealth))
        {
            return maxHealth;
        }
        return DEFAULT_MAX_HEALTH;
    }
    
    /// <summary>
    /// Try to get a character's mind value
    /// </summary>
    /// <param name="characterId">Unique ID for the character</param>
    /// <param name="defaultMind">Default value if not found</param>
    /// <returns>The stored mind value or the default value if not found</returns>
    public int GetCharacterMind(string characterId, int defaultMind)
    {
        if (characterMind.TryGetValue(characterId, out int mind))
        {
            return mind;
        }
        return defaultMind;
    }
    
    /// <summary>
    /// Try to get a character's maximum mind value
    /// </summary>
    /// <param name="characterId">Unique ID for the character</param>
    /// <returns>The stored maximum mind value or the default value if not found</returns>
    public int GetCharacterMaxMind(string characterId)
    {
        if (characterMaxMind.TryGetValue(characterId, out int maxMind))
        {
            return maxMind;
        }
        return DEFAULT_MAX_MIND;
    }
    
    #endregion
    
    #region Interactable Object State Persistence
    
    /// <summary>
    /// Save the state of an interactable object
    /// </summary>
    /// <param name="sceneId">Scene containing the object</param>
    /// <param name="objectId">Object identifier</param>
    /// <param name="hasBeenLooted">Whether the object has been looted</param>
    public void SaveInteractableState(string sceneId, string objectId, bool hasBeenLooted)
    {
        string fullId = $"{sceneId}:{objectId}";
        interactableStates[fullId] = hasBeenLooted;
        
        Debug.Log($"Saved interactable state for {fullId}: hasBeenLooted = {hasBeenLooted}");
        
        // If this is a chest being looted for the first time, increment the counter
        if (hasBeenLooted && !interactableStates.ContainsKey(fullId))
        {
            variables.chestsLooted++;
            OnGameVariableChanged?.Invoke("chestsLooted");
            Debug.Log($"Incremented chestsLooted to {variables.chestsLooted}");
        }
    }
    
    /// <summary>
    /// Get the state of an interactable object
    /// </summary>
    /// <param name="sceneId">Scene containing the object</param>
    /// <param name="objectId">Object identifier</param>
    /// <param name="defaultState">Default state if not found</param>
    /// <returns>The saved state or the default state if not found</returns>
    public bool GetInteractableState(string sceneId, string objectId, bool defaultState)
    {
        string fullId = $"{sceneId}:{objectId}";
        
        if (interactableStates.TryGetValue(fullId, out bool state))
        {
            return state;
        }
        
        return defaultState;
    }
    
    #endregion
    
    #region Custom Variables
    
    /// <summary>
    /// Increment the chests looted counter
    /// </summary>
    public void IncrementChestsLooted()
    {
        variables.chestsLooted++;
        OnGameVariableChanged?.Invoke("chestsLooted");
        Debug.Log($"Incremented chestsLooted to {variables.chestsLooted}");
    }
    
    /// <summary>
    /// Increment the deaths counter
    /// </summary>
    public void IncrementDeaths()
    {
        variables.deaths++;
        OnGameVariableChanged?.Invoke("deaths");
        Debug.Log($"Incremented deaths to {variables.deaths}");
    }
    
    /// <summary>
    /// Get the current count of chests looted
    /// </summary>
    public int GetChestsLooted()
    {
        return variables.chestsLooted;
    }
    
    /// <summary>
    /// Get the current death count
    /// </summary>
    public int GetDeaths()
    {
        return variables.deaths;
    }
    
    #endregion
    
    #region Player Inventory Persistence
    
    /// <summary>
    /// Update the player's inventory with the current items
    /// </summary>
    /// <param name="items">Dictionary of items where the key is the item name and the value is the amount</param>
    public void UpdatePlayerInventory(Dictionary<string, int> items)
    {
        playerInventory.Clear();
        
        foreach (var pair in items)
        {
            playerInventory[pair.Key] = pair.Value;
        }
        
        Debug.Log($"Updated player inventory with {playerInventory.Count} items");
    }
    
    /// <summary>
    /// Update the player's inventory from a list of ItemData objects
    /// </summary>
    /// <param name="items">List of ItemData objects</param>
    public void UpdatePlayerInventory(List<ItemData> items)
    {
        playerInventory.Clear();
        
        foreach (var item in items)
        {
            playerInventory[item.name] = item.amount;
        }
        
        Debug.Log($"Updated player inventory with {playerInventory.Count} items from ItemData list");
    }
    
    /// <summary>
    /// Add an item to the player's inventory
    /// </summary>
    /// <param name="itemName">Name of the item</param>
    /// <param name="amount">Amount to add</param>
    public void AddItemToInventory(string itemName, int amount)
    {
        if (playerInventory.ContainsKey(itemName))
        {
            playerInventory[itemName] += amount;
        }
        else
        {
            playerInventory[itemName] = amount;
        }
        
        Debug.Log($"Added {amount}x {itemName} to player inventory");
    }
    
    /// <summary>
    /// Remove an item from the player's inventory
    /// </summary>
    /// <param name="itemName">Name of the item</param>
    /// <param name="amount">Amount to remove</param>
    public void RemoveItemFromInventory(string itemName, int amount)
    {
        if (playerInventory.ContainsKey(itemName))
        {
            playerInventory[itemName] -= amount;
            
            if (playerInventory[itemName] <= 0)
            {
                playerInventory.Remove(itemName);
                Debug.Log($"Removed {itemName} from player inventory (amount reached zero)");
            }
            else
            {
                Debug.Log($"Removed {amount}x {itemName} from player inventory, {playerInventory[itemName]} remaining");
            }
        }
    }
    
    /// <summary>
    /// Get the entire player inventory
    /// </summary>
    /// <returns>Dictionary of item names to amounts</returns>
    public Dictionary<string, int> GetPlayerInventory()
    {
        return new Dictionary<string, int>(playerInventory);
    }
    
    /// <summary>
    /// Convert the persistent inventory to a list of ItemData objects
    /// </summary>
    /// <returns>List of ItemData objects</returns>
    public List<ItemData> GetPlayerInventoryAsItemData()
    {
        List<ItemData> result = new List<ItemData>();
        
        foreach (var pair in playerInventory)
        {
            // Create a new ItemData object for each item
            // Note: This only sets name and amount; other properties will be default
            ItemData item = new ItemData(pair.Key, "", pair.Value, false);
            result.Add(item);
        }
        
        return result;
    }
    
    #endregion
    
    /// <summary>
    /// Reset all persistent data - FOR DEBUGGING ONLY
    /// </summary>
    public void ResetAllData()
    {
        // Reset all dictionaries
        characterHealth.Clear();
        characterMind.Clear();
        characterMaxHealth.Clear();
        characterMaxMind.Clear();
        interactableStates.Clear();
        defeatedEnemyIds.Clear();
        playerInventory.Clear();
        
        // Reset custom variables
        variables.chestsLooted = 0;
        variables.deaths = 0;
        
        // Notify that a reset occurred
        Debug.Log(">>> PERSISTENT GAME MANAGER: All data has been reset <<<");
        
        // Trigger refresh of any interactable objects in the scene
        StartCoroutine(ApplyInteractableStatesAfterLoad());
    }
} 