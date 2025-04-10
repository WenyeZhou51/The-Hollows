using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Controls the behavior of the Aperature_post enemy in combat (after metamorphosis)
/// </summary>
public class AperatureBehaviorPost : EnemyBehavior
{
    [Header("Skill Probabilities")]
    [Tooltip("Probability of using Tunneled Focus skill (deals 5 damage 4-6 times, decreases target defense by 50%)")]
    [Range(0, 100)]
    public float tunneledFocusChance = 50f;
    
    [Tooltip("Probability of using Cascading Gaze skill (deals 10 damage, doubles future cascading gaze damage)")]
    [Range(0, 100)]
    public float cascadingGazeChance = 50f;
    
    [Header("Tunneled Focus Settings")]
    [Tooltip("Minimum number of hits for Tunneled Focus")]
    [Range(1, 10)]
    public int minTunneledFocusHits = 4;
    
    [Tooltip("Maximum number of hits for Tunneled Focus")]
    [Range(1, 10)]
    public int maxTunneledFocusHits = 6;
    
    [Header("Cascading Gaze Settings")]
    [Tooltip("Base damage for Cascading Gaze")]
    public float cascadingGazeBaseDamage = 10f;
    
    [Header("Status Effect Tracking")]
    // Dictionary to track players affected by defense reduction
    private Dictionary<CombatStats, bool> defenseReducedPlayers = new Dictionary<CombatStats, bool>();
    
    // Dictionary to track players' Cascading Gaze damage multiplier
    private Dictionary<CombatStats, int> cascadingGazeMultipliers = new Dictionary<CombatStats, int>();
    
    // Target player that the Aperature_post will focus on
    private CombatStats focusedTarget = null;

