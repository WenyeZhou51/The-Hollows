using UnityEngine;

[CreateAssetMenu(fileName = "TowerShield", menuName = "Items/Tower Shield")]
public class TowerShield : BaseItem
{
    [SerializeField] private int effectDuration = 3; // turns
    
    private void OnEnable()
    {
        Name = "Tower Shield";
        Description = "Gives TOUGH to ally for 3 turns";
        RequiresTarget = true;
        Type = ItemData.ItemType.Consumable;
    }
    
    public override void Use(CombatStats user, CombatStats target = null)
    {
        if (target != null && !target.isEnemy)
        {
            // Apply TOUGH status effect
            StatusManager statusManager = StatusManager.Instance;
            if (statusManager != null)
            {
                statusManager.ApplyStatus(target, StatusType.Tough, effectDuration);
                Debug.Log($"{Name} used: Applied TOUGH status to {target.name} for {effectDuration} turns");
            }
        }
        else if (target != null && target.isEnemy)
        {
            Debug.LogWarning($"{Name} cannot be used on enemies.");
        }
        else
        {
            // Use on self if no target
            StatusManager statusManager = StatusManager.Instance;
            if (statusManager != null)
            {
                statusManager.ApplyStatus(user, StatusType.Tough, effectDuration);
                Debug.Log($"{Name} used: Applied TOUGH status to {user.name} for {effectDuration} turns");
            }
        }
    }
} 