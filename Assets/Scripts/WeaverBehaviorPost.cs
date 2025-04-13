using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Controls the behavior of the Weaver_post enemy in combat (after metamorphosis)
/// </summary>
public class WeaverBehaviorPost : EnemyBehavior
{
    [Header("Skill Probabilities")]
    [Tooltip("Probability of using Gordian Knot skill (deals 20-30 damage to all players and SLOW all players)")]
    [Range(0, 100)]
    public float gordianKnotChance = 40f;
    
    [Tooltip("Probability of using Dodge and Weave skill (gives TOUGH to all allies and heals them for 5)")]
    [Range(0, 100)]
    public float dodgeAndWeaveChance = 30f;
    
    [Tooltip("Probability of using Hangman skill (deals 30-35 damage to target and applies VULNERABLE)")]
    [Range(0, 100)]
    public float hangmanChance = 30f;
    
    [Header("Status Effect Tracking")]
    // Dictionary to track players affected by Hangman vulnerability
    private Dictionary<CombatStats, bool> hangmanAffectedPlayers = new Dictionary<CombatStats, bool>();
    // Track if Dodge and Weave is active
    private bool dodgeAndWeaveActive = false;

    public override IEnumerator ExecuteTurn(CombatStats enemy, List<CombatStats> players, CombatUI combatUI)
    {
        // Normalize chance values to ensure they add up to 100%
        float totalChance = gordianKnotChance + dodgeAndWeaveChance + hangmanChance;
        if (totalChance <= 0)
        {
            Debug.LogWarning("All Weaver_post skill chances are set to 0, defaulting to basic attack");
            yield return UseBasicAttack(enemy, players, combatUI);
            yield break;
        }
        
        // Calculate normalized probabilities
        float normalizedGordianKnot = gordianKnotChance / totalChance * 100f;
        float normalizedDodgeAndWeave = dodgeAndWeaveChance / totalChance * 100f;
        float normalizedHangman = hangmanChance / totalChance * 100f;
        
        // Roll for skill selection
        float roll = Random.Range(0f, 100f);
        
        if (roll < normalizedGordianKnot)
        {
            yield return UseGordianKnotSkill(enemy, players, combatUI);
        }
        else if (roll < normalizedGordianKnot + normalizedDodgeAndWeave)
        {
            yield return UseDodgeAndWeaveSkill(enemy, players, combatUI);
        }
        else
        {
            yield return UseHangmanSkill(enemy, players, combatUI);
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
            float baseDamage = 8f;
            
            // Round down to whole number
            int finalDamage = Mathf.FloorToInt(baseDamage);
            
            target.TakeDamage(finalDamage);
        }
    }
    
    private IEnumerator UseGordianKnotSkill(CombatStats enemy, List<CombatStats> players, CombatUI combatUI)
    {
        // Display attack message in text panel
        if (combatUI != null && combatUI.turnText != null)
        {
            combatUI.DisplayTurnAndActionMessage($"{enemy.characterName} weaves an intricate knot!");
        }
        
        // Display specific skill name in action display label
        if (combatUI != null)
        {
            combatUI.DisplayActionLabel("Gordian Knot");
        }
        
        // Wait for action display to complete
        yield return WaitForActionDisplay();
        
        // Get all living players
        var livingPlayers = players.Where(p => !p.IsDead()).ToList();
        if (livingPlayers.Count == 0) yield break;
        
        // Get status manager
        StatusManager statusManager = StatusManager.Instance;
        
        // Deal 20-30 damage to ALL players
        foreach (var player in livingPlayers)
        {
            // Generate random damage for each player
            float damage = Random.Range(20f, 30.1f); // 30.1 to include 30 in the range
            
            // Apply the enemy's attack multiplier
            float calculatedDamage = enemy.CalculateDamage(damage);
            
            // Round to whole number
            int finalDamage = Mathf.FloorToInt(calculatedDamage);
            
            // Apply damage
            player.TakeDamage(finalDamage);
            
            // Apply SLOW status to all players
            if (statusManager != null)
            {
                statusManager.ApplyStatus(player, StatusType.Slowed, 2); // Apply for 2 turns
                Debug.Log($"[Gordian Knot] Hit {player.characterName} for {finalDamage} damage and applied Slowed status for 2 turns");
            }
            else
            {
                // Fallback to old system if status manager not available
                float baseActionSpeed = player.actionSpeed;
                float newSpeed = baseActionSpeed * 0.5f; // 50% reduction
                player.actionSpeed = newSpeed;
                Debug.LogWarning($"[Gordian Knot] StatusManager not found, using legacy speed reduction for {player.characterName}");
            }
        }
        
        Debug.Log($"Gordian Knot hit all players for 20-30 damage and applied SLOW status");
    }
    
