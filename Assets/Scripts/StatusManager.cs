using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum StatusType
{
    Strength,   // Attack up by 50%
    Weakness,   // Attack down by 50%
    Tough,      // Defense up by 50%
    Vulnerable, // Defense down by 50%
    Agile,      // Speed up by 50%
    Slowed,     // Speed down by 50%
    Guarded     // Ally takes damage for you
}

public class StatusManager : MonoBehaviour
{
    [Header("Status Icons")]
    public Sprite strengthIcon;
    public Sprite weaknessIcon;
    public Sprite toughIcon;
    public Sprite vulnerableIcon;
    public Sprite agileIcon;
    public Sprite slowedIcon;
    public Sprite guardedIcon;

    [Header("Status Settings")]
    public float statusIconSize = 0.5f;
    public float statusIconSpacing = 0.3f;
    public Vector3 statusIconBaseOffset = new Vector3(0, 1.5f, 0);
    
    // Dictionary to track status effects for each character
    private Dictionary<CombatStats, Dictionary<StatusType, GameObject>> characterStatuses = 
        new Dictionary<CombatStats, Dictionary<StatusType, GameObject>>();
    
    // Dictionary to track status effect durations
    private Dictionary<CombatStats, Dictionary<StatusType, int>> statusDurations = 
        new Dictionary<CombatStats, Dictionary<StatusType, int>>();
    
    // Dictionary to track which character is guarding another
    private Dictionary<CombatStats, CombatStats> guardianRelationships = new Dictionary<CombatStats, CombatStats>();
    
    // Singleton pattern
    private static StatusManager _instance;
    public static StatusManager Instance => _instance;
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
            _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Log the configured settings
        Debug.Log($"[Status Manager] Initialized with: iconSize={statusIconSize}, iconSpacing={statusIconSpacing}, baseOffset={statusIconBaseOffset}");
        
