using UnityEngine;
using UnityEngine.SceneManagement;

public class Character : MonoBehaviour
{
    public string characterName;
    public int maxHealth = 100;
    public int maxSanity = 100;
    public int currentHealth;
    public int currentSanity;
    public bool isPlayer = true;
    
    [Tooltip("Unique ID for this character across scenes. Leave empty to use characterName.")]
    [SerializeField] private string characterId;

    private void Awake()
    {
        // If no character ID is specified, use the character name
        if (string.IsNullOrEmpty(characterId))
        {
            characterId = characterName;
        }
    }

    private void Start()
    {
        // Load saved stats from PersistentGameManager or use defaults
        LoadStats();
    }
    
    private void OnDestroy()
    {
        // Save character stats when destroyed
        if (isPlayer && gameObject.scene.isLoaded)
        {
            SaveStats();
        }
    }
    
    /// <summary>
    /// Loads character stats from PersistentGameManager
    /// </summary>
    private void LoadStats()
    {
        // Make sure PersistentGameManager exists
        PersistentGameManager.EnsureExists();
        
        // Only load stats for player characters
        if (isPlayer)
        {
            // Get saved health and sanity values
            currentHealth = PersistentGameManager.Instance.GetCharacterHealth(characterId, maxHealth);
            currentSanity = PersistentGameManager.Instance.GetCharacterMind(characterId, maxSanity);
            
            Debug.Log($"Loaded stats for {characterId}: Health={currentHealth}, Mind={currentSanity}");
        }
        else
        {
            // Non-player characters just initialize to defaults
            currentHealth = maxHealth;
            currentSanity = maxSanity;
        }
    }
    
    /// <summary>
    /// Saves character stats to PersistentGameManager
    /// </summary>
    private void SaveStats()
    {
        // Make sure PersistentGameManager exists
        PersistentGameManager.EnsureExists();
        
        // Only save stats for player characters
        if (isPlayer)
        {
            // Save current health and sanity values
            PersistentGameManager.Instance.SaveCharacterStats(characterId, currentHealth, currentSanity);
            
            Debug.Log($"Saved stats for {characterId}: Health={currentHealth}, Mind={currentSanity}");
        }
    }
    
    /// <summary>
    /// Get the character's unique ID
    /// </summary>
    public string GetCharacterId()
    {
        return characterId;
    }

    public bool IsDead()
    {
        return currentHealth <= 0 || currentSanity <= 0;
    }

    public void TakeDamage(int physicalDamage, int sanityDamage)
    {
        bool wasDead = IsDead();
        
        currentHealth -= physicalDamage;
        currentSanity -= sanityDamage;
        
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        currentSanity = Mathf.Clamp(currentSanity, 0, maxSanity);
        
        // Save stats after taking damage to ensure state is preserved
        if (isPlayer)
        {
            SaveStats();
        }
        
        // If the character died from this damage and is a player, increment the death counter
        if (!wasDead && IsDead() && isPlayer)
        {
            PersistentGameManager.Instance.IncrementDeaths();
        }
    }
    
    /// <summary>
    /// Heals the character for the specified amounts
    /// </summary>
    public void Heal(int healthAmount, int sanityAmount)
    {
        currentHealth += healthAmount;
        currentSanity += sanityAmount;
        
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        currentSanity = Mathf.Clamp(currentSanity, 0, maxSanity);
        
        // Save stats after healing to ensure state is preserved
        if (isPlayer)
        {
            SaveStats();
        }
    }
} 