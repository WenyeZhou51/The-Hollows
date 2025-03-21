using UnityEngine;

[CreateAssetMenu(fileName = "HealingWords", menuName = "Skills/Healing Words")]
public class HealingWords : BaseSkill
{
    [SerializeField] private float healthHealAmount = 50f;
    [SerializeField] private float sanityHealAmount = 30f;
    
    private void OnEnable()
    {
        Name = "Healing Words";
        Description = "Heal an ally for 50 HP and 30 sanity. Costs 10 sanity to cast.";
        SPCost = 10f;
        RequiresTarget = true;
    }
    
    public override void Use(CombatStats user, CombatStats target = null)
    {
        if (target != null)
        {
            target.HealHealth(healthHealAmount);
            target.HealSanity(sanityHealAmount);
            Debug.Log($"{Name} used: Healed {target.name} for {healthHealAmount} HP and {sanityHealAmount} sanity");
            
            // Deduct sanity cost
            user.UseSanity(SPCost);
        }
        else
        {
            Debug.LogWarning($"{Name} requires a target but none was provided");
        }
    }
} 