using UnityEngine;

[CreateAssetMenu(fileName = "WardingCharm", menuName = "Items/Warding Charm")]
public class WardingCharm : BaseItem
{
    [SerializeField] private float defenseBuff = 25f; // Percentage
    
    private void OnEnable()
    {
        Name = "Warding Charm";
        Description = "Protects against harm";
        RequiresTarget = true;
        Type = ItemData.ItemType.Consumable;
    }
    
    public override void Use(CombatStats user, CombatStats target = null)
    {
        if (target != null)
        {
            // In a real implementation, this would apply a defense buff effect
            target.ActivateGuard();
            Debug.Log($"{Name} used: Applied protection to {target.name}");
        }
        else
        {
            user.ActivateGuard();
            Debug.Log($"{Name} used: Applied protection to {user.name}");
        }
    }
} 