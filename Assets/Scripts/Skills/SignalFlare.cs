using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SignalFlare", menuName = "Skills/Signal Flare")]
public class SignalFlare : BaseSkill
{
    private void OnEnable()
    {
        Name = "Signal Flare";
        Description = "Remove all status effects from all enemies. Costs 5 sanity.";
        SPCost = 5f;
        RequiresTarget = false; // Targets all enemies automatically
    }
    
    public override void Use(CombatStats user, CombatStats target = null)
    {
        // Find the combat manager to get all enemies
        CombatManager combatManager = GameObject.FindObjectOfType<CombatManager>();
        if (combatManager != null)
        {
            // Get all living enemies
            List<CombatStats> allEnemies = combatManager.GetLivingEnemies();
            
            // Get the status manager
            StatusManager statusManager = StatusManager.Instance;
            
            if (statusManager != null)
            {
                int clearedCount = 0;
                
                // Loop through all enemies
                foreach (CombatStats enemy in allEnemies)
                {
                    if (enemy != null && !enemy.IsDead())
                    {
                        // Clear all status effects from this enemy
                        statusManager.ClearAllStatuses(enemy);
                        clearedCount++;
                        
                        Debug.Log($"{Name} used: Cleared all status effects from {enemy.characterName}");
                    }
                }
                
                if (clearedCount > 0)
                {
                    Debug.Log($"{Name} used: Cleared status effects from {clearedCount} enemies");
                }
                else
                {
                    Debug.Log($"{Name} used: No enemies had status effects to clear");
                }
            }
            else
            {
                Debug.LogWarning($"{Name} couldn't find StatusManager. Skill had no effect.");
            }
            
            // Deduct sanity cost
            user.UseSanity(SPCost);
        }
        else
        {
            Debug.LogWarning($"{Name} couldn't find CombatManager. Skill had no effect.");
        }
    }
} 