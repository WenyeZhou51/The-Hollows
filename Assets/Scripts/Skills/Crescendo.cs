using UnityEngine;

[CreateAssetMenu(fileName = "Crescendo", menuName = "Skills/Crescendo")]
public class Crescendo : BaseSkill
{
    [SerializeField] private int agileDuration = 2; // Duration for the Agile status
    
    private void OnEnable()
    {
        Name = "Crescendo";
        Description = "Make an ally AGILE (+50% action speed) for 2 turns. Targets allies only.";
        SPCost = 10f;
        RequiresTarget = true; // Requires an ally as target
    }
    
    public override void Use(CombatStats user, CombatStats target = null)
    {
        if (target != null && !target.isEnemy) // Only allow ally targets
        {
            // Apply AGILE status effect to the ally
            StatusManager statusManager = StatusManager.Instance;
            if (statusManager != null)
            {
                // Apply Agile status with the status system
                statusManager.ApplyStatus(target, StatusType.Agile, agileDuration);
                Debug.Log($"{Name} used: {target.characterName} gained Agile for {agileDuration} turns");
            }
            else
            {
                // Fallback to direct modification if status manager not available
                target.BoostActionSpeed(0.5f, agileDuration); // 50% speed boost
                Debug.LogWarning($"{Name} used: StatusManager not found, applied direct speed boost to {target.characterName}");
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