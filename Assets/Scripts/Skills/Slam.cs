using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Slam", menuName = "Skills/Slam")]
public class Slam : BaseSkill
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private int strengthDuration = 2; // Duration for the Strength status
    
    private void OnEnable()
    {
        Name = "Slam!";
        Description = "Deal 10 damage to all enemies and gain Strength (+50% attack) for 2 turns";
        SPCost = 0f;
        RequiresTarget = false;
    }
    
    public override void Use(CombatStats user, CombatStats target = null)
    {
        // This skill affects all enemies, so we need to find them
        CombatManager combatManager = GameObject.FindObjectOfType<CombatManager>();
        if (combatManager != null)
        {
            // Get all enemies
            List<CombatStats> enemies = new List<CombatStats>(combatManager.enemies);
            
            // Apply Strength status to the user
            StatusManager statusManager = StatusManager.Instance;
            if (statusManager != null)
            {
                statusManager.ApplyStatus(user, StatusType.Strength, strengthDuration);
                Debug.Log($"{Name} used: {user.name} gains Strength status for {strengthDuration} turns");
            }
            
            // Calculate damage with user's attack multiplier
            float calculatedDamage = user.CalculateDamage(damage);
            
            foreach (CombatStats enemy in enemies)
            {
                if (enemy != null && !enemy.IsDead())
                {
                    enemy.TakeDamage(calculatedDamage);
                    Debug.Log($"{Name} used: Hit {enemy.name} for {calculatedDamage} damage");
                }
            }
            
            // Deduct sanity cost
            user.UseSanity(SPCost);
        }
        else
        {
            Debug.LogWarning("Could not find Combat Manager to use Slam! skill");
        }
    }
} 