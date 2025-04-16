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
            
            // Apply the enemy's attack multiplier
            float calculatedDamage = enemy.CalculateDamage(baseDamage);
            
            // Round to whole number
            int finalDamage = Mathf.FloorToInt(calculatedDamage);
            
            Debug.Log($"[COMBAT] {enemy.name} basic attack with base damage: {baseDamage}, attackMultiplier: {enemy.attackMultiplier}, final damage: {finalDamage}");
            
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
        
        // Deal 10-20 random damage
        float damage = Random.Range(10f, 20.1f); // 20.1 to include 20 in the range
        
        // Apply the enemy's attack multiplier
        float calculatedDamage = enemy.CalculateDamage(damage);
        
        // Round to whole number
        int finalDamage = Mathf.FloorToInt(calculatedDamage);
        
        Debug.Log($"[COMBAT] {enemy.name} Tunneled Focus with base damage: {damage}, attackMultiplier: {enemy.attackMultiplier}, final damage: {finalDamage}");
        
        // Apply damage
        focusedTarget.TakeDamage(finalDamage);
        
        Debug.Log($"Tunneled Focus hit {focusedTarget.characterName} for {finalDamage} damage");
        
        // Apply VULNERABLE status (50% more damage taken)
        StatusManager statusManager = StatusManager.Instance;
        if (statusManager != null)
        {
            statusManager.ApplyStatus(focusedTarget, StatusType.Vulnerable, 2); // Apply for 2 turns
            Debug.Log($"[Tunneled Focus] Applied Vulnerable status to {focusedTarget.characterName} for 2 turns");
        }
        else
        {
            // Fallback to old system if status manager not available
            defenseReducedPlayers[focusedTarget] = true;
            focusedTarget.ApplyDefenseReduction();
            Debug.LogWarning("[Tunneled Focus] StatusManager not found, using legacy defense reduction system instead");
        }
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
        
        // Check if target has Vulnerable status
        bool isTargetVulnerable = false;
        StatusManager statusManager = StatusManager.Instance;
        if (statusManager != null)
        {
            isTargetVulnerable = statusManager.HasStatus(focusedTarget, StatusType.Vulnerable);
            Debug.Log($"[Cascading Gaze] Target {focusedTarget.characterName} vulnerability status: {isTargetVulnerable}");
        }
        else
        {
            // Fall back to legacy system
            isTargetVulnerable = defenseReducedPlayers.ContainsKey(focusedTarget) && defenseReducedPlayers[focusedTarget];
            Debug.LogWarning("[Cascading Gaze] StatusManager not found, using legacy system to check vulnerability");
        }
        
        // Fixed at 5 hits
        int hitCount = 5;
        int totalDamage = 0;
        
        // Determine damage range based on vulnerability
        float minDamage, maxDamage;
        if (isTargetVulnerable) {
            // 7-10 damage if vulnerable (before vulnerability modifier)
            minDamage = 7f;
            maxDamage = 10.1f; // Add 0.1 to include 10 in the range
        } else {
            // 2-4 damage if not vulnerable
            minDamage = 2f;
            maxDamage = 4.1f; // Add 0.1 to include 4 in the range
        }
        
        Debug.Log($"[Cascading Gaze] Using damage range {minDamage}-{maxDamage-0.1f} for {hitCount} hits");
        
        // Process each hit individually without pausing
        for (int i = 0; i < hitCount; i++)
        {
            // Random damage within the appropriate range
            float damage = Random.Range(minDamage, maxDamage);
            
            // Apply enemy's attack multiplier
            float calculatedDamage = enemy.CalculateDamage(damage);
            
            // Round to whole number and apply damage normally
            // Let the TakeDamage method handle the defense multiplier
            int finalDamage = Mathf.FloorToInt(calculatedDamage);
            focusedTarget.TakeDamage(finalDamage);
            
            // Add to total for logging
            totalDamage += finalDamage;
            
            Debug.Log($"[Cascading Gaze] Hit {i+1}: {finalDamage} damage");
            
            // Brief pause between hits (very short)
            yield return new WaitForSeconds(0.1f);
        }
        
        // Display total damage summary after all hits
        if (combatUI != null && combatUI.turnText != null)
        {
            combatUI.DisplayTurnAndActionMessage($"Cascading Gaze hit {focusedTarget.characterName} {hitCount} times for a total of {totalDamage} damage!");
        }
        
        Debug.Log($"[Cascading Gaze] Complete attack on {focusedTarget.characterName}: {hitCount} hits for {totalDamage} total damage");
    }
} 