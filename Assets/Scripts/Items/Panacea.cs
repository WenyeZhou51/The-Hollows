using UnityEngine;

[CreateAssetMenu(fileName = "Panacea", menuName = "Items/Panacea")]
public class Panacea : BaseItem
{
    [SerializeField] private float healAmount = 100f;
    [SerializeField] private float spRestoreAmount = 100f;
    
    private void OnEnable()
    {
        Name = "Panacea";
        Description = "Heal target party member for 100HP and 100SP, remove all negative status effects";
        RequiresTarget = true;
        Type = ItemData.ItemType.Consumable;
    }
    
    public override void Use(CombatStats user, CombatStats target = null)
    {
        if (target != null && !target.isEnemy)
        {
            // Heal Health and Sanity
            target.HealHealth(healAmount);
            target.HealSanity(spRestoreAmount);
            
            // Remove negative status effects
            StatusManager statusManager = StatusManager.Instance;
            if (statusManager != null)
            {
                // Check and remove each negative status
                if (statusManager.HasStatus(target, StatusType.Weakness))
                    statusManager.RemoveStatus(target, StatusType.Weakness);
                    
                if (statusManager.HasStatus(target, StatusType.Vulnerable))
                    statusManager.RemoveStatus(target, StatusType.Vulnerable);
                    
                if (statusManager.HasStatus(target, StatusType.Slowed))
                    statusManager.RemoveStatus(target, StatusType.Slowed);
            }
            
            Debug.Log($"{Name} used: Healed {target.name} for {healAmount} HP and {spRestoreAmount} SP and removed negative status effects");
        }
        else if (target != null && target.isEnemy)
        {
            Debug.LogWarning($"{Name} cannot be used on enemies.");
        }
        else
        {
            // Use on self if no target
            user.HealHealth(healAmount);
            user.HealSanity(spRestoreAmount);
            
            // Remove negative status effects
            StatusManager statusManager = StatusManager.Instance;
            if (statusManager != null)
            {
                // Check and remove each negative status
                if (statusManager.HasStatus(user, StatusType.Weakness))
                    statusManager.RemoveStatus(user, StatusType.Weakness);
                    
                if (statusManager.HasStatus(user, StatusType.Vulnerable))
                    statusManager.RemoveStatus(user, StatusType.Vulnerable);
                    
                if (statusManager.HasStatus(user, StatusType.Slowed))
                    statusManager.RemoveStatus(user, StatusType.Slowed);
            }
            
            Debug.Log($"{Name} used: Healed {user.name} for {healAmount} HP and {spRestoreAmount} SP and removed negative status effects");
        }
    }
} 