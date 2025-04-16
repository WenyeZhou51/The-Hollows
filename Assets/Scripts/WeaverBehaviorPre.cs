using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Controls the behavior of the Weaver_pre enemy in combat
/// </summary>
public class WeaverBehaviorPre : EnemyBehavior
{
    [Header("Skill Probabilities")]
    [Tooltip("Probability of using Tangle skill (deals 4-7 damage and applies SLOW status)")]
    [Range(0, 100)]
    public float tangleChance = 40f;
    
    [Tooltip("Probability of using Poke skill (deals 7-12 damage to target)")]
    [Range(0, 100)]
    public float pokeChance = 30f;
    
    [Tooltip("Probability of using Connect skill (gives STRENGTH to a random ally or self)")]
    [Range(0, 100)]
    public float connectChance = 20f;
    
    [Tooltip("Probability of using Metamorphosis skill (transforms into Weaver_post)")]
    [Range(0, 100)]
    public float metamorphosisChance = 10f;
    
    [Header("Metamorphosis Settings")]
    [Tooltip("The Weaver_post prefab to spawn when metamorphosis is used")]
    public GameObject weaverPostPrefab;

    public override IEnumerator ExecuteTurn(CombatStats enemy, List<CombatStats> players, CombatUI combatUI)
    {
        // Normalize chance values to ensure they add up to 100%
        float totalChance = tangleChance + pokeChance + connectChance + metamorphosisChance;
        if (totalChance <= 0)
        {
            Debug.LogWarning("All Weaver_pre skill chances are set to 0, defaulting to basic attack");
            yield return UseBasicAttack(enemy, players, combatUI);
            yield break;
        }
        
        // Calculate normalized probabilities
        float normalizedTangle = tangleChance / totalChance * 100f;
        float normalizedPoke = pokeChance / totalChance * 100f;
        float normalizedConnect = connectChance / totalChance * 100f;
        float normalizedMetamorphosis = metamorphosisChance / totalChance * 100f;
        
        // Roll for skill selection
        float roll = Random.Range(0f, 100f);
        
        if (roll < normalizedTangle)
        {
            yield return UseTangleSkill(enemy, players, combatUI);
        }
        else if (roll < normalizedTangle + normalizedPoke)
        {
            yield return UsePokeSkill(enemy, players, combatUI);
        }
        else if (roll < normalizedTangle + normalizedPoke + normalizedConnect)
        {
            yield return UseConnectSkill(enemy, players, combatUI);
        }
        else
        {
            yield return UseMetamorphosisSkill(enemy, players, combatUI);
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
            float baseDamage = 5f;
            
            // Apply the enemy's attack multiplier
            float calculatedDamage = enemy.CalculateDamage(baseDamage);
            
            // Round down to whole number
            int finalDamage = Mathf.FloorToInt(calculatedDamage);
            
            target.TakeDamage(finalDamage);
        }
    }
    
    private IEnumerator UseTangleSkill(CombatStats enemy, List<CombatStats> players, CombatUI combatUI)
    {
        // Display generic attack message in text panel
        if (combatUI != null && combatUI.turnText != null)
        {
            combatUI.DisplayTurnAndActionMessage($"{enemy.characterName} prepares to entangle you!");
        }
        
        // Display specific skill name in action display label
        if (combatUI != null)
        {
            combatUI.DisplayActionLabel("Tangle");
        }
        
        // Wait for action display to complete
        yield return WaitForActionDisplay();
        
        // Get random living player as target
        var livingPlayers = players.Where(p => !p.IsDead()).ToList();
        if (livingPlayers.Count == 0) yield break;
        
        int randomIndex = Random.Range(0, livingPlayers.Count);
        var target = livingPlayers[randomIndex];
        
        // Deal 4-7 damage
        float damage = Random.Range(4f, 7.1f); // 7.1 to include 7 in the range
        
        // Apply the enemy's attack multiplier
        float calculatedDamage = enemy.CalculateDamage(damage);
        
        // Round to whole number
        int finalDamage = Mathf.FloorToInt(calculatedDamage);
        
        // Apply damage
        target.TakeDamage(finalDamage);
        
        // Apply SLOW status to target
        StatusManager statusManager = StatusManager.Instance;
        if (statusManager != null)
        {
            statusManager.ApplyStatus(target, StatusType.Slowed, 2); // Apply slow for 2 turns
            Debug.Log($"[Tangle] Applied Slowed status to {target.characterName} for 2 turns");
        }
        else
        {
            // Fallback to old system if status manager not available
            // Store original action speed if this is the first debuff
            float baseActionSpeed = target.actionSpeed;
            float newSpeed = baseActionSpeed * 0.5f; // 50% reduction
            
            // Set the new action speed directly
            target.actionSpeed = newSpeed;
            Debug.LogWarning("[Tangle] StatusManager not found, using legacy speed reduction system");
        }
        
        Debug.Log($"Tangle hit {target.characterName} for {finalDamage} damage and applied SLOW status");
    }
    
