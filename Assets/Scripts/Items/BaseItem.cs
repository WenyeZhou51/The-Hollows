using UnityEngine;

public abstract class BaseItem : ScriptableObject
{
    public string Name;
    public string Description;
    public bool RequiresTarget;
    public ItemData.ItemType Type;
    public Sprite Icon;
    
    // Method that will be implemented by specific items
    public abstract void Use(CombatStats user, CombatStats target = null);
    
    // Convert to ItemData for backwards compatibility
    public ItemData ToItemData(int amount = 1)
    {
        return new ItemData(Name, Description, amount, RequiresTarget, Type, Icon);
    }
} 