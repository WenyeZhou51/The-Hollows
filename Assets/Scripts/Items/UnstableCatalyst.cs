using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "UnstableCatalyst", menuName = "Items/Unstable Catalyst")]
public class UnstableCatalyst : BaseItem
{
    [SerializeField] private float damage = 40f;
    
    private void OnEnable()
    {
        Name = "Unstable Catalyst";
        Description = "Deals 40 damage to all enemies";
        RequiresTarget = false;
        Type = ItemData.ItemType.Consumable;
    }
    
    public override void Use(CombatStats user, CombatStats target = null)
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
                if (enemy.isEnemy && !enemy.IsDead())
                {
                    allEnemies.Add(enemy);
                }
            }
        }
        
        // Deal damage to all enemies
        if (allEnemies.Count > 0)
        {
            foreach (CombatStats enemy in allEnemies)
            {
                enemy.TakeDamage(damage);
            }
            
            Debug.Log($"{Name} used: Dealt {damage} damage to {allEnemies.Count} enemies");
        }
        else
        {
            Debug.LogWarning($"{Name}: No enemies found to apply damage");
        }
    }
} 