    private IEnumerator UsePokeSkill(CombatStats enemy, List<CombatStats> players, CombatUI combatUI)
    {
        // Display generic attack message in text panel
        if (combatUI != null && combatUI.turnText != null)
        {
            combatUI.DisplayTurnAndActionMessage($"{enemy.characterName} readies a sharp poke!");
        }
        
        // Display specific skill name in action display label
        if (combatUI != null)
        {
            combatUI.DisplayActionLabel("Poke");
        }
        
        // Wait for action display to complete
        yield return WaitForActionDisplay();
        
        // Get random living player as target
        var livingPlayers = players.Where(p => !p.IsDead()).ToList();
        if (livingPlayers.Count == 0) yield break;
        
        int randomIndex = Random.Range(0, livingPlayers.Count);
        var target = livingPlayers[randomIndex];
        
        // Deal 7-12 damage
        float damage = Random.Range(7f, 12.1f); // 12.1 to include 12 in the range
        
        // Apply the enemy's attack multiplier
        float calculatedDamage = enemy.CalculateDamage(damage);
        
        // Round to whole number
        int finalDamage = Mathf.FloorToInt(calculatedDamage);
        
        // Apply damage
        target.TakeDamage(finalDamage);
        
        Debug.Log($"Poke hit {target.characterName} for {finalDamage} damage");
    }
    
    private IEnumerator UseConnectSkill(CombatStats enemy, List<CombatStats> players, CombatUI combatUI)
    {
        // Display ability message in text panel
        if (combatUI != null && combatUI.turnText != null)
        {
            combatUI.DisplayTurnAndActionMessage($"{enemy.characterName} weaves a strengthening thread!");
        }
        
        // Display specific skill name in action display label
        if (combatUI != null)
        {
            combatUI.DisplayActionLabel("Connect");
        }
        
        // Wait for action display to complete
        yield return WaitForActionDisplay();
        
        // Get all living enemies (potential allies)
        var allEnemies = GameObject.FindObjectsOfType<CombatStats>()
            .Where(e => e.isEnemy && !e.IsDead() && e != enemy) // exclude self for now
            .ToList();
        
        CombatStats target = null;
        
        // Try to find a random ally first
        if (allEnemies.Count > 0)
        {
            int randomIndex = Random.Range(0, allEnemies.Count);
            target = allEnemies[randomIndex];
            
            if (combatUI != null && combatUI.turnText != null)
            {
                combatUI.DisplayTurnAndActionMessage($"{enemy.characterName} connects with {target.characterName}!");
            }
            yield return WaitForActionDisplay();
        }
        else
        {
            // No allies available, buff self
            target = enemy;
            
            if (combatUI != null && combatUI.turnText != null)
            {
                combatUI.DisplayTurnAndActionMessage($"{enemy.characterName} strengthens itself!");
            }
            yield return WaitForActionDisplay();
        }
        
        // Apply STRENGTH status to target
        StatusManager statusManager = StatusManager.Instance;
        if (statusManager != null)
        {
            statusManager.ApplyStatus(target, StatusType.Strength, 2); // Apply for 2 turns
            Debug.Log($"[Connect] Applied Strength status to {target.characterName} for 2 turns");
        }
        else
        {
            // Fallback to direct stat modifier if status manager not available
            target.attackMultiplier = 1.5f; // 50% more damage
            Debug.LogWarning("[Connect] StatusManager not found, using direct attack multiplier increase");
        }
    }
    
