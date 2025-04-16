using UnityEngine;

[CreateAssetMenu(fileName = "PrimordialPile", menuName = "Skills/Primordial Pile")]
public class PrimordialPile : BaseSkill
{
    [SerializeField] private float minDamagePerHit = 7f;
    [SerializeField] private float maxDamagePerHit = 10f;
    [SerializeField] private int numberOfHits = 3;
    [SerializeField] private int weaknessDuration = 2; // Duration for the Weakness status
    
    private void OnEnable()
    {
        Name = "Primordial Pile";
        Description = "Deal 7-10 damage to a target enemy 3 times and apply WEAKNESS (-50% attack) for 2 turns. Costs 20 sanity.";
        SPCost = 20f;
        RequiresTarget = true;
    }
    
    public override void Use(CombatStats user, CombatStats target = null)
    {
        if (target != null && target.isEnemy) // Only allow enemy targets
        {
            float totalDamage = 0f;
            
            // Deal damage multiple times
            for (int i = 0; i < numberOfHits; i++)
            {
                float damage = Random.Range(minDamagePerHit, maxDamagePerHit);
                damage *= user.attackMultiplier;
                target.TakeDamage(damage);
                totalDamage += damage;
            }
            
            // Apply WEAKNESS status effect to the enemy
            StatusManager statusManager = StatusManager.Instance;
            if (statusManager != null)
            {
                // Apply Weakness status with the status system
                statusManager.ApplyStatus(target, StatusType.Weakness, weaknessDuration);
                Debug.Log($"{Name} used: Hit {target.name} {numberOfHits} times for a total of {totalDamage} damage and applied Weakness for {weaknessDuration} turns");
            }
            else
            {
                // Fallback to direct modification if status manager not available
                target.attackMultiplier = 0.5f; // 50% reduction
                Debug.LogWarning($"{Name} used: StatusManager not found, applied direct attack reduction to {target.name}");
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