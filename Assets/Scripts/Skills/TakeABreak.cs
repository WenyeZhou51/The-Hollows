using UnityEngine;

[CreateAssetMenu(fileName = "TakeABreak", menuName = "Skills/Take A Break")]
public class TakeABreak : BaseSkill
{
    [SerializeField] private float healthHealAmount = 20f;
    [SerializeField] private float sanityHealAmount = 20f;
    [SerializeField] private int slowDuration = 2; // Duration for SLOW effect in turns
    
    private void OnEnable()
    {
        Name = "Take a Break!";
        Description = "Target ally recovers 20 HP and 20 Mind but becomes SLOW for 2 turns.";
        SPCost = 5f;
        RequiresTarget = true; // Requires an ally as target
    }
    
    public override void Use(CombatStats user, CombatStats target = null)
    {
        if (target != null && !target.isEnemy) // Only allow ally targets
        {
            // Heal the target
            target.HealHealth(healthHealAmount);
            target.HealSanity(sanityHealAmount);
            
            // Apply SLOW status effect
            StatusManager statusManager = StatusManager.Instance;
            if (statusManager != null)
            {
                // Apply Slowed status with the status system
                statusManager.ApplyStatus(target, StatusType.Slowed, slowDuration);
                Debug.Log($"{Name} used: {target.characterName} healed for {healthHealAmount} HP and {sanityHealAmount} Mind, and is now SLOW for {slowDuration} turns");
            }
            else
            {
                // Fallback to direct modification if status manager not available
                float baseActionSpeed = target.actionSpeed;
                float newSpeed = baseActionSpeed * 0.5f; // 50% reduction
                target.actionSpeed = newSpeed;
                Debug.LogWarning($"{Name} used: StatusManager not found, applied direct speed reduction to {target.characterName}");
            }
            
            // Deduct sanity cost
            user.UseSanity(SPCost);
        }
        else
        {
            if (target == null)
            {
                Debug.LogWarning($"{Name} requires an ally target but none was provided");
            }
            else if (target.isEnemy)
            {
                Debug.LogWarning($"{Name} cannot target enemies, only allies");
            }
        }
    }
} 