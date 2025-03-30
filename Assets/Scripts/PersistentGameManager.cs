using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent game manager that stores global game state across scene transitions
/// </summary>
public class PersistentGameManager : MonoBehaviour
{
    // Singleton pattern
    public static PersistentGameManager Instance { get; private set; }
    
    // List of defeated enemy IDs
    private HashSet<string> defeatedEnemyIds = new HashSet<string>();
    
    // Scene was just loaded flag
    private bool sceneJustLoaded = false;
    
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
    
    /// <summary>
    /// Called when a scene is loaded
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        sceneJustLoaded = true;
        
        // Start a delayed check for enemies to destroy after everything is initialized
        StartCoroutine(DestroyDefeatedEnemiesAfterLoad());
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
} 