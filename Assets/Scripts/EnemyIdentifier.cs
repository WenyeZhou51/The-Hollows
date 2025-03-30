using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Component for giving enemies unique identifiers to track them across scene loads
/// </summary>
public class EnemyIdentifier : MonoBehaviour
{
    // A unique ID for this enemy
    [SerializeField] private string enemyId;
    
    // Whether to generate a new ID automatically if none is assigned
    [SerializeField] private bool generateIdIfEmpty = true;
    
    // Flag to track if we're being destroyed due to being defeated
    private bool markedForDestruction = false;
    
    private void Awake()
    {
        // If there's no enemy ID and we should generate one
        if (string.IsNullOrEmpty(enemyId) && generateIdIfEmpty)
        {
            GenerateUniqueId();
        }
        
        // Log the ID for this enemy
        Debug.Log($"Enemy with ID {enemyId} is awake in {SceneManager.GetActiveScene().name}");
    }
    
    private void Start()
    {
        // Double check in Start - this is later than Awake, so PersistentGameManager should be ready
        if (!markedForDestruction)
        {
            CheckIfDefeatedAndDestroy();
        }
    }
    
    /// <summary>
    /// Generates a unique ID for this enemy based on its position and scene
    /// </summary>
    private void GenerateUniqueId()
    {
        // Generate a more stable ID based on scene name and position (rounded to whole numbers for stability)
        float roundedX = Mathf.Round(transform.position.x * 10f) / 10f;
        float roundedY = Mathf.Round(transform.position.y * 10f) / 10f;
        float roundedZ = Mathf.Round(transform.position.z * 10f) / 10f;
        
        // Add a stable hash code from the GameObject name
        int nameHash = gameObject.name.GetHashCode();
        
        // Combine all elements into a unique ID
        enemyId = $"{SceneManager.GetActiveScene().name}_{roundedX}_{roundedY}_{roundedZ}_{nameHash}";
        
        Debug.Log($"Generated enemy ID: {enemyId} for {gameObject.name} at position {transform.position}");
    }
    
    /// <summary>
    /// Get the unique ID for this enemy
    /// </summary>
    /// <returns>The enemy's unique ID</returns>
    public string GetEnemyId()
    {
        // If we somehow don't have an ID yet, generate one
        if (string.IsNullOrEmpty(enemyId) && generateIdIfEmpty)
        {
            GenerateUniqueId();
        }
        
        return enemyId;
    }
    
    /// <summary>
    /// Set a custom ID for this enemy
    /// </summary>
    /// <param name="newId">The new ID to assign</param>
    public void SetEnemyId(string newId)
    {
        if (!string.IsNullOrEmpty(newId))
        {
            enemyId = newId;
            Debug.Log($"Changed enemy ID to: {enemyId} for {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Checks if this enemy has been defeated and destroys it if needed
    /// </summary>
    private void CheckIfDefeatedAndDestroy()
    {
        if (string.IsNullOrEmpty(enemyId))
        {
            Debug.LogWarning($"Enemy {gameObject.name} has no ID when checking if defeated");
            return;
        }
        
        // Make sure the persistent game manager exists
        if (PersistentGameManager.EnsureExists() == null)
        {
            Debug.LogError("Failed to find or create PersistentGameManager!");
            return;
        }
        
        // Check if this enemy has been defeated
        if (PersistentGameManager.Instance.IsEnemyDefeated(enemyId))
        {
            Debug.Log($"Enemy {enemyId} ({gameObject.name}) was previously defeated. Destroying it now!");
            markedForDestruction = true;
            
            // Destroy immediately - don't wait for next frame
            DestroyImmediate(gameObject);
        }
        else
        {
            Debug.Log($"Enemy {enemyId} ({gameObject.name}) has not been defeated. Keeping alive.");
        }
    }
    
    // Force a check when enabled (scene loaded)
    private void OnEnable()
    {
        if (!markedForDestruction)
        {
            CheckIfDefeatedAndDestroy();
        }
    }
} 