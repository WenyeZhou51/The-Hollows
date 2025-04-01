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
    public float triangulationChance = 50f;
    
    [Tooltip("Probability of using Unblinking Gaze skill (drains 50 Mind from target character)")]
    [Range(0, 100)]
    public float unblinkingGazeChance = 40f;
    
    [Tooltip("Probability of using Malice of Stone skill (deals 100 damage to all party members)")]
    [Range(0, 100)]
    public float maliceOfStoneChance = 10f;
    
    public override IEnumerator ExecuteTurn(CombatStats enemy, List<CombatStats> players, CombatUI combatUI)
    {
        // Normalize chance values to ensure they add up to 100%
        float totalChance = triangulationChance + unblinkingGazeChance + maliceOfStoneChance;
        if (totalChance <= 0)
        {
            Debug.LogWarning("All Obelisk skill chances are set to 0, defaulting to basic attack");
            yield return UseBasicAttack(enemy, players, combatUI);
            yield break;
        }
        
        // Calculate normalized probabilities
        float normalizedTriangulation = triangulationChance / totalChance * 100f;
        float normalizedUnblinkingGaze = unblinkingGazeChance / totalChance * 100f;
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
            target.TakeDamage(30f);
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
            target.TakeDamage(damage);
            
            Debug.Log($"Triangulation hit {target.characterName} for {damage} damage");
            
            // Don't remove from list - the same character can be hit multiple times
        }
    }
    
    private IEnumerator UseUnblinkingGazeSkill(CombatStats enemy, List<CombatStats> players, CombatUI combatUI)
    {
        // Display generic attack message in text panel
        if (combatUI != null && combatUI.turnText != null)
        {
            combatUI.DisplayTurnAndActionMessage($"{enemy.characterName} attacks!");
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
        
        // Drain 50 Mind
        float mindDrain = 50f;
        target.UseSanity(mindDrain);
        
        Debug.Log($"Unblinking Gaze drained {mindDrain} Mind from {target.characterName}");
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
        
        // Deal 100 damage to all living players
        foreach (var player in livingPlayers)
        {
            player.TakeDamage(100f);
            Debug.Log($"Malice of Stone hit {player.characterName} for 100 damage");
        }
    }
} 