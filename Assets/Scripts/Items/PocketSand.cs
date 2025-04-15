using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PocketSand", menuName = "Items/Pocket Sand")]
public class PocketSand : BaseItem
{
    [SerializeField] private int effectDuration = 3; // turns
    
    private void OnEnable()
    {
        Name = "Pocket Sand";
        Description = "WEAKENS all target enemies";
        RequiresTarget = true;
        Type = ItemData.ItemType.Consumable;
    }
    
    public override void Use(CombatStats user, CombatStats target = null)
    {
        if (target != null && target.isEnemy)
        {
            // Get all enemies in combat
            List<CombatStats> allEnemies = new List<CombatStats>();
            
            // Find the combat manager to get all enemies
            CombatManager combatManager = FindObjectOfType<CombatManager>();
            if (combatManager != null)
            {
                allEnemies = combatManager.GetLivingEnemies();
            }
            else
            {
                // Fallback to finding enemies in scene if combat manager not found
                CombatStats[] foundEnemies = FindObjectsOfType<CombatStats>();
                foreach (CombatStats enemy in foundEnemies)
                {
                    if (enemy.isEnemy)
                    {
                        allEnemies.Add(enemy);
                    }
                }
            }
            
            // Apply WEAKNESS status to all enemies
            StatusManager statusManager = StatusManager.Instance;
            if (statusManager != null && allEnemies.Count > 0)
            {
                foreach (CombatStats enemy in allEnemies)
                {
                    statusManager.ApplyStatus(enemy, StatusType.Weakness, effectDuration);
                }
                
                Debug.Log($"{Name} used: Applied WEAKNESS status to {allEnemies.Count} enemies for {effectDuration} turns");
            }
            else
            {
                Debug.LogWarning($"{Name}: No enemies found to apply effect");
            }
        }
        else
        {
            Debug.LogWarning($"{Name} must be used on an enemy target.");
        }
    }
} 