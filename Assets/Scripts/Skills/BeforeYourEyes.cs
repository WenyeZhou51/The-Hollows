using UnityEngine;

[CreateAssetMenu(fileName = "BeforeYourEyes", menuName = "Skills/Before Your Eyes")]
public class BeforeYourEyes : BaseSkill
{
    private void OnEnable()
    {
        Name = "Before Your Eyes";
        Description = "Reset target's action gauge to 0";
        SPCost = 15f;
        RequiresTarget = true;
    }
    
    public override void Use(CombatStats user, CombatStats target = null)
    {
        if (target != null)
        {
            target.ResetAction();
            Debug.Log($"{Name} used: Reset {target.name}'s action gauge to 0");
            
            // Deduct sanity cost
            user.UseSanity(SPCost);
        }
        else
        {
            Debug.LogWarning($"{Name} requires a target but none was provided");
        }
    }
} 