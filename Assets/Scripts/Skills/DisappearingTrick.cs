using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DisappearingTrick", menuName = "Skills/Disappearing Trick")]
public class DisappearingTrick : BaseSkill
{
    private void OnEnable()
    {
        Name = "Cleansing Wave";
        Description = "Remove all status effects from allies (not including self). Costs 5 Mind.";
        SPCost = 5f;
        RequiresTarget = false; // Targets all allies automatically
    }
    
    public override void Use(CombatStats user, CombatStats target = null)
    {
        // Find the combat manager to get all players
        CombatManager combatManager = GameObject.FindObjectOfType<CombatManager>();
        if (combatManager != null)
        {
            // Get all player characters
            List<CombatStats> allPlayers = new List<CombatStats>(combatManager.players);
            
            // Get the status manager
            StatusManager statusManager = StatusManager.Instance;
            
            if (statusManager != null)
            {
                int clearedCount = 0;
                
                // Loop through all players
                foreach (CombatStats player in allPlayers)
                {
                    // Skip self (the caster)
                    if (player == user) continue;
                    
                    if (player != null && !player.IsDead())
                    {
                        // Clear all status effects from this ally
                        statusManager.ClearAllStatuses(player);
                        clearedCount++;
                        
                        Debug.Log($"{Name} used: Cleared all status effects from {player.characterName}");
                    }
                }
                
                if (clearedCount > 0)
                {
                    Debug.Log($"{Name} used: Cleared status effects from {clearedCount} allies");
                }
                else
                {
                    Debug.Log($"{Name} used: No allies had status effects to clear");
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