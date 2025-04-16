using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "OtherworldlyTome", menuName = "Items/Otherworldly Tome")]
public class OtherworldlyTome : BaseItem
{
    [SerializeField] private int effectDuration = 3; // turns
    
    private void OnEnable()
    {
        Name = "Otherworldly Tome";
        Description = "Gives STRENGTH to all party members for 3 turns";
        RequiresTarget = false;
        Type = ItemData.ItemType.Consumable;
    }
    
    public override void Use(CombatStats user, CombatStats target = null)
    {
        // Get all party members in combat
        List<CombatStats> allPlayers = new List<CombatStats>();
        
        // Find the combat manager to get all players
        CombatManager combatManager = FindObjectOfType<CombatManager>();
        if (combatManager != null)
        {
            allPlayers = combatManager.players;
        }
        else
        {
            // Fallback to finding players in scene if combat manager not found
            CombatStats[] foundPlayers = FindObjectsOfType<CombatStats>();
            foreach (CombatStats player in foundPlayers)
            {
                if (!player.isEnemy && !player.IsDead())
                {
                    allPlayers.Add(player);
                }
            }
        }
        
        // Apply STRENGTH status to all players
        StatusManager statusManager = StatusManager.Instance;
        if (statusManager != null && allPlayers.Count > 0)
        {
            foreach (CombatStats player in allPlayers)
            {
                if (!player.IsDead())
                {
                    statusManager.ApplyStatus(player, StatusType.Strength, effectDuration);
                }
            }
            
            Debug.Log($"{Name} used: Applied STRENGTH status to {allPlayers.Count} party members for {effectDuration} turns");
        }
        else
        {
            Debug.LogWarning($"{Name}: No party members found to apply effect or StatusManager is null");
        }
    }
} 