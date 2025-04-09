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
    [Tooltip("Probability of using Tangle skill (deals 4 damage and reduces speed by 50%)")]
    [Range(0, 100)]
    public float tangleChance = 40f;
    
    [Tooltip("Probability of using Poke skill (deals 10 damage to target)")]
    [Range(0, 100)]
    public float pokeChance = 50f;
    
    [Tooltip("Probability of using Metamorphosis skill (transforms into Weaver_post)")]
    [Range(0, 100)]
    public float metamorphosisChance = 10f;
    
    [Header("Metamorphosis Settings")]
    [Tooltip("The Weaver_post prefab to spawn when metamorphosis is used")]
    public GameObject weaverPostPrefab;

    public override IEnumerator ExecuteTurn(CombatStats enemy, List<CombatStats> players, CombatUI combatUI)
    {
        // Normalize chance values to ensure they add up to 100%
        float totalChance = tangleChance + pokeChance + metamorphosisChance;
        if (totalChance <= 0)
        {
            Debug.LogWarning("All Weaver_pre skill chances are set to 0, defaulting to basic attack");
            yield return UseBasicAttack(enemy, players, combatUI);
            yield break;
        }
        
        // Calculate normalized probabilities
        float normalizedTangle = tangleChance / totalChance * 100f;
        float normalizedPoke = pokeChance / totalChance * 100f;
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
            
            // Round down to whole number
            int finalDamage = Mathf.FloorToInt(baseDamage);
            
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
        
        // Deal 4 damage
        float damage = 4f;
        int finalDamage = Mathf.FloorToInt(damage);
        target.TakeDamage(finalDamage);
        
        // Apply speed reduction (50%)
        // Store original action speed if this is the first debuff
        float baseActionSpeed = target.actionSpeed;
        float newSpeed = baseActionSpeed * 0.5f; // 50% reduction
        
        // Set the new action speed directly
        target.actionSpeed = newSpeed;
        
        Debug.Log($"Tangle hit {target.characterName} for {finalDamage} damage and reduced speed by 50%");
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
        
        // Deal 10 damage
        float damage = 10f;
        int finalDamage = Mathf.FloorToInt(damage);
        target.TakeDamage(finalDamage);
        
        Debug.Log($"Poke hit {target.characterName} for {finalDamage} damage");
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
    }
} 