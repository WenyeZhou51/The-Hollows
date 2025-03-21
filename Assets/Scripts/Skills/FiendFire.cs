using UnityEngine;

[CreateAssetMenu(fileName = "FiendFire", menuName = "Skills/Fiend Fire")]
public class FiendFire : BaseSkill
{
    [SerializeField] private float damagePerHit = 10f;
    [SerializeField] private int minHits = 1;
    [SerializeField] private int maxHits = 5;
    
    private void OnEnable()
    {
        Name = "Fiend Fire";
        Description = "Deal 10 damage to a target 1-5 times randomly";
        SPCost = 0f;
        RequiresTarget = true;
    }
    
    public override void Use(CombatStats user, CombatStats target = null)
    {
        if (target != null)
        {
            int hits = Random.Range(minHits, maxHits + 1);
            float totalDamage = damagePerHit * hits;
            
            target.TakeDamage(totalDamage);
            Debug.Log($"{Name} used: Hit {target.name} {hits} times for a total of {totalDamage} damage");
            
            // Deduct sanity cost
            user.UseSanity(SPCost);
        }
        else
        {
            Debug.LogWarning($"{Name} requires a target but none was provided");
        }
    }
} 