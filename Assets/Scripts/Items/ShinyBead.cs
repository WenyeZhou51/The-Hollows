using UnityEngine;

[CreateAssetMenu(fileName = "ShinyBead", menuName = "Items/Shiny Bead")]
public class ShinyBead : BaseItem
{
    [SerializeField] private float damage = 20f;
    
    private void OnEnable()
    {
        Name = "Shiny Bead";
        Description = "Deals 20 damage to target enemy";
        RequiresTarget = true;
        Type = ItemData.ItemType.Consumable;
    }
    
    public override void Use(CombatStats user, CombatStats target = null)
    {
        if (target != null && target.isEnemy)
        {
            target.TakeDamage(damage);
            Debug.Log($"{Name} used: Dealt {damage} damage to {target.name}");
        }
        else
        {
            Debug.LogWarning($"{Name} requires an enemy target");
        }
    }
} 