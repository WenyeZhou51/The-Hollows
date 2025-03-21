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
            user.GuardAlly(target);
            Debug.Log($"{Name} used: {user.name} is now protecting {target.name}");
            
            // Deduct sanity cost
            user.UseSanity(SPCost);
        }
        else
        {
            Debug.LogWarning($"{Name} requires an ally target but none was provided or target is an enemy");
        }
    }
} 