    public override IEnumerator ExecuteTurn(CombatStats enemy, List<CombatStats> players, CombatUI combatUI)
    {
        // Choose a target player if we don't have one yet
        if (focusedTarget == null || focusedTarget.IsDead())
        {
            // Choose a random living player to focus on
            var livingPlayers = players.Where(p => !p.IsDead()).ToList();
            if (livingPlayers.Count > 0)
            {
                int randomIndex = Random.Range(0, livingPlayers.Count);
                focusedTarget = livingPlayers[randomIndex];
                
                // Display focus message
                if (combatUI != null && combatUI.turnText != null)
                {
                    combatUI.DisplayTurnAndActionMessage($"{enemy.characterName} focuses on {focusedTarget.characterName}!");
                }
                
                // Wait for action display to complete
                yield return WaitForActionDisplay();
            }
            else
            {
                // No living players, can't do anything
                yield break;
            }
        }
        
        // Normalize chance values to ensure they add up to 100%
        float totalChance = tunneledFocusChance + cascadingGazeChance;
        if (totalChance <= 0)
        {
            Debug.LogWarning("All Aperature_post skill chances are set to 0, defaulting to basic attack");
            yield return UseBasicAttack(enemy, players, combatUI);
            yield break;
        }
        
        // Calculate normalized probabilities
        float normalizedTunneledFocus = tunneledFocusChance / totalChance * 100f;
        float normalizedCascadingGaze = cascadingGazeChance / totalChance * 100f;
        
        // Roll for skill selection
        float roll = Random.Range(0f, 100f);
        
        if (roll < normalizedTunneledFocus)
        {
            yield return UseTunneledFocusSkill(enemy, players, combatUI);
        }
        else
        {
            yield return UseCascadingGazeSkill(enemy, players, combatUI);
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
        
        // If we have a focused target, attack them
        if (focusedTarget != null && !focusedTarget.IsDead())
        {
            // Base damage
            float baseDamage = 5f;
            
            // Round down to whole number
            int finalDamage = Mathf.FloorToInt(baseDamage);
            
            focusedTarget.TakeDamage(finalDamage);
            
            Debug.Log($"Basic Attack hit {focusedTarget.characterName} for {finalDamage} damage");
        }
        else
        {
            // Focus target is dead, find a new one for next turn
            focusedTarget = null;
        }
    }
    
    private IEnumerator UseTunneledFocusSkill(CombatStats enemy, List<CombatStats> players, CombatUI combatUI)
    {
        // Check if our focus target is still alive
        if (focusedTarget == null || focusedTarget.IsDead())
        {
            // Our target is gone, find a new one next turn
            focusedTarget = null;
            yield return UseBasicAttack(enemy, players, combatUI);
            yield break;
        }
        
        // Display attack message in text panel
        if (combatUI != null && combatUI.turnText != null)
        {
            combatUI.DisplayTurnAndActionMessage($"{enemy.characterName} concentrates its gaze on {focusedTarget.characterName}!");
        }
        
        // Display specific skill name in action display label
        if (combatUI != null)
        {
            combatUI.DisplayActionLabel("Tunneled Focus");
        }
        
        // Wait for action display to complete
        yield return WaitForActionDisplay();
        
        // Determine number of hits (between minTunneledFocusHits and maxTunneledFocusHits)
        int hitCount = Random.Range(minTunneledFocusHits, maxTunneledFocusHits + 1);
        
        // Deal 5 damage for each hit
        float damagePerHit = 5f;
        int finalDamagePerHit = Mathf.FloorToInt(damagePerHit);
        int totalDamage = 0;
        
        // Apply the hits one by one with a small delay between each
        for (int i = 0; i < hitCount; i++)
        {
            // Display hit message
            if (combatUI != null && combatUI.turnText != null)
            {
                combatUI.DisplayTurnAndActionMessage($"Hit {i+1} of {hitCount}!");
            }
            
            // Wait for a moment between hits
            yield return new WaitForSeconds(0.2f);
            
            // Deal damage
            focusedTarget.TakeDamage(finalDamagePerHit);
            totalDamage += finalDamagePerHit;
            
            // Check if target died
            if (focusedTarget.IsDead())
            {
                // Target died, stop attacking
                break;
            }
        }
        
        // Apply defense reduction (50%)
        // Store the fact that this player has reduced defense
        defenseReducedPlayers[focusedTarget] = true;
        
        // If the CombatStats class has a defense reduction method, call it here
        // For this example, we're just using ApplyDefenseReduction which was seen in the CombatStats class
        focusedTarget.ApplyDefenseReduction();
        
        Debug.Log($"Tunneled Focus hit {focusedTarget.characterName} {hitCount} times for a total of {totalDamage} damage and reduced defense by 50%");
    }
    
    private IEnumerator UseCascadingGazeSkill(CombatStats enemy, List<CombatStats> players, CombatUI combatUI)
    {
        // Check if our focus target is still alive
        if (focusedTarget == null || focusedTarget.IsDead())
        {
            // Our target is gone, find a new one next turn
            focusedTarget = null;
            yield return UseBasicAttack(enemy, players, combatUI);
            yield break;
        }
        
        // Display attack message in text panel
        if (combatUI != null && combatUI.turnText != null)
        {
            combatUI.DisplayTurnAndActionMessage($"{enemy.characterName} unleashes a cascading gaze upon {focusedTarget.characterName}!");
        }
        
        // Display specific skill name in action display label
        if (combatUI != null)
        {
            combatUI.DisplayActionLabel("Cascading Gaze");
        }
        
        // Wait for action display to complete
        yield return WaitForActionDisplay();
        
        // Determine number of hits (between minTunneledFocusHits and maxTunneledFocusHits)
        int hitCount = Random.Range(minTunneledFocusHits, maxTunneledFocusHits + 1);
        
        // Calculate damage (5 damage multiplied by the number of hits)
        float damagePerHit = 5f;
        int totalDamage = Mathf.FloorToInt(damagePerHit * hitCount);
        
        // Deal damage all at once
        focusedTarget.TakeDamage(totalDamage);
        
        Debug.Log($"Cascading Gaze hit {focusedTarget.characterName} for {totalDamage} damage ({hitCount} hits at {damagePerHit} damage each)");
    }
} 