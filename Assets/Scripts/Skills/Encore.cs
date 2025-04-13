using UnityEngine;

[CreateAssetMenu(fileName = "Encore", menuName = "Skills/Encore")]
public class Encore : BaseSkill
{
    private void OnEnable()
    {
        Name = "Encore";
        Description = "Instantly fills an ally's action bar to maximum. Costs 0 sanity.";
        SPCost = 0f;
        RequiresTarget = true; // Requires an ally as target
    }
    
    public override void Use(CombatStats user, CombatStats target = null)
    {
        if (target != null && !target.isEnemy) // Only allow ally targets
        {
            // Fill the target's action bar to maximum
            float currentAction = target.currentAction;
            float maxAction = target.maxAction;
            
            // Set the current action to max
            target.currentAction = maxAction;
            
            Debug.Log($"{Name} used: Filled {target.characterName}'s action bar from {currentAction} to {maxAction}");
            
            // No sanity cost for this skill
        }
        else
        {
            Debug.LogWarning($"{Name} requires an ally target but none was provided or target is an enemy");
        }
    }
} 