        // Validate status icon sprites
        Debug.Log($"[Status Manager] Status icon sprites: Strength={strengthIcon != null}, Weakness={weaknessIcon != null}, " +
                  $"Tough={toughIcon != null}, Vulnerable={vulnerableIcon != null}, Agile={agileIcon != null}, " +
                  $"Slowed={slowedIcon != null}, Guarded={guardedIcon != null}");
    }
    
    // Apply a status effect to a character
    public void ApplyStatus(CombatStats character, StatusType statusType, int duration = 3)
    {
        if (character == null) return;
        
        // Initialize dictionaries if needed
        if (!characterStatuses.ContainsKey(character))
        {
            characterStatuses[character] = new Dictionary<StatusType, GameObject>();
            statusDurations[character] = new Dictionary<StatusType, int>();
        }
        
        // Check for opposing status effects that cancel each other out
        StatusType? opposingStatus = GetOpposingStatus(statusType);
        if (opposingStatus.HasValue && HasStatus(character, opposingStatus.Value))
        {
            // If opposing status exists, remove it and reset the appropriate multiplier
            Debug.Log($"[Status Manager] {statusType} status is canceling out {opposingStatus.Value} status on {character.characterName}");
            RemoveStatus(character, opposingStatus.Value);
            
            // Reset the appropriate multiplier based on the status effect pair
            ResetAppropriateMultiplier(character, statusType);
            
            return; // Exit without applying the new status since they cancel out
        }
        
        // Apply the status effect
        switch (statusType)
        {
            case StatusType.Strength:
                // Handle attack up
                ApplyStrengthStatus(character, duration);
                break;
            case StatusType.Weakness:
                // Handle attack down
                ApplyWeaknessStatus(character, duration);
                break;
            case StatusType.Tough:
                // Handle defense up
                ApplyToughStatus(character, duration);
                break;
            case StatusType.Vulnerable:
                // Handle defense down
                ApplyVulnerableStatus(character, duration);
                break;
            case StatusType.Agile:
                // Handle speed up
                ApplyAgileStatus(character, duration);
                break;
            case StatusType.Slowed:
                // Handle speed down
                ApplySlowedStatus(character, duration);
                break;
            case StatusType.Guarded:
                // Handle guarded status (special case, no duration)
                ApplyGuardedStatus(character);
                break;
        }
        
        // Update the status icons
        UpdateStatusIcons(character);
    }
    
    // Helper method to reset the appropriate multiplier when status effects cancel each other
    private void ResetAppropriateMultiplier(CombatStats character, StatusType statusType)
    {
        switch (statusType)
        {
            case StatusType.Strength:
            case StatusType.Weakness:
                // Reset attack multiplier
                character.attackMultiplier = 1.0f;
                Debug.Log($"[Status Manager] Reset attack multiplier to 1.0 for {character.characterName}");
                break;
                
            case StatusType.Tough:
            case StatusType.Vulnerable:
                // Reset defense multiplier
                character.defenseMultiplier = 1.0f;
                Debug.Log($"[Status Manager] Reset defense multiplier to 1.0 for {character.characterName}");
                break;
                
            case StatusType.Agile:
            case StatusType.Slowed:
                // Reset action speed to base speed
                character.actionSpeed = character.baseActionSpeed;
                Debug.Log($"[Status Manager] Reset action speed to base speed ({character.baseActionSpeed}) for {character.characterName}");
                break;
        }
    }
    
    // Helper method to determine the opposing status for cancellation
    private StatusType? GetOpposingStatus(StatusType statusType)
    {
        switch (statusType)
        {
            case StatusType.Strength:
                return StatusType.Weakness;
            case StatusType.Weakness:
                return StatusType.Strength;
            case StatusType.Tough:
                return StatusType.Vulnerable;
            case StatusType.Vulnerable:
                return StatusType.Tough;
            case StatusType.Agile:
                return StatusType.Slowed;
            case StatusType.Slowed:
                return StatusType.Agile;
            default:
                return null; // No opposing status for Guarded
        }
    }
    
    // Remove a status effect from a character
    public void RemoveStatus(CombatStats character, StatusType statusType)
    {
        if (character == null || !characterStatuses.ContainsKey(character)) return;
        
        Debug.Log($"[Status Manager] Removing {statusType} status from {character.characterName}");
        
        // Remove the status effect
        switch (statusType)
        {
            case StatusType.Strength:
                RemoveStrengthStatus(character);
                break;
            case StatusType.Weakness:
                RemoveWeaknessStatus(character);
                break;
            case StatusType.Tough:
                RemoveToughStatus(character);
                break;
            case StatusType.Vulnerable:
                RemoveVulnerableStatus(character);
                break;
            case StatusType.Agile:
                RemoveAgileStatus(character);
                break;
            case StatusType.Slowed:
                RemoveSlowedStatus(character);
                break;
            case StatusType.Guarded:
                RemoveGuardedStatus(character);
                break;
        }
        
        // Remove the status icon
        if (characterStatuses[character].ContainsKey(statusType))
        {
            if (characterStatuses[character][statusType] != null)
            {
                Destroy(characterStatuses[character][statusType]);
                Debug.Log($"[Status Manager] Destroyed status icon for {statusType} on {character.characterName}");
            }
            characterStatuses[character].Remove(statusType);
        }
        
        // Remove the status duration
        if (statusDurations[character].ContainsKey(statusType))
        {
            statusDurations[character].Remove(statusType);
        }
        
        // Update the status icons
        UpdateStatusIcons(character);
    }
    
    // Clear all status effects from a character
    public void ClearAllStatuses(CombatStats character)
    {
        if (character == null || !characterStatuses.ContainsKey(character)) return;
        
        // Remove all status effects
        foreach (StatusType statusType in System.Enum.GetValues(typeof(StatusType)))
        {
            if (characterStatuses[character].ContainsKey(statusType))
            {
                switch (statusType)
                {
                    case StatusType.Strength:
                        RemoveStrengthStatus(character);
                        break;
                    case StatusType.Weakness:
                        RemoveWeaknessStatus(character);
                        break;
                    case StatusType.Tough:
                        RemoveToughStatus(character);
                        break;
                    case StatusType.Vulnerable:
                        RemoveVulnerableStatus(character);
                        break;
                    case StatusType.Agile:
                        RemoveAgileStatus(character);
                        break;
                    case StatusType.Slowed:
                        RemoveSlowedStatus(character);
                        break;
                    case StatusType.Guarded:
                        RemoveGuardedStatus(character);
                        break;
                }
                
                // Don't destroy the icon here, because we're going to iterate through all statuses
                // and the collection would be modified during iteration
            }
        }
        
        // Now remove all the icons after iteration is done
        foreach (var statusIcon in characterStatuses[character].Values)
        {
            if (statusIcon != null)
            {
                Destroy(statusIcon);
            }
        }
        
        // Clear all status data for the character
        characterStatuses[character].Clear();
        statusDurations[character].Clear();
        
        // Remove any guardian relationship
        if (guardianRelationships.ContainsKey(character))
        {
            guardianRelationships.Remove(character);
        }
        
        // Remove this character as a guardian for any other character
        List<CombatStats> charactersToUpdate = new List<CombatStats>();
        foreach (var kvp in guardianRelationships)
        {
            if (kvp.Value == character)
            {
                charactersToUpdate.Add(kvp.Key);
            }
        }
        
        foreach (var protectedCharacter in charactersToUpdate)
        {
            RemoveGuardian(protectedCharacter);
        }
    }
    
    // New method to remove all statuses from a character when they die
    public void RemoveAllStatuses(CombatStats character)
    {
        if (character == null) return;
        
        Debug.Log($"[Status Manager] Removing all statuses from {character.characterName} (dead)");
        
        // Reset all multipliers to default
        character.attackMultiplier = 1.0f;
        character.defenseMultiplier = 1.0f;
        character.actionSpeed = character.baseActionSpeed;
        
        // Clear all status effects
        ClearAllStatuses(character);
    }
    
    // New method to remove all status visuals without affecting the gameplay stats
    public void RemoveAllStatusVisuals(CombatStats character)
    {
        if (character == null || !characterStatuses.ContainsKey(character)) return;
        
        Debug.Log($"[Status Manager] Removing all status visuals from {character.characterName}");
        
        // Destroy all status icons
        foreach (var statusIcon in characterStatuses[character].Values)
        {
            if (statusIcon != null)
            {
                Destroy(statusIcon);
            }
        }
        
        // Clear the status icons dictionary for this character
        characterStatuses[character].Clear();
    }
    
    // Update status durations at the end of a character's turn
    public void UpdateStatusDurations(CombatStats character)
    {
        if (character == null || !statusDurations.ContainsKey(character)) return;
        
        List<StatusType> statusesToRemove = new List<StatusType>();
        
        foreach (var kvp in statusDurations[character])
        {
            StatusType statusType = kvp.Key;
            int durationLeft = kvp.Value - 1;
            
            if (durationLeft <= 0)
            {
                statusesToRemove.Add(statusType);
            }
            else
            {
                statusDurations[character][statusType] = durationLeft;
            }
        }
        
        foreach (StatusType statusType in statusesToRemove)
        {
            RemoveStatus(character, statusType);
        }
    }
    
    // Check if a character has a specific status effect
    public bool HasStatus(CombatStats character, StatusType statusType)
    {
        if (character == null || !characterStatuses.ContainsKey(character)) return false;
        
        return characterStatuses[character].ContainsKey(statusType);
    }
    
    // Get remaining duration of a status effect
    public int GetStatusDuration(CombatStats character, StatusType statusType)
    {
        if (character == null || !statusDurations.ContainsKey(character) ||
            !statusDurations[character].ContainsKey(statusType))
            return 0;
        
        return statusDurations[character][statusType];
    }
    
    // Get guardian of a character
    public CombatStats GetGuardian(CombatStats character)
    {
        if (character == null || !guardianRelationships.ContainsKey(character)) return null;
        
        return guardianRelationships[character];
    }
    
    // Status effect application functions
    private void ApplyStrengthStatus(CombatStats character, int duration)
    {
        Debug.Log($"[Status Manager] Applying Strength status to {character.characterName} for {duration} turns");
        
        // Apply 50% attack boost
        character.attackMultiplier = 1.5f;
        
        // Set status duration
        statusDurations[character][StatusType.Strength] = duration;
        
        // Create status icon if it doesn't exist
        if (!characterStatuses[character].ContainsKey(StatusType.Strength))
        {
            if (strengthIcon == null)
            {
                Debug.LogError("[Status Manager] Strength icon sprite is null! Check inspector references.");
                return;
            }
            
            GameObject statusIcon = CreateStatusIcon(character, strengthIcon);
            characterStatuses[character][StatusType.Strength] = statusIcon;
            Debug.Log($"[Status Manager] Created Strength icon for {character.characterName}");
        }
    }
    
    private void ApplyWeaknessStatus(CombatStats character, int duration)
    {
        // Apply 50% attack reduction
        character.attackMultiplier = 0.5f;
        
        // Set status duration
        statusDurations[character][StatusType.Weakness] = duration;
        
        // Create status icon if it doesn't exist
        if (!characterStatuses[character].ContainsKey(StatusType.Weakness))
        {
            GameObject statusIcon = CreateStatusIcon(character, weaknessIcon);
            characterStatuses[character][StatusType.Weakness] = statusIcon;
        }
    }
    
    private void ApplyToughStatus(CombatStats character, int duration)
    {
        // Apply 50% defense boost (take 50% less damage)
        character.defenseMultiplier = 0.5f;
        
        // Set status duration
        statusDurations[character][StatusType.Tough] = duration;
        
        // Create status icon if it doesn't exist
        if (!characterStatuses[character].ContainsKey(StatusType.Tough))
        {
            GameObject statusIcon = CreateStatusIcon(character, toughIcon);
            characterStatuses[character][StatusType.Tough] = statusIcon;
        }
    }
    
    private void ApplyVulnerableStatus(CombatStats character, int duration)
    {
        Debug.Log($"[Status Manager] Applying Vulnerable status to {character.characterName} for {duration} turns");
        
        // Apply 50% defense reduction (take 50% more damage)
        character.defenseMultiplier = 1.5f;
        
        // Set status duration
        statusDurations[character][StatusType.Vulnerable] = duration;
        
        // Create status icon if it doesn't exist
        if (!characterStatuses[character].ContainsKey(StatusType.Vulnerable))
        {
            if (vulnerableIcon == null)
            {
                Debug.LogError("[Status Manager] Vulnerable icon sprite is null! Check inspector references.");
                return;
            }
            
            GameObject statusIcon = CreateStatusIcon(character, vulnerableIcon);
            characterStatuses[character][StatusType.Vulnerable] = statusIcon;
            Debug.Log($"[Status Manager] Created Vulnerable icon for {character.characterName}");
        }
    }
    
    private void ApplyAgileStatus(CombatStats character, int duration)
    {
        // Store original action speed
        float originalSpeed = character.baseActionSpeed;
        
        // Apply 50% speed boost
        character.actionSpeed = originalSpeed * 1.5f;
        
        // Set status duration
        statusDurations[character][StatusType.Agile] = duration;
        
        // Create status icon if it doesn't exist
        if (!characterStatuses[character].ContainsKey(StatusType.Agile))
        {
            GameObject statusIcon = CreateStatusIcon(character, agileIcon);
            characterStatuses[character][StatusType.Agile] = statusIcon;
        }
    }
    
    private void ApplySlowedStatus(CombatStats character, int duration)
    {
        // Store original action speed
        float originalSpeed = character.baseActionSpeed;
        
        // Apply 50% speed reduction
        character.actionSpeed = originalSpeed * 0.5f;
        
        // Set status duration
        statusDurations[character][StatusType.Slowed] = duration;
        
        // Create status icon if it doesn't exist
        if (!characterStatuses[character].ContainsKey(StatusType.Slowed))
        {
            GameObject statusIcon = CreateStatusIcon(character, slowedIcon);
            characterStatuses[character][StatusType.Slowed] = statusIcon;
        }
    }
    
    private void ApplyGuardedStatus(CombatStats character)
    {
        // Create status icon if it doesn't exist
        if (!characterStatuses[character].ContainsKey(StatusType.Guarded))
        {
            GameObject statusIcon = CreateStatusIcon(character, guardedIcon);
            characterStatuses[character][StatusType.Guarded] = statusIcon;
        }
    }
    
    // Set up a guardian relationship between characters
    public void GuardAlly(CombatStats guardian, CombatStats ally)
    {
        if (guardian == null || ally == null) return;
        
        // Remove any existing guardian relationship for the ally
        if (guardianRelationships.ContainsKey(ally))
        {
            CombatStats oldGuardian = guardianRelationships[ally];
            if (oldGuardian != null && characterStatuses.ContainsKey(oldGuardian))
            {
                // Update the old guardian
            }
        }
        
        // Set the new guardian relationship
        guardianRelationships[ally] = guardian;
        
        // Apply guarded status to the ally
        ApplyStatus(ally, StatusType.Guarded);
    }
    
    // Remove guardian relationship
    public void RemoveGuardian(CombatStats ally)
    {
        if (ally == null) return;
        
        if (guardianRelationships.ContainsKey(ally))
        {
            guardianRelationships.Remove(ally);
            RemoveStatus(ally, StatusType.Guarded);
        }
    }
    
    // Status effect removal functions
    private void RemoveStrengthStatus(CombatStats character)
    {
        // Reset attack multiplier
        character.attackMultiplier = 1.0f;
    }
    
    private void RemoveWeaknessStatus(CombatStats character)
    {
        // Reset attack multiplier
        character.attackMultiplier = 1.0f;
    }
    
    private void RemoveToughStatus(CombatStats character)
    {
        // Reset defense multiplier
        character.defenseMultiplier = 1.0f;
    }
    
    private void RemoveVulnerableStatus(CombatStats character)
    {
        // Reset defense multiplier
        character.defenseMultiplier = 1.0f;
    }
    
    private void RemoveAgileStatus(CombatStats character)
    {
        // Reset action speed
        character.actionSpeed = character.baseActionSpeed;
    }
    
    private void RemoveSlowedStatus(CombatStats character)
    {
        // Reset action speed
        character.actionSpeed = character.baseActionSpeed;
    }
    
    private void RemoveGuardedStatus(CombatStats character)
    {
        // No additional logic needed as the guardian relationship is handled separately
    }
    
    // Create a status icon game object
    private GameObject CreateStatusIcon(CombatStats character, Sprite iconSprite)
    {
        Debug.Log($"[Status Manager] Creating status icon for {character.characterName} with sprite {iconSprite.name}");
        
        // Create a new GameObject for the icon
        GameObject iconObject = new GameObject($"{character.characterName}_{iconSprite.name}");
        
        // Add a sprite renderer component
        SpriteRenderer renderer = iconObject.AddComponent<SpriteRenderer>();
        renderer.sprite = iconSprite;
        renderer.sortingOrder = 10; // Make sure it renders on top
        
        // Apply the size defined in the inspector
        Debug.Log($"[Status Manager] Setting icon size to {statusIconSize}");
        renderer.transform.localScale = new Vector3(statusIconSize, statusIconSize, 1);
        
        // Position the icon directly above the character
        Vector3 characterPosition = character.transform.position;
        Vector3 iconPosition = characterPosition + statusIconBaseOffset;
        iconObject.transform.position = iconPosition;
        
        Debug.Log($"[Status Manager] Icon positioned at {iconPosition} (character at {characterPosition} + offset {statusIconBaseOffset})");
        
        // Don't set as child of transform initially, to avoid incorrect positioning
        // We'll manage the position manually to ensure it stays above the character
        
        return iconObject;
    }
    
    // Update status icon positions for a character
    private void UpdateStatusIcons(CombatStats character)
    {
        if (character == null || !characterStatuses.ContainsKey(character)) 
        {
            Debug.Log($"[Status Manager] Cannot update icons: character is null or has no status effects");
            return;
        }
        
        // Get all status icons for this character
        var statuses = characterStatuses[character];
        if (statuses.Count == 0) 
        {
            Debug.Log($"[Status Manager] No status icons to update for {character.characterName}");
            return;
        }
        
        Debug.Log($"[Status Manager] Updating {statuses.Count} status icons for {character.characterName}");
        
        // Calculate total width of all icons
        float totalWidth = (statuses.Count - 1) * statusIconSpacing;
        
        // Start position offset to the left
        float startX = -totalWidth / 2;
        
        // Position each icon
        int index = 0;
        foreach (var statusPair in statuses)
        {
            StatusType statusType = statusPair.Key;
            GameObject statusIcon = statusPair.Value;
            
            if (statusIcon != null)
            {
                // Position above character's head
                Vector3 characterPosition = character.transform.position;
                float xOffset = startX + (index * statusIconSpacing);
                Vector3 iconPosition = characterPosition + statusIconBaseOffset + new Vector3(xOffset, 0, 0);
                
                statusIcon.transform.position = iconPosition;
                
                Debug.Log($"[Status Manager] Placed {statusType} icon at {iconPosition} for {character.characterName} (index {index})");
                
                index++;
            }
            else
            {
                Debug.LogWarning($"[Status Manager] Status icon for {statusType} is null for {character.characterName}");
            }
        }
    }

    // Add this Update method to ensure status icons follow characters
    private void Update()
    {
        // Update the position of all status icons to follow their characters
        foreach (var characterPair in characterStatuses)
        {
            CombatStats character = characterPair.Key;
            Dictionary<StatusType, GameObject> statuses = characterPair.Value;
            
            if (character != null && statuses.Count > 0)
            {
                // Update positions for this character's icons
                UpdateStatusIcons(character);
            }
        }
    }
} 