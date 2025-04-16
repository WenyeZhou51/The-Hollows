using UnityEngine;

[CreateAssetMenu(fileName = "FruitJuice", menuName = "Items/Fruit Juice")]
public class FruitJuice : BaseItem
{
    [SerializeField] private float healAmount = 30f;
    
    private void OnEnable()
    {
        Name = "Fruit Juice";
        Description = "Heals 30 HP";
        RequiresTarget = true;
        Type = ItemData.ItemType.Consumable;
    }
    
    public override void Use(CombatStats user, CombatStats target = null)
    {
        if (target != null)
        {
            target.HealHealth(healAmount);
            Debug.Log($"{Name} used: Healed {target.name} for {healAmount} HP");
        }
        else
        {
            user.HealHealth(healAmount);
            Debug.Log($"{Name} used: Healed {user.name} for {healAmount} HP");
        }
    }
} 