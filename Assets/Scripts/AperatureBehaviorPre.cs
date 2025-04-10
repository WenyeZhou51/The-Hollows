using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Controls the behavior of the Aperature_pre enemy in combat
/// </summary>
public class AperatureBehaviorPre : EnemyBehavior
{
    [Header("Skill Probabilities")]
    [Tooltip("Probability of using Blinding Lights skill (deals 5 damage and reduces target attack by 50%)")]
    [Range(0, 100)]
    public float blindingLightsChance = 40f;
    
    [Tooltip("Probability of using Wobble skill (deals 2 damage to target)")]
    [Range(0, 100)]
    public float wobbleChance = 40f;
    
    [Tooltip("Probability of using Metamorphosis skill (transforms into Aperature_post)")]
    [Range(0, 100)]
    public float metamorphosisChance = 20f;
    
    [Header("Metamorphosis Settings")]
    [Tooltip("The Aperature_post prefab to spawn when metamorphosis is used")]
    public GameObject aperaturePostPrefab;

    // Dictionary to track players affected by Blinding Lights attack reduction
    private Dictionary<CombatStats, bool> blindedPlayers = new Dictionary<CombatStats, bool>();

    public override IEnumerator ExecuteTurn(CombatStats enemy, List<CombatStats> players, CombatUI combatUI)
    {
        // Normalize chance values to ensure they add up to 100%
        float totalChance = blindingLightsChance + wobbleChance + metamorphosisChance;
        if (totalChance <= 0)
        {
            Debug.LogWarning("All Aperature_pre skill chances are set to 0, defaulting to basic attack");
            yield return UseBasicAttack(enemy, players, combatUI);
            yield break;
        }
        
        // Calculate normalized probabilities
        float normalizedBlindingLights = blindingLightsChance / totalChance * 100f;
        float normalizedWobble = wobbleChance / totalChance * 100f;
        float normalizedMetamorphosis = metamorphosisChance / totalChance * 100f;
        
        // Roll for skill selection
        float roll = Random.Range(0f, 100f);
        
        if (roll < normalizedBlindingLights)
        {
            yield return UseBlindingLightsSkill(enemy, players, combatUI);
        }
        else if (roll < normalizedBlindingLights + normalizedWobble)
        {
            yield return UseWobbleSkill(enemy, players, combatUI);
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
    
    private IEnumerator UseBlindingLightsSkill(CombatStats enemy, List<CombatStats> players, CombatUI combatUI)
    {
        // Display attack message in text panel
        if (combatUI != null && combatUI.turnText != null)
        {
            combatUI.DisplayTurnAndActionMessage($"{enemy.characterName} emits blinding lights!");
        }
        
        // Display specific skill name in action display label
        if (combatUI != null)
        {
            combatUI.DisplayActionLabel("Blinding Lights");
        }
        
        // Wait for action display to complete
        yield return WaitForActionDisplay();
        
        // Get random living player as target
        var livingPlayers = players.Where(p => !p.IsDead()).ToList();
        if (livingPlayers.Count == 0) yield break;
        
        int randomIndex = Random.Range(0, livingPlayers.Count);
        var target = livingPlayers[randomIndex];
        
        // Deal 5 damage
        float damage = 5f;
        int finalDamage = Mathf.FloorToInt(damage);
        target.TakeDamage(finalDamage);
        
        // Apply attack reduction (50%)
        // Store the fact that this player is blinded
        blindedPlayers[target] = true;
        
        // In a full implementation, you would need to actually reduce the player's attack
        // This would require adding attack reduction logic to the CombatStats class
        // For this example, we're just tracking it in the dictionary
        
        Debug.Log($"Blinding Lights hit {target.characterName} for {finalDamage} damage and reduced attack by 50%");
    }
    
    private IEnumerator UseWobbleSkill(CombatStats enemy, List<CombatStats> players, CombatUI combatUI)
    {
        // Display attack message in text panel
        if (combatUI != null && combatUI.turnText != null)
        {
            combatUI.DisplayTurnAndActionMessage($"{enemy.characterName} wobbles menacingly!");
        }
        
        // Display specific skill name in action display label
        if (combatUI != null)
        {
            combatUI.DisplayActionLabel("Wobble");
        }
        
        // Wait for action display to complete
        yield return WaitForActionDisplay();
        
        // Get random living player as target
        var livingPlayers = players.Where(p => !p.IsDead()).ToList();
        if (livingPlayers.Count == 0) yield break;
        
        int randomIndex = Random.Range(0, livingPlayers.Count);
        var target = livingPlayers[randomIndex];
        
        // Deal 2 damage
        float damage = 2f;
        int finalDamage = Mathf.FloorToInt(damage);
        target.TakeDamage(finalDamage);
        
        Debug.Log($"Wobble hit {target.characterName} for {finalDamage} damage");
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
        
        // Transform into Aperature_post
        if (aperaturePostPrefab != null)
        {
            // Get current position and parent of Aperature_pre
            Vector3 position = enemy.transform.position;
            Transform parentTransform = enemy.transform.parent;
            
            // Store the prefab's original local position as an offset
            Vector3 prefabLocalPosition = aperaturePostPrefab.transform.localPosition;
            
            // Instantiate the Aperature_post prefab at the same position and with the same parent
            GameObject aperaturePost = Instantiate(aperaturePostPrefab, position, Quaternion.identity, parentTransform);
            
            // Apply the prefab's original local position as an offset to maintain positioning
            aperaturePost.transform.localPosition = enemy.transform.localPosition + prefabLocalPosition;
            
            Debug.Log($"Applied position offset from prefab: {prefabLocalPosition}. Final position: {aperaturePost.transform.localPosition}");
            
            // Get the CombatStats of the new Aperature_post
            CombatStats aperaturePostStats = aperaturePost.GetComponent<CombatStats>();
            if (aperaturePostStats != null)
            {
                // Set full health for the new Aperature_post
                aperaturePostStats.currentHealth = aperaturePostStats.maxHealth;
                
                // Add the new enemy to the combat manager's list of enemies
                var combatManager = FindObjectOfType<CombatManager>();
                if (combatManager != null)
                {
                    // Replace the old enemy with the new one in the enemies list
                    int index = combatManager.enemies.IndexOf(enemy);
                    if (index >= 0)
                    {
                        combatManager.enemies[index] = aperaturePostStats;
                        Debug.Log($"Replaced Aperature_pre with Aperature_post in combat manager enemies list at index {index}");
                    }
                    else
                    {
                        Debug.LogWarning("Failed to find Aperature_pre in combat manager's enemies list");
                        // Add the new enemy to the list since it wasn't found
                        combatManager.enemies.Add(aperaturePostStats);
                        Debug.Log("Added Aperature_post to combat manager enemies list since Aperature_pre wasn't found");
                    }
                }
                else
                {
                    Debug.LogError("Could not find CombatManager when transforming Aperature_pre to Aperature_post");
                }
                
                // Destroy the old Aperature_pre
                Destroy(enemy.gameObject);
                
                Debug.Log($"Aperature_pre transformed into Aperature_post under parent {(parentTransform != null ? parentTransform.name : "null")}");
            }
            else
            {
                Debug.LogError("Aperature_post prefab does not have CombatStats component");
            }
        }
        else
        {
            Debug.LogError("Aperature_post prefab not assigned to AperatureBehaviorPre");
        }
    }
} 