    private IEnumerator UseDodgeAndWeaveSkill(CombatStats enemy, List<CombatStats> players, CombatUI combatUI)
    {
        // Display skill message in text panel
        if (combatUI != null && combatUI.turnText != null)
        {
            combatUI.DisplayTurnAndActionMessage($"{enemy.characterName} weaves a protective pattern!");
        }
        
        // Display specific skill name in action display label
        if (combatUI != null)
        {
            combatUI.DisplayActionLabel("Dodge and Weave");
        }
        
        // Wait for action display to complete
        yield return WaitForActionDisplay();
        
        // Get status manager
        StatusManager statusManager = StatusManager.Instance;
        
        // Get all living enemies (allies of this enemy)
        var allEnemies = GameObject.FindObjectsOfType<CombatStats>()
            .Where(e => e.isEnemy && !e.IsDead())
            .ToList();
        
        // Apply Tough status to all enemies and heal them for 5
        foreach (var enemyChar in allEnemies)
        {
            // Heal all allies for 5 HP
            enemyChar.HealHealth(5f);
            
            if (statusManager != null)
            {
                // Apply TOUGH status with the new system
                statusManager.ApplyStatus(enemyChar, StatusType.Tough, 2);
                Debug.Log($"[Dodge and Weave] Applied Tough status to {enemyChar.characterName} for 2 turns and healed for 5 HP");
            }
            else
            {
                // Legacy implementation
                // Set a flag that dodge and weave is active
                dodgeAndWeaveActive = true;
                enemyChar.defenseMultiplier = 0.5f; // Take 50% less damage
                Debug.LogWarning($"[Dodge and Weave] StatusManager not found, using legacy defense boost for {enemyChar.characterName}");
            }
        }
        
        Debug.Log($"Dodge and Weave healed all allies for 5 HP and applied TOUGH status");
    }
    
    private IEnumerator UseHangmanSkill(CombatStats enemy, List<CombatStats> players, CombatUI combatUI)
    {
        // Display attack message in text panel
        if (combatUI != null && combatUI.turnText != null)
        {
            combatUI.DisplayTurnAndActionMessage($"{enemy.characterName} weaves a crippling pattern!");
        }
        
        // Display specific skill name in action display label
        if (combatUI != null)
        {
            combatUI.DisplayActionLabel("Hangman");
        }
        
        // Wait for action display to complete
        yield return WaitForActionDisplay();
        
        // Get all living players
        var livingPlayers = players.Where(p => !p.IsDead()).ToList();
        if (livingPlayers.Count == 0) yield break;
        
        // Find player with highest action
        var target = livingPlayers.OrderByDescending(p => p.currentAction).First();
        
        // Get status manager
        StatusManager statusManager = StatusManager.Instance;
        
        // Deal 30-35 damage
        float damage = Random.Range(30f, 35.1f); // 35.1 to include 35 in the range
        
        // Apply the enemy's attack multiplier
        float calculatedDamage = enemy.CalculateDamage(damage);
        
        // Round to whole number
        int finalDamage = Mathf.FloorToInt(calculatedDamage);
        
        // Apply damage
        target.TakeDamage(finalDamage);
        
        // Apply VULNERABLE status
        if (statusManager != null)
        {
            // Apply Vulnerable status with the new system
            statusManager.ApplyStatus(target, StatusType.Vulnerable, 2);
            Debug.Log($"[Hangman] Hit {target.characterName} for {finalDamage} damage and applied Vulnerable status for 2 turns");
        }
        else
        {
            // Legacy implementation
            hangmanAffectedPlayers[target] = true;
            target.defenseMultiplier = 1.5f; // Take 50% more damage
            Debug.LogWarning($"[Hangman] StatusManager not found, using legacy vulnerability for {target.characterName}");
        }
        
        Debug.Log($"Hangman hit {target.characterName} for {finalDamage} damage and applied VULNERABLE status");
    }
} 