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
    
    [Tooltip("Whether this is one of the four main characters (The Magician, The Fighter, The Bard, The Ranger)")]
    [SerializeField] private bool isMainCharacter = false;

    private void Awake()
    {
        // If no character ID is specified, use the character name
        if (string.IsNullOrEmpty(characterId))
        {
            characterId = characterName;
        }
        
        // Auto-detect if this is a main character
        if (!isMainCharacter && PersistentGameManager.Instance != null)
        {
            foreach (string mainCharName in PersistentGameManager.Instance.mainCharacters)
            {
                if (characterName == mainCharName || characterId == mainCharName)
                {
                    isMainCharacter = true;
                    break;
                }
            }
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
        if ((isPlayer || isMainCharacter) && gameObject.scene.isLoaded)
        {
            SaveStats();
        }
    }
    
    private void OnDisable()
    {
        // Save character stats when disabled
        if ((isPlayer || isMainCharacter) && gameObject.scene.isLoaded)
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
        
        // Only load stats for player characters or main characters
        if (isPlayer || isMainCharacter)
        {
            // First get max values (may override inspector values)
            maxHealth = PersistentGameManager.Instance.GetCharacterMaxHealth(characterId);
            maxSanity = PersistentGameManager.Instance.GetCharacterMaxMind(characterId);
            
            // Get saved health and sanity values
            currentHealth = PersistentGameManager.Instance.GetCharacterHealth(characterId, maxHealth);
            currentSanity = PersistentGameManager.Instance.GetCharacterMind(characterId, maxSanity);
            
            Debug.Log($"Loaded stats for {characterId}: HP={currentHealth}/{maxHealth}, Mind={currentSanity}/{maxSanity}");
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
        
        // Only save stats for player characters or main characters
        if (isPlayer || isMainCharacter)
        {
            // Save current and max health and sanity values
            PersistentGameManager.Instance.SaveCharacterStats(
                characterId, 
                currentHealth, 
                maxHealth, 
                currentSanity, 
                maxSanity
            );
            
            Debug.Log($"Saved stats for {characterId}: HP={currentHealth}/{maxHealth}, Mind={currentSanity}/{maxSanity}");
        }
    }
    
    /// <summary>
    /// Get the character's unique ID
    /// </summary>
    public string GetCharacterId()
    {
        return characterId;
    }
    
    /// <summary>
    /// Check if this is one of the main characters
    /// </summary>
    public bool IsMainCharacter()
    {
        return isMainCharacter;
    }

    public bool IsDead()
    {
        return currentHealth <= 0 || currentSanity <= 0;
    }

    // WARNING: This method doesn't apply the defense multiplier for combat statuses like Tough/Vulnerable
    // Use CombatStats.TakeDamage() for combat calculations that should respect status effects
    public void TakeDamage(int physicalDamage, int sanityDamage)
    {
        bool wasDead = IsDead();
        
        currentHealth -= physicalDamage;
        currentSanity -= sanityDamage;
        
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        currentSanity = Mathf.Clamp(currentSanity, 0, maxSanity);
        
        // Save stats after taking damage to ensure state is preserved
        if (isPlayer || isMainCharacter)
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
        if (isPlayer || isMainCharacter)
        {
            SaveStats();
        }
    }
} 