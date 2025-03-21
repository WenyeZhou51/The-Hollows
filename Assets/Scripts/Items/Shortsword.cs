using UnityEngine;

[CreateAssetMenu(fileName = "Shortsword", menuName = "Items/Shortsword")]
public class Shortsword : BaseItem
{
    private void OnEnable()
    {
        Name = "Shortsword";
        Description = "A simple weapon";
        RequiresTarget = false;
        Type = ItemData.ItemType.Equipment;
    }
    
    public override void Use(CombatStats user, CombatStats target = null)
    {
        // Equipment items typically don't have a "use" function
        // but would be equipped instead. This is a placeholder.
        Debug.Log($"{Name} equipped by {user.name}");
    }
} 