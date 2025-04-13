using UnityEngine;

[CreateAssetMenu(fileName = "HumanShield", menuName = "Skills/Human Shield")]
public class HumanShield : BaseSkill
{
    private void OnEnable()
    {
        Name = "Human Shield!";
        Description = "Protect an ally by taking all damage they would receive until your next turn";
        SPCost = 0f;
        RequiresTarget = true;
    }
    
    public override void Use(CombatStats user, CombatStats target = null)
    {
        if (target != null && !target.isEnemy)
        {
            // Get the status manager
            StatusManager statusManager = StatusManager.Instance;
            if (statusManager != null)
            {
                // Apply the guarded status using the new system
                statusManager.GuardAlly(user, target);
                Debug.Log($"{Name} used: {user.name} is now protecting {target.name} using status system");
            }
            else
            {
                // Fall back to the legacy system
                user.GuardAlly(target);
                Debug.Log($"{Name} used: {user.name} is now protecting {target.name} using legacy system");
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