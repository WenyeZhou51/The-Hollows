using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Slam", menuName = "Skills/Slam")]
public class Slam : BaseSkill
{
    [SerializeField] private float minDamage = 15f;
    [SerializeField] private float maxDamage = 30f;
    [SerializeField] private int strengthDuration = 2; // Duration for the Strength status
    
    private void OnEnable()
    {
        Name = "Slam!";
        Description = "Deal 15-30 damage to all enemies and gain Strength (+50% attack) for 2 turns";
        SPCost = 15f;
        RequiresTarget = false;
    }
    
    public override void Use(CombatStats user, CombatStats target = null)
    {
        // This skill is now implemented in CombatUI.ExecuteSkillAfterMessage
        // to avoid double application of damage
        
        // Apply Strength status to the user
        StatusManager statusManager = StatusManager.Instance;
        if (statusManager != null)
        {
            statusManager.ApplyStatus(user, StatusType.Strength, strengthDuration);
            Debug.Log($"{Name} used: {user.name} gains Strength status for {strengthDuration} turns");
        }
        
        // Deduct sanity cost
        user.UseSanity(SPCost);
    }
} 