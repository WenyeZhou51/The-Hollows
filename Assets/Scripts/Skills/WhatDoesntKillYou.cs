using UnityEngine;

[CreateAssetMenu(fileName = "WhatDoesntKillYou", menuName = "Skills/What Doesn't Kill You")]
public class WhatDoesntKillYou : BaseSkill
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private int strengthDuration = 2; // Duration for the Strength status
    
    private void OnEnable()
    {
        Name = "What Doesn't Kill You";
        Description = "Deal 10 damage to an ally and give them STRENGTH (+50% attack) for 2 turns. Targets allies only.";
        SPCost = 5f;
        RequiresTarget = true; // Requires an ally as target
    }
    
    public override void Use(CombatStats user, CombatStats target = null)
    {
        if (target != null && !target.isEnemy) // Only allow ally targets
        {
            // Deal damage to the ally
            target.TakeDamage(damage);
            
            // Apply STRENGTH status effect to the ally
            StatusManager statusManager = StatusManager.Instance;
            if (statusManager != null)
            {
                // Apply Strength status with the status system
                statusManager.ApplyStatus(target, StatusType.Strength, strengthDuration);
                Debug.Log($"{Name} used: {target.characterName} took {damage} damage and gained Strength for {strengthDuration} turns");
            }
            else
            {
                // Fallback to direct modification if status manager not available
                float baseAttackMultiplier = target.attackMultiplier;
                float newAttackMultiplier = 1.5f; // 50% increase
                target.attackMultiplier = newAttackMultiplier;
                Debug.LogWarning($"{Name} used: StatusManager not found, applied direct attack multiplier to {target.characterName}");
            }
            
            // Deduct sanity cost
            user.UseSanity(SPCost);
        }
        else
        {
            Debug.LogWarning($"{Name} requires an ally target but none was provided or target is an enemy");
        }
    }
} 