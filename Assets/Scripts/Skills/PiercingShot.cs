using UnityEngine;

[CreateAssetMenu(fileName = "PiercingShot", menuName = "Skills/Piercing Shot")]
public class PiercingShot : BaseSkill
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private int vulnerableDuration = 2;
    
    private void OnEnable()
    {
        Name = "Piercing Shot";
        Description = "Deal 10 damage and apply Vulnerable status (50% more damage taken) for 2 turns.";
        SPCost = 0f;
        RequiresTarget = true;
    }
    
    public override void Use(CombatStats user, CombatStats target = null)
    {
        if (target != null)
        {
            // Calculate damage with user's attack multiplier
            float calculatedDamage = user.CalculateDamage(damage);
            
            // Apply damage
            target.TakeDamage(calculatedDamage);
            
            // Apply the Vulnerable status effect using the status system ONLY
            StatusManager statusManager = StatusManager.Instance;
            if (statusManager != null)
            {
                Debug.Log($"[PiercingShot] Applying Vulnerable status to {target.characterName} for {vulnerableDuration} turns");
                statusManager.ApplyStatus(target, StatusType.Vulnerable, vulnerableDuration);
                Debug.Log($"[PiercingShot] Hit {target.characterName} for {calculatedDamage} damage with Vulnerable status");
            }
            else
            {
                Debug.LogError("[PiercingShot] StatusManager not found! Cannot apply Vulnerable status.");
                
                // No fallback to legacy system - we want to identify issues with status system
            }
            
            // Deduct sanity cost
            user.UseSanity(SPCost);
        }
        else
        {
            Debug.LogWarning($"{Name} requires a target but none was provided");
        }
    }
} 