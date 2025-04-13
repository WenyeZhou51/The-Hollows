using UnityEngine;

[CreateAssetMenu(fileName = "Bola", menuName = "Skills/Bola")]
public class Bola : BaseSkill
{
    [SerializeField] private float minDamage = 2f;
    [SerializeField] private float maxDamage = 4f;
    [SerializeField] private int slowedDuration = 2; // Duration for the Slowed status
    
    private void OnEnable()
    {
        Name = "Bola";
        Description = "Deal 2-4 damage to a target enemy and apply SLOWED (-50% action speed) for 2 turns. Costs 20 sanity.";
        SPCost = 20f;
        RequiresTarget = true;
    }
    
    public override void Use(CombatStats user, CombatStats target = null)
    {
        if (target != null && target.isEnemy)
        {
            // Calculate random damage within range
            float baseDamage = Random.Range(minDamage, maxDamage);
            
            // Calculate damage with user's attack multiplier
            float calculatedDamage = user.CalculateDamage(baseDamage);
            
            // Apply damage
            target.TakeDamage(calculatedDamage);
            
            // Apply SLOWED status effect to the enemy
            StatusManager statusManager = StatusManager.Instance;
            if (statusManager != null)
            {
                // Apply Slowed status with the status system
                statusManager.ApplyStatus(target, StatusType.Slowed, slowedDuration);
                Debug.Log($"{Name} used: Hit {target.name} for {calculatedDamage} damage and applied SLOWED for {slowedDuration} turns");
            }
            else
            {
                // Fallback to direct modification if status manager not available
                float baseActionSpeed = target.actionSpeed;
                float newSpeed = baseActionSpeed * 0.5f; // 50% reduction
                target.actionSpeed = newSpeed;
                Debug.LogWarning($"{Name} used: StatusManager not found, applied direct speed reduction to {target.name}");
            }
            
            // Deduct sanity cost
            user.UseSanity(SPCost);
        }
        else
        {
            Debug.LogWarning($"{Name} requires an enemy target but none was provided or target is not an enemy");
        }
    }
} 