    private IEnumerator UseMetamorphosisSkill(CombatStats enemy, List<CombatStats> players, CombatUI combatUI)
    {
        // Display metamorphosis message
        if (combatUI != null && combatUI.turnText != null)
        {
            combatUI.DisplayTurnAndActionMessage($"{enemy.characterName} begins to transform!");
        }
        
        // Display specific skill name in action display label
        if (combatUI != null)
        {
            combatUI.DisplayActionLabel("Metamorphosis");
        }
        
        // Wait for action display to complete
        yield return WaitForActionDisplay();
        
        // Fade to black effect for metamorphosis
        ScreenFader.EnsureExists();
        if (ScreenFader.Instance != null)
        {
            // Fade to black
            yield return ScreenFader.Instance.FadeToBlack();
            
            // Wait a moment at black screen for dramatic effect
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            Debug.LogWarning("ScreenFader not found, proceeding without fade effect");
        }
        
        // Transform into Weaver_post
        if (weaverPostPrefab != null)
        {
            // Get current position and parent of Weaver_pre
            Vector3 position = enemy.transform.position;
            Transform parentTransform = enemy.transform.parent;
            
            // Store the prefab's original local position as an offset
            Vector3 prefabLocalPosition = weaverPostPrefab.transform.localPosition;
            
            // Instantiate the Weaver_post prefab at the same position and with the same parent
            GameObject weaverPost = Instantiate(weaverPostPrefab, position, Quaternion.identity, parentTransform);
            
            // Apply the prefab's original local position as an offset to maintain positioning
            weaverPost.transform.localPosition = enemy.transform.localPosition + prefabLocalPosition;
            
            Debug.Log($"Applied position offset from prefab: {prefabLocalPosition}. Final position: {weaverPost.transform.localPosition}");
            
            // Get the CombatStats of the new Weaver_post
            CombatStats weaverPostStats = weaverPost.GetComponent<CombatStats>();
            if (weaverPostStats != null)
            {
                // Set full health for the new Weaver_post
                weaverPostStats.currentHealth = weaverPostStats.maxHealth;
                
                // Add the new enemy to the combat manager's list of enemies
                var combatManager = FindObjectOfType<CombatManager>();
                if (combatManager != null)
                {
                    // Replace the old enemy with the new one in the enemies list
                    int index = combatManager.enemies.IndexOf(enemy);
                    if (index >= 0)
                    {
                        combatManager.enemies[index] = weaverPostStats;
                        Debug.Log($"Replaced Weaver_pre with Weaver_post in combat manager enemies list at index {index}");
                    }
                    else
                    {
                        Debug.LogWarning("Failed to find Weaver_pre in combat manager's enemies list");
                        // Add the new enemy to the list since it wasn't found
                        combatManager.enemies.Add(weaverPostStats);
                        Debug.Log("Added Weaver_post to combat manager enemies list since Weaver_pre wasn't found");
                    }
                }
                else
                {
                    Debug.LogError("Could not find CombatManager when transforming Weaver_pre to Weaver_post");
                }
                
                // Destroy the old Weaver_pre
                Destroy(enemy.gameObject);
                
                Debug.Log($"Weaver_pre transformed into Weaver_post under parent {(parentTransform != null ? parentTransform.name : "null")}");
            }
            else
            {
                Debug.LogError("Weaver_post prefab does not have CombatStats component");
            }
        }
        else
        {
            Debug.LogError("Weaver_post prefab not assigned to WeaverBehaviorPre");
        }
        
        // Fade back from black to show the transformation
        if (ScreenFader.Instance != null)
        {
            yield return ScreenFader.Instance.FadeFromBlack();
        }
    }
} 