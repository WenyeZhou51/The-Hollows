using UnityEngine;

[CreateAssetMenu(fileName = "Gaintkiller", menuName = "Skills/Gaintkiller")]
public class Gaintkiller : BaseSkill
{
    [SerializeField] private float minDamage = 60f;
    [SerializeField] private float maxDamage = 80f;
    
    private void OnEnable()
    {
        Name = "Gaintkiller";
        Description = "Deal 60-80 damage to a target enemy. Costs 70 sanity.";
        SPCost = 70f;
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
            
            Debug.Log($"{Name} used: Hit {target.name} for {calculatedDamage} damage!");
            
            // Deduct sanity cost
            user.UseSanity(SPCost);
        }
        else
        {
            Debug.LogWarning($"{Name} requires an enemy target but none was provided or target is not an enemy");
        }
    }
} 