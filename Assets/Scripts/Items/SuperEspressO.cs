using UnityEngine;

[CreateAssetMenu(fileName = "SuperEspressO", menuName = "Items/Super Espress-O")]
public class SuperEspressO : BaseItem
{
    [SerializeField] private float spRestoreAmount = 50f;
    [SerializeField] private float speedBoostPercentage = 50f;
    [SerializeField] private int speedBoostDuration = 3; // turns
    
    private void OnEnable()
    {
        Name = "Super Espress-O";
        Description = "Restores 50 SP and increases ally's action generation by 50% for 3 turns";
        RequiresTarget = true;
        Type = ItemData.ItemType.Consumable;
    }
    
    public override void Use(CombatStats user, CombatStats target = null)
    {
        if (target != null && !target.isEnemy)
        {
            // Restore SP (sanity in this game)
            target.HealSanity(spRestoreAmount);
            
            // Increase action speed
            target.BoostActionSpeed(speedBoostPercentage / 100f, speedBoostDuration);
            
            Debug.Log($"{Name} used: Restored {spRestoreAmount} SP and boosted action generation by {speedBoostPercentage}% for {target.name} for {speedBoostDuration} turns");
        }
        else if (target != null && target.isEnemy)
        {
            // Notify that item cannot be used on enemies
            Debug.LogWarning($"{Name} cannot be used on enemies.");
        }
        else
        {
            // Use on self if no target
            user.HealSanity(spRestoreAmount);
            user.BoostActionSpeed(speedBoostPercentage / 100f, speedBoostDuration);
            
            Debug.Log($"{Name} used: Restored {spRestoreAmount} SP and boosted action generation by {speedBoostPercentage}% for {user.name} for {speedBoostDuration} turns");
        }
    }
} 