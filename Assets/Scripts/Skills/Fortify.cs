using UnityEngine;

[CreateAssetMenu(fileName = "Fortify", menuName = "Skills/Fortify")]
public class Fortify : BaseSkill
{
    [SerializeField] private float healAmount = 10f;
    [SerializeField] private int toughDuration = 2; // Duration for the Tough status
    
    private void OnEnable()
    {
        Name = "Fortify";
        Description = "Heal self for 10 HP and gain TOUGH (50% damage reduction) for 2 turns";
        SPCost = 10f;
        RequiresTarget = false; // Self-targeting skill
    }
    
    public override void Use(CombatStats user, CombatStats target = null)
    {
        // Heal the user
        user.HealHealth(healAmount);
        
        // Apply TOUGH status effect to self
        StatusManager statusManager = StatusManager.Instance;
        if (statusManager != null)
        {
            // Apply Tough status with the status system
            statusManager.ApplyStatus(user, StatusType.Tough, toughDuration);
            Debug.Log($"{Name} used: {user.characterName} healed for {healAmount} HP and gained Tough status for {toughDuration} turns");
        }
        else
        {
            // Fallback to direct modification if status manager not available
            float baseDefenseMultiplier = user.defenseMultiplier;
            float newDefenseMultiplier = 0.5f; // 50% damage reduction
            user.defenseMultiplier = newDefenseMultiplier;
            Debug.LogWarning($"{Name} used: StatusManager not found, applied direct defense multiplier to {user.characterName}");
        }
        
        // Deduct sanity cost
        user.UseSanity(SPCost);
    }
} 