using UnityEngine;

[CreateAssetMenu(fileName = "PiercingShot", menuName = "Skills/Piercing Shot")]
public class PiercingShot : BaseSkill
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private int defenseReductionTurns = 2;
    
    private void OnEnable()
    {
        Name = "Piercing Shot";
        Description = "Deal 10 damage and reduce target's defense by 50% for 2 turns.";
        SPCost = 0f;
        RequiresTarget = true;
    }
    
    public override void Use(CombatStats user, CombatStats target = null)
    {
        if (target != null)
        {
            // Deal damage
            target.TakeDamage(damage);
            
            // Apply defense reduction
            target.ApplyDefenseReduction();
            
            Debug.Log($"{Name} used: Hit {target.name} for {damage} damage and reduced defense for {defenseReductionTurns} turns");
            
            // Deduct sanity cost
            user.UseSanity(SPCost);
        }
        else
        {
            Debug.LogWarning($"{Name} requires a target but none was provided");
        }
    }
} 