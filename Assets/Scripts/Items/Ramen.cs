using UnityEngine;

[CreateAssetMenu(fileName = "Ramen", menuName = "Items/Ramen")]
public class Ramen : BaseItem
{
    [SerializeField] private float healAmount = 15f;
    
    private void OnEnable()
    {
        Name = "Ramen";
        Description = "Heals ally for 15 HP";
        RequiresTarget = true;
        Type = ItemData.ItemType.Consumable;
    }
    
    public override void Use(CombatStats user, CombatStats target = null)
    {
        if (target != null && !target.isEnemy)
        {
            target.HealHealth(healAmount);
            Debug.Log($"{Name} used: Healed {target.name} for {healAmount} HP");
        }
        else if (target != null && target.isEnemy)
        {
            Debug.LogWarning($"{Name} cannot be used on enemies.");
        }
        else
        {
            // Use on self if no target
            user.HealHealth(healAmount);
            Debug.Log($"{Name} used: Healed {user.name} for {healAmount} HP");
        }
    }
} 