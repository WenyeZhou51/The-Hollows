using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Controls the behavior of the Obelisk enemy in combat
/// </summary>
public class ObeliskBehavior : EnemyBehavior
{
    [Header("Skill Probabilities")]
    [Tooltip("Probability of using Triangulation skill (deals 20-30 damage to 3 random characters)")]
    [Range(0, 100)]
    public float triangulationChance = 30f;
    
    [Tooltip("Probability of using Unblinking Gaze skill (drains 30-40 Mind from target character and applies SLOW)")]
    [Range(0, 100)]
    public float unblinkingGazeChance = 30f;
    
    [Tooltip("Probability of using Crippling Doubt skill (deals 15-30 damage and applies WEAK and VULNERABLE)")]
    [Range(0, 100)]
    public float cripplingDoubtChance = 30f;
    
    [Tooltip("Probability of using Malice of Stone skill (deals 40-70 damage to all party members)")]
    [Range(0, 100)]
    public float maliceOfStoneChance = 10f;
    
    public override IEnumerator ExecuteTurn(CombatStats enemy, List<CombatStats> players, CombatUI combatUI)
    {
        // Normalize chance values to ensure they add up to 100%
        float totalChance = triangulationChance + unblinkingGazeChance + cripplingDoubtChance + maliceOfStoneChance;
        if (totalChance <= 0)
        {
            Debug.LogWarning("All Obelisk skill chances are set to 0, defaulting to basic attack");
            yield return UseBasicAttack(enemy, players, combatUI);
            yield break;
        }
        
        // Calculate normalized probabilities
        float normalizedTriangulation = triangulationChance / totalChance * 100f;
        float normalizedUnblinkingGaze = unblinkingGazeChance / totalChance * 100f;
        float normalizedCripplingDoubt = cripplingDoubtChance / totalChance * 100f;
        float normalizedMaliceOfStone = maliceOfStoneChance / totalChance * 100f;
        
        // Roll for skill selection
        float roll = Random.Range(0f, 100f);
        
        if (roll < normalizedTriangulation)
        {
            yield return UseTriangulationSkill(enemy, players, combatUI);
        }
        else if (roll < normalizedTriangulation + normalizedUnblinkingGaze)
        {
            yield return UseUnblinkingGazeSkill(enemy, players, combatUI);
        }
        else if (roll < normalizedTriangulation + normalizedUnblinkingGaze + normalizedCripplingDoubt)
        {
            yield return UseCripplingDoubtSkill(enemy, players, combatUI);
        }
        else
        {
            yield return UseMaliceOfStoneSkill(enemy, players, combatUI);
        }
    }
    
    private IEnumerator UseBasicAttack(CombatStats enemy, List<CombatStats> players, CombatUI combatUI)
    {
        // Display generic attack message in text panel
        if (combatUI != null && combatUI.turnText != null)
        {
            combatUI.DisplayTurnAndActionMessage($"{enemy.characterName} attacks!");
        }
        
        // Display specific skill name in action display label
        if (combatUI != null)
        {
            combatUI.DisplayActionLabel("Basic Attack");
        }
        
        // Wait for action display to complete
        yield return WaitForActionDisplay();
        
        // Find player with lowest HP
        var target = players
            .Where(p => !p.IsDead())
            .OrderBy(p => p.currentHealth)
            .FirstOrDefault();

        if (target != null)
        {
            // Base damage
            float baseDamage = 30f;
            
            // Round down to whole number
            int finalDamage = Mathf.FloorToInt(baseDamage);
            
            target.TakeDamage(finalDamage);
        }
    }
    
    private IEnumerator UseTriangulationSkill(CombatStats enemy, List<CombatStats> players, CombatUI combatUI)
    {
        // Display generic attack message in text panel
        if (combatUI != null && combatUI.turnText != null)
        {
            combatUI.DisplayTurnAndActionMessage($"{enemy.characterName} attacks!");
        }
        
        // Display specific skill name in action display label
        if (combatUI != null)
        {
            combatUI.DisplayActionLabel("Triangulation");
        }
        
        // Wait for action display to complete
        yield return WaitForActionDisplay();
        
        // Get all living players
        var livingPlayers = players.Where(p => !p.IsDead()).ToList();
        
        // If we have fewer than 3 players, we'll hit some multiple times
        for (int i = 0; i < 3; i++)
        {
            if (livingPlayers.Count == 0) break;
            
            // Select a random player
            int randomIndex = Random.Range(0, livingPlayers.Count);
            var target = livingPlayers[randomIndex];
            
            // Deal 20-30 random damage
            float damage = Random.Range(20f, 30f);
            
            // Round down to whole number
            int finalDamage = Mathf.FloorToInt(damage);
            
            target.TakeDamage(finalDamage);
            
            Debug.Log($"Triangulation hit {target.characterName} for {finalDamage} damage");
            
            // Don't remove from list - the same character can be hit multiple times
        }
    }
    
