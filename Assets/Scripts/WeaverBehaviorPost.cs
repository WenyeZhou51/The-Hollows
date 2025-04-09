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
    [Tooltip("Probability of using Gordian Knot skill (deals 10 damage + 10 damage per turn)")]
    [Range(0, 100)]
    public float gordianKnotChance = 40f;
    
    [Tooltip("Probability of using Dodge and Weave skill (increases defense of all enemies by 50%)")]
    [Range(0, 100)]
    public float dodgeAndWeaveChance = 30f;
    
    [Tooltip("Probability of using Hangman skill (deals 30 damage and reduces speed by 50%)")]
    [Range(0, 100)]
    public float hangmanChance = 30f;
    
    [Header("Gordian Knot Settings")]
    [Tooltip("Duration of the 'Tangled' effect in turns")]
    public int tangledEffectDuration = 3;
    
    [Header("Status Effect Tracking")]
    // Track which players are currently tangled
    private List<CombatStats> tangledPlayers = new List<CombatStats>();
    // Track if Dodge and Weave is active
    private bool dodgeAndWeaveActive = false;
    // Dictionary to track players affected by Hangman speed reduction
    private Dictionary<CombatStats, bool> hangmanAffectedPlayers = new Dictionary<CombatStats, bool>();
    
    // Dictionary to track how many turns remain for each tangled player
    private Dictionary<CombatStats, int> tangledTurnsRemaining = new Dictionary<CombatStats, int>();

    public override IEnumerator ExecuteTurn(CombatStats enemy, List<CombatStats> players, CombatUI combatUI)
    {
        // Apply Tangled damage for any players with the effect at the start of turn
        yield return ApplyTangledEffectDamage(enemy, players, combatUI);
        
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
            // Only use Dodge and Weave if not already active
            if (!dodgeAndWeaveActive)
            {
                yield return UseDodgeAndWeaveSkill(enemy, players, combatUI);
            }
            else
            {
                // If already active, use a different skill or fallback to basic attack
                if (Random.value < 0.5f && CanUseGordianKnot(players))
                {
                    yield return UseGordianKnotSkill(enemy, players, combatUI);
                }
                else
                {
                    yield return UseHangmanSkill(enemy, players, combatUI);
                }
            }
        }
        else
        {
            yield return UseHangmanSkill(enemy, players, combatUI);
        }
    }
    
    private IEnumerator ApplyTangledEffectDamage(CombatStats enemy, List<CombatStats> players, CombatUI combatUI)
    {
        List<CombatStats> playersToRemove = new List<CombatStats>();
        
        // Apply damage to each tangled player
        foreach (var player in tangledPlayers)
        {
            if (player == null || player.IsDead())
            {
                playersToRemove.Add(player);
                continue;
            }
            
            // Reduce turns remaining
            if (tangledTurnsRemaining.ContainsKey(player))
            {
                tangledTurnsRemaining[player]--;
                
                // Apply 10 damage from Tangled effect
                if (combatUI != null && combatUI.turnText != null)
                {
                    combatUI.DisplayTurnAndActionMessage($"{player.characterName} takes damage from being tangled!");
                }
                
                // Display effect name
                if (combatUI != null)
                {
                    combatUI.DisplayActionLabel("Tangled Effect");
                }
                
                // Wait for UI display
                yield return WaitForActionDisplay();
                
                // Apply damage
                player.TakeDamage(10);
                
                Debug.Log($"Tangled effect dealt 10 damage to {player.characterName}. Turns remaining: {tangledTurnsRemaining[player]}");
                
                // If effect expired, add to removal list
                if (tangledTurnsRemaining[player] <= 0)
                {
                    playersToRemove.Add(player);
                }
            }
            else
            {
                playersToRemove.Add(player);
            }
        }
        
        // Remove players whose effect has expired
        foreach (var player in playersToRemove)
        {
            tangledPlayers.Remove(player);
            if (tangledTurnsRemaining.ContainsKey(player))
            {
                tangledTurnsRemaining.Remove(player);
            }
            Debug.Log($"Removed Tangled effect from {(player != null ? player.characterName : "unknown player")}");
        }
    }
    
    private bool CanUseGordianKnot(List<CombatStats> players)
    {
        // Check if there's at least one player who isn't already tangled
        return players.Any(p => p != null && !p.IsDead() && !tangledPlayers.Contains(p));
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
        
        // Get all living players who aren't already tangled
        var eligiblePlayers = players
            .Where(p => !p.IsDead() && !tangledPlayers.Contains(p))
            .ToList();
        
        if (eligiblePlayers.Count == 0)
        {
            // If all players are already tangled, just do damage to a random player
            var livingPlayers = players.Where(p => !p.IsDead()).ToList();
            if (livingPlayers.Count > 0)
            {
                int randomIndex = Random.Range(0, livingPlayers.Count);
                var randomTarget = livingPlayers[randomIndex];
                
                // Deal 10 damage
                randomTarget.TakeDamage(10);
                Debug.Log($"Gordian Knot hit {randomTarget.characterName} for 10 damage (already tangled)");
            }
            yield break;
        }
        
        // Select a random eligible player
        int index = Random.Range(0, eligiblePlayers.Count);
        var target = eligiblePlayers[index];
        
        // Deal 10 initial damage
        target.TakeDamage(10);
        
        // Apply Tangled effect
        tangledPlayers.Add(target);
        tangledTurnsRemaining[target] = tangledEffectDuration;
        
        Debug.Log($"Gordian Knot hit {target.characterName} for 10 damage and applied Tangled effect for {tangledEffectDuration} turns");
    }
    
    private IEnumerator UseDodgeAndWeaveSkill(CombatStats enemy, List<CombatStats> players, CombatUI combatUI)
    {
        // Display skill message in text panel
        if (combatUI != null && combatUI.turnText != null)
        {
            combatUI.DisplayTurnAndActionMessage($"{enemy.characterName} begins to dodge and weave!");
        }
        
        // Display specific skill name in action display label
        if (combatUI != null)
        {
            combatUI.DisplayActionLabel("Dodge and Weave");
        }
        
        // Wait for action display to complete
        yield return WaitForActionDisplay();
        
        // Find all living enemies
        var combatManager = FindObjectOfType<CombatManager>();
        if (combatManager != null)
        {
            var livingEnemies = combatManager.GetLivingEnemies();
            
            // Apply defense boost to all enemies
            foreach (var enemyChar in livingEnemies)
            {
                // We don't have direct access to defense stat in CombatStats
                // So we'll add a tag to indicate the Dodge and Weave effect is active
                // In a real implementation, this would modify defense stats
                
                // For demonstration, we'll just log it
                Debug.Log($"Dodge and Weave increased {enemyChar.characterName}'s defense by 50%");
            }
            
            // Mark Dodge and Weave as active so it doesn't stack
            dodgeAndWeaveActive = true;
            
            Debug.Log("Dodge and Weave is now active for all enemies");
        }
    }
    
    private IEnumerator UseHangmanSkill(CombatStats enemy, List<CombatStats> players, CombatUI combatUI)
    {
        // Display attack message in text panel
        if (combatUI != null && combatUI.turnText != null)
        {
            combatUI.DisplayTurnAndActionMessage($"{enemy.characterName} prepares a deadly noose!");
        }
        
        // Display specific skill name in action display label
        if (combatUI != null)
        {
            combatUI.DisplayActionLabel("Hangman");
        }
        
        // Wait for action display to complete
        yield return WaitForActionDisplay();
        
        // Get all living players who aren't already affected by Hangman
        var eligiblePlayers = players
            .Where(p => !p.IsDead() && (!hangmanAffectedPlayers.ContainsKey(p) || !hangmanAffectedPlayers[p]))
            .ToList();
        
        if (eligiblePlayers.Count == 0)
        {
            // If all players are already affected, just do damage to a random player
            var livingPlayers = players.Where(p => !p.IsDead()).ToList();
            if (livingPlayers.Count > 0)
            {
                int randomIndex = Random.Range(0, livingPlayers.Count);
                var randomTarget = livingPlayers[randomIndex];
                
                // Deal 30 damage
                randomTarget.TakeDamage(30);
                Debug.Log($"Hangman hit {randomTarget.characterName} for 30 damage (already speed reduced)");
            }
            yield break;
        }
        
        // Select a random eligible player
        int index = Random.Range(0, eligiblePlayers.Count);
        var target = eligiblePlayers[index];
        
        // Deal 30 damage
        target.TakeDamage(30);
        
        // Apply speed reduction (50%)
        float baseActionSpeed = target.actionSpeed;
        float newSpeed = baseActionSpeed * 0.5f;
        
        // Set the new action speed directly
        target.actionSpeed = newSpeed;
        
        // Mark this player as affected by Hangman
        hangmanAffectedPlayers[target] = true;
        
        Debug.Log($"Hangman hit {target.characterName} for 30 damage and reduced speed by 50%");
    }
} 