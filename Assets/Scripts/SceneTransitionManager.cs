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
        
        // Load the combat scene
        SceneManager.LoadScene(combatSceneName);
        
        // Register for scene loaded event
        SceneManager.sceneLoaded += OnCombatSceneLoaded;
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
        if (playerInventory != null)
        {
            combatManager.SetupPlayerInventory(playerInventory.Items);
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
                // Copy items from saved inventory to new inventory
                foreach (ItemData item in playerInventory.Items)
                {
                    newInventory.AddItem(item);
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
} 