    private IEnumerator UseUnblinkingGazeSkill(CombatStats enemy, List<CombatStats> players, CombatUI combatUI)
    {
        // Display generic attack message in text panel
        if (combatUI != null && combatUI.turnText != null)
        {
            combatUI.DisplayTurnAndActionMessage($"{enemy.characterName} fixes its unblinking gaze upon you!");
        }
        
        // Display specific skill name in action display label
        if (combatUI != null)
        {
            combatUI.DisplayActionLabel("Unblinking Gaze");
        }
        
        // Wait for action display to complete
        yield return WaitForActionDisplay();
        
        // Get all living players
        var livingPlayers = players.Where(p => !p.IsDead()).ToList();
        if (livingPlayers.Count == 0) yield break;
        
        // Find player with highest mind
        var target = livingPlayers.OrderByDescending(p => p.currentSanity).First();
        
        // Drain 30-40 Mind
        float mindDrain = Random.Range(30f, 40.1f); // 40.1 to include 40 in the range
        
        // Round down to whole number
        int finalMindDrain = Mathf.FloorToInt(mindDrain);
        
        // Apply mind damage
        target.UseSanity(finalMindDrain);
        
        // Apply SLOW status
        StatusManager statusManager = StatusManager.Instance;
        if (statusManager != null)
        {
            statusManager.ApplyStatus(target, StatusType.Slowed, 2); // Apply for 2 turns
            Debug.Log($"[Unblinking Gaze] Applied Slowed status to {target.characterName} for 2 turns");
        }
        else
        {
            // Fallback to old system if status manager not available
            float baseActionSpeed = target.actionSpeed;
            float newSpeed = baseActionSpeed * 0.5f; // 50% reduction
            target.actionSpeed = newSpeed;
            Debug.LogWarning($"[Unblinking Gaze] StatusManager not found, using legacy speed reduction for {target.characterName}");
        }
        
        Debug.Log($"Unblinking Gaze drained {finalMindDrain} Mind from {target.characterName} and applied SLOW status");
    }
    
    private IEnumerator UseCripplingDoubtSkill(CombatStats enemy, List<CombatStats> players, CombatUI combatUI)
    {
        // Display attack message in text panel
        if (combatUI != null && combatUI.turnText != null)
        {
            combatUI.DisplayTurnAndActionMessage($"{enemy.characterName} whispers crippling doubts into your mind!");
        }
        
        // Display specific skill name in action display label
        if (combatUI != null)
        {
            combatUI.DisplayActionLabel("Crippling Doubt");
        }
        
        // Wait for action display to complete
        yield return WaitForActionDisplay();
        
        // Get all living players
        var livingPlayers = players.Where(p => !p.IsDead()).ToList();
        if (livingPlayers.Count == 0) yield break;
        
        // Find random player as target
        int randomIndex = Random.Range(0, livingPlayers.Count);
        var target = livingPlayers[randomIndex];
        
        // Deal 15-30 damage
        float damage = Random.Range(15f, 30.1f); // 30.1 to include 30 in the range
        
        // Apply the enemy's attack multiplier
        float calculatedDamage = enemy.CalculateDamage(damage);
        
        // Round to whole number
        int finalDamage = Mathf.FloorToInt(calculatedDamage);
        
        // Apply damage
        target.TakeDamage(finalDamage);
        
        // Get status manager to apply WEAK and VULNERABLE
        StatusManager statusManager = StatusManager.Instance;
        if (statusManager != null)
        {
            // Apply WEAK status (reduces attack)
            statusManager.ApplyStatus(target, StatusType.Weakness, 2); // Apply for 2 turns
            Debug.Log($"[Crippling Doubt] Applied Weakness status to {target.characterName} for 2 turns");
            
            // Apply VULNERABLE status (increases damage taken)
            statusManager.ApplyStatus(target, StatusType.Vulnerable, 2); // Apply for 2 turns
            Debug.Log($"[Crippling Doubt] Applied Vulnerable status to {target.characterName} for 2 turns");
        }
        else
        {
            // Fallback to old system if status manager not available
            target.attackMultiplier = 0.5f; // 50% less damage dealt
            target.defenseMultiplier = 1.5f; // 50% more damage taken
            Debug.LogWarning($"[Crippling Doubt] StatusManager not found, using legacy stat modifiers for {target.characterName}");
        }
        
        Debug.Log($"Crippling Doubt hit {target.characterName} for {finalDamage} damage and applied WEAK and VULNERABLE status");
    }
    
    private IEnumerator UseMaliceOfStoneSkill(CombatStats enemy, List<CombatStats> players, CombatUI combatUI)
    {
        // Display generic attack message in text panel
        if (combatUI != null && combatUI.turnText != null)
        {
            combatUI.DisplayTurnAndActionMessage($"{enemy.characterName} attacks!");
        }
        
        // Display specific skill name in action display label
        if (combatUI != null)
        {
            combatUI.DisplayActionLabel("Malice of Stone");
        }
        
        // Wait for action display to complete
        yield return WaitForActionDisplay();
        
        // Get all living players
        var livingPlayers = players.Where(p => !p.IsDead()).ToList();
        
        // Deal 40-70 random damage to all living players
        foreach (var player in livingPlayers)
        {
            // Random damage between 40-70
            float baseDamage = Random.Range(40f, 70f);
            
            // Round down to whole number
            int finalDamage = Mathf.FloorToInt(baseDamage);
            
            player.TakeDamage(finalDamage);
            Debug.Log($"Malice of Stone hit {player.characterName} for {finalDamage} damage");
        }